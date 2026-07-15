using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace TreeDrive.FileService.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _basePath;
    private readonly ILogger<LocalFileStorageService> _logger;

    public LocalFileStorageService(IConfiguration configuration, ILogger<LocalFileStorageService> logger)
    {
        _basePath = configuration["FileStorage:Path"] ?? Path.Combine(Directory.GetCurrentDirectory(), "server_files");
        _logger = logger;
        
        Directory.CreateDirectory(_basePath);
    }

    public async Task<string> SaveFileAsync(string filename, Stream fileStream, string owner, string fileId)
    {
        var extension = Path.GetExtension(filename);
        var safeFilename = $"{fileId}{extension}";
        var filePath = Path.Combine(_basePath, safeFilename);

        using var fileStreamOut = File.Create(filePath);
        await fileStream.CopyToAsync(fileStreamOut);
        
        _logger.LogInformation("File saved: {Filename} by {Owner} with ID: {FileId}", filename, owner, fileId);
        return fileId;
    }

    public async Task<(byte[] Content, string MimeType, string Filename)> GetFileAsync(string fileId)
    {
        // Try to find file with any extension
        var files = Directory.GetFiles(_basePath, $"{fileId}.*");
        if (!files.Any())
        {
            throw new FileNotFoundException($"File with ID {fileId} not found");
        }

        var filePath = files.First();
        var filename = Path.GetFileName(filePath);
        var content = await File.ReadAllBytesAsync(filePath);
        
        return (content, GetMimeType(filename), filename);
    }

    public async Task<bool> DeleteFileAsync(string fileId, string owner)
    {
        var files = Directory.GetFiles(_basePath, $"{fileId}.*");
        if (!files.Any()) return false;

        foreach (var filePath in files)
        {
            File.Delete(filePath);
            _logger.LogInformation("File deleted: {FilePath} by {Owner}", filePath, owner);
        }

        return true;
    }

    public async Task<bool> FileExistsAsync(string fileId)
    {
        return Directory.GetFiles(_basePath, $"{fileId}.*").Any();
    }

    private string GetMimeType(string filename)
    {
        var extension = Path.GetExtension(filename).ToLower();
        return extension switch
        {
            ".txt" => "text/plain",
            ".pdf" => "application/pdf",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".ppt" => "application/vnd.ms-powerpoint",
            ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            ".zip" => "application/zip",
            ".rar" => "application/x-rar-compressed",
            ".7z" => "application/x-7z-compressed",
            ".mp4" => "video/mp4",
            ".mp3" => "audio/mpeg",
            ".json" => "application/json",
            ".xml" => "application/xml",
            ".csv" => "text/csv",
            ".html" => "text/html",
            ".css" => "text/css",
            ".js" => "application/javascript",
            _ => "application/octet-stream"
        };
    }
}