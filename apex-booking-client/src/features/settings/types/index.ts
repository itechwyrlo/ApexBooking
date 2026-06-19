export type GatewayProvider = 'PayPal' | 'Stripe' | 'PayMongo';
export type GatewayMode = 'Test' | 'Live';
export type CancellationPolicy = 'NoRefund' | 'PartialRefund' | 'FullRefund';
export type BookingConfirmationMode = 'Automatic' | 'Manual';
export type DepositType = 'Percentage' | 'FixedAmount';

export interface TenantPaymentGatewayStatusDto {
  gatewayProvider: GatewayProvider | null;
  mode: GatewayMode | null;
  isActive: boolean;
  validatedAt: string | null;
}

export interface TenantSettingsDto {
  bookingConfirmationMode: BookingConfirmationMode;
  minAdvanceBookingHours: number;
  maxAdvanceBookingDays: number;
  cancellationCutoffHours: number;
  lateCancellationPolicy: CancellationPolicy;
  guestBookingEnabled: boolean;
  notifyBookingConfirmed: boolean;
  notifyBookingCancelled: boolean;
  notifyBookingReminder: boolean;
  notifyNewCustomer: boolean;
  reminderHoursBefore: number;
}

export interface UpdateTenantSettingsRequest {
  bookingConfirmationMode?: BookingConfirmationMode;
  minAdvanceBookingHours?: number;
  maxAdvanceBookingDays?: number;
  cancellationCutoffHours?: number;
  lateCancellationPolicy?: CancellationPolicy;
  guestBookingEnabled?: boolean;
  notifyBookingConfirmed?: boolean;
  notifyBookingCancelled?: boolean;
  notifyBookingReminder?: boolean;
  notifyNewCustomer?: boolean;
  reminderHoursBefore?: number;
}

export interface TenantPaymentPolicyDto {
  paymentRequired: boolean;
  depositOnly: boolean;
  depositType: DepositType;
  depositValue: number;
  refundPercent: number;
}

export interface UpdateTenantPaymentPolicyRequest {
  paymentRequired?: boolean;
  depositOnly?: boolean;
  depositType?: DepositType;
  depositValue?: number;
  refundPercent?: number;
}

export type TimeFormat = '12h' | '24h';

export interface TenantProfileDto {
  logoUrl: string | null;
  addressLine1: string | null;
  addressLine2: string | null;
  city: string | null;
  state: string | null;
  postalCode: string | null;
  countryCode: string | null;
  timezone: string;
  currencyCode: string;
  websiteUrl: string | null;
  contactEmail: string | null;
  contactPhone: string | null;
  dateFormat: string;
  timeFormat: TimeFormat;
  languageCode: string;
}

export interface UpdateTenantProfileRequest {
  logoUrl?: string;
  addressLine1?: string;
  addressLine2?: string;
  city?: string;
  state?: string;
  postalCode?: string;
  countryCode?: string;
  timezone?: string;
  currencyCode?: string;
  websiteUrl?: string;
  contactEmail?: string;
  contactPhone?: string;
  dateFormat?: string;
  timeFormat?: TimeFormat;
  languageCode?: string;
}
