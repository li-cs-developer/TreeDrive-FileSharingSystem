import React, { createContext, useContext, useState, type ReactNode, useEffect, useCallback, useRef } from 'react';
import { api } from '../api/client';

interface AuthContextType {
  isAuthenticated: boolean;
  username: string | null;
  token: string | null;
  login: (username: string, password: string) => Promise<boolean>;
  logout: () => void;
  loading: boolean;
  error: string | null;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider: React.FC<{ children: ReactNode }> = ({ children }) => {
  const [token, setToken] = useState<string | null>(() => {
    return sessionStorage.getItem('token');
  });
  const [username, setUsername] = useState<string | null>(() => {
    return sessionStorage.getItem('username');
  });
  const [isAuthenticated, setIsAuthenticated] = useState<boolean>(() => {
    return !!sessionStorage.getItem('token');
  });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const isLoggingIn = useRef(false);

  useEffect(() => {
    if (token) {
      api.setAuthToken(token);
    } else {
      api.clearAuthToken();
    }
  }, [token]);

  const login = useCallback(async (username: string, password: string): Promise<boolean> => {
    // Prevent multiple simultaneous login attempts
    if (isLoggingIn.current) {
      console.log('Login already in progress, skipping');
      return false;
    }

    isLoggingIn.current = true;
    setLoading(true);
    setError(null);
    
    try {
      console.log('AuthContext: Making login request');
      const response = await api.login(username, password);
      console.log('AuthContext: Response received', response);
      
      if (response.success && response.token) {
        sessionStorage.setItem('token', response.token);
        sessionStorage.setItem('username', response.username);
        
        setToken(response.token);
        setUsername(response.username);
        setIsAuthenticated(true);
        api.setAuthToken(response.token);
        setError(null);
        isLoggingIn.current = false;
        return true;
      }
      
      setError(response.message || 'Invalid username or password');
      isLoggingIn.current = false;
      return false;
    } catch (error: any) {
      console.error('AuthContext: Login error:', error);
      
      if (error.response?.status === 401) {
        setError('Invalid username or password');
      } else if (error.response?.data?.message) {
        setError(error.response.data.message);
      } else if (error.request) {
        setError('Cannot connect to server. Please check your connection.');
      } else {
        setError('An error occurred. Please try again.');
      }
      isLoggingIn.current = false;
      return false;
    } finally {
      setLoading(false);
    }
  }, []);

  const logout = useCallback(() => {
    sessionStorage.removeItem('token');
    sessionStorage.removeItem('username');
    setToken(null);
    setUsername(null);
    setIsAuthenticated(false);
    api.clearAuthToken();
    setError(null);
  }, []);

  const value = {
    isAuthenticated,
    username,
    token,
    login,
    logout,
    loading,
    error,
  };

  return (
    <AuthContext.Provider value={value}>
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};