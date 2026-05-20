import React from 'react';
import { useAuth } from '../../../context/AuthContext';
import { useDashboard } from '../hooks/useDashboard';
import { DashboardSkeleton } from '../components/DashboardSkeleton';
import { StatCard } from '../components/StatCard';
import { WeeklyRevenueChart } from '../components/WeeklyRevenueChart';
import { ServiceBreakdownCard } from '../components/ServiceBreakdownCard';
import { TodayScheduleTable } from '../components/TodayScheduleTable';

const TenantDashboardPage: React.FC = () => {
  const { user } = useAuth();
  const { data, isLoading, error } = useDashboard();

  const firstName = user?.fullName?.split(' ')[0] ?? 'there';
  const isStaff = user?.role === 'staff';

  if (isLoading) return <DashboardSkeleton />;

  if (error) return <div className="alert alert-danger">{error}</div>;

  if (!data) return <div className="p-4 text-center text-muted small">No dashboard data available.</div>;

  if (isStaff) {
    return (
      <>
        <h1 className="fw-bold mb-1" style={{ fontSize: 20, color: '#111827' }}>Dashboard</h1>
        <p className="text-muted mb-4" style={{ fontSize: 13 }}>
          Welcome back, {firstName}. Here is your schedule for today.
        </p>

        <div className="row g-3 mb-4">
          <div className="col-6">
            <StatCard
              label="Today's Appointments"
              value={String(data.todayBookingCount)}
              icon="fas fa-calendar-check"
              iconBg="#e8f0fe"
              iconColor="var(--bs-primary)"
              sub={`${data.pendingConfirmationCount} pending confirmation`}
            />
          </div>
          <div className="col-6">
            <StatCard
              label="Pending Confirmation"
              value={String(data.pendingConfirmationCount)}
              icon="fas fa-clock"
              iconBg="#fef9c3"
              iconColor="#854d0e"
              sub="Awaiting your confirmation"
            />
          </div>
        </div>

        <TodayScheduleTable items={data.todaySchedule} />
      </>
    );
  }

  return (
    <>
      <h1 className="fw-bold mb-1" style={{ fontSize: 20, color: '#111827' }}>Dashboard</h1>
      <p className="text-muted mb-4" style={{ fontSize: 13 }}>
        Welcome back, {firstName}. Here is what is happening today.
      </p>

      <div className="row g-3 mb-4">
        <div className="col-6 col-lg-3">
          <StatCard
            label="Today's Bookings"
            value={String(data.todayBookingCount)}
            icon="fas fa-calendar-check"
            iconBg="#e8f0fe"
            iconColor="var(--bs-primary)"
            sub={`${data.pendingConfirmationCount} pending confirmation`}
          />
        </div>
        <div className="col-6 col-lg-3">
          <StatCard
            label="Pending Confirmation"
            value={String(data.pendingConfirmationCount)}
            icon="fas fa-clock"
            iconBg="#fef9c3"
            iconColor="#854d0e"
            sub="Awaiting your confirmation"
          />
        </div>
        <div className="col-6 col-lg-3">
          <StatCard
            label="Revenue Today"
            value={`₱${data.revenueToday.toLocaleString('en-PH', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`}
            icon="fas fa-coins"
            iconBg="#fef9c3"
            iconColor="#854d0e"
            sub="From confirmed bookings"
          />
        </div>
        <div className="col-6 col-lg-3">
          <StatCard
            label="Total Bookings"
            value={String(data.totalBookingCount)}
            icon="fas fa-bookmark"
            iconBg="#e8f0fe"
            iconColor="var(--bs-primary)"
            sub="All time"
          />
        </div>
      </div>

      <div className="row g-3 mb-3">
        <div className="col-12 col-lg-6">
          <WeeklyRevenueChart data={data.weeklyRevenue} />
        </div>
        <div className="col-12 col-lg-6">
          <ServiceBreakdownCard data={data.serviceBreakdown} />
        </div>
      </div>

      <TodayScheduleTable items={data.todaySchedule} />
    </>
  );
};

export default TenantDashboardPage;
