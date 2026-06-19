// import React from "react";
// import { Navigate, Outlet, useParams } from "react-router-dom";
// import { useAuth } from "../context/AuthContext";

// export const CustomerProtectedRoute: React.FC = () => {
//   const {
//     isAuthenticated,
//     accessToken,
//     emailVerified,
//     user,
//     tenantSlug,
//     isInitializing,
//   } = useAuth();


//   const { tenant } = useParams<{ tenant: string }>();

//   if (isInitializing) return null;

//   if (!isAuthenticated || !accessToken) {
//     return <Navigate to={`/book/${tenant}/customer/login`} replace />;
//   }

//   if (!emailVerified) {
//     return <Navigate to="/verify-email-notice" replace />;
//   }

//   if (!tenantSlug) {
//     return <Navigate to={`/book/${tenant}/customer/login`} replace />;
//   }

//   if (user?.role !== "customer") {
//     return <Navigate to="/login" replace />;
//   }

//   return <Outlet />;
// };
