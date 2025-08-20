using OpenAI;
using OpenAI.Chat;
using OpenAI.Embeddings;
using AIOrchestrationService.Services;
using AIOrchestrationService.Models;
using System.Diagnostics;

namespace AIOrchestrationService.Services;

public class OpenAIService : IAIService
{
    private readonly ChatClient _chatClient;
    private readonly EmbeddingClient _embeddingClient;
    private readonly ILogger<OpenAIService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _defaultModel;
    private readonly string _embeddingModel;

    public OpenAIService(ILogger<OpenAIService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        var apiKey = System.Environment.GetEnvironmentVariable("OPENAI_Key", EnvironmentVariableTarget.Machine) ?? throw new InvalidOperationException("OpenAI API key not configured");
        _defaultModel = _configuration["OpenAI:Model"] ?? "gpt-4.1";
        _embeddingModel = _configuration["OpenAI:EmbeddingModel"] ?? "text-embedding-ada-002";
        
        _chatClient = new ChatClient(_defaultModel, apiKey);
        _embeddingClient = new EmbeddingClient(_embeddingModel, apiKey);
    }

    public async Task<AIResponse> GenerateResponseAsync(AIRequest request)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var messages = new List<ChatMessage>();
            
            if (request.ConversationHistory != null)
            {
                foreach (var historyMessage in request.ConversationHistory)
                {
                    messages.Add(new UserChatMessage(historyMessage.Content));
                }
            }
            
            messages.Add(new UserChatMessage(request.Message));

            var chatCompletionOptions = new ChatCompletionOptions
            {
                Temperature = (float)request.Temperature,
                MaxOutputTokenCount= request.MaxTokens,

            };

            var completion = await _chatClient.CompleteChatAsync(messages, chatCompletionOptions);
            
            stopwatch.Stop();

            return new AIResponse
            {
                Content = completion.Value.Content[0].Text,
                Language = request.Language,
                Confidence = 0.9,
                TokensUsed = completion.Value.Usage?.OutputTokenCount ?? 0,
                ProcessingTime = stopwatch.Elapsed,
                IsSuccessful = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating AI response");
            stopwatch.Stop();
            
            return new AIResponse
            {
                Content = "I apologize, but I'm having trouble processing your request right now. Please try again later.",
                Language = request.Language,
                ProcessingTime = stopwatch.Elapsed,
                IsSuccessful = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<AIResponse> GenerateResponseWithContextAsync(AIRequest request, List<string> contextDocuments)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var messages = new List<ChatMessage>();
            
            if (contextDocuments.Any())
            {
                var contextText = string.Join("\n\n", contextDocuments);
                var systemPrompt = $"Use the following context to answer the user's question:\n\n{contextText}\n\nIf the context doesn't contain relevant information, don't user your general knowledge, answer \"I don’t know based on the available information.\" and don't include the text \"based on the available information\" if reply is found";
                messages.Add(new SystemChatMessage(systemPrompt));
            }
            
            foreach (var historyMessage in request.ConversationHistory)
            {
                if (historyMessage.Role == "user")
                    messages.Add(new UserChatMessage(historyMessage.Content));
                else if (historyMessage.Role == "assistant")
                    messages.Add(new AssistantChatMessage(historyMessage.Content));
            }
            
            messages.Add(new UserChatMessage(request.Message));

            var chatCompletionOptions = new ChatCompletionOptions
            {
                Temperature = (float)request.Temperature,
                MaxOutputTokenCount = request.MaxTokens
            };

            var completion = await _chatClient.CompleteChatAsync(messages, chatCompletionOptions);
            
            stopwatch.Stop();

            return new AIResponse
            {
                Content = completion.Value.Content[0].Text,
                Language = request.Language,
                Confidence = 0.9,
                SourceDocuments = contextDocuments.Take(3).ToList(),
                TokensUsed = completion.Value.Usage?.OutputTokenCount ?? 0,
                ProcessingTime = stopwatch.Elapsed,
                IsSuccessful = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating AI response with context");
            stopwatch.Stop();
            
            return new AIResponse
            {
                Content = "I apologize, but I'm having trouble processing your request right now. Please try again later.",
                Language = request.Language,
                ProcessingTime = stopwatch.Elapsed,
                IsSuccessful = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<string> GenerateEmbeddingAsync(string text)
    {
        try
        {
            var embedding = await _embeddingClient.GenerateEmbeddingAsync(text);
            var embeddingVector = embedding.Value;
            
            return string.Join(",", embeddingVector);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating embedding");
            throw;
        }
    }

    public async Task<List<string>> ExtractIntentsAsync(string message)
    {
        try
        {
            var systemPrompt = @"Extract the intent(s) from the user message. Return only the intent names as a comma-separated list. 
Possible intents: greeting, goodbye, pricing, request_support, question, general_inquiry, complaint, compliment, booking, cancellation.
If multiple intents are present, list them all. If no specific intent is clear, return 'general_inquiry'.";

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(message)
            };

            var chatCompletionOptions = new ChatCompletionOptions
            {
                Temperature = 0.1f,
                MaxOutputTokenCount = 50
            };

            var completion = await _chatClient.CompleteChatAsync(messages, chatCompletionOptions);
            var response = completion.Value.Content[0].Text.Trim();
            
            var intents = response.Split(',')
                .Select(i => i.Trim().ToLower())
                .Where(i => !string.IsNullOrEmpty(i))
                .ToList();
                
            return intents.Any() ? intents : new List<string> { "general_inquiry" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting intents");
            return new List<string> { "general_inquiry" };
        }
    }

    public async Task<string> TranslateTextAsync(string text, string targetLanguage)
    {
        try
        {
            var systemPrompt = $"Translate the following text to {targetLanguage}. Return only the translated text without any additional explanation.";

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(text)
            };

            var chatCompletionOptions = new ChatCompletionOptions
            {
                Temperature = 0.1f,
                MaxOutputTokenCount = 1000
            };

            var completion = await _chatClient.CompleteChatAsync(messages, chatCompletionOptions);
            return completion.Value.Content[0].Text.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error translating text");
            return text;
        }
    }

    public async Task<string> SummarizeConversationAsync(List<string> messages)
    {
        try
        {
            var conversationText = string.Join("\n", messages);
            var systemPrompt = "Summarize the following conversation in 2-3 sentences, highlighting the main topics discussed and any key outcomes or decisions.";

            var chatMessages = new List<ChatMessage>
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(conversationText)
            };

            var chatCompletionOptions = new ChatCompletionOptions
            {
                Temperature = 0.3f,
                MaxOutputTokenCount = 200
            };

            var completion = await _chatClient.CompleteChatAsync(chatMessages, chatCompletionOptions);
            return completion.Value.Content[0].Text.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error summarizing conversation");
            return "Unable to generate conversation summary.";
        }
    }

    public async Task<string> AnalyzeSentimentAsync(string message)
    {
        try
        {
            var systemPrompt = "Analyze the sentiment of the following message. Respond with only one word: 'positive', 'negative', or 'neutral'.";

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(message)
            };

            var chatCompletionOptions = new ChatCompletionOptions
            {
                Temperature = 0.1f,
                MaxOutputTokenCount = 10
            };

            var completion = await _chatClient.CompleteChatAsync(messages, chatCompletionOptions);
            var sentiment = completion.Value.Content[0].Text.Trim().ToLower();
            
            if (sentiment == "positive" || sentiment == "negative" || sentiment == "neutral")
                return sentiment;
            else
                return "neutral";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing sentiment");
            return "neutral";
        }
    }
}
