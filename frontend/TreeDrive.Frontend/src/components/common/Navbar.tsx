import React from 'react';
import { Link, useLocation } from 'react-router-dom';
import { useAuth } from '../../context/AuthContext';
import { LogOut, FolderOpen, Home } from 'lucide-react';

export const Navbar: React.FC = () => {
  const { username, logout } = useAuth();
  const location = useLocation();

  const isActive = (path: string) => location.pathname === path;

  const handleLogout = () => {
    logout();
    window.location.href = '/login';
  };

  return (
    <nav className="bg-white shadow-md sticky top-0 z-50">
      <div className="container mx-auto px-4 py-3">
        <div className="flex justify-between items-center">
          <div className="flex items-center gap-8">
            <div className="flex items-center gap-2">
              <FolderOpen className="text-blue-600" size={24} />
              <span className="text-xl font-bold text-gray-800">TreeDrive</span>
            </div>
            <div className="hidden sm:flex items-center gap-4">
              <Link
                to="/"
                className={`flex items-center gap-1 text-sm ${
                  isActive('/') ? 'text-blue-600' : 'text-gray-600 hover:text-blue-600'
                }`}
              >
                <Home size={16} />
                Home
              </Link>
              <Link
                to="/files"
                className={`flex items-center gap-1 text-sm ${
                  isActive('/files') ? 'text-blue-600' : 'text-gray-600 hover:text-blue-600'
                }`}
              >
                <FolderOpen size={16} />
                Files
              </Link>
            </div>
          </div>
          <div className="flex items-center gap-4">
            <span className="text-sm text-gray-600 hidden sm:inline">
              👋 {username}
            </span>
            <button
              onClick={handleLogout}
              className="flex items-center gap-1 text-red-600 hover:text-red-800 text-sm hover:bg-red-50 px-3 py-1.5 rounded-lg transition"
            >
              <LogOut size={16} />
              <span className="hidden sm:inline">Logout</span>
            </button>
          </div>
        </div>
      </div>
    </nav>
  );
};