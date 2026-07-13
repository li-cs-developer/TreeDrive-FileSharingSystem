using MongoDB.Driver;
using TreeDrive.Core.Models;
using TreeDrive.Infrastructure.Data;

namespace TreeDrive.Infrastructure.Repositories;

public class UserRepository
{
    private readonly MongoDbContext _context;

    public UserRepository(MongoDbContext context)
    {
        _context = context;
    }

    public virtual async Task<User?> GetUserByUsernameAsync(string username)
    {
        return await _context.Users
            .Find(u => u.Username == username && u.IsActive)
            .FirstOrDefaultAsync();
    }

    public virtual async Task<bool> UserExistsAsync(string username)
    {
        var count = await _context.Users
            .CountDocumentsAsync(u => u.Username == username);
        return count > 0;
    }

    public virtual async Task CreateUserAsync(User user)
    {
        user.CreatedAt = DateTime.UtcNow;
        await _context.Users.InsertOneAsync(user);
    }

    public virtual async Task UpdateLastLoginAsync(string username)
    {
        var update = Builders<User>.Update
            .Set(u => u.LastLogin, DateTime.UtcNow);
        
        await _context.Users.UpdateOneAsync(
            u => u.Username == username,
            update
        );
    }

    public virtual async Task<List<string>> SearchUsersAsync(string query)
    {
        // Use regex for case-insensitive search
        var filter = Builders<User>.Filter.And(
            Builders<User>.Filter.Regex(u => u.Username, 
                new MongoDB.Bson.BsonRegularExpression(query, "i")),
            Builders<User>.Filter.Eq(u => u.IsActive, true)
        );
        
        var users = await _context.Users
            .Find(filter)
            .Limit(10)
            .Project(u => u.Username)
            .ToListAsync();
        
        return users;
    }
}