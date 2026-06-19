export interface AvailableSlotsDto {
    serviceId: string;
    resourceId: string;
    date: string;
    durationMinutes: number;
    availableSlots: string[];
  }
  
  export interface GetAvailableSlotsRequest {
    serviceId: string;
    resourceId: string;
    date: string;
  }

  export interface AvailableSlotsResponse {
    serviceId: string;
    resourceId: string;
    date: string;
    durationMinutes: number;
    availableSlots: string[];
  }