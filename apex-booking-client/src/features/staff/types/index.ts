export type ExceptionType =
  | 'UnavailableAllDay'
  | 'UnavailableHours'
  | 'AvailableExtraHours';

export interface StaffDto {
  id: string;
  firstName: string;
  lastName: string;
  email: string | null;
  contactNumber: string | null;
  description: string | null;
  capacity: number;
  isActive: boolean;
  photoUrl: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface CreateStaffRequest {
  firstName: string;
  lastName: string;
  email: string | null;
  contactNumber: string | null;
  capacity: number;
  description?: string;
}

export interface UpdateStaffRequest {
  firstName: string;
  lastName: string;
  email: string | null;
  contactNumber: string | null;
  capacity: number;
  description?: string;
}

export interface BreakPeriod {
  breakStartTime: string;
  breakEndTime: string;
  label?: string;
}

export interface DaySchedule {
  dayOfWeek: number;
  isAvailable: boolean;
  startTime: string | null;
  endTime: string | null;
  breaks: BreakPeriod[];
}

export interface StaffAvailabilityDto {
  staffId: string;
  schedules: DaySchedule[];
}

export interface SetAvailabilityRequest {
  schedules: DaySchedule[];
}

export interface AvailabilityException {
  id: string;
  exceptionDate: string;
  exceptionType: ExceptionType;
  startTime: string | null;
  endTime: string | null;
  note: string | null;
}

export interface CreateExceptionRequest {
  exceptionDate: string;
  exceptionType: ExceptionType;
  startTime?: string;
  endTime?: string;
  note?: string;
}
