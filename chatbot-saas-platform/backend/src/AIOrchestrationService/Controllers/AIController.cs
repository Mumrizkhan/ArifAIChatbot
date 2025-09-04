using AIOrchestrationService.Models;
using AIOrchestrationService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Application.Common.Interfaces;
using Shared.Application.Services;
using Shared.Domain.Entities;
using Shared.Domain.Enums;
using Shared.Infrastructure.Services;

namespace AIOrchestrationService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AIController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly IAIService _aiService;
    private readonly IVectorService _vectorService;
    private readonly ITenantService _tenantService;
    private readonly ILogger<AIController> _logger;

    public AIController(
        IApplicationDbContext context,
        IAIService aiService,
        IVectorService vectorService,
        ITenantService tenantService,
        ILogger<AIController> logger)
    {
        _context = context;
        _aiService = aiService;
        _vectorService = vectorService;
        _tenantService = tenantService;
        _logger = logger;
    }

    [HttpPost("chat")]
    [AllowAnonymous]
    
    public async Task<IActionResult> Chat([FromBody] ChatRequest request)
    {
        try
        {
            var conversation = await _context.Conversations
                .Include(c => c.Messages.OrderBy(m => m.CreatedAt))
                .FirstOrDefaultAsync(c => c.Id == request.ConversationId);

            if (conversation == null)
            {
                return NotFound(new { message = "Conversation not found" });
            }

            var conversationHistory = conversation.Messages
                .TakeLast(10)
                .Select(m => new ConversationMessage
                {
                    Role = m.Sender == MessageSender.Customer ? "user" : "assistant",
                    Content = m.Content,
                    Timestamp = m.CreatedAt
                })
                .ToList();

            var contextDocuments = new List<string>();
            //var collectionName = $"tenant_{_tenantService.GetCurrentTenantId():N}_knowledge";
            
            try
            {
                var searchResults = await _vectorService.SearchAcrossAllCollectionsAsync(_tenantService.GetCurrentTenantId(),//SearchSimilarAsync(
                    request.Message, 5);
                contextDocuments = searchResults.Select(r => r.Content).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not search knowledge base for context");
            }

            var aiRequest = new AIRequest
            {
                Message = request.Message,
                Language = request.Language ?? conversation.Language ?? "en",
                ConversationId = request.ConversationId,
                TenantId = _tenantService.GetCurrentTenantId(),
                ConversationHistory = conversationHistory,
                Model = request.Model ?? "gpt-4.1",
                Temperature = request.Temperature ?? 0.1,
                MaxTokens = request.MaxTokens ?? 1000
            };

            AIResponse aiResponse;
            if (contextDocuments.Any())
            {
                aiResponse = await _aiService.GenerateResponseWithContextAsync(aiRequest, contextDocuments);
            }
            else
            {
                aiResponse = await _aiService.GenerateResponseAsync(aiRequest);
            }

            aiResponse.Intents = await _aiService.ExtractIntentsAsync(request.Message);

            //if (aiResponse.IsSuccessful)
            //{
            //    var message = new Message
            //    {
            //        ConversationId = request.ConversationId,
            //        Content = aiResponse.Content,
            //        Type = MessageType.Text,
            //        Sender = MessageSender.Bot,
            //        CreatedAt = DateTime.UtcNow,
            //        TenantId = _tenantService.GetCurrentTenantId()
            //    };

            //    _context.Messages.Add(message);
            //    conversation.MessageCount++;
            //    conversation.UpdatedAt = DateTime.UtcNow;
            //    await _context.SaveChangesAsync();
            //}

            return Ok(new ChatResponse
            {
                Content = aiResponse.Content,
                Language = aiResponse.Language,
                Intents = aiResponse.Intents,
                Confidence = aiResponse.Confidence,
                SourceDocuments = aiResponse.SourceDocuments,
                TokensUsed = aiResponse.TokensUsed,
                ProcessingTime = aiResponse.ProcessingTime,
                IsSuccessful = aiResponse.IsSuccessful,
                ErrorMessage = aiResponse.ErrorMessage
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat request");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("translate")]
    [Authorize]
    public async Task<IActionResult> Translate([FromBody] TranslateRequest request)
    {
        try
        {
            var translatedText = await _aiService.TranslateTextAsync(request.Text, request.TargetLanguage);
            
            return Ok(new TranslateResponse
            {
                OriginalText = request.Text,
                TranslatedText = translatedText,
                SourceLanguage = request.SourceLanguage ?? "auto",
                TargetLanguage = request.TargetLanguage
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error translating text");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("summarize")]
    [Authorize]
    public async Task<IActionResult> SummarizeConversation([FromBody] SummarizeRequest request)
    {
        try
        {
            var conversation = await _context.Conversations
                .Include(c => c.Messages.OrderBy(m => m.CreatedAt))
                .FirstOrDefaultAsync(c => c.Id == request.ConversationId && 
                                   c.TenantId == _tenantService.GetCurrentTenantId());

            if (conversation == null)
            {
                return NotFound(new { message = "Conversation not found" });
            }

            var messages = conversation.Messages
                .Select(m => $"{m.Sender}: {m.Content}")
                .ToList();

            var summary = await _aiService.SummarizeConversationAsync(messages);

            return Ok(new SummarizeResponse
            {
                ConversationId = request.ConversationId,
                Summary = summary,
                MessageCount = messages.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error summarizing conversation");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("intents")]
    [AllowAnonymous]
    public async Task<IActionResult> ExtractIntents([FromBody] IntentRequest request)
    {
        try
        {
            var intents = await _aiService.ExtractIntentsAsync(request.Message);
            
            return Ok(new IntentResponse
            {
                Message = request.Message,
                Intents = intents
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting intents");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("sentiment")]
    [AllowAnonymous]
    public async Task<IActionResult> AnalyzeSentiment([FromBody] SentimentRequest request)
    {
        try
        {
            var sentiment = await _aiService.AnalyzeSentimentAsync(request.Message);
            
            return Ok(new SentimentResponse
            {
                Message = request.Message,
                Sentiment = sentiment
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing sentiment");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("embeddings")]
    [Authorize]
    public async Task<IActionResult> GenerateEmbedding([FromBody] EmbeddingRequest request)
    {
        try
        {
            var embedding = await _aiService.GenerateEmbeddingAsync(request.Text);
            
            return Ok(new EmbeddingResponse
            {
                Embedding = embedding.Split(',').Select(double.Parse).ToList(),
                TokensUsed = 0 // OpenAI doesn't provide token count for embeddings in this implementation
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating embedding");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}
