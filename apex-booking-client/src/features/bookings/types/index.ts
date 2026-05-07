export type BookingStatus =
  | 'Pending'
  | 'Confirmed'
  | 'Cancelled'
  | 'Completed';

export interface Booking {
  id: string;
  tenantId: string;
  serviceId: string;
  serviceName: string;
  resourceId: string;
  resourceName: string;
  customerName: string;
  customerEmail: string;
  startTime: string;
  endTime: string;
  status: BookingStatus;
  totalPrice: number;
  currencyCode: string;
  notes: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface CreateBookingRequest {
  serviceId: string;
  resourceId: string;
  scheduledDate: string;
  scheduledStartTime: string;
  customerNotes?: string;
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

// UC-3.2.1 Steps 6-7, TR-9.1 Step 11
export interface AvailableSlotsResponse {
  serviceId: string;
  resourceId: string;
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
  resourceId: string;
  name: string;
  description: string | null;
}
// export type BookingStatus =
//   | 'Pending'
//   | 'Confirmed'
//   | 'Cancelled'
//   | 'Completed';

// export interface Booking {
//   id: string;
//   tenantId: string;

//   serviceId: string;
//   serviceName: string;

//   resourceId: string;
//   resourceName: string;

//   customerName: string;
//   customerEmail: string;

//   startTime: string;
//   endTime: string;

//   status: BookingStatus;

//   totalPrice: number;
//   currencyCode: string;

//   notes: string | null;

//   createdAt: string;
//   updatedAt: string;
// }

// export interface CreateBookingRequest {
//   serviceId: string;
//   resourceId: string;
//   scheduledDate: string;
//   scheduledStartTime: string;
//   customerNotes?: string;
// }

// export interface UpdateBookingRequest {
//   serviceId?: string;
//   resourceId?: string;

//   customerName?: string;
//   customerEmail?: string;

//   startTime?: string;
//   endTime?: string;

//   status?: BookingStatus;

//   notes?: string;
// }