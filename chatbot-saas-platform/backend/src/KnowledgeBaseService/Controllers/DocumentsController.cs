using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Application.Common.Interfaces;
using KnowledgeBaseService.Services;
using KnowledgeBaseService.Models;

namespace KnowledgeBaseService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DocumentsController : ControllerBase
{
    private readonly IKnowledgeBaseService _knowledgeBaseService;
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(
        IKnowledgeBaseService knowledgeBaseService,
        ITenantService tenantService,
        ICurrentUserService currentUserService,
        ILogger<DocumentsController> logger)
    {
        _knowledgeBaseService = knowledgeBaseService;
        _tenantService = tenantService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadDocument([FromForm] DocumentUploadRequest request)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var userId = _currentUserService.UserId;

            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            if (request.File == null || request.File.Length == 0)
            {
                return BadRequest(new { message = "No file provided" });
            }

            var allowedExtensions = new[] { ".pdf", ".docx", ".txt", ".md" };
            var fileExtension = Path.GetExtension(request.File.FileName).ToLower();
            
            if (!allowedExtensions.Contains(fileExtension))
            {
                return BadRequest(new { message = $"File type {fileExtension} is not supported. Allowed types: {string.Join(", ", allowedExtensions)}" });
            }

            if (request.File.Length > 10 * 1024 * 1024)
            {
                return BadRequest(new { message = "File size cannot exceed 10MB" });
            }

            var document = await _knowledgeBaseService.UploadDocumentAsync(request, tenantId, userId.Value);

            return Ok(new
            {
                DocumentId = document.Id,
                Title = document.Title,
                Status = document.Status.ToString(),
                Message = "Document uploaded successfully and is being processed"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading document");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetDocuments([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var documents = await _knowledgeBaseService.GetDocumentsAsync(tenantId, page, pageSize);

            return Ok(documents.Select(d => new
            {
                d.Id,
                d.Title,
                d.OriginalFileName,
                d.FileType,
                d.FileSize,
                Status = d.Status.ToString(),
                d.Summary,
                d.Tags,
                d.Language,
                d.ChunkCount,
                d.IsEmbedded,
                d.CreatedAt,
                d.UpdatedAt
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting documents");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("{documentId}")]
    public async Task<IActionResult> GetDocument(Guid documentId)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var document = await _knowledgeBaseService.GetDocumentAsync(documentId, tenantId);

            if (document == null)
            {
                return NotFound(new { message = "Document not found" });
            }

            return Ok(new
            {
                document.Id,
                document.Title,
                document.Content,
                document.OriginalFileName,
                document.FileType,
                document.FileSize,
                Status = document.Status.ToString(),
                document.Summary,
                document.Tags,
                document.Language,
                document.Metadata,
                document.ChunkCount,
                document.IsEmbedded,
                document.CreatedAt,
                document.UpdatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting document {DocumentId}", documentId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpDelete("{documentId}")]
    public async Task<IActionResult> DeleteDocument(Guid documentId)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var deleted = await _knowledgeBaseService.DeleteDocumentAsync(documentId, tenantId);

            if (!deleted)
            {
                return NotFound(new { message = "Document not found" });
            }

            return Ok(new { message = "Document deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document {DocumentId}", documentId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("search")]
    public async Task<IActionResult> SearchDocuments([FromBody] DocumentSearchRequest request)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var results = await _knowledgeBaseService.SearchDocumentsAsync(request, tenantId);

            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching documents");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("rag")]
    public async Task<IActionResult> GenerateRAGResponse([FromBody] RAGRequest request)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var response = await _knowledgeBaseService.GenerateRAGResponseAsync(request, tenantId);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating RAG response");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics()
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var statistics = await _knowledgeBaseService.GetStatisticsAsync(tenantId);

            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting knowledge base statistics");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("{documentId}/reprocess")]
    public async Task<IActionResult> ReprocessDocument(Guid documentId)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var reprocessed = await _knowledgeBaseService.ReprocessDocumentAsync(documentId, tenantId);

            if (!reprocessed)
            {
                return NotFound(new { message = "Document not found or cannot be reprocessed" });
            }

            return Ok(new { message = "Document reprocessing started" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reprocessing document {DocumentId}", documentId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("{documentId}/download")]
    public async Task<IActionResult> DownloadDocument(Guid documentId)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var document = await _knowledgeBaseService.GetDocumentAsync(documentId, tenantId);

            if (document == null)
            {
                return NotFound(new { message = "Document not found" });
            }

            var fileStream = await _knowledgeBaseService.DownloadDocumentAsync(documentId, tenantId);
            var contentType = GetContentType(document.FileType);

            return File(fileStream, contentType, document.OriginalFileName);
        }
        catch (FileNotFoundException)
        {
            return NotFound(new { message = "Document file not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading document {DocumentId}", documentId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    private string GetContentType(string fileExtension)
    {
        return fileExtension.ToLower() switch
        {
            ".pdf" => "application/pdf",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".txt" => "text/plain",
            ".md" => "text/markdown",
            _ => "application/octet-stream"
        };
    }

    [HttpGet("validate-multi-document")]
    public async Task<IActionResult> ValidateMultiDocumentSupport()
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var documents = await _knowledgeBaseService.GetDocumentsAsync(tenantId);
            var statistics = await _knowledgeBaseService.GetStatisticsAsync(tenantId);
            
            var validation = new
            {
                TenantId = tenantId,
                DocumentCount = documents.Count,
                ProcessedDocuments = statistics.ProcessedDocuments,
                FailedDocuments = statistics.FailedDocuments,
                TotalChunks = statistics.TotalChunks,
                CollectionName = $"tenant_{tenantId:N}_knowledge",
                MultiDocumentSupported = documents.Count > 1,
                Documents = documents.Select(d => new
                {
                    d.Id,
                    d.Title,
                    d.Status,
                    d.ChunkCount,
                    d.IsEmbedded
                }).ToList()
            };

            return Ok(validation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating multi-document support");
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }
}
