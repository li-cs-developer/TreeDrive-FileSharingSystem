namespace TreeDrive.Shared.DTOs;

public class FileUploadRequest
{
    public string Filename { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public List<string> Tags { get; set; } = new();
}

public class FileUploadResponse
{
    public string FileId { get; set; } = string.Empty;
    public string Filename { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool Success { get; set; }
}

public class FileMetadataResponse
{
    public string Id { get; set; } = string.Empty;
    public string Filename { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public long Size { get; set; }
    public double SizeInMB => Math.Round(Size / 1048576.0, 2);
    public DateTime UploadedAt { get; set; }
    public int DownloadCount { get; set; }
    public List<string> Tags { get; set; } = new();
    public bool IsOwner { get; set; }  
    public bool IsShared { get; set; }  
}

public class FileListResponse
{
    public int TotalFiles { get; set; }
    public List<FileMetadataResponse> Files { get; set; } = new();
}
