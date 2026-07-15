using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TreeDrive.Core.Models;

public class User
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;
    
    [BsonElement("username")]
    public string Username { get; set; } = string.Empty;
    
    [BsonElement("password_hash")]
    public string PasswordHash { get; set; } = string.Empty;  // Store hashed password
    
    [BsonElement("created_at")]
    public DateTime CreatedAt { get; set; }
    
    [BsonElement("last_login")]
    public DateTime? LastLogin { get; set; }
    
    [BsonElement("is_active")]
    public bool IsActive { get; set; } = true;
    
    [BsonElement("role")]
    public string Role { get; set; } = "User";
}
