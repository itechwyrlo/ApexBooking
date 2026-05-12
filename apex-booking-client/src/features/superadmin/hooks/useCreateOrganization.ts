import { useState } from 'react';
import axiosInstance from '../../../services/axiosInstance';
import type { CreateOrganizationRequest, OrganizationSummaryDto } from '../types';
import type { BaseResponse } from '../../../types';

export const useCreateOrganization = () => {
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const createOrganization = async (
    request: CreateOrganizationRequest
  ): Promise<OrganizationSummaryDto | null> => {
    setIsLoading(true);
    setError(null);
    try {
      const result = await axiosInstance.post<BaseResponse<OrganizationSummaryDto>>(
        '/superadmin/organizations',
        request
      );
      if (!result.isSuccess) {
        setError(result?.errors?.[0]?.message ?? 'Failed to create organization.');
        return null;
      }
      return result.data ?? null;
    } catch {
      setError('Failed to create organization.');
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
