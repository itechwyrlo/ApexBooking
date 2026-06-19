import React from 'react';
import { Navigate, Outlet } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

export const StaffOnlyRoute: React.FC = () => {
  const { user, tenantSlug } = useAuth();

  if (user?.role !== 'staff') {
    return <Navigate to={`/t/${tenantSlug}/dashboard`} replace />;
  }

  return <Outlet />;
};
