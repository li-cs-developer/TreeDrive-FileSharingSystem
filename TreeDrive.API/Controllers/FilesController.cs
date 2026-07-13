using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using TreeDrive.FileService.Services;
using TreeDrive.Infrastructure.Repositories;
using TreeDrive.Shared.DTOs;

namespace TreeDrive.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FilesController : ControllerBase
{
    private readonly FileRepository _fileRepository;
    private readonly ShareRepository _shareRepository;
    private readonly UserRepository _userRepository;  // ✅ Added this
    private readonly IFileStorageService _fileStorage;
    private readonly ILogger<FilesController> _logger;

    public FilesController(
        FileRepository fileRepository,
        ShareRepository shareRepository,
        UserRepository userRepository,  // ✅ Added this
        IFileStorageService fileStorage,
        ILogger<FilesController> logger)
    {
        _fileRepository = fileRepository;
        _shareRepository = shareRepository;
        _userRepository = userRepository;  // ✅ Added this
        _fileStorage = fileStorage;
        _logger = logger;
    }

    [HttpGet("list")]
    public async Task<IActionResult> ListFiles([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var username = User.FindFirst(ClaimTypes.Name)?.Value;
        if (string.IsNullOrEmpty(username))
            return Unauthorized();

        try
        {
            // Get user's own files
            var ownFiles = await _fileRepository.GetUserFilesAsync(username, page, pageSize);
            
            // Get files shared with user
            var sharedFiles = await _shareRepository.GetSharesForUserAsync(username);
            
            // Get the actual file metadata for shared files
            var sharedFileIds = sharedFiles.Select(s => s.FileId).ToList();
            var sharedFileMetadata = new List<FileMetadataResponse>();
            
            foreach (var fileId in sharedFileIds)
            {
                var file = await _fileRepository.GetFileByIdAsync(fileId);
                if (file != null && !file.IsDeleted)
                {
                    sharedFileMetadata.Add(new FileMetadataResponse
                    {
                        Id = file.Id,
                        Filename = file.Filename,
                        Owner = file.Owner,
                        Size = file.Size,
                        UploadedAt = file.UploadedAt,
                        DownloadCount = file.DownloadCount,
                        Tags = file.Tags,
                        IsOwner = false,
                        IsShared = true
                    });
                }
            }

            // Combine own files and shared files
            var allFiles = ownFiles.Select(f => new FileMetadataResponse
            {
                Id = f.Id,
                Filename = f.Filename,
                Owner = f.Owner,
                Size = f.Size,
                UploadedAt = f.UploadedAt,
                DownloadCount = f.DownloadCount,
                Tags = f.Tags,
                IsOwner = true,
                IsShared = false
            }).ToList();

            allFiles.AddRange(sharedFileMetadata);

            var response = new FileListResponse
            {
                TotalFiles = allFiles.Count,
                Files = allFiles
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing files for user {Username}", username);
            return StatusCode(500, new { error = "Failed to list files", details = ex.Message });
        }
    }

    [HttpGet("download/{id}")]
    public async Task<IActionResult> DownloadFile(string id)
    {
        var username = User.FindFirst(ClaimTypes.Name)?.Value;
        if (string.IsNullOrEmpty(username))
            return Unauthorized();

        // Check if user has access to the file
        var metadata = await _fileRepository.GetFileByIdAsync(id);
        if (metadata == null || metadata.IsDeleted)
            return NotFound("File not found");

        // Check if user is the owner OR the file is shared with them
        bool isOwner = metadata.Owner == username;
        bool isShared = await _shareRepository.GetShareAsync(id, username) != null;

        if (!isOwner && !isShared)
            return Forbid();

        try
        {
            var (content, mimeType, filename) = await _fileStorage.GetFileAsync(id);
            await _fileRepository.IncrementDownloadCountAsync(id);

            return File(content, mimeType, metadata.Filename);
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogError(ex, "File not found on disk: {FileId}", id);
            return NotFound("File content not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading file {FileId}", id);
            return StatusCode(500, new { error = "Download failed", details = ex.Message });
        }
    }

    [HttpPost("upload")]
    [RequestSizeLimit(100 * 1024 * 1024)]
    public async Task<IActionResult> UploadFile([FromForm] IFormFile file, [FromForm] List<string>? tags)
    {
        var username = User.FindFirst(ClaimTypes.Name)?.Value;
        if (string.IsNullOrEmpty(username))
            return Unauthorized();

        if (file == null || file.Length == 0)
            return BadRequest("No file provided");

        try
        {
            // Check if file already exists for this user
            var existingFile = await _fileRepository.GetFileByFilenameAsync(file.FileName, username);
            
            if (existingFile != null)
            {
                // If it's soft deleted, permanently delete it
                if (existingFile.IsDeleted)
                {
                    await _fileRepository.DeleteFileAsync(existingFile.Id, username);
                }
                else
                {
                    // Active file exists - overwrite it
                    await _fileStorage.DeleteFileAsync(existingFile.Id, username);
                    await _fileRepository.DeleteFileAsync(existingFile.Id, username);
                    // Also remove any shares
                    await _shareRepository.RemoveAllSharesAsync(existingFile.Id);
                    _logger.LogInformation("Overwriting existing file: {Filename} by {Username}", file.FileName, username);
                }
            }

            // Create metadata first to get MongoDB ID
            var metadata = new Core.Models.FileMetadata
            {
                Filename = file.FileName,
                Owner = username,
                Size = file.Length,
                MimeType = file.ContentType,
                Tags = tags ?? new List<string>(),
                IsDeleted = false,
                UploadedAt = DateTime.UtcNow
            };

            // Insert metadata to get the MongoDB-generated ID
            await _fileRepository.CreateFileAsync(metadata);

            // Now save the file using the MongoDB ID
            using var stream = file.OpenReadStream();
            await _fileStorage.SaveFileAsync(file.FileName, stream, username, metadata.Id);

            _logger.LogInformation("File uploaded: {Filename} by {Username}, ID: {FileId}", 
                file.FileName, username, metadata.Id);

            return Ok(new FileUploadResponse
            {
                FileId = metadata.Id,
                Filename = file.FileName,
                Success = true,
                Message = existingFile != null && !existingFile.IsDeleted ? "File overwritten successfully" : "File uploaded successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Upload failed for {Filename} by {Username}", file.FileName, username);
            return StatusCode(500, new { error = "Upload failed", details = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteFile(string id)
    {
        var username = User.FindFirst(ClaimTypes.Name)?.Value;
        if (string.IsNullOrEmpty(username))
            return Unauthorized();

        var metadata = await _fileRepository.GetFileByIdAsync(id);
        if (metadata == null || metadata.Owner != username)
            return NotFound("File not found or access denied");

        try
        {
            await _fileStorage.DeleteFileAsync(id, username);
            await _fileRepository.DeleteFileAsync(id, username);
            // Also remove any shares
            await _shareRepository.RemoveAllSharesAsync(id);

            _logger.LogInformation("File deleted: {Filename} by {Username}", metadata.Filename, username);

            return Ok(new { message = "File deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delete failed for file {FileId} by {Username}", id, username);
            return StatusCode(500, new { error = "Delete failed", details = ex.Message });
        }
    }

    [HttpPost("share/{id}")]
    public async Task<IActionResult> ShareFile(string id, [FromBody] ShareRequest request)
    {
        var username = User.FindFirst(ClaimTypes.Name)?.Value;
        if (string.IsNullOrEmpty(username))
            return Unauthorized();

        // Check if user owns the file
        var metadata = await _fileRepository.GetFileByIdAsync(id);
        if (metadata == null || metadata.Owner != username)
            return NotFound("File not found or you don't own it");

        if (request.Users == null || request.Users.Count == 0)
            return BadRequest(new { error = "No users specified to share with" });

        // ✅ Validate that all users exist
        var invalidUsers = new List<string>();
        var validUsers = new List<string>();
        
        foreach (var user in request.Users)
        {
            // Skip sharing with yourself
            if (user == username) continue;
            
            // Check if user exists
            var userExists = await _userRepository.UserExistsAsync(user);
            if (userExists)
            {
                validUsers.Add(user);
            }
            else
            {
                invalidUsers.Add(user);
            }
        }

        // ✅ Return error if any invalid users
        if (invalidUsers.Count > 0)
        {
            return BadRequest(new 
            { 
                error = "Some users do not exist", 
                invalidUsers = invalidUsers,
                message = $"Users not found: {string.Join(", ", invalidUsers)}"
            });
        }

        if (validUsers.Count == 0)
        {
            return BadRequest(new { error = "No valid users to share with" });
        }

        try
        {
            // Add each valid user to the share list
            foreach (var user in validUsers)
            {
                // Check if already shared
                var existingShare = await _shareRepository.GetShareAsync(id, user);
                if (existingShare == null)
                {
                    await _shareRepository.CreateShareAsync(new Core.Models.FileShareRecord
                    {
                        FileId = id,
                        SharedBy = username,
                        SharedWith = new List<string> { user },
                        Permission = request.Permission ?? "read",
                        ExpiresAt = request.ExpiresInDays.HasValue 
                            ? DateTime.UtcNow.AddDays(request.ExpiresInDays.Value) 
                            : null
                    });
                }
            }

            _logger.LogInformation("File {Filename} shared by {Username} with {Count} users", 
                metadata.Filename, username, validUsers.Count);

            return Ok(new 
            { 
                message = $"File shared with {validUsers.Count} user(s)",
                sharedWith = validUsers,
                invalidUsers = invalidUsers
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Share failed for file {FileId} by {Username}", id, username);
            return StatusCode(500, new { error = "Share failed", details = ex.Message });
        }
    }
}

// DTO for share request (defined at the bottom)
public class ShareRequest
{
    public List<string> Users { get; set; } = new();
    public string? Permission { get; set; } = "read";
    public int? ExpiresInDays { get; set; }
}