// ─── Enums ────────────────────────────────────────────────────────────────────

/**
 * Maps to ResourceType enum in backend.
 * UC-3.1.1: resource type (person, room, equipment, or slot-based)
 */
export type ResourceType = 'Person' | 'Room' | 'Equipment' | 'SlotBased';

/**
 * Maps to ExceptionType enum in backend.
 * UC-3.1.4: exception_type drives the branching logic in the slot calculator.
 */
export type ExceptionType =
  | 'UnavailableAllDay'
  | 'UnavailableHours'
  | 'AvailableExtraHours';

// ─── Resource ─────────────────────────────────────────────────────────────────

/**
 * Resource as returned from the API.
 * UC-3.1.1, TR-7.1
 */
export interface Resource {
  id: string;
  tenantId: string;
  name: string;
  description: string | null;
  resourceType: ResourceType;
  capacity: number;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

/**
 * Request body for creating a resource.
 * TR-7.1: Required fields: name, resource_type, capacity.
 * Optional: description. location_id ignored in MVP.
 */
export interface CreateResourceRequest {
  name: string;
  resourceType: ResourceType;
  capacity: number;
  description?: string;
}

/**
 * Request body for updating a resource.
 * TR-7.2: All fields optional. Partial update.
 */
export interface UpdateResourceRequest {
  name?: string;
  resourceType?: ResourceType;
  capacity?: number;
  description?: string;
}

// ─── Availability Schedule ────────────────────────────────────────────────────

/**
 * A single break period within a working day.
 * Maps to BreakDto in backend.
 * UC-3.1.3: break periods subtract time from the working window.
 */
export interface BreakPeriod {
  breakStartTime: string; // HH:mm
  breakEndTime: string;   // HH:mm
  label?: string;
}

/**
 * One day's schedule entry.
 * Maps to DayScheduleDto in backend.
 * UC-3.1.3: day_of_week, is_available, start_time, end_time, breaks.
 */
export interface DaySchedule {
  dayOfWeek: number; // 0 = Sunday, 6 = Saturday
  isAvailable: boolean;
  startTime: string | null; // HH:mm, null when isAvailable is false
  endTime: string | null;   // HH:mm, null when isAvailable is false
  breaks: BreakPeriod[];
}

/**
 * Request body for PUT /resources/:id/availability.
 * TR-7.4: full replacement — all 7 days must be included.
 * Maps to SetResourceAvailabilityRequestDto in backend.
 */
export interface SetAvailabilityRequest {
  schedules: DaySchedule[];
}

// ─── Availability Exception ───────────────────────────────────────────────────

/**
 * An availability exception as returned from the API.
 * UC-3.1.4, TR-7.5
 */
export interface AvailabilityException {
  id: string;
  resourceId: string;
  exceptionDate: string;    // YYYY-MM-DD
  exceptionType: ExceptionType;
  startTime: string | null; // HH:mm, null when UnavailableAllDay
  endTime: string | null;   // HH:mm, null when UnavailableAllDay
  note: string | null;
  createdAt: string;
  updatedAt: string;
}

/**
 * Request body for POST /resources/:id/exceptions.
 * TR-7.5: exception_date must be a future date.
 */
export interface CreateExceptionRequest {
  exceptionDate: string;    // YYYY-MM-DD
  exceptionType: ExceptionType;
  startTime?: string;       // HH:mm, required if not UnavailableAllDay
  endTime?: string;         // HH:mm, required if not UnavailableAllDay
  note?: string;
}

// ─── Slot Availability ────────────────────────────────────────────────────────

/**
 * Response from GET /services/:serviceId/slots
 * Maps to AvailableSlotsDto in backend.
 * TR-9.1 Step 11: available slot start times in HH:mm format.
 */
export interface AvailableSlotsResponse {
  serviceId: string;
  resourceId: string;
  date: string;           // YYYY-MM-DD
  durationMinutes: number;
  availableSlots: string[]; // HH:mm strings
}