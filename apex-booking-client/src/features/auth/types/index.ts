// Auth request and response types matching backend endpoints

export interface AuthResponseData {
  accessToken: string;
  refreshToken: string;
  userId: string;
  tenantId: string;
  tenantSlug: string | null
}

// Login Request
export interface LoginRequest {
  email: string;
  password: string;
}

// Account Verification Request
export interface AccountVerificationRequest {
  token: string;
}

// Direct DTO mirror of backend AccountVerificationResponseDto
export interface AccountVerificationResponseDto {
  url: string;
  tenantSlug?: string | null;
}

// Forgot Password Request
export interface ForgotPasswordRequest {
  email: string;
}

// Forgot Password Response Data
export interface ForgotPasswordResponseData {
  message: string;
}

// Reset Password Request
export interface ResetPasswordRequest {
  token: string;
  newPassword: string;
  confirmPassword: string;
}

// Refresh Token Request (empty - uses HTTP-only cookie)
export interface RefreshTokenRequest {}

// Refresh Token Response Data
export interface RefreshTokenResponseData {
  accessToken: string;
  userId: string;
  tenantId: string;
}

// Logout Request (empty)
export interface LogoutRequest {}

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
