using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TreeDrive.Core.Models;

public class FileShareRecord
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;
    
    [BsonElement("file_id")]
    public string FileId { get; set; } = string.Empty;
    
    [BsonElement("shared_by")]
    public string SharedBy { get; set; } = string.Empty;  // Owner who shared it
    
    [BsonElement("shared_with")]
    public List<string> SharedWith { get; set; } = new();  // Usernames who can access
    
    [BsonElement("permission")]
    public string Permission { get; set; } = "read";  // "read" or "write"
    
    [BsonElement("shared_at")]
    public DateTime SharedAt { get; set; }
    
    [BsonElement("expires_at")]
    public DateTime? ExpiresAt { get; set; }  // Optional expiration
}