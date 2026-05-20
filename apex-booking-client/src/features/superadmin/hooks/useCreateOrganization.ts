import { useState } from 'react';
import axiosInstance from '../../../services/axiosInstance';
import type { CreateOrganizationRequest, OrganizationSummaryDto } from '../types';

export const useCreateOrganization = () => {
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const createOrganization = async (
    request: CreateOrganizationRequest
  ): Promise<OrganizationSummaryDto | null> => {
    setIsLoading(true);
    setError(null);
    try {
      const result = await axiosInstance.post<OrganizationSummaryDto>(
        '/superadmin/organizations',
        request
      );
      return result ?? null;
    } catch (err: any) {
      setError(err?.message || 'Failed to create organization.');
      return null;
    } finally {
      setIsLoading(false);
    }
  };

  return {
    createOrganization,
    isLoading,
    error,
    clearError: () => setError(null),
  };
};
