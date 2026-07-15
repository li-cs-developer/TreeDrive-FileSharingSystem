using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using TreeDrive.Infrastructure.Repositories;
using TreeDrive.FileService.Services;
using TreeDrive.Shared.DTOs;

namespace TreeDrive.FileService.Controllers;

[ApiController]
[Route("api/files")]
[Authorize]
public class FilesController : ControllerBase
{
    private readonly FileRepository _fileRepository;
    private readonly ShareRepository _shareRepository;
    private readonly UserRepository _userRepository;
    private readonly IFileStorageService _fileStorage;
    private readonly ILogger<FilesController> _logger;

    public FilesController(
        FileRepository fileRepository,
        ShareRepository shareRepository,
        UserRepository userRepository,
        IFileStorageService fileStorage,
        ILogger<FilesController> logger)
    {
        _fileRepository = fileRepository;
        _shareRepository = shareRepository;
        _userRepository = userRepository;
        _fileStorage = fileStorage;
        _logger = logger;
    }

    [HttpGet("list")]
    public async Task<IActionResult> ListFiles()
    {
        var username = User.FindFirst(ClaimTypes.Name)?.Value;
        if (string.IsNullOrEmpty(username))
            return Unauthorized();

        // ✅ Trim username for comparison
        var trimmedUsername = username.Trim();
        _logger.LogInformation("ListFiles for user: '{Username}'", trimmedUsername);

        // Get user's own files
        var ownFiles = await _fileRepository.GetUserFilesAsync(trimmedUsername);
        
        // Get shared files
        var shares = await _shareRepository.GetSharesForUserAsync(trimmedUsername);
        var sharedFileIds = shares.Select(s => s.FileId).ToList();
        
        // Get shared files metadata
        var sharedFiles = new List<Core.Models.FileMetadata>();
        foreach (var fileId in sharedFileIds)
        {
            var file = await _fileRepository.GetFileByIdAsync(fileId);
            if (file != null && file.Owner != trimmedUsername)
            {
                sharedFiles.Add(file);
            }
        }

        // Combine own and shared files
        var allFiles = ownFiles.Concat(sharedFiles).ToList();

        var response = allFiles.Select(f => {
            // ✅ Trim owner for comparison
            var isOwner = f.Owner?.Trim() == trimmedUsername;
            var isShared = f.SharedWith?.Contains(trimmedUsername) ?? false;
            
            return new FileMetadataResponse
            {
                Id = f.Id,
                Filename = f.Filename,
                Owner = f.Owner,
                Size = f.Size,
                UploadedAt = f.UploadedAt,
                DownloadCount = f.DownloadCount,
                Tags = f.Tags,
                IsOwner = isOwner,
                IsShared = isShared
            };
        }).ToList();

        return Ok(response);
    }

    [HttpGet("download/{id}")]
    public async Task<IActionResult> DownloadFile(string id)
    {
        var username = User.FindFirst(ClaimTypes.Name)?.Value;
        if (string.IsNullOrEmpty(username))
            return Unauthorized();

        var metadata = await _fileRepository.GetFileByIdAsync(id);
        if (metadata == null)
            return NotFound("File not found");

        // Check if user owns the file or has access
        var isOwner = metadata.Owner == username;
        var isShared = metadata.SharedWith?.Contains(username) ?? false;
        var isPublic = metadata.IsPublic;
        
        if (!isOwner && !isShared && !isPublic)
            return Unauthorized("You don't have permission to access this file");

        var (content, mimeType, filename) = await _fileStorage.GetFileAsync(id);
        await _fileRepository.IncrementDownloadCountAsync(id);

        return File(content, mimeType, metadata.Filename);
    }

    [HttpPost("upload")]
    [RequestSizeLimit(100 * 1024 * 1024)]
    public async Task<IActionResult> UploadFile([FromForm] IFormFile file)
    {
        var username = User.FindFirst(ClaimTypes.Name)?.Value;
        if (string.IsNullOrEmpty(username))
            return Unauthorized();

        if (file == null || file.Length == 0)
            return BadRequest("No file provided");

        // Check if file exists
        var existingFile = await _fileRepository.GetFileByFilenameAsync(file.FileName, username);
        if (existingFile != null)
        {
            await _fileStorage.DeleteFileAsync(existingFile.Id, username);
            await _fileRepository.DeleteFileAsync(existingFile.Id, username);
            await _shareRepository.RemoveAllSharesAsync(existingFile.Id);
        }

        var metadata = new Core.Models.FileMetadata
        {
            Filename = file.FileName,
            Owner = username,
            Size = file.Length,
            MimeType = file.ContentType,
            IsDeleted = false,
            SharedWith = new List<string>(),
            IsPublic = false
        };

        await _fileRepository.CreateFileAsync(metadata);

        using var stream = file.OpenReadStream();
        await _fileStorage.SaveFileAsync(file.FileName, stream, username, metadata.Id);

        return Ok(new FileUploadResponse
        {
            FileId = metadata.Id,
            Filename = file.FileName,
            Success = true,
            Message = "File uploaded successfully"
        });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteFile(string id)
    {
        var username = User.FindFirst(ClaimTypes.Name)?.Value;
        if (string.IsNullOrEmpty(username))
            return Unauthorized();

        var metadata = await _fileRepository.GetFileByIdAsync(id);
        if (metadata == null)
            return NotFound("File not found");

        if (metadata.Owner != username)
            return Unauthorized("You don't have permission to delete this file");

        await _fileStorage.DeleteFileAsync(id, username);
        await _fileRepository.DeleteFileAsync(id, username);
        await _shareRepository.RemoveAllSharesAsync(id);

        return Ok(new { message = "File deleted successfully" });
    }

    [HttpPost("share/{id}")]
    public async Task<IActionResult> ShareFile(string id, [FromBody] ShareRequest request)
    {
        var username = User.FindFirst(ClaimTypes.Name)?.Value;
        if (string.IsNullOrEmpty(username))
            return Unauthorized();

        if (request.Users == null || request.Users.Count == 0)
            return BadRequest("No users specified to share with");

        var metadata = await _fileRepository.GetFileByIdAsync(id);
        if (metadata == null)
            return NotFound("File not found");

        // Only owner can share
        if (metadata.Owner != username)
            return Unauthorized("You don't have permission to share this file");

        var existingShare = await _shareRepository.GetShareByFileIdAsync(id);
        if (existingShare != null)
        {
            // Add new users to existing share
            foreach (var user in request.Users)
            {
                if (!existingShare.SharedWith.Contains(user))
                {
                    existingShare.SharedWith.Add(user);
                }
            }
            await _shareRepository.UpdateShareAsync(existingShare);
        }
        else
        {
            // Create new share
            var share = new Core.Models.FileShareRecord
            {
                FileId = id,
                SharedBy = username,
                SharedWith = request.Users,
                Permission = request.Permission ?? "read",
                SharedAt = DateTime.UtcNow
            };
            await _shareRepository.CreateShareAsync(share);
        }

        // Update the file metadata with shared users
        if (metadata.SharedWith == null)
            metadata.SharedWith = new List<string>();
        
        foreach (var user in request.Users)
        {
            if (!metadata.SharedWith.Contains(user))
            {
                metadata.SharedWith.Add(user);
            }
        }
        await _fileRepository.UpdateFileAsync(metadata);

        return Ok(new { 
            message = $"File shared with {request.Users.Count} user(s)",
            users = request.Users
        });
    }

    [HttpGet("search/users")]
    public async Task<IActionResult> SearchUsers([FromQuery] string query)
    {
        var username = User.FindFirst(ClaimTypes.Name)?.Value;
        if (string.IsNullOrEmpty(username))
            return Unauthorized();

        if (string.IsNullOrEmpty(query) || query.Length < 2)
            return Ok(new { users = new List<string>() });

        var users = await _userRepository.SearchUsersAsync(query, username);
        return Ok(new { users = users });
    }
}