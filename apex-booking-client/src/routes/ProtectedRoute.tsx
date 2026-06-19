import React from "react";
import { Navigate, Outlet } from "react-router-dom";
import { useAuth } from "../context/AuthContext";

export const ProtectedRoute: React.FC = () => {
  const {
    isAuthenticated,
    accessToken,
    emailVerified,
    // user,
    tenantSlug,
    isInitializing,
  } = useAuth();

  if (isInitializing) return null;

  if (!isAuthenticated || !accessToken) {
    return <Navigate to="/login" replace />;
  }

  if (!emailVerified) {
    return <Navigate to="/verify-email-notice" replace />;
  }

  if (!tenantSlug) {
    return <Navigate to="/login" replace />;
  }

  return <Outlet />;
};
