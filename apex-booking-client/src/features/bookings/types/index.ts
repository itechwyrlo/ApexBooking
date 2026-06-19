export type BookingStatus =
  | 'PendingPayment'
  | 'Pending'
  | 'Confirmed'
  | 'Cancelled'
  | 'Completed'
  | 'NoShow';

export interface GuestDto {
  guestId: string;
  firstName: string;
  lastName: string;
  email: string;
  phone: string | null;
}

export interface Booking {
  bookingId: string;
  bookingReference: string;
  serviceId: string;
  serviceName: string;
  resourceId: string | null;
  resourceName: string | null;
  guest: GuestDto | null;
  scheduledDate: string;
  scheduledStartTime: string;
  scheduledEndTime: string;
  durationMinutes: number;
  status: BookingStatus;
  confirmationMode: string;
  priceSnapshot: number;
  currencyCode: string;
  customerNotes: string | null;
  cancellationReason: string | null;
  cancelledAt: string | null;
  createdAt: string;
}

export interface PublicBookingDto {
  bookingId: string;
  bookingReference: string;
  serviceName: string;
  resourceName: string;
  scheduledDate: string;
  scheduledStartTime: string;
  scheduledEndTime: string;
  status: BookingStatus;
  guestFirstName: string;
}

export interface CreateBookingRequest {
  tenantSlug: string;
  serviceId: string;
  resourceId: string | null;
  scheduledDate: string;
  scheduledStartTime: string;
  guestFirstName: string;
  guestLastName: string;
  guestEmail: string;
  guestPhone?: string;
  customerNotes?: string;
}

export interface BookingResult {
  bookingId: string;
  bookingReference: string;
  status: BookingStatus;
  priceSnapshot: number;
  currencyCode: string;
  serviceName: string;
  resourceName: string;
  scheduledDate: string;
  scheduledStartTime: string;
}

export interface UpdateBookingRequest {
  serviceId?: string;
  resourceId?: string;
  customerName?: string;
  customerEmail?: string;
  startTime?: string;
  endTime?: string;
  status?: BookingStatus;
  notes?: string;
}

export interface ResourceScheduleDto {
  dayOfWeek: string;
  startTime: string;
  endTime: string;
}

export interface PublicService {
  serviceId: string;
  name: string;
  description: string | null;
  durationMinutes: number;
  price: number;
  currencyCode: string;
}

export interface PublicResource {
  staffId: string;
  name: string;
  description: string | null;
  availabilitySchedule: ResourceScheduleDto[];
  servicesOffered: PublicService[];
}

export interface AvailableSlotsResponse {
  serviceId: string;
  staffId: string | null;
  date: string;
  durationMinutes: number;
  availableSlots: string[];
}

export interface PublicTenant {
  businessName: string;
  logoUrl: string | null;
  contactEmail: string | null;
  contactPhone: string | null;
  city: string | null;
  countryCode: string | null;
  websiteUrl: string | null;
  minAdvanceBookingHours: number;
  maxAdvanceBookingDays: number;
  cancellationCutoffHours: number;
  timezone: string;
}

export interface CustomerBooking {
  bookingId: string;
  bookingReference: string;
  serviceName: string;
  resourceName: string;
  scheduledDate: string;
  scheduledStartTime: string;
  status: BookingStatus;
  priceSnapshot: number;
  currencyCode: string;
}

export interface DayAvailabilityDto {
  date: string;
  isAvailable: boolean;
}

export interface MonthlyAvailabilityDto {
  year: number;
  month: number;
  days: DayAvailabilityDto[];
}

export interface InitiatePaymentResult {
  approvalUrl: string;
  gatewayTransactionId: string;
  bookingReference: string;
}

export interface CancellationTokenValidation {
  bookingId: string;
  bookingReference: string;
  serviceName: string;
  resourceName: string;
  scheduledDate: string;
  scheduledStartTime: string;
  status: BookingStatus;
  guestFirstName: string;
  guestLastName: string;
  priceSnapshot: number;
  currencyCode: string;
  refundPercent: number;
  depositOnly: boolean;
  depositType: 'Percentage' | 'FixedAmount';
  depositValue: number;
}
