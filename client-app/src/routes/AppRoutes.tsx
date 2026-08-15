import { Navigate, Route, Routes } from 'react-router-dom'
import { LandingPage } from '../pages/LandingPage'
import { PwaInit } from '../pages/PwaInit'
import { FindWorkspacePage } from '../pages/FindWorkspacePage'
import { ResetPasswordPage } from '../pages/ResetPasswordPage'
import { RequestAccessPage } from '../pages/RequestAccessPage'
import { RequestAccessPendingPage } from '../pages/RequestAccessPendingPage'
import { DashboardLayout } from '../layouts/DashboardLayout'
import { SettingsLayout } from '../layouts/SettingsLayout'
import { SuperAdminLayout } from '../layouts/SuperAdminLayout'
import { DashboardPage } from '../pages/booking/DashboardPage'
import { AppointmentsPage } from '../pages/booking/AppointmentsPage'
import { CalendarPage } from '../pages/booking/CalendarPage'
import { TimeOffsPage } from '../pages/booking/TimeOffsPage'
import { StaffPage } from '../pages/booking/StaffPage'
import { RefundRequestsPage } from '../pages/booking/RefundRequestsPage'
import { ServicesPage } from '../pages/booking/ServicesPage'
import { ClientsPage } from '../pages/booking/ClientsPage'
import { BusinessProfilePage } from '../pages/booking/BusinessProfilePage'
import { BranchesPage } from '../pages/booking/BranchesPage'
import { BookingSettingsPage } from '../pages/booking/settings/BookingSettingsPage'
import { PaymentSettingsPage } from '../pages/booking/settings/PaymentSettingsPage'
import { AppearanceSettingsPage } from '../pages/booking/settings/AppearanceSettingsPage'
import { PublicBookingPage } from '../pages/public/PublicBookingPage'
import { CancelBookingPage } from '../pages/public/CancelBookingPage'
import { RefundStatusPage } from '../pages/public/RefundStatusPage'
import { SuperAdminLoginPage } from '../pages/admin/SuperAdminLoginPage'
import { SuperAdminDashboardPage } from '../pages/admin/SuperAdminDashboardPage'
import { TenantRequestManagementPage } from '../pages/admin/TenantRequestManagementPage'
import { AdminBookingsPage } from '../pages/admin/AdminBookingsPage'
import { FailedNotificationsPage } from '../pages/admin/FailedNotificationsPage'
import { ProtectedRoute } from './ProtectedRoute'
import { SuperAdminProtectedRoute } from './SuperAdminProtectedRoute'
import { Role } from '../types/Role'

export function AppRoutes() {
  return (
    <Routes>
      <Route path="/" element={<LandingPage />} />
      <Route path="/pwa-init" element={<PwaInit />} />
      <Route path="/find-workspace" element={<FindWorkspacePage />} />
      <Route path="/:slug/login" element={<FindWorkspacePage />} />
      <Route path="/:slug/reset-password" element={<ResetPasswordPage />} />
      <Route path="/request-access" element={<RequestAccessPage />} />
      <Route path="/request-access/pending" element={<RequestAccessPendingPage />} />

      <Route path="/:slug/book" element={<PublicBookingPage />} />
      <Route path="/:slug/cancel-booking" element={<CancelBookingPage />} />
      <Route path="/:slug/refund-status" element={<RefundStatusPage />} />

      <Route path="/superadmin/login" element={<SuperAdminLoginPage />} />
      <Route
        path="/admin"
        element={
          <SuperAdminProtectedRoute>
            <SuperAdminLayout />
          </SuperAdminProtectedRoute>
        }
      >
        <Route index element={<SuperAdminDashboardPage />} />
        <Route path="tenant-requests" element={<TenantRequestManagementPage />} />
        <Route path="bookings" element={<AdminBookingsPage />} />
        <Route path="failed-notifications" element={<FailedNotificationsPage />} />
      </Route>

      <Route
        path="/:slug/dashboard"
        element={
          <ProtectedRoute>
            <DashboardLayout />
          </ProtectedRoute>
        }
      >
        <Route index element={<DashboardPage />} />
        <Route path="appointments" element={<AppointmentsPage />} />
        <Route path="calendar" element={<CalendarPage />} />
        <Route
          path="clients"
          element={
            <ProtectedRoute allowedRoles={[Role.Owner, Role.Admin]}>
              <ClientsPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="staff"
          element={
            <ProtectedRoute allowedRoles={[Role.Owner, Role.Admin]}>
              <StaffPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="refunds"
          element={
            <ProtectedRoute allowedRoles={[Role.Owner, Role.Admin]}>
              <RefundRequestsPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="services"
          element={
            <ProtectedRoute allowedRoles={[Role.Owner, Role.Admin]}>
              <ServicesPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="business-profile"
          element={
            <ProtectedRoute allowedRoles={[Role.Owner]}>
              <BusinessProfilePage />
            </ProtectedRoute>
          }
        />
        <Route
          path="branches"
          element={
            <ProtectedRoute allowedRoles={[Role.Owner]}>
              <BranchesPage />
            </ProtectedRoute>
          }
        />
        <Route path="time-offs" element={<TimeOffsPage />} />
        <Route
          path="settings"
          element={
            <ProtectedRoute allowedRoles={[Role.Owner]}>
              <SettingsLayout />
            </ProtectedRoute>
          }
        >
          <Route index element={<Navigate to="booking" replace />} />
          <Route path="booking" element={<BookingSettingsPage />} />
          <Route path="payment" element={<PaymentSettingsPage />} />
          <Route path="appearance" element={<AppearanceSettingsPage />} />
        </Route>
      </Route>
    </Routes>
  )
}
