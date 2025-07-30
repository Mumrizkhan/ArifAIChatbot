using Microsoft.EntityFrameworkCore;
using Shared.Application.Common.Interfaces;
using Shared.Domain.Entities;
using Shared.Domain.Enums;

namespace ChatRuntimeService.Services;

public class MessageQueueService : IMessageQueueService
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<MessageQueueService> _logger;

    public MessageQueueService(
        IApplicationDbContext context,
        ILogger<MessageQueueService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task QueueMessageAsync(Message message)
    {
        try
        {
            message.IsRead = false;
            message.SentAt = DateTime.UtcNow;
            
            _context.Messages.Add(message);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation($"Message {message.Id} queued for delivery");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error queuing message {message.Id}");
            throw;
        }
    }

    public async Task<IEnumerable<Message>> GetPendingMessagesAsync(Guid userId)
    {
        try
        {
            var conversations = await _context.Conversations
                .Where(c => c.AssignedAgentId == userId || c.UserId == userId)
                .Select(c => c.Id)
                .ToListAsync();

            return await _context.Messages
                .Where(m => conversations.Contains(m.ConversationId) && !m.IsRead)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting pending messages for user {userId}");
            throw;
        }
    }

    public async Task MarkMessageAsDeliveredAsync(Guid messageId)
    {
        try
        {
            var message = await _context.Messages.FindAsync(messageId);
            if (message != null)
            {
                message.IsRead = true;
                message.ReadAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                
                _logger.LogInformation($"Message {messageId} marked as delivered");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error marking message {messageId} as delivered");
            throw;
        }
    }

    public async Task ProcessOfflineMessagesAsync(Guid userId)
    {
        try
        {
            var pendingMessages = await GetPendingMessagesAsync(userId);
            
            foreach (var message in pendingMessages)
            {
                await MarkMessageAsDeliveredAsync(message.Id);
            }
            
            _logger.LogInformation($"Processed {pendingMessages.Count()} offline messages for user {userId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error processing offline messages for user {userId}");
            throw;
        }
    }
}
