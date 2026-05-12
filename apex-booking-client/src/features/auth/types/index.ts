// Auth request and response types matching backend endpoints

import type { BaseResponse } from "../../../types";

export interface AuthResponseData {
  accessToken: string;
  refreshToken: string;
  userId: string;
  tenantId: string;
  tenantSlug: string | null
}

export interface AuthResponse extends BaseResponse<AuthResponseData> {}

export interface EmailVerificationData {
  url: string;
  tenantSlug: string;
}

export interface EmailVerificationResponse extends BaseResponse<EmailVerificationData> {}

// Login Request
export interface LoginRequest {
  email: string;
  password: string;
}

// Account Verification Request
export interface AccountVerificationRequest {
  token: string;
}

// Account Verification Response Data
export interface AccountVerificationResponseData {
  url: string;
  tenantSlug?: string;
}

export interface AccountVerificationResponse extends BaseResponse<AccountVerificationResponseData> {}

// Forgot Password Request
export interface ForgotPasswordRequest {
  email: string;
}

// Forgot Password Response Data
export interface ForgotPasswordResponseData {
  message: string;
}

export interface ForgotPasswordResponse extends BaseResponse<ForgotPasswordResponseData> {}

// Reset Password Request
export interface ResetPasswordRequest {
  token: string;
  newPassword: string;
  confirmPassword: string;
}

// Reset Password Response Data
export interface ResetPasswordResponseData {
  accessToken: string | null;
  refreshToken: string | null;
  userId: string;
  tenantId: string;
}

export interface ResetPasswordResponse extends BaseResponse<ResetPasswordResponseData> {}

// Refresh Token Request (empty - uses HTTP-only cookie)
export interface RefreshTokenRequest {}

// Refresh Token Response Data
export interface RefreshTokenResponseData {
  accessToken: string;
  userId: string;
  tenantId: string;
}

export interface RefreshTokenResponse extends BaseResponse<RefreshTokenResponseData> {}

// Logout Request (empty)
export interface LogoutRequest {}

// Paginated Response for search/list operations
export interface PaginatedResponse<T> extends BaseResponse<T[]> {
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

// User info from auth context
export interface UserInfo {
  email: string;
  tenantId: string;
}

export interface Service {
  id: string;
  name: string;
  description: string;
  durationMinutes: number;
  price: number;
  currency: string;
  isActive: boolean;
}

export interface StaffMember {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  isPending: boolean;
  assignedServiceIds: string[];
}

export interface CreateServiceRequest {
  name: string;
  description: string;
  durationMinutes: number;
  price: number;
}

