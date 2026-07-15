// Authentication types
export interface LoginRequest {
  username: string;
  password: string;
}

export interface LoginResponse {
  success: boolean;
  token: string;
  username: string;
  message: string;
}

// File types
export interface FileMetadata {
  id: string;
  filename: string;
  owner: string;
  size: number;
  sizeInMB: number;
  uploadedAt: string;
  downloadCount: number;
  tags: string[];
  isOwner: boolean; 
  isShared: boolean;
}

export interface FileListResponse {
  totalFiles: number;
  files: FileMetadata[];
}

export interface FileUploadResponse {
  fileId: string;
  filename: string;
  message: string;
  success: boolean;
}

export interface ApiResponse<T> {
  data?: T;
  error?: string;
}