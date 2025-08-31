using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shared.Application.Common.Interfaces;

namespace Shared.Infrastructure.Services;

public class FileStorageService : IFileStorageService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<FileStorageService> _logger;
    private readonly string _uploadPath;

    public FileStorageService(IConfiguration configuration, ILogger<FileStorageService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _uploadPath = _configuration.GetValue<string>("FileStorage:UploadPath") ?? "uploads";
        
        // Ensure upload directory exists
        if (!Directory.Exists(_uploadPath))
        {
            Directory.CreateDirectory(_uploadPath);
        }
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType)
    {
        try
        {
            var fileId = Guid.NewGuid().ToString();
            var fileExtension = Path.GetExtension(fileName);
            var storedFileName = $"{fileId}{fileExtension}";
            var filePath = Path.Combine(_uploadPath, storedFileName);

            using var fileStreamOut = new FileStream(filePath, FileMode.Create);
            await fileStream.CopyToAsync(fileStreamOut);

            _logger.LogInformation("File uploaded successfully: {FileName} -> {StoredPath}", fileName, filePath);
            return storedFileName;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file: {FileName}", fileName);
            throw;
        }
    }

    public async Task<Stream> DownloadFileAsync(string filePath)
    {
        try
        {
            var fullPath = Path.Combine(_uploadPath, filePath);
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"File not found: {filePath}");
            }

            var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
            return fileStream;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading file: {FilePath}", filePath);
            throw;
        }
    }

    public async Task<bool> DeleteFileAsync(string filePath)
    {
        try
        {
            var fullPath = Path.Combine(_uploadPath, filePath);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                _logger.LogInformation("File deleted successfully: {FilePath}", filePath);
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file: {FilePath}", filePath);
            return false;
        }
    }

    public async Task<bool> FileExistsAsync(string filePath)
    {
        try
        {
            var fullPath = Path.Combine(_uploadPath, filePath);
            return File.Exists(fullPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking file existence: {FilePath}", filePath);
            return false;
        }
    }

    public async Task<string> GetFileUrlAsync(string filePath)
    {
        try
        {
            var baseUrl = _configuration.GetValue<string>("FileStorage:BaseUrl") ?? "https://localhost:5001";
            return $"{baseUrl}/files/{filePath}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting file URL: {FilePath}", filePath);
            throw;
        }
    }
}