namespace ChatRuntimeService.Services;

public class FileUploadService : IFileUploadService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<FileUploadService> _logger;
    private readonly string _uploadPath;
    private readonly long _maxFileSize;
    private readonly string[] _allowedExtensions;

    public FileUploadService(
        IConfiguration configuration,
        ILogger<FileUploadService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _uploadPath = _configuration["FileUpload:Path"] ?? "uploads";
        _maxFileSize = long.Parse(_configuration["FileUpload:MaxSizeBytes"] ?? "10485760"); // 10MB
        _allowedExtensions = _configuration.GetSection("FileUpload:AllowedExtensions").Get<string[]>() 
            ?? new[] { ".jpg", ".jpeg", ".png", ".gif", ".pdf", ".doc", ".docx", ".txt" };
        
        Directory.CreateDirectory(_uploadPath);
    }

    public async Task<string> UploadFileAsync(IFormFile file, string conversationId)
    {
        try
        {
            if (!IsValidFileType(file.FileName))
                throw new ArgumentException("File type not allowed");

            if (!IsValidFileSize(file.Length))
                throw new ArgumentException("File size exceeds limit");

            var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
            var conversationFolder = Path.Combine(_uploadPath, conversationId);
            Directory.CreateDirectory(conversationFolder);
            
            var filePath = Path.Combine(conversationFolder, fileName);

            using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);

            var fileUrl = $"/api/files/{conversationId}/{fileName}";
            _logger.LogInformation($"File uploaded: {fileUrl}");
            
            return fileUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error uploading file for conversation {conversationId}");
            throw;
        }
    }

    public async Task<bool> DeleteFileAsync(string fileUrl)
    {
        try
        {
            var filePath = GetFilePathFromUrl(fileUrl);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.LogInformation($"File deleted: {fileUrl}");
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting file {fileUrl}");
            return false;
        }
    }

    public async Task<Stream> GetFileStreamAsync(string fileUrl)
    {
        try
        {
            var filePath = GetFilePathFromUrl(fileUrl);
            if (File.Exists(filePath))
            {
                return new FileStream(filePath, FileMode.Open, FileAccess.Read);
            }
            throw new FileNotFoundException($"File not found: {fileUrl}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting file stream for {fileUrl}");
            throw;
        }
    }

    public bool IsValidFileType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return _allowedExtensions.Contains(extension);
    }

    public bool IsValidFileSize(long fileSize)
    {
        return fileSize <= _maxFileSize;
    }

    private string GetFilePathFromUrl(string fileUrl)
    {
        var relativePath = fileUrl.Replace("/api/files/", "");
        return Path.Combine(_uploadPath, relativePath);
    }
}
