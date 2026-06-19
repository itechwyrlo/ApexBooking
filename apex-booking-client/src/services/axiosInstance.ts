import axios from 'axios';
import type { AxiosInstance, AxiosRequestConfig, InternalAxiosRequestConfig } from 'axios';
import type { RefreshTokenResponseData } from '../features/auth/types';

// Define the shape of the error object your catch blocks will receive
export interface ApiError {
  message: string;
  status?: number;
  data?: any;
}

interface TypedAxiosInstance extends Omit<AxiosInstance, 'get' | 'post' | 'put' | 'patch' | 'delete'> {
  get<T = any>(url: string, config?: AxiosRequestConfig): Promise<T>;
  post<T = any>(url: string, data?: unknown, config?: AxiosRequestConfig): Promise<T>;
  put<T = any>(url: string, data?: unknown, config?: AxiosRequestConfig): Promise<T>;
  patch<T = any>(url: string, data?: unknown, config?: AxiosRequestConfig): Promise<T>;
  delete<T = any>(url: string, config?: AxiosRequestConfig): Promise<T>;
}
const axiosInstance = axios.create({
  baseURL: '/api',
  withCredentials: true,
  headers: {
    'Content-Type': 'application/json',
  },
}) as unknown as TypedAxiosInstance;

// Request interceptor
(axiosInstance as any).interceptors.request.use(
  (config: InternalAxiosRequestConfig) => {
    const token = sessionStorage.getItem('access_token');
    if (token && config.headers) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error: any) => Promise.reject(error)
);

// Response interceptor
(axiosInstance as any).interceptors.response.use(
  // Success path: Unwraps axios envelope
  (response: any) => response.data,

  // Error path: Handles refreshes and extracts server error messages
  async (error: any) => {
    const originalRequest = error.config;
    const isAuthenticated = sessionStorage.getItem('isAuthenticated') === 'true';
    const isSuperAdmin = sessionStorage.getItem('user_type') === 'superadmin';
    const isAuthEndpoint =
      originalRequest.url?.includes('/auth/login') ||
      originalRequest.url?.includes('/auth/refresh') ||
      originalRequest.url?.includes('/auth/register');

    // 1. Handle Token Refresh (401)
    if (
      error.response?.status === 401 &&
      isAuthenticated &&
      !originalRequest._retry &&
      !isAuthEndpoint
    ) {
      originalRequest._retry = true;

      const refreshUrl = isSuperAdmin ? '/auth/refresh/superadmin' : '/auth/refresh';

      try {
        const refreshResponse = await axiosInstance.post<RefreshTokenResponseData>(refreshUrl);

        if (refreshResponse.accessToken) {
          sessionStorage.setItem('access_token', refreshResponse.accessToken);

          window.dispatchEvent(
            new CustomEvent('auth_token_refreshed', {
              detail: refreshResponse.accessToken,
            })
          );

          originalRequest.headers.Authorization = `Bearer ${refreshResponse.accessToken}`;

          return (axiosInstance as any)(originalRequest);
        }
      } catch {
        sessionStorage.removeItem('access_token');
        sessionStorage.removeItem('isAuthenticated');
        window.location.href = '/login';
      }
    }

    // 2. Extract and Format Error for the Page
    const serverData = error.response?.data as { message?: string } | undefined;

    const formattedError: ApiError = {
      message: serverData?.message || error.message || 'An unexpected error occurred.',
      status: error.response?.status,
      data: serverData
    };

    return Promise.reject(formattedError);
  }
);

export default axiosInstance;
