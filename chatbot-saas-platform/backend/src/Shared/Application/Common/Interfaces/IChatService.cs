using Shared.Domain.Entities;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;

namespace Shared.Application.Common.Interfaces;

public interface IChatService
{
    Task<Conversation> StartConversationAsync(StartConversationRequest request, Guid tenantId);
    Task<Message> SendMessageAsync(SendChatMessageRequest request, Guid tenantId);
    Task<bool> AssignConversationAsync(Guid conversationId, Guid agentId);
    Task<bool> EscalateConversationAsync(Guid conversationId, Guid toAgentId);
    Task<List<Conversation>> GetActiveConversationsAsync();
    Task<List<Message>> GetConversationMessagesAsync(Guid conversationId);
    Task<bool> CloseConversationAsync(Guid conversationId);
}

public class StartConversationRequest
{
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string InitialMessage { get; set; } = string.Empty;
    public string? CustomerPhone { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class SendChatMessageRequest
{
    public Guid ConversationId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string MessageType { get; set; } = "text";
    public string? SenderName { get; set; }
    public Guid? SenderId { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}