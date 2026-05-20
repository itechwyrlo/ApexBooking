import React from "react";
import { BrowserRouter, Routes, Route } from "react-router-dom";
import { AuthProvider } from "../context/AuthContext";
import { ProtectedRoute } from "./ProtectedRoute";
import { AdminManagerRoute } from "./AdminManagerRoute";
import { AdminOnlyRoute } from "./AdminOnlyRoute";
import { StaffOnlyRoute } from "./StaffOnlyRoute";
// import { CustomerProtectedRoute } from "./CustomerProtectedRoute";
import { SuperAdminProtectedRoute } from "./SuperAdminProtectedRoute";

import LoginPage from "../features/auth/pages/LoginPage";
import EmailVerificationPage from "../features/auth/pages/EmailVerificationPage";
import ForgotPasswordPage from "../features/auth/pages/ForgotPasswordPage";
import ResetPasswordPage from "../features/auth/pages/ResetPasswordPage";
import VerifyEmailNoticePage from "../features/auth/pages/VerifyEmailNoticePage";
// import CustomerProfilePage from "../features/auth/pages/CustomerProfilePage";
import { MainLayout } from "../components/layout/MainLayout";
// import { CustomerLayout } from "../components/layout/CustomerLayout";
import { SuperAdminLayout } from "../components/layout/SuperAdminLayout";
import TenantDashboardPage from "../features/dashboard/pages/TenantDashboardPage";
import StaffPage from "../features/staff/pages/StaffPage";
import StaffAvailabilityPage from "../features/staff/pages/StaffAvailabilityPage";
import MyAvailabilityPage from "../features/staff/pages/MyAvailabilityPage";
import MyProfilePage from "../features/staff/pages/MyProfilePage";
import ServicesPage from "../features/service/pages/ServicesPage";
import BookingsPage from "../features/bookings/pages/BookingsPage";
import CalendarPage from "../features/bookings/pages/CalendarPage";
import ClientsPage from "../features/clients/pages/ClientsPage";
// import CustomerBookingsPage from "../features/bookings/pages/CustomerBookingsPage";
// import PublicTenantPage from "../features/bookings/pages/PublicTenantPage";
import CustomerBookingWizardPage from "../features/bookings/pages/CustomerBookingWizardPage";
import CustomerRegisterPage from "../features/bookings/pages/CustomerRegisterPage";
import SettingsPage from "../features/settings/pages/SettingsPage";
import PaymentSuccessPage from "../features/bookings/pages/PaymentSuccessPage";
import CancelBookingPage from "../features/bookings/pages/CancelBookingPage";
import LandingPage from "../features/public/pages/landingpage";
import PricingPage from "../features/public/pages/PricingPage";
import RequestAccessPage from "../features/public/pages/RequestAccessPage";
import SuperAdminLoginPage from "../features/superadmin/pages/SuperAdminLoginPage";
import SuperAdminPaymentGatewayPage from "../features/superadmin/pages/SuperAdminPaymentGatewayPage";
import SuperAdminOverviewPage from "../features/superadmin/pages/SuperAdminOverviewPage";
import NewOrganizationPage from "../features/superadmin/pages/NewOrganizationPage";
import OrganizationDetailPage from "../features/superadmin/pages/OrganizationDetailPage";
import SetupAccountPage from "../features/superadmin/pages/SetupAccountPage";
import TenantRequestsPage from "../features/superadmin/pages/TenantRequestsPage";

export const AppRouter: React.FC = () => {
  return (
    <BrowserRouter>
      <AuthProvider>
        <Routes>
          {/* Public auth routes */}
          <Route path="/login" element={<LoginPage />} />
          <Route path="/forgot-password" element={<ForgotPasswordPage />} />
          <Route path="/reset-password" element={<ResetPasswordPage />} />
          <Route path="/verify-account" element={<EmailVerificationPage />} />
          <Route path="/verify-email-notice" element={<VerifyEmailNoticePage />} />
          <Route path="/setup-account" element={<SetupAccountPage />} />

          {/* Public marketing routes */}
          <Route path="/pricing" element={<PricingPage />} />
          <Route path="/request-access" element={<RequestAccessPage />} />

          {/* Super admin — public login */}
          <Route path="/superadmin/login" element={<SuperAdminLoginPage />} />

          {/* Public customer portal */}
          {/* <Route path="/book/:tenant" element={<PublicTenantPage />} /> */}
          <Route path="/book/:tenant/new" element={<CustomerBookingWizardPage />} />
          <Route path="/book/:tenant/customer/login" element={<LoginPage />} />
          <Route path="/book/:tenant/customer/register" element={<CustomerRegisterPage />} />
          <Route path="/book/:tenant/payment/success" element={<PaymentSuccessPage />} />
          <Route path="/book/:tenant/cancel-booking" element={<CancelBookingPage />} />

          {/* Protected customer routes */}
          {/* <Route element={<CustomerProtectedRoute />}>
            <Route element={<CustomerLayout />}>
              <Route path="/book/:tenant/customer/bookings" element={<CustomerBookingsPage />} />
              <Route path="/book/:tenant/customer/profile" element={<CustomerProfilePage />} />
            </Route>
          </Route> */}

          {/* Protected tenant routes */}
          <Route element={<ProtectedRoute />}>
            <Route element={<MainLayout />}>
              {/* All roles */}
              <Route path="/t/:tenant/dashboard" element={<TenantDashboardPage />} />
              <Route path="/t/:tenant/bookings" element={<BookingsPage />} />
              <Route path="/t/:tenant/calendar" element={<CalendarPage />} />
              <Route path="/t/:tenant/clients" element={<ClientsPage />} />

              {/* Admin and manager only */}
              <Route element={<AdminManagerRoute />}>
                <Route path="/t/:tenant/staff" element={<StaffPage />} />
                <Route path="/t/:tenant/staff/:id/availability" element={<StaffAvailabilityPage />} />
                <Route path="/t/:tenant/services" element={<ServicesPage />} />
              </Route>

              {/* Admin only */}
              <Route element={<AdminOnlyRoute />}>
                <Route path="/t/:tenant/settings" element={<SettingsPage />} />
              </Route>

              {/* Staff only */}
              <Route element={<StaffOnlyRoute />}>
                <Route path="/t/:tenant/my-availability" element={<MyAvailabilityPage />} />
                <Route path="/t/:tenant/my-profile" element={<MyProfilePage />} />
              </Route>
            </Route>
          </Route>

          {/* Protected super admin routes */}
          <Route element={<SuperAdminProtectedRoute />}>
            <Route element={<SuperAdminLayout />}>
              <Route path="/superadmin" element={<SuperAdminOverviewPage />} />
              <Route path="/superadmin/organizations" element={<SuperAdminOverviewPage />} />
              <Route path="/superadmin/organizations/new" element={<NewOrganizationPage />} />
              <Route path="/superadmin/organizations/:slug" element={<OrganizationDetailPage />} />
              <Route path="/superadmin/requests" element={<TenantRequestsPage />} />
              <Route path="/superadmin/payment-gateway" element={<SuperAdminPaymentGatewayPage />} />
            </Route>
          </Route>

          <Route path="/" element={<LandingPage />} />
        </Routes>
      </AuthProvider>
    </BrowserRouter>
  );
};
