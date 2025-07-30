using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Application.Common.Interfaces;
using Shared.Domain.Entities;
using Shared.Domain.Enums;

namespace ChatRuntimeService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConversationsController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITenantService _tenantService;
    private readonly ILogger<ConversationsController> _logger;

    public ConversationsController(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ITenantService tenantService,
        ILogger<ConversationsController> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _tenantService = tenantService;
        _logger = logger;
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> CreateConversation([FromBody] CreateConversationRequest request)
    {
        try
        {
            var conversation = new Conversation
            {
                CustomerName = request.CustomerName,
                CustomerEmail = request.CustomerEmail,
                CustomerPhone = request.CustomerPhone,
                Subject = request.Subject,
                Channel = request.Channel,
                Language = request.Language ?? "en",
                Status = ConversationStatus.Active,
                TenantId = _tenantService.GetCurrentTenantId()
            };

            _context.Conversations.Add(conversation);
            await _context.SaveChangesAsync();

            return Ok(new ConversationDto
            {
                Id = conversation.Id,
                CustomerName = conversation.CustomerName,
                CustomerEmail = conversation.CustomerEmail,
                Subject = conversation.Subject,
                Status = conversation.Status.ToString(),
                Channel = conversation.Channel.ToString(),
                Language = conversation.Language,
                CreatedAt = conversation.CreatedAt,
                MessageCount = conversation.MessageCount
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating conversation");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetConversations([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            var query = _context.Conversations
                .Where(c => c.TenantId == _tenantService.GetCurrentTenantId())
                .OrderByDescending(c => c.UpdatedAt ?? c.CreatedAt);

            var totalCount = await query.CountAsync();
            var conversations = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new ConversationDto
                {
                    Id = c.Id,
                    CustomerName = c.CustomerName,
                    CustomerEmail = c.CustomerEmail,
                    Subject = c.Subject,
                    Status = c.Status.ToString(),
                    Channel = c.Channel.ToString(),
                    Language = c.Language,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt,
                    MessageCount = c.MessageCount,
                    AssignedAgentId = c.AssignedAgentId
                })
                .ToListAsync();

            return Ok(new
            {
                Conversations = conversations,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving conversations");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> GetConversation(Guid id)
    {
        try
        {
            var conversation = await _context.Conversations
                .Include(c => c.Messages.OrderBy(m => m.CreatedAt))
                .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == _tenantService.GetCurrentTenantId());

            if (conversation == null)
            {
                return NotFound(new { message = "Conversation not found" });
            }

            return Ok(new ConversationDetailDto
            {
                Id = conversation.Id,
                CustomerName = conversation.CustomerName,
                CustomerEmail = conversation.CustomerEmail,
                CustomerPhone = conversation.CustomerPhone,
                Subject = conversation.Subject,
                Status = conversation.Status.ToString(),
                Channel = conversation.Channel.ToString(),
                Language = conversation.Language,
                CreatedAt = conversation.CreatedAt,
                UpdatedAt = conversation.UpdatedAt,
                EndedAt = conversation.EndedAt,
                MessageCount = conversation.MessageCount,
                AssignedAgentId = conversation.AssignedAgentId,
                CustomerSatisfactionRating = conversation.CustomerSatisfactionRating,
                CustomerFeedback = conversation.CustomerFeedback,
                Messages = conversation.Messages.Select(m => new MessageDto
                {
                    Id = m.Id,
                    Content = m.Content,
                    Type = m.Type.ToString(),
                    Sender = m.Sender.ToString(),
                    SenderId = m.SenderId,
                    CreatedAt = m.CreatedAt,
                    IsRead = m.IsRead,
                    AttachmentUrl = m.AttachmentUrl
                }).ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error retrieving conversation {id}");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPut("{id}/assign")]
    [Authorize]
    public async Task<IActionResult> AssignConversation(Guid id, [FromBody] AssignConversationRequest request)
    {
        try
        {
            var conversation = await _context.Conversations
                .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == _tenantService.GetCurrentTenantId());

            if (conversation == null)
            {
                return NotFound(new { message = "Conversation not found" });
            }

            conversation.AssignedAgentId = request.AgentId;
            conversation.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Conversation assigned successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error assigning conversation {id}");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPut("{id}/status")]
    [Authorize]
    public async Task<IActionResult> UpdateConversationStatus(Guid id, [FromBody] UpdateStatusRequest request)
    {
        try
        {
            var conversation = await _context.Conversations
                .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == _tenantService.GetCurrentTenantId());

            if (conversation == null)
            {
                return NotFound(new { message = "Conversation not found" });
            }

            if (Enum.TryParse<ConversationStatus>(request.Status, out var status))
            {
                conversation.Status = status;
                conversation.UpdatedAt = DateTime.UtcNow;

                if (status == ConversationStatus.Closed || status == ConversationStatus.Resolved)
                {
                    conversation.EndedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                return Ok(new { message = "Conversation status updated successfully" });
            }

            return BadRequest(new { message = "Invalid status" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating conversation status {id}");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}

public class CreateConversationRequest
{
    public string? CustomerName { get; set; }
    public string? CustomerEmail { get; set; }
    public string? CustomerPhone { get; set; }
    public string? Subject { get; set; }
    public ConversationChannel Channel { get; set; } = ConversationChannel.Widget;
    public string? Language { get; set; }
}

public class AssignConversationRequest
{
    public Guid? AgentId { get; set; }
}

public class UpdateStatusRequest
{
    public string Status { get; set; } = string.Empty;
}

public class ConversationDto
{
    public Guid Id { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerEmail { get; set; }
    public string? Subject { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int MessageCount { get; set; }
    public Guid? AssignedAgentId { get; set; }
}

public class ConversationDetailDto : ConversationDto
{
    public string? CustomerPhone { get; set; }
    public DateTime? EndedAt { get; set; }
    public int? CustomerSatisfactionRating { get; set; }
    public string? CustomerFeedback { get; set; }
    public List<MessageDto> Messages { get; set; } = new();
}

public class MessageDto
{
    public Guid Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Sender { get; set; } = string.Empty;
    public Guid? SenderId { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsRead { get; set; }
    public string? AttachmentUrl { get; set; }
}
