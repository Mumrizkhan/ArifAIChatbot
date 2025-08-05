using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ChatRuntimeService.Services;

namespace ChatRuntimeService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FilesController : ControllerBase
{
    private readonly IFileUploadService _fileUploadService;
    private readonly ILogger<FilesController> _logger;

    public FilesController(
        IFileUploadService fileUploadService,
        ILogger<FilesController> logger)
    {
        _fileUploadService = fileUploadService;
        _logger = logger;
    }

    [HttpPost("upload/{conversationId}")]
    [Authorize]
    public async Task<IActionResult> UploadFile([FromForm] UploadFileDto file)
    {
        try
        {
            if (file == null || file.File.Length == 0)
                return BadRequest(new { message = "No file provided" });

            var fileUrl = await _fileUploadService.UploadFileAsync(file.File, file.ConversationId);
            
            return Ok(new { 
                FileUrl = fileUrl,
                FileName = file.File.FileName,
                FileSize = file.File.Length
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error uploading file for conversation {file.ConversationId}");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("{conversationId}/{fileName}")]
    public async Task<IActionResult> GetFile(string conversationId, string fileName)
    {
        try
        {
            var fileUrl = $"/api/files/{conversationId}/{fileName}";
            var stream = await _fileUploadService.GetFileStreamAsync(fileUrl);
            
            var contentType = GetContentType(fileName);
            return File(stream, contentType, fileName);
        }
        catch (FileNotFoundException)
        {
            return NotFound(new { message = "File not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error retrieving file {fileName} for conversation {conversationId}");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpDelete("{conversationId}/{fileName}")]
    [Authorize]
    public async Task<IActionResult> DeleteFile(string conversationId, string fileName)
    {
        try
        {
            var fileUrl = $"/api/files/{conversationId}/{fileName}";
            var deleted = await _fileUploadService.DeleteFileAsync(fileUrl);
            
            if (deleted)
                return Ok(new { message = "File deleted successfully" });
            else
                return NotFound(new { message = "File not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting file {fileName} for conversation {conversationId}");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("upload")]
    [AllowAnonymous]
    public async Task<IActionResult> UploadFileForWidget([FromForm] UploadFileDto file)
    {
        try
        {
            if (file == null || file.File == null || file.File.Length == 0)
                return BadRequest(new { message = "No file provided" });

            if (!Guid.TryParse(file.ConversationId, out var convId))
                return BadRequest(new { message = "Invalid conversation ID" });

            var fileUrl = await _fileUploadService.UploadFileAsync(file.File, file.ConversationId);

            return Ok(new { fileUrl });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file for widget");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    private static string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".txt" => "text/plain",
            _ => "application/octet-stream"
        };
    }
}


public class UploadFileDto
{
    [FromForm(Name = "file")]
    public IFormFile File { get; set; }

    [FromForm(Name = "conversationId")]
    public string ConversationId { get; set; }
}