using MongoDB.Driver;
using TreeDrive.Core.Models;
using TreeDrive.Infrastructure.Data;

namespace TreeDrive.Infrastructure.Repositories;

public class FileRepository
{
    private readonly MongoDbContext _context;

    public FileRepository(MongoDbContext context)
    {
        _context = context;
    }

    public virtual async Task<FileMetadata?> GetFileByIdAsync(string id)
    {
        return await _context.Files
            .Find(f => f.Id == id)
            .FirstOrDefaultAsync();
    }

    public virtual async Task<FileMetadata?> GetFileByFilenameAsync(string filename, string owner)
    {
        return await _context.Files
            .Find(f => f.Filename == filename && f.Owner == owner)
            .FirstOrDefaultAsync();
    }

    public virtual async Task<List<FileMetadata>> GetUserFilesAsync(string owner, int page = 1, int pageSize = 20)
    {
        var skip = (page - 1) * pageSize;
        return await _context.Files
            .Find(f => f.Owner == owner)
            .SortByDescending(f => f.UploadedAt)
            .Skip(skip)
            .Limit(pageSize)
            .ToListAsync();
    }

    public virtual async Task CreateFileAsync(FileMetadata file)
    {
        file.UploadedAt = DateTime.UtcNow;
        file.IsDeleted = false;
        await _context.Files.InsertOneAsync(file);
    }

    public virtual async Task DeleteFileAsync(string id, string owner)
    {
        await _context.Files.DeleteOneAsync(
            f => f.Id == id && f.Owner == owner
        );
    }

    public virtual async Task IncrementDownloadCountAsync(string id)
    {
        var update = Builders<FileMetadata>.Update
            .Inc(f => f.DownloadCount, 1)
            .Set(f => f.LastAccessed, DateTime.UtcNow);
        
        await _context.Files.UpdateOneAsync(
            f => f.Id == id,
            update
        );
    }
}
