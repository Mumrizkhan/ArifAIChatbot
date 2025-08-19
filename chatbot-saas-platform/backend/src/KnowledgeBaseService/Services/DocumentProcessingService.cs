// using OpenAI;
// using OpenAI.Embeddings;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

using KnowledgeBaseService.Services;
using KnowledgeBaseService.Models;
using System.Text;
using System.Text.RegularExpressions;
using Shared.Domain.Entities;
using Document = Shared.Domain.Entities.Document;

namespace KnowledgeBaseService.Services;

public class DocumentProcessingService : IDocumentProcessingService
{
    // private readonly OpenAIClient _openAIClient;
    private readonly ILogger<DocumentProcessingService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IVectorSearchService _vectorSearchService;

    public DocumentProcessingService(
        ILogger<DocumentProcessingService> logger, 
        IConfiguration configuration, 
        IVectorSearchService vectorSearchService)
    {
        _logger = logger;
        _configuration = configuration;
        _vectorSearchService = vectorSearchService;

        var apiKey = _configuration["OpenAI:ApiKey"] ?? throw new InvalidOperationException("OpenAI API key not configured");
        // _openAIClient = new OpenAIClient(apiKey);
    }

    public async Task<DocumentProcessingResult> ProcessDocumentAsync(Document document, Stream fileStream)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            var content = await ExtractTextFromFileAsync(fileStream, document.FileType);
            document.Content = content;

            document.Summary = await GenerateDocumentSummaryAsync(content);

            var extractedTags = await ExtractTagsAsync(content);
            document.Tags.AddRange(extractedTags);

            var chunks = await ChunkDocumentAsync(document, content);
            document.ChunkCount = chunks.Count;

            var embeddingsGenerated = await GenerateEmbeddingsAsync(chunks);
            
            var collectionName = $"tenant_{document.TenantId:N}_knowledge";
            var storedInVector = await StoreInVectorDatabaseAsync(chunks, collectionName);
            
            document.IsEmbedded = embeddingsGenerated && storedInVector;
            document.VectorCollectionName = collectionName;
            document.Status = DocumentStatus.Processed;

            stopwatch.Stop();

            return new DocumentProcessingResult
            {
                IsSuccessful = true,
                ChunksCreated = chunks.Count,
                Summary = document.Summary,
                ExtractedTags = extractedTags,
                ProcessingTime = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing document {DocumentId}", document.Id);
            document.Status = DocumentStatus.Failed;
            stopwatch.Stop();

            return new DocumentProcessingResult
            {
                IsSuccessful = false,
                ErrorMessage = ex.Message,
                ProcessingTime = stopwatch.Elapsed
            };
        }
    }

    public async Task<string> ExtractTextFromFileAsync(Stream fileStream, string fileType)
    {
        try
        {
            return fileType.ToLower() switch
            {
                ".pdf" => await ExtractTextFromPdfAsync(fileStream),
                ".docx" => await ExtractTextFromDocxAsync(fileStream),
                ".txt" => await ExtractTextFromTxtAsync(fileStream),
                ".md" => await ExtractTextFromTxtAsync(fileStream),
                _ => throw new NotSupportedException($"File type {fileType} is not supported")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting text from file type {FileType}", fileType);
            throw;
        }
    }

    public async Task<List<DocumentChunk>> ChunkDocumentAsync(Document document, string content)
    {
        try
        {
            var chunks = new List<DocumentChunk>();
            const int chunkSize = 1000; // Characters per chunk
            const int overlap = 200; // Overlap between chunks

            var sentences = SplitIntoSentences(content);
            var currentChunk = new StringBuilder();
            var currentPosition = 0;
            var chunkIndex = 0;

            foreach (var sentence in sentences)
            {
                if (currentChunk.Length + sentence.Length > chunkSize && currentChunk.Length > 0)
                {
                    var chunkContent = currentChunk.ToString().Trim();
                    var chunk = new DocumentChunk
                    {
                        Id = Guid.NewGuid(),
                        DocumentId = document.Id,
                        Content = chunkContent,
                        ChunkIndex = chunkIndex,
                        StartPosition = currentPosition - chunkContent.Length,
                        EndPosition = currentPosition,
                        TenantId = document.TenantId,
                        Metadata = new Dictionary<string, object>
                        {
                            ["document_title"] = document.Title,
                            ["document_type"] = document.FileType,
                            ["language"] = document.Language,
                            ["chunk_size"] = chunkContent.Length
                        }
                    };

                    chunks.Add(chunk);
                    chunkIndex++;

                    var overlapText = GetLastWords(chunkContent, overlap);
                    currentChunk.Clear();
                    currentChunk.Append(overlapText);
                }

                currentChunk.Append(" ").Append(sentence);
                currentPosition += sentence.Length + 1;
            }

            if (currentChunk.Length > 0)
            {
                var chunkContent = currentChunk.ToString().Trim();
                var chunk = new DocumentChunk
                {
                    Id = Guid.NewGuid(),
                    DocumentId = document.Id,
                    Content = chunkContent,
                    ChunkIndex = chunkIndex,
                    StartPosition = currentPosition - chunkContent.Length,
                    EndPosition = currentPosition,
                    TenantId = document.TenantId,
                    Metadata = new Dictionary<string, object>
                    {
                        ["document_title"] = document.Title,
                        ["document_type"] = document.FileType,
                        ["language"] = document.Language,
                        ["chunk_size"] = chunkContent.Length
                    }
                };

                chunks.Add(chunk);
            }

            return chunks;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error chunking document {DocumentId}", document.Id);
            throw;
        }
    }

    public async Task<string> GenerateDocumentSummaryAsync(string content)
    {
        try
        {
            if (content.Length < 500)
                return content.Substring(0, Math.Min(content.Length, 200)) + "...";

            var prompt = "Summarize the following document in 2-3 sentences, highlighting the main topics and key information:";
            var truncatedContent = content.Length > 3000 ? content.Substring(0, 3000) + "..." : content;

            await Task.Delay(100);
            
            var words = content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var firstWords = string.Join(" ", words.Take(10));
            return $"Document summary: This document contains {words.Length} words starting with '{firstWords}...' This is a placeholder summary as OpenAI integration is not configured.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating document summary");
            return "Unable to generate summary.";
        }
    }

    public async Task<List<string>> ExtractTagsAsync(string content)
    {
        try
        {
            var prompt = @"Extract 5-10 relevant tags/keywords from the following document content. 
Return only the tags as a comma-separated list, focusing on main topics, concepts, and important terms.
Tags should be single words or short phrases (2-3 words max).";

            var truncatedContent = content.Length > 2000 ? content.Substring(0, 2000) + "..." : content;

            await Task.Delay(50);
            
            var words = content.ToLower()
                .Split(new char[] { ' ', '\n', '\r', '\t', '.', ',', '!', '?' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 3)
                .GroupBy(w => w)
                .OrderByDescending(g => g.Count())
                .Take(8)
                .Select(g => g.Key);
            
            var tagsText = string.Join(",", words);
            
            return tagsText.Split(',')
                .Select(tag => tag.Trim().ToLower())
                .Where(tag => !string.IsNullOrEmpty(tag))
                .Take(10)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting tags");
            return new List<string>();
        }
    }

    public async Task<bool> GenerateEmbeddingsAsync(List<DocumentChunk> chunks)
    {
        try
        {
            foreach (var chunk in chunks)
            {
                try
                {
                    var random = new Random(chunk.Content.GetHashCode());
                    var embedding = new float[1536];
                    for (int i = 0; i < embedding.Length; i++)
                    {
                        embedding[i] = (float)(random.NextDouble() * 2 - 1);
                    }
                    chunk.Embedding = string.Join(",", embedding);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to generate embedding for chunk {ChunkId}", chunk.Id);
                    chunk.Embedding = null;
                }
            }

            return chunks.All(c => !string.IsNullOrEmpty(c.Embedding));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating embeddings for chunks");
            return false;
        }
    }

    public async Task<bool> StoreInVectorDatabaseAsync(List<DocumentChunk> chunks, string collectionName)
    {
        try
        {
            // Ensure the collection exists
            var collectionCreated = await _vectorSearchService.CreateCollectionAsync(collectionName, chunks.First().TenantId);
            if (!collectionCreated)
            {
                _logger.LogError("Failed to create or access collection {CollectionName}", collectionName);
                return false;
            }

            // Upsert points into the vector database
            var upserted = await _vectorSearchService.UpdateDocumentInVectorAsync(new Document
            {
                Id = chunks.First().DocumentId,
                VectorCollectionName = collectionName,
                TenantId = chunks.First().TenantId
            }, chunks);

            return upserted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing chunks in vector database");
            return false;
        }
    }

    private async Task<string> ExtractTextFromPdfAsync(Stream fileStream)
    {
        try
        {
            using var pdfReader = new PdfReader(fileStream);
            using var pdfDocument = new PdfDocument(pdfReader);
            
            var text = new StringBuilder();
            for (int i = 1; i <= pdfDocument.GetNumberOfPages(); i++)
            {
                var page = pdfDocument.GetPage(i);
                var pageText = PdfTextExtractor.GetTextFromPage(page);
                text.AppendLine(pageText);
            }

            return text.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting text from PDF");
            throw;
        }
    }

    private async Task<string> ExtractTextFromDocxAsync(Stream fileStream)
    {
        try
        {
            using var document = WordprocessingDocument.Open(fileStream, false);
            var body = document.MainDocumentPart?.Document?.Body;
            
            if (body == null)
                return string.Empty;

            var text = new StringBuilder();
            foreach (var paragraph in body.Elements<Paragraph>())
            {
                text.AppendLine(paragraph.InnerText);
            }

            return text.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting text from DOCX");
            throw;
        }
    }

    private async Task<string> ExtractTextFromTxtAsync(Stream fileStream)
    {
        try
        {
            using var reader = new StreamReader(fileStream, Encoding.UTF8);
            return await reader.ReadToEndAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting text from TXT");
            throw;
        }
    }

    private List<string> SplitIntoSentences(string text)
    {
        var sentences = new List<string>();
        var regex = new Regex(@"[.!?]+\s+", RegexOptions.Compiled);
        var parts = regex.Split(text);

        foreach (var part in parts)
        {
            if (!string.IsNullOrWhiteSpace(part))
            {
                sentences.Add(part.Trim());
            }
        }

        return sentences;
    }

    private string GetLastWords(string text, int maxLength)
    {
        if (text.Length <= maxLength)
            return text;

        var lastSpaceIndex = text.LastIndexOf(' ', maxLength);
        return lastSpaceIndex > 0 ? text.Substring(lastSpaceIndex + 1) : text.Substring(text.Length - maxLength);
    }
}
