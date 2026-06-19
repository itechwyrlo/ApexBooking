import React from "react";
import { Navigate, Outlet } from "react-router-dom";
import { useAuth } from "../context/AuthContext";

export const SuperAdminProtectedRoute: React.FC = () => {
  const { isAuthenticated, accessToken, user, isInitializing } = useAuth();

  if (isInitializing) return null;

  if (!isAuthenticated || !accessToken) {
    return <Navigate to="/superadmin/login" replace />;
  }

  if (user?.role !== "superadmin") {
    return <Navigate to="/superadmin/login" replace />;
  }

  return <Outlet />;
};
