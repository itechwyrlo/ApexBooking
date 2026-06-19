export interface DailyRevenueDto {
  date: string;
  dayName: string;
  revenue: number;
}

export interface ServiceBreakdownDto {
  serviceName: string;
  bookingCount: number;
  percentage: number;
}

export interface ScheduleItemDto {
  guestName: string;
  serviceName: string;
  staffName: string;
  scheduledStartTime: string;
  scheduledEndTime: string;
  status: string;
}

export interface DashboardSummaryDto {
  todayBookingCount: number;
  pendingConfirmationCount: number;
  revenueToday: number;
  totalBookingCount: number;
  weeklyRevenue: DailyRevenueDto[];
  serviceBreakdown: ServiceBreakdownDto[];
  todaySchedule: ScheduleItemDto[];
}
