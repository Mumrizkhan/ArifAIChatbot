namespace ChatRuntimeService.Services;

public interface IFileUploadService
{
    Task<string> UploadFileAsync(IFormFile file, string conversationId);
    Task<bool> DeleteFileAsync(string fileUrl);
    Task<Stream> GetFileStreamAsync(string fileUrl);
    bool IsValidFileType(string fileName);
    bool IsValidFileSize(long fileSize);
}
