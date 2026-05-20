import React from 'react';
import { Navigate, Outlet } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

export const AdminOnlyRoute: React.FC = () => {
  const { user, tenantSlug } = useAuth();

  if (user?.role !== 'tenantadmin') {
    return <Navigate to={`/t/${tenantSlug}/dashboard`} replace />;
  }

  return <Outlet />;
};
