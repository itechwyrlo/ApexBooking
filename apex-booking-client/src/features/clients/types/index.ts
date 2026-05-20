import type { BookingStatus } from '../../bookings/types';
export type { BookingStatus };

export interface ClientSummaryDto {
  email: string;
  fullName: string;
  phone: string | null;
  totalBookings: number;
  lastVisit: string | null;
  totalSpent: number;
  currencyCode: string;
}

export interface ClientBookingDto {
  bookingId: string;
  bookingReference: string;
  serviceName: string;
  resourceName: string;
  scheduledDate: string;
  scheduledStartTime: string;
  scheduledEndTime: string;
  status: BookingStatus;
  priceSnapshot: number;
  currencyCode: string;
}

export interface ClientDetailDto {
  email: string;
  fullName: string;
  phone: string | null;
  totalBookings: number;
  lastVisit: string | null;
  totalSpent: number;
  currencyCode: string;
  bookings: ClientBookingDto[];
}
