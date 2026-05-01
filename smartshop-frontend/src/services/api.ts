import axios from 'axios';
import { useAuthStore } from '../store/authStore';
import type { ApiResponse, AuthResponse } from '../types/auth';

const API_BASE_URL = import.meta.env.VITE_API_URL ?? 'https://localhost:7046/api';

const api = axios.create({
  baseURL: API_BASE_URL,
});

// Attach access token to every request
api.interceptors.request.use((config) => {
  const token = useAuthStore.getState().accessToken;
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// Auto-refresh on 401
let isRefreshing = false;
let failedQueue: Array<{
  resolve: (token: string) => void;
  reject: (err: unknown) => void;
}> = [];

const processQueue = (error: unknown, token: string | null = null) => {
  failedQueue.forEach((p) => (error ? p.reject(error) : p.resolve(token!)));
  failedQueue = [];
};

api.interceptors.response.use(
  (response) => response,
  async (error) => {
    const original = error.config;

    if (error.response?.status !== 401 || original._retry) {
      return Promise.reject(error);
    }

    if (isRefreshing) {
      return new Promise<string>((resolve, reject) => {
        failedQueue.push({ resolve, reject });
      }).then((token) => {
        original.headers.Authorization = `Bearer ${token}`;
        return api(original);
      });
    }

    original._retry = true;
    isRefreshing = true;

    const { refreshToken, setTokens, logout } = useAuthStore.getState();

    if (!refreshToken) {
      logout();
      isRefreshing = false;
      return Promise.reject(error);
    }

    try {
      const { data } = await axios.post<ApiResponse<AuthResponse>>(
        `${API_BASE_URL}/auth/refresh`,
        { refreshToken }
      );
      const { token, refreshToken: newRefreshToken } = data.data;
      setTokens(token, newRefreshToken);
      processQueue(null, token);
      original.headers.Authorization = `Bearer ${token}`;
      return api(original);
    } catch (err) {
      processQueue(err, null);
      logout();
      return Promise.reject(err);
    } finally {
      isRefreshing = false;
    }
  }
);

export default api;
