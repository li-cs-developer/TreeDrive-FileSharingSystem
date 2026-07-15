import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../../context/AuthContext';
import toast from 'react-hot-toast';
import { api } from '../../api/client';

export const Login: React.FC = () => {
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [isRegistering, setIsRegistering] = useState(false);
  const { login, loading, error } = useAuth();
  const navigate = useNavigate();

  const handleLogin = async () => {
    console.log('Login button clicked');
    
    if (!username || !password) {
      toast.error('❌ Please enter username and password');
      return;
    }

    try {
      const success = await login(username, password);
      console.log('Login result:', success);
      
      if (success) {
        toast.success('✅ Login successful!');
        setTimeout(() => {
          navigate('/');
        }, 300);
      } else {
        const errorMsg = error || 'Invalid username or password';
        console.log('Showing error:', errorMsg);
        toast.error(`❌ ${errorMsg}`);
      }
    } catch (err) {
      console.error('Login error:', err);
      toast.error('❌ An error occurred during login');
    }
  };

  const handleRegister = async () => {
    if (!username || !password) {
      toast.error('❌ Please enter username and password');
      return;
    }

    if (password.length < 6) {
      toast.error('❌ Password must be at least 6 characters');
      return;
    }

    setIsRegistering(true);
    try {
      const response = await api.register(username, password);
      console.log('📡 Registration response:', response);
      
      // ✅ Check if the response indicates an error
      if (response.message && response.message.includes('already exists')) {
        // ❌ This is an error - show RED toast
        toast.error(`❌ ${response.message}`);
        return;
      }
      
      // ✅ SUCCESS - GREEN TOAST
      toast.success(`✅ ${response.message || 'Registration successful! Please login.'}`);
      setUsername('');
      setPassword('');
      
    } catch (error: any) {
      console.error('❌ Registration error:', error);
      
      // ❌ ERROR - RED TOAST
      if (error.response?.status === 409) {
        toast.error('❌ Username already exists. Please choose a different username.');
      } else if (error.response?.status === 400) {
        const message = error.response?.data?.message || 'Invalid registration data';
        toast.error(`❌ ${message}`);
      } else if (error.response?.data?.message) {
        toast.error(`❌ ${error.response.data.message}`);
      } else if (error.code === 'ERR_NETWORK') {
        toast.error('❌ Cannot connect to server. Please check your connection.');
      } else {
        toast.error('❌ Registration failed. Please try again.');
      }
    } finally {
      setIsRegistering(false);
    }
  };

  const handleKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'Enter') {
      e.preventDefault();
      e.stopPropagation();
      handleLogin();
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-blue-50 to-indigo-100 py-12 px-4 sm:px-6 lg:px-8">
      <div className="max-w-md w-full space-y-8 bg-white p-10 rounded-2xl shadow-2xl">
        <div>
          <div className="text-center text-6xl mb-4">🌳</div>
          <h2 className="mt-6 text-center text-3xl font-extrabold text-gray-900">
            TreeDrive
          </h2>
          <p className="mt-2 text-center text-sm text-gray-600">
            Secure File Sharing Platform
          </p>
        </div>
        
        <div className="mt-8 space-y-6">
          <div className="space-y-4">
            <div>
              <label htmlFor="username" className="block text-sm font-medium text-gray-700">
                Username
              </label>
              <input
                id="username"
                type="text"
                autoComplete="username"
                className="mt-1 appearance-none relative block w-full px-3 py-2 border border-gray-300 placeholder-gray-500 text-gray-900 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent sm:text-sm"
                placeholder="Enter your username"
                value={username}
                onChange={(e) => setUsername(e.target.value)}
                onKeyDown={handleKeyDown}
                disabled={loading || isRegistering}
              />
            </div>
            <div>
              <label htmlFor="password" className="block text-sm font-medium text-gray-700">
                Password
              </label>
              <input
                id="password"
                type="password"
                autoComplete="current-password"
                className="mt-1 appearance-none relative block w-full px-3 py-2 border border-gray-300 placeholder-gray-500 text-gray-900 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent sm:text-sm"
                placeholder="Enter your password (min 6 characters)"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                onKeyDown={handleKeyDown}
                disabled={loading || isRegistering}
              />
            </div>
          </div>

          <div className="space-y-3">
            <button
              type="button"
              onClick={handleLogin}
              disabled={loading || isRegistering}
              className="group relative w-full flex justify-center py-3 px-4 border border-transparent text-sm font-medium rounded-lg text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 disabled:opacity-50 disabled:cursor-not-allowed transition"
            >
              {loading ? (
                <span className="flex items-center">
                  <svg className="animate-spin -ml-1 mr-3 h-5 w-5 text-white" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                    <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                    <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                  </svg>
                  Logging in...
                </span>
              ) : (
                'Sign in'
              )}
            </button>

            <button
              type="button"
              onClick={handleRegister}
              disabled={loading || isRegistering}
              className="group relative w-full flex justify-center py-3 px-4 border border-gray-300 text-sm font-medium rounded-lg text-gray-700 bg-white hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 disabled:opacity-50 disabled:cursor-not-allowed transition"
            >
              {isRegistering ? (
                <span className="flex items-center">
                  <svg className="animate-spin -ml-1 mr-3 h-5 w-5 text-gray-700" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                    <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                    <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                  </svg>
                  Registering...
                </span>
              ) : (
                'Create Account'
              )}
            </button>
          </div>
        </div>
      </div>
    </div>
  );
};