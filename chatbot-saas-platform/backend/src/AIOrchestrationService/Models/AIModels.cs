namespace AIOrchestrationService.Models;

public class AIRequest
{
    public string Message { get; set; } = string.Empty;
    public string Language { get; set; } = "en";
    public Guid ConversationId { get; set; }
    public Guid TenantId { get; set; }
    public List<ConversationMessage> ConversationHistory { get; set; } = new();
    public Dictionary<string, object> Context { get; set; } = new();
    public string Model { get; set; } = "gpt-3.5-turbo";
    public double Temperature { get; set; } = 0.7;
    public int MaxTokens { get; set; } = 1000;
}

public class AIResponse
{
    public string Content { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public List<string> Intents { get; set; } = new();
    public double Confidence { get; set; }
    public List<string> SourceDocuments { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public int TokensUsed { get; set; }
    public TimeSpan ProcessingTime { get; set; }
    public bool IsSuccessful { get; set; } = true;
    public string? ErrorMessage { get; set; }
}

public class ConversationMessage
{
    public string Role { get; set; } = string.Empty; // "user", "assistant", "system"
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

public class SearchResult
{
    public string Id { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public double Score { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class EmbeddingRequest
{
    public string Text { get; set; } = string.Empty;
    public string Model { get; set; } = "text-embedding-ada-002";
}

public class EmbeddingResponse
{
    public List<double> Embedding { get; set; } = new();
    public int TokensUsed { get; set; }
}

public class IntentExtractionRequest
{
    public string Message { get; set; } = string.Empty;
    public string Language { get; set; } = "en";
    public List<string> PossibleIntents { get; set; } = new();
}

public class TranslationRequest
{
    public string Text { get; set; } = string.Empty;
    public string SourceLanguage { get; set; } = "auto";
    public string TargetLanguage { get; set; } = "en";
}
