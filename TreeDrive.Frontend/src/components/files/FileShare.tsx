import React, { useState, useEffect, useRef } from 'react';
import { api } from '../../api/client';
import toast from 'react-hot-toast';
import { X, Search, User, UserPlus } from 'lucide-react';

interface FileShareProps {
  fileId: string;
  filename: string;
  onClose: () => void;
  onShared: () => void;
}

export const FileShare: React.FC<FileShareProps> = ({ fileId, filename, onClose, onShared }) => {
  const [searchQuery, setSearchQuery] = useState('');
  const [searchResults, setSearchResults] = useState<string[]>([]);
  const [selectedUsers, setSelectedUsers] = useState<string[]>([]);
  const [loading, setLoading] = useState(false);
  const [searching, setSearching] = useState(false);
  const searchTimeout = useRef<ReturnType<typeof setTimeout> | null>(null);  // ✅ Fixed

  // Search for users
  useEffect(() => {
    if (searchTimeout.current) {
      clearTimeout(searchTimeout.current);
    }

    if (searchQuery.length < 2) {
      setSearchResults([]);
      return;
    }

    searchTimeout.current = setTimeout(async () => {
      setSearching(true);
      try {
        const results = await api.searchUsers(searchQuery);
        const filtered = results.filter((u: string) => 
          !selectedUsers.includes(u) && u !== sessionStorage.getItem('username')
        );
        setSearchResults(filtered);
      } catch (error) {
        console.error('Search failed:', error);
      } finally {
        setSearching(false);
      }
    }, 300);

    return () => {
      if (searchTimeout.current) {
        clearTimeout(searchTimeout.current);
      }
    };
  }, [searchQuery, selectedUsers]);

  const addUser = (username: string) => {
    if (!selectedUsers.includes(username)) {
      setSelectedUsers([...selectedUsers, username]);
    }
    setSearchQuery('');
    setSearchResults([]);
  };

  const removeUser = (username: string) => {
    setSelectedUsers(selectedUsers.filter(u => u !== username));
  };

  const handleShare = async () => {
    if (selectedUsers.length === 0) {
      toast.error('Please select at least one user');
      return;
    }

    setLoading(true);
    try {
      const response = await api.shareFile(fileId, selectedUsers);
      toast.success(response.message || `File shared with ${selectedUsers.length} user(s)`);
      onShared();
      onClose();
    } catch (error: any) {
      if (error.response?.data?.message) {
        toast.error(error.response.data.message);
      } else {
        toast.error('Share failed');
      }
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
      <div className="bg-white rounded-xl p-6 max-w-md w-full max-h-[80vh] flex flex-col">
        <div className="flex justify-between items-center mb-4">
          <h3 className="text-lg font-semibold">Share: {filename}</h3>
          <button onClick={onClose} className="text-gray-500 hover:text-gray-700">
            <X size={20} />
          </button>
        </div>

        {/* Selected Users */}
        {selectedUsers.length > 0 && (
          <div className="mb-4">
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Selected Users ({selectedUsers.length})
            </label>
            <div className="flex flex-wrap gap-2">
              {selectedUsers.map(user => (
                <span
                  key={user}
                  className="inline-flex items-center gap-1 bg-blue-100 text-blue-800 px-3 py-1 rounded-full text-sm"
                >
                  <User size={14} />
                  {user}
                  <button
                    onClick={() => removeUser(user)}
                    className="hover:text-blue-600"
                  >
                    <X size={14} />
                  </button>
                </span>
              ))}
            </div>
          </div>
        )}

        {/* Search Input */}
        <div className="relative">
          <div className="relative">
            <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400" size={18} />
            <input
              type="text"
              className="w-full pl-10 pr-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent"
              placeholder="Search for users..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              disabled={loading}
            />
            {searching && (
              <div className="absolute right-3 top-1/2 transform -translate-y-1/2">
                <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-blue-600"></div>
              </div>
            )}
          </div>

          {/* Search Results */}
          {searchResults.length > 0 && (
            <div className="absolute z-10 w-full mt-1 bg-white border border-gray-200 rounded-lg shadow-lg max-h-48 overflow-y-auto">
              {searchResults.map(user => (
                <button
                  key={user}
                  onClick={() => addUser(user)}
                  className="w-full flex items-center gap-2 px-4 py-2 hover:bg-gray-50 transition text-left"
                >
                  <UserPlus size={16} className="text-green-500" />
                  <span>{user}</span>
                </button>
              ))}
            </div>
          )}

          {searchQuery.length >= 2 && searchResults.length === 0 && !searching && (
            <div className="absolute z-10 w-full mt-1 bg-white border border-gray-200 rounded-lg shadow-lg p-4 text-center text-gray-500">
              No users found
            </div>
          )}
        </div>

        <p className="text-xs text-gray-500 mt-2">
          Type at least 2 characters to search for users
        </p>

        <button
          onClick={handleShare}
          disabled={loading || selectedUsers.length === 0}
          className="mt-4 w-full bg-green-600 text-white py-2 rounded-lg hover:bg-green-700 transition disabled:opacity-50 disabled:cursor-not-allowed"
        >
          {loading ? 'Sharing...' : `Share with ${selectedUsers.length} user(s)`}
        </button>
      </div>
    </div>
  );
};