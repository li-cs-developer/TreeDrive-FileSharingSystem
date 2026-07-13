using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using TreeDrive.Core.Models;

namespace TreeDrive.Infrastructure.Data;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(IConfiguration configuration)
    {
        var connectionString = configuration["MongoDB:ConnectionString"] 
            ?? "mongodb://localhost:27017";
        var databaseName = configuration["MongoDB:DatabaseName"] 
            ?? "TreeDrive";
        
        var client = new MongoClient(connectionString);
        _database = client.GetDatabase(databaseName);
        
        CreateIndexes();
    }

    public IMongoCollection<FileMetadata> Files => 
        _database.GetCollection<FileMetadata>("files");
    
    public IMongoCollection<User> Users => 
        _database.GetCollection<User>("users");

    public IMongoCollection<FileShareRecord> Shares => 
        _database.GetCollection<FileShareRecord>("shares");
    private void CreateIndexes()
    {
        // Create unique index on filename + owner
        try
        {
            var indexModel = new CreateIndexModel<FileMetadata>(
                Builders<FileMetadata>.IndexKeys.Combine(
                    Builders<FileMetadata>.IndexKeys.Ascending(f => f.Owner),
                    Builders<FileMetadata>.IndexKeys.Ascending(f => f.Filename)
                ),
                new CreateIndexOptions { Unique = true }
            );
            
            Files.Indexes.CreateOne(indexModel);
            
            // Create index on owner
            var ownerIndex = new CreateIndexModel<FileMetadata>(
                Builders<FileMetadata>.IndexKeys.Ascending(f => f.Owner)
            );
            Files.Indexes.CreateOne(ownerIndex);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Index creation warning: {ex.Message}");
        }
    }
}