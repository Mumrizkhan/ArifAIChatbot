using ChatRuntimeService.Models;
using Shared.Domain.Entities;

namespace ChatRuntimeService.Services;

public interface IChatRuntimeService
{
    Task<ConversationDto> CreateConversationAsync(CreateConversationRequest request, Guid tenantId);
    Task<PaginatedResult<ConversationDto>> GetConversationsAsync(Guid tenantId, int page, int pageSize);
    Task<ConversationDetailDto?> GetConversationAsync(Guid id, Guid tenantId);
    Task<bool> AssignConversationAsync(Guid id, AssignConversationRequest request, Guid tenantId);
    Task<bool> UpdateConversationStatusAsync(Guid id, UpdateStatusRequest request, Guid tenantId);
    Task<MessageDto> SendMessageAsync(Guid conversationId, SendMessageRequest request, Guid tenantId, Guid? userId);
}

public interface IChatService
{
    Task<MessageDto> SendMessageAsync(SendChatMessageRequest request, Guid tenantId);
    Task<ConversationDto> StartConversationAsync(StartConversationRequest request, Guid tenantId);
    Task<bool> EscalateConversationAsync(Guid conversationId, Guid tenantId);
}
