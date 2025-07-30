using Microsoft.EntityFrameworkCore;
using Shared.Application.Common.Interfaces;
using Shared.Domain.Enums;
using LiveAgentService.Services;
using LiveAgentService.Models;

namespace LiveAgentService.Services;

public class QueueManagementService : IQueueManagementService
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<QueueManagementService> _logger;
    private readonly Dictionary<Guid, QueuedConversation> _conversationQueue = new();

    public QueueManagementService(IApplicationDbContext context, ILogger<QueueManagementService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> AddToQueueAsync(Guid conversationId, QueuePriority priority = QueuePriority.Normal)
    {
        try
        {
            var conversation = await _context.Conversations
                .FirstOrDefaultAsync(c => c.Id == conversationId);

            if (conversation == null)
                return false;

            var queuedConversation = new QueuedConversation
            {
                ConversationId = conversationId,
                CustomerName = conversation.CustomerName,
                CustomerEmail = conversation.CustomerEmail,
                Subject = conversation.Subject,
                Priority = priority,
                QueuedAt = DateTime.UtcNow,
                WaitTime = TimeSpan.Zero,
                Position = GetNextQueuePosition(conversation.TenantId, priority),
                Language = conversation.Language ?? "en",
                Channel = conversation.Channel
            };

            _conversationQueue[conversationId] = queuedConversation;

            conversation.Status = ConversationStatus.Queued;
            conversation.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding conversation {ConversationId} to queue", conversationId);
            return false;
        }
    }

    public async Task<bool> RemoveFromQueueAsync(Guid conversationId)
    {
        try
        {
            if (_conversationQueue.ContainsKey(conversationId))
            {
                _conversationQueue.Remove(conversationId);
                
                var conversation = await _context.Conversations
                    .FirstOrDefaultAsync(c => c.Id == conversationId);

                if (conversation != null)
                {
                    conversation.Status = ConversationStatus.Active;
                    conversation.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }

                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing conversation {ConversationId} from queue", conversationId);
            return false;
        }
    }

    public async Task<List<QueuedConversation>> GetQueuedConversationsAsync(Guid tenantId)
    {
        try
        {
            var tenantConversations = await _context.Conversations
                .Where(c => c.TenantId == tenantId && c.Status == ConversationStatus.Queued)
                .Select(c => c.Id)
                .ToListAsync();

            var queuedConversations = _conversationQueue.Values
                .Where(qc => tenantConversations.Contains(qc.ConversationId))
                .OrderBy(qc => qc.Priority)
                .ThenBy(qc => qc.QueuedAt)
                .ToList();

            for (int i = 0; i < queuedConversations.Count; i++)
            {
                queuedConversations[i].Position = i + 1;
                queuedConversations[i].WaitTime = DateTime.UtcNow - queuedConversations[i].QueuedAt;
            }

            return queuedConversations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting queued conversations for tenant {TenantId}", tenantId);
            return new List<QueuedConversation>();
        }
    }

    public async Task<QueuedConversation?> GetNextInQueueAsync(Guid tenantId, string? department = null)
    {
        try
        {
            var queuedConversations = await GetQueuedConversationsAsync(tenantId);
            
            var nextConversation = queuedConversations
                .Where(qc => department == null || qc.Department == department)
                .OrderByDescending(qc => qc.Priority)
                .ThenBy(qc => qc.QueuedAt)
                .FirstOrDefault();

            return nextConversation;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting next conversation in queue for tenant {TenantId}", tenantId);
            return null;
        }
    }

    public async Task<bool> UpdateQueuePositionAsync(Guid conversationId, int newPosition)
    {
        try
        {
            if (_conversationQueue.ContainsKey(conversationId))
            {
                _conversationQueue[conversationId].Position = newPosition;
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating queue position for conversation {ConversationId}", conversationId);
            return false;
        }
    }

    public async Task<QueueStatistics> GetQueueStatisticsAsync(Guid tenantId)
    {
        try
        {
            var queuedConversations = await GetQueuedConversationsAsync(tenantId);
            var agentStatuses = await GetAgentStatusesAsync(tenantId);

            var statistics = new QueueStatistics
            {
                TotalQueued = queuedConversations.Count,
                HighPriorityQueued = queuedConversations.Count(qc => qc.Priority >= QueuePriority.High),
                AverageWaitTime = queuedConversations.Any() ? 
                    TimeSpan.FromTicks((long)queuedConversations.Average(qc => qc.WaitTime.Ticks)) : 
                    TimeSpan.Zero,
                LongestWaitTime = queuedConversations.Any() ? 
                    queuedConversations.Max(qc => qc.WaitTime) : 
                    TimeSpan.Zero,
                AvailableAgents = agentStatuses.Count(s => s.Value == AgentStatus.Online),
                BusyAgents = agentStatuses.Count(s => s.Value == AgentStatus.Busy),
                ServiceLevel = CalculateServiceLevel(queuedConversations)
            };

            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting queue statistics for tenant {TenantId}", tenantId);
            return new QueueStatistics();
        }
    }

    public async Task<bool> EscalateConversationAsync(Guid conversationId, string reason)
    {
        try
        {
            if (_conversationQueue.ContainsKey(conversationId))
            {
                _conversationQueue[conversationId].Priority = QueuePriority.Urgent;
                
                var conversation = await _context.Conversations
                    .FirstOrDefaultAsync(c => c.Id == conversationId);

                if (conversation != null)
                {
                    conversation.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation("Conversation {ConversationId} escalated: {Reason}", conversationId, reason);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error escalating conversation {ConversationId}", conversationId);
            return false;
        }
    }

    private int GetNextQueuePosition(Guid tenantId, QueuePriority priority)
    {
        var tenantQueue = _conversationQueue.Values
            .Where(qc => qc.Priority == priority)
            .OrderBy(qc => qc.QueuedAt);

        return tenantQueue.Count() + 1;
    }

    private async Task<Dictionary<Guid, AgentStatus>> GetAgentStatusesAsync(Guid tenantId)
    {
        return new Dictionary<Guid, AgentStatus>();
    }

    private double CalculateServiceLevel(List<QueuedConversation> queuedConversations)
    {
        if (!queuedConversations.Any())
            return 100.0;

        var targetTime = TimeSpan.FromMinutes(2); // 2 minutes target response time
        var withinTarget = queuedConversations.Count(qc => qc.WaitTime <= targetTime);
        
        return (double)withinTarget / queuedConversations.Count * 100.0;
    }
}
