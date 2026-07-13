namespace TreeDrive.FileService.Services;

public interface IFileStorageService
{
    Task<string> SaveFileAsync(string filename, Stream fileStream, string owner, string fileId);
    Task<(byte[] Content, string MimeType, string Filename)> GetFileAsync(string fileId);
    Task<bool> DeleteFileAsync(string fileId, string owner);
    Task<bool> FileExistsAsync(string fileId);
}