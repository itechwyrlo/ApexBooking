import axios from 'axios';
import type { AxiosInstance, AxiosRequestConfig, InternalAxiosRequestConfig } from 'axios';
import type { BaseResponse, RefreshTokenResponse } from '../features/auth/types';

interface TypedAxiosInstance extends Omit<AxiosInstance, 'get' | 'post' | 'put' | 'patch' | 'delete'> {
  get<T extends BaseResponse>(url: string, config?: AxiosRequestConfig): Promise<T>;
  post<T extends BaseResponse>(url: string, data?: unknown, config?: AxiosRequestConfig): Promise<T>;
  put<T extends BaseResponse>(url: string, data?: unknown, config?: AxiosRequestConfig): Promise<T>;
  patch<T extends BaseResponse>(url: string, data?: unknown, config?: AxiosRequestConfig): Promise<T>;
  delete<T extends BaseResponse>(url: string, config?: AxiosRequestConfig): Promise<T>;
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

// Response interceptor - unwraps axios envelope so callers get server response directly
(axiosInstance as any).interceptors.response.use(
  (response: any) => response.data,
  async (error: any) => {
    const originalRequest = error.config;

    const isAuthenticated = sessionStorage.getItem('isAuthenticated') === 'true';

    const isAuthEndpoint =
      originalRequest.url?.includes('/auth/login') ||
      originalRequest.url?.includes('/auth/refresh') ||
      originalRequest.url?.includes('/auth/register');

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

    return Promise.reject(error);
  }
);

export default axiosInstance;