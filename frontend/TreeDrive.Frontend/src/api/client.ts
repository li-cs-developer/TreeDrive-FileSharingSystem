import axios, { type AxiosInstance, type InternalAxiosRequestConfig } from 'axios';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000';

class ApiClient {
  private client: AxiosInstance;
  private token: string | null = null;

  constructor() {
    this.client = axios.create({
      baseURL: API_BASE_URL,
      headers: {
        'Content-Type': 'application/json',
      },
    });

    // Add token to requests if available
    this.client.interceptors.request.use(
      (config: InternalAxiosRequestConfig) => {
        if (!this.token) {
          const storedToken = sessionStorage.getItem('token');
          if (storedToken) {
            this.token = storedToken;
          }
        }
        
        if (this.token) {
          config.headers.Authorization = `Bearer ${this.token}`;
        }
        return config;
      },
      (error) => Promise.reject(error)
    );

    // Handle 401 responses
    this.client.interceptors.response.use(
      (response) => response,
      (error) => {
        if (error.response?.status === 401) {
          sessionStorage.removeItem('token');
          sessionStorage.removeItem('username');
          this.token = null;
          window.location.href = '/login';
        }
        return Promise.reject(error);
      }
    );
  }

  setAuthToken(token: string) {
    this.token = token;
  }

  clearAuthToken() {
    this.token = null;
  }

  // Auth endpoints
  async login(username: string, password: string) {
    const response = await this.client.post('/api/auth/login', {
      username,
      password,
    });
    return response.data;
  }

  async register(username: string, password: string) {
    try {
      const response = await this.client.post('/api/auth/register', {
        username,
        password,
      });
      return response.data;
    } catch (error: any) {
      throw error;
    }
  }

  // File endpoints
  async listFiles() {
    const response = await this.client.get('/api/files/list');
    console.log('📡 API /files/list response:', response.data);
    return response.data;
  }

  async uploadFile(file: File, tags?: string[]) {
    const formData = new FormData();
    formData.append('file', file);
    if (tags) {
      tags.forEach(tag => formData.append('tags', tag));
    }

    const response = await this.client.post('/api/files/upload', formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    });
    return response.data;
  }

  async downloadFile(fileId: string) {
    const response = await this.client.get(`/api/files/download/${fileId}`, {
      responseType: 'blob',
    });
    return response;
  }

  async deleteFile(fileId: string) {
    const response = await this.client.delete(`/api/files/${fileId}`);
    return response.data;
  }

  async shareFile(fileId: string, users: string[]) {
    const response = await this.client.post(`/api/files/share/${fileId}`, {
      users: users,
      permission: "read"
    });
    return response.data;
  }

  async healthCheck() {
    const response = await this.client.get('/health');
    return response.data;
  }

  // ✅ FIXED: Correct endpoint
  async searchUsers(query: string) {
    const response = await this.client.get(`/api/files/search/users?query=${encodeURIComponent(query)}`);
    return response.data.users;
  }
}

export const api = new ApiClient();