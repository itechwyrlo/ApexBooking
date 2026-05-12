import axios from 'axios';
import type { AxiosInstance, AxiosRequestConfig, InternalAxiosRequestConfig } from 'axios';
import type { RefreshTokenResponse } from '../features/auth/types';
import type { BaseResponse } from '../types';

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

      try {
        const refreshResponse = await axiosInstance.post<RefreshTokenResponse>('/auth/refresh');

        if (refreshResponse.isSuccess && refreshResponse.data?.accessToken) {
          sessionStorage.setItem('access_token', refreshResponse.data.accessToken);

          window.dispatchEvent(
            new CustomEvent('auth_token_refreshed', {
              detail: refreshResponse.data.accessToken,
            })
          );

          originalRequest.headers.Authorization = `Bearer ${refreshResponse.data.accessToken}`;

          return (axiosInstance as any)(originalRequest);
        }
      } catch {
        sessionStorage.removeItem('access_token');
        sessionStorage.removeItem('isAuthenticated');
        window.location.href = '/login';
      }
    }

    // 2. Extract and Format Error for the Page
    // This looks for the error message in your BaseResponse structure
    const serverData = error.response?.data as BaseResponse | undefined;
    
    const formattedError: ApiError = {
      message: serverData?.errors?.[0]?.message || error.message || 'An unexpected error occurred.',
      status: error.response?.status,
      data: serverData
    };

    return Promise.reject(formattedError);
  }
);

export default axiosInstance;