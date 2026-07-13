using MongoDB.Driver;
using TreeDrive.Core.Models;
using TreeDrive.Infrastructure.Data;

namespace TreeDrive.Infrastructure.Repositories;

public class ShareRepository
{
    private readonly MongoDbContext _context;

    public ShareRepository(MongoDbContext context)
    {
        _context = context;
    }

    public async Task<FileShareRecord?> GetShareAsync(string fileId, string sharedWith)
    {
        return await _context.Shares
            .Find(s => s.FileId == fileId && s.SharedWith.Contains(sharedWith))
            .FirstOrDefaultAsync();
    }

    public async Task<List<FileShareRecord>> GetSharesForUserAsync(string username)
    {
        return await _context.Shares
            .Find(s => s.SharedWith.Contains(username))
            .ToListAsync();
    }

    public async Task CreateShareAsync(FileShareRecord share)
    {
        share.SharedAt = DateTime.UtcNow;
        await _context.Shares.InsertOneAsync(share);
    }

    public async Task RemoveShareAsync(string fileId, string sharedWith)
    {
        await _context.Shares.DeleteOneAsync(
            s => s.FileId == fileId && s.SharedWith.Contains(sharedWith)
        );
    }

    public async Task RemoveAllSharesAsync(string fileId)
    {
        await _context.Shares.DeleteManyAsync(s => s.FileId == fileId);
    }
}