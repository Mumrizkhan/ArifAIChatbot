namespace ChatRuntimeService.Models;

public class AIResponse
{
    public string Response { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public string[] Intents { get; set; } = Array.Empty<string>();
    public bool RequiresHumanAgent { get; set; }
}

public class TransferResponse
{
    public bool ShouldTransfer { get; set; }
    public string Reason { get; set; } = string.Empty;
    public double Confidence { get; set; }
}

public class SentimentResponse
{
    public string Sentiment { get; set; } = string.Empty;
    public double Score { get; set; }
    public double Confidence { get; set; }
}

public class IntentResponse
{
    public string[] Intents { get; set; } = Array.Empty<string>();
    public Dictionary<string, double> Confidence { get; set; } = new();
}

public class SpamResponse
{
    public bool IsSpam { get; set; }
    public double Confidence { get; set; }
    public string[] Reasons { get; set; } = Array.Empty<string>();
}
