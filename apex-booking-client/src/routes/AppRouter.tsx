import React from "react";
import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom";
import { AuthProvider } from "../context/AuthContext";
import { ProtectedRoute } from "./ProtectedRoute";

import LoginPage from "../features/auth/pages/LoginPage";
import RegisterPage from "../features/auth/pages/RegisterPage";
import EmailVerificationPage from "../features/auth/pages/EmailVerificationPage";
import ForgotPasswordPage from "../features/auth/pages/ForgotPasswordPage";
import ResetPasswordPage from "../features/auth/pages/ResetPasswordPage";
import VerifyEmailNoticePage from "../features/auth/pages/VerifyEmailNoticePage";
import { MainLayout } from "../components/layout/MainLayout";
import TenantDashboardPage from "../features/dashboard/TenantDashboardPage";
import ResourcesPage from "../features/resources/pages/ResourcesPage";
import ResourceAvailabilityPage from "../features/resources/pages/ResourceAvailabilityPage";
import ServicesPage from "../features/service/pages/ServicesPage";
import BookingsPage from "../features/bookings/pages/BookingsPage";
import PublicTenantPage from "../features/bookings/pages/PublicTenantPage";
import CustomerBookingWizardPage from "../features/bookings/pages/CustomerBookingWizardPage";

export const AppRouter: React.FC = () => {
  return (
    <BrowserRouter>
      <AuthProvider>
        <Routes>
          {/* Public auth routes */}
          <Route path="/login" element={<LoginPage />} />
          <Route path="/register" element={<RegisterPage />} />
          <Route path="/forgot-password" element={<ForgotPasswordPage />} />
          <Route path="/reset-password" element={<ResetPasswordPage />} />
          <Route path="/verify-account" element={<EmailVerificationPage />} />
          <Route path="/verify-email-notice" element={<VerifyEmailNoticePage />} />

          {/* Public customer portal — TR-1.4, TR-9.3, UC-3.2.1 */}
          <Route path="/book/:tenant" element={<PublicTenantPage />} />
          <Route path="/book/:tenant/new" element={<CustomerBookingWizardPage />} />

          {/* Protected tenant admin routes */}
          <Route element={<ProtectedRoute />}>
            <Route element={<MainLayout />}>
              <Route path="/t/:tenant/dashboard" element={<TenantDashboardPage />} />
              <Route path="/t/:tenant/resources" element={<ResourcesPage />} />
              <Route path="/t/:tenant/resources/:id/availability" element={<ResourceAvailabilityPage />} />
              <Route path="/t/:tenant/services" element={<ServicesPage />} />
              <Route path="/t/:tenant/bookings" element={<BookingsPage />} />
            </Route>
          </Route>

          <Route path="/" element={<Navigate to="/login" replace />} />
        </Routes>
      </AuthProvider>
    </BrowserRouter>
  );
};
// import React from "react";
// import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom";
// import { AuthProvider } from "../context/AuthContext";
// import { ProtectedRoute } from "./ProtectedRoute";

// import LoginPage from "../features/auth/pages/LoginPage";
// import RegisterPage from "../features/auth/pages/RegisterPage";
// import EmailVerificationPage from "../features/auth/pages/EmailVerificationPage";
// import ForgotPasswordPage from "../features/auth/pages/ForgotPasswordPage";
// import ResetPasswordPage from "../features/auth/pages/ResetPasswordPage";
// import VerifyEmailNoticePage from "../features/auth/pages/VerifyEmailNoticePage";
// import { MainLayout } from "../components/layout/MainLayout";
// import TenantDashboardPage from "../features/dashboard/TenantDashboardPage";
// import ResourcesPage from "../features/resources/pages/ResourcesPage";
// import ResourceAvailabilityPage from "../features/resources/pages/ResourceAvailabilityPage";
// import ServicesPage from "../features/service/pages/ServicesPage";
// import BookingsPage from "../features/bookings/pages/BookingsPage";


// export const AppRouter: React.FC = () => {
//   return (
//     <BrowserRouter>
//       <AuthProvider>
//         <Routes>
//           <Route path="/login" element={<LoginPage />} />
//           <Route path="/register" element={<RegisterPage />} />
//           <Route path="/forgot-password" element={<ForgotPasswordPage />} />
//           <Route path="/reset-password" element={<ResetPasswordPage />} />
//           <Route path="/verify-account" element={<EmailVerificationPage />} />

//           <Route
//             path="/verify-email-notice"
//             element={<VerifyEmailNoticePage />}
//           />

//           <Route element={<ProtectedRoute />}>
//             <Route element={<MainLayout />}>
//               <Route
//                 path="/t/:tenant/dashboard"
//                 element={<TenantDashboardPage />}
//               />

//               <Route path="/t/:tenant/resources" element={<ResourcesPage />} />
//               <Route path="/t/:tenant/bookings" element={<BookingsPage />} />
//               {/* <Route
//                 path="/t/:tenant/resources/:id"
//                 element={<ResourceDetailPage />}
//               /> */}
//               <Route
//                 path="/t/:tenant/resources/:id/availability"
//                 element={<ResourceAvailabilityPage />}
//               />

//               <Route path="/t/:tenant/services" element={<ServicesPage />} />
//             </Route>
//           </Route>

//           <Route path="/" element={<Navigate to="/login" replace />} />
//         </Routes>
//       </AuthProvider>
//     </BrowserRouter>
//   );
// };