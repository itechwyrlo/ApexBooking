export interface Service {
    id: string;
    name: string;
    description?: string;
    durationMinutes: number;
    bufferBeforeMinutes: number;
    bufferAfterMinutes: number;
    price: number;
    currencyCode: string;
    isActive: boolean;
    staffIds: string[];
    createdAt: string;
    updatedAt: string;
  }
  
  export interface CreateServiceRequest {
    name: string;
    description?: string;
    durationMinutes: number;
    price: number;
    currencyCode: string;
    staffIds: string[];
    bufferBeforeMinutes: number;
    bufferAfterMinutes: number;
  }

  export interface UpdateServiceRequest {
    name: string;
    description?: string;
    durationMinutes: number;
    price: number;
    currencyCode: string;
    staffIds: string[];
    bufferBeforeMinutes: number;
    bufferAfterMinutes: number;
  }