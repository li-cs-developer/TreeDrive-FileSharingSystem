using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TreeDrive.Core.Models;

public class FileMetadata
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;
    
    [BsonElement("filename")]
    public string Filename { get; set; } = string.Empty;
    
    [BsonElement("owner")]
    public string Owner { get; set; } = string.Empty;
    
    [BsonElement("size")]
    public long Size { get; set; }
    
    [BsonElement("uploaded_at")]
    public DateTime UploadedAt { get; set; }
    
    [BsonElement("last_accessed")]
    public DateTime? LastAccessed { get; set; }
    
    [BsonElement("download_count")]
    public int DownloadCount { get; set; }
    
    [BsonElement("file_hash")]
    public string FileHash { get; set; } = string.Empty;
    
    [BsonElement("mime_type")]
    public string MimeType { get; set; } = string.Empty;
    
    [BsonElement("tags")]
    public List<string> Tags { get; set; } = new();
    
    [BsonElement("is_deleted")]
    public bool IsDeleted { get; set; }
    
    [BsonElement("deleted_at")]
    public DateTime? DeletedAt { get; set; }
    
    // NEW: Sharing properties
    [BsonElement("is_public")]
    public bool IsPublic { get; set; }  // If true, anyone can view
    
    [BsonElement("shared_users")]
    public List<string> SharedWith { get; set; } = new();  // Explicit user access
}