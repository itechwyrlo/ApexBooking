export type GatewayProvider = 'PayPal' | 'Stripe' | 'PayMongo';
export type GatewayMode = 'Test' | 'Live';
export type UserRole = 'TenantAdmin' | 'Manager' | 'Staff' | 'Customer';
export type UserStatus = 'Invited' | 'Active' | 'Inactive';
export type OrgStatus = 'Pending' | 'Active' | 'Trial' | 'Suspended' | 'Deactivated';
export type TenantPlan = 'Basic' | 'Professional';
export type TenantRequestStatus = 'Pending' | 'Approved' | 'Rejected';

export interface ConfigurePlatformPaymentGatewayRequest {
  gatewayProvider: GatewayProvider;
  clientId: string;
  secretKey: string;
  webhookId: string;
  mode: GatewayMode;
}

export interface PlatformPaymentGatewayDto {
  id: string;
  gatewayProvider: GatewayProvider;
  mode: GatewayMode;
  isActive: boolean;
  validatedAt: string | null;
}

export interface TenantUserDto {
  id: string;
  fullName: string;
  email: string;
  role: string;
  status: string;
}

export interface OrganizationSummaryDto {
  id: string;
  slug: string;
  businessName: string;
  ownerEmail: string;
  status: OrgStatus;
  userCount: number;
  createdAt: string;
}

export interface OrganizationDetailDto {
  id: string;
  slug: string;
  businessName: string;
  ownerFullName: string;
  ownerEmail: string;
  ownerPhone: string;
  status: OrgStatus;
  bookingCount: number;
  serviceCount: number;
  staffCount: number;
  clientCount: number;
  userCount: number;
  createdAt: string;
  users: TenantUserDto[];
}

export interface PlatformOverviewDto {
  totalOrgs: number;
  activeOrgs: number;
  inactiveOrgs: number;
  organizations: OrganizationSummaryDto[];
}

export interface CreateOrganizationRequest {
  slug: string;
  businessName: string;
  ownerFullName: string;
  ownerEmail: string;
  ownerPhone: string;
  adminPassword: string;
}

export interface CreateTenantUserRequest {
  fullName: string;
  email: string;
  role: string;
}

export interface AssignExistingUserRequest {
  email: string;
  role: string;
}

export interface TenantRequestDto {
  id: string;
  businessName: string;
  ownerFullName: string;
  ownerEmail: string;
  plan: TenantPlan;
  status: TenantRequestStatus;
  createdAt: string;
}

export interface TenantRequestDetailDto extends TenantRequestDto {
  ownerPhone: string;
  message: string | null;
  rejectionReason: string | null;
  reviewedAt: string | null;
}

export interface ApproveTenantRequestRequest {
  slug: string;
  trialDays: number;
}

export interface RejectTenantRequestRequest {
  reason: string;
}
