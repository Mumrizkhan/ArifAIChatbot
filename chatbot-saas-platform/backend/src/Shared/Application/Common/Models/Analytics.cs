using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Application.Common.Models
{


    // Request DTOs
    public record StartConversationAnalyticsRequest
    {
        public string? UserId { get; init; }
        public required string TenantId { get; init; }
        public string? InitialMessage { get; init; }
    }

    public record SendBotMessageAnalyticsRequest
    {
        public required Guid ConversationId { get; init; }
        public required string MessageText { get; init; }
        public required string TenantId { get; init; }
        public required string SessionId { get; init; }
        public bool IsIntentMatch { get; init; }
        public string? MatchedIntent { get; init; }
        public double? ConfidenceScore { get; init; }
        public string? ResponseSource { get; init; }
    }

    public record FeedbackAnalyticsRequest
    {
        public required Guid ConversationId { get; init; }
        public required string MessageId { get; init; }
        public string? UserId { get; init; }
        public required string TenantId { get; init; }
        public required string SessionId { get; init; }
        //public  FeedbackType FeedbackType { get; init; }
        public  string FeedbackType { get; init; }
        public int? Rating { get; init; }
        public string? FeedbackText { get; init; }
        public bool IsHelpful { get; init; }
    }

    public record ProcessIntentAnalyticsRequest
    {
        public required Guid ConversationId { get; init; }
        public required string MessageId { get; init; }
        public required string UserMessage { get; init; }
        public string? UserId { get; init; }
        public required string TenantId { get; init; }
        public required string SessionId { get; init; }
        public string? DetectedIntent { get; init; }
        public double? ConfidenceScore { get; init; }
        public List<string>? ExtractedEntities { get; init; }
        public string? ResponseStrategy { get; init; }
    }
}
