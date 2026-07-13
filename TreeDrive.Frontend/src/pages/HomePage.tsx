import React from 'react';
import { Link } from 'react-router-dom';
import { FolderOpen } from 'lucide-react';

export const HomePage: React.FC = () => {
  return (
    <div className="container mx-auto px-4 py-16">
      <div className="text-center mb-16">
        <div className="text-6xl mb-4">🌳</div>
        <h1 className="text-4xl md:text-5xl font-bold text-gray-900 mb-4">
          TreeDrive
        </h1>
        <p className="text-xl text-gray-600 max-w-2xl mx-auto">
          Secure, fast, and reliable file sharing platform
        </p>
        <div className="mt-8">
          <Link
            to="/files"
            className="bg-blue-600 text-white px-8 py-3 rounded-lg hover:bg-blue-700 transition inline-flex items-center gap-2 shadow-lg hover:shadow-xl"
          >
            <FolderOpen size={20} />
            Go to My Files
          </Link>
        </div>
      </div>

      <div className="grid md:grid-cols-3 gap-8 max-w-4xl mx-auto">
        <div className="bg-white p-6 rounded-xl shadow-sm border border-gray-100 text-center">
          <div className="text-3xl mb-3">📤</div>
          <h3 className="text-lg font-semibold mb-2">Upload Files</h3>
          <p className="text-gray-600 text-sm">
            Upload and store your files securely in the cloud
          </p>
        </div>
        <div className="bg-white p-6 rounded-xl shadow-sm border border-gray-100 text-center">
          <div className="text-3xl mb-3">🔒</div>
          <h3 className="text-lg font-semibold mb-2">Secure Access</h3>
          <p className="text-gray-600 text-sm">
            Only you can access your files with JWT authentication
          </p>
        </div>
        <div className="bg-white p-6 rounded-xl shadow-sm border border-gray-100 text-center">
          <div className="text-3xl mb-3">⚡</div>
          <h3 className="text-lg font-semibold mb-2">Fast Downloads</h3>
          <p className="text-gray-600 text-sm">
            Download your files quickly with our optimized service
          </p>
        </div>
      </div>
    </div>
  );
};