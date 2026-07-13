import React, { useEffect, useState } from 'react';
import { api } from '../../api/client';
import type { FileMetadata, FileListResponse } from '../../types';
import toast from 'react-hot-toast';
import { Download, Trash2, Upload, File, Share2 } from 'lucide-react';
import { FileShare } from './FileShare';

export const FileList: React.FC = () => {
  const [files, setFiles] = useState<FileMetadata[]>([]);
  const [loading, setLoading] = useState(true);
  const [uploading, setUploading] = useState(false);
  const [sharingFile, setSharingFile] = useState<FileMetadata | null>(null);

  const loadFiles = async () => {
    try {
      setLoading(true);
      const data: FileListResponse = await api.listFiles();
      setFiles(data.files || []);
    } catch (error) {
      toast.error('Failed to load files');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadFiles();
  }, []);

  const handleUpload = async (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (!file) return;

    setUploading(true);
    try {
      const response = await api.uploadFile(file);
      if (response.success) {
        toast.success('File uploaded successfully!');
        loadFiles();
      }
    } catch (error) {
      toast.error('Upload failed');
    } finally {
      setUploading(false);
      event.target.value = '';
    }
  };

  const handleDownload = async (fileId: string, filename: string) => {
    try {
      const response = await api.downloadFile(fileId);
      const url = window.URL.createObjectURL(new Blob([response.data]));
      const link = document.createElement('a');
      link.href = url;
      link.setAttribute('download', filename);
      document.body.appendChild(link);
      link.click();
      link.remove();
      window.URL.revokeObjectURL(url);
      toast.success('Download started');
    } catch (error) {
      toast.error('Download failed');
    }
  };

  const handleDelete = async (fileId: string, filename: string) => {
    if (!confirm(`Are you sure you want to delete "${filename}"?`)) return;

    try {
      await api.deleteFile(fileId);
      toast.success('File deleted');
      loadFiles();
    } catch (error) {
      toast.error('Delete failed');
    }
  };

  const handleShare = (file: FileMetadata) => {
    setSharingFile(file);
  };

  const handleShareClose = () => {
    setSharingFile(null);
  };

  const handleShareSuccess = () => {
    loadFiles();
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center py-20">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600"></div>
      </div>
    );
  }

  return (
    <div className="container mx-auto px-4 py-8">
      <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4 mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">My Files</h1>
          <p className="text-sm text-gray-500">{files.length} files stored</p>
        </div>
        <label className="cursor-pointer bg-blue-600 text-white px-6 py-2.5 rounded-lg hover:bg-blue-700 transition flex items-center gap-2 shadow-md hover:shadow-lg">
          <Upload size={20} />
          {uploading ? 'Uploading...' : 'Upload File'}
          <input
            type="file"
            className="hidden"
            onChange={handleUpload}
            disabled={uploading}
          />
        </label>
      </div>

      {files.length === 0 ? (
        <div className="text-center py-20 bg-white rounded-xl shadow-sm border border-gray-100">
          <div className="text-6xl mb-4">📂</div>
          <h3 className="text-xl font-medium text-gray-900 mb-2">No files yet</h3>
          <p className="text-gray-500">Click "Upload File" to get started!</p>
        </div>
      ) : (
        <div className="bg-white rounded-xl shadow-sm border border-gray-100 overflow-hidden">
          <div className="overflow-x-auto">
            <table className="min-w-full divide-y divide-gray-200">
              <thead className="bg-gray-50">
                <tr>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    File
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Size
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider hidden md:table-cell">
                    Uploaded
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider hidden sm:table-cell">
                    Downloads
                  </th>
                  <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Actions
                  </th>
                </tr>
              </thead>
              <tbody className="bg-white divide-y divide-gray-200">
                {files.map((file) => (
                  <tr key={file.id} className="hover:bg-gray-50 transition">
                    <td className="px-6 py-4 whitespace-nowrap">
                      <div className="flex items-center">
                        <File size={20} className="text-blue-500 mr-3" />
                        <div>
                          <div className="text-sm font-medium text-gray-900">
                            {file.filename}
                            {/* Show "Owner" badge for user's own files */}
                            {file.isOwner && (
                              <span className="ml-2 text-xs bg-blue-100 text-blue-800 px-2 py-0.5 rounded">
                                Owner
                              </span>
                            )}
                            {/* Show "Shared by: owner" for shared files */}
                            {file.isShared && !file.isOwner && (
                              <span className="ml-2 text-xs bg-green-100 text-green-800 px-2 py-0.5 rounded">
                                Shared by: {file.owner}
                              </span>
                            )}
                          </div>
                          <div className="text-xs text-gray-500">
                            {file.isShared && !file.isOwner && `Shared with you`}
                          </div>
                        </div>
                      </div>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                      {file.sizeInMB.toFixed(2)} MB
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500 hidden md:table-cell">
                      {formatDate(file.uploadedAt)}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500 hidden sm:table-cell">
                      {file.downloadCount}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
                      <div className="flex justify-end gap-2">
                        <button
                          onClick={() => handleDownload(file.id, file.filename)}
                          className="text-blue-600 hover:text-blue-900 p-1 hover:bg-blue-50 rounded-lg transition"
                          title="Download"
                        >
                          <Download size={18} />
                        </button>
                        {/* Show share button only if user owns the file */}
                        {file.isOwner && (
                          <button
                            onClick={() => handleShare(file)}
                            className="text-green-600 hover:text-green-900 p-1 hover:bg-green-50 rounded-lg transition"
                            title="Share"
                          >
                            <Share2 size={18} />
                          </button>
                        )}
                        {/* Show delete button only if user owns the file */}
                        {file.isOwner && (
                          <button
                            onClick={() => handleDelete(file.id, file.filename)}
                            className="text-red-600 hover:text-red-900 p-1 hover:bg-red-50 rounded-lg transition"
                            title="Delete"
                          >
                            <Trash2 size={18} />
                          </button>
                        )}
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* Share Modal */}
      {sharingFile && (
        <FileShare
          fileId={sharingFile.id}
          filename={sharingFile.filename}
          onClose={handleShareClose}
          onShared={handleShareSuccess}
        />
      )}
    </div>
  );
};