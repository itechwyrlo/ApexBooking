import { useState, useCallback } from 'react';
import axiosInstance from '../../../services/axiosInstance';
import { useAuth } from '../../../context/AuthContext';
import type {
  Resource,
  CreateResourceRequest,
  UpdateResourceRequest,
  ResourceType,
} from '../types';

const resourceTypeToInt: Record<ResourceType, number> = {
  Person: 0,
  Room: 1,
  Equipment: 2,
  SlotBased: 3,
};

export const useResources = () => {
  const { tenantSlug } = useAuth();
  const [resources, setResources] = useState<Resource[]>([]);
  const [total, setTotal] = useState(0);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const headers = { 'X-Tenant': tenantSlug };

  const getAll = useCallback(async (pageNumber = 1, pageSize = 10) => {
    setIsLoading(true);
    setError(null);
    try {
      const result = await axiosInstance.get('/resource', {
        headers,
        params: { pageNumber, pageSize },
      });
      setResources(result.data ?? []);
      setTotal(result.total ?? 0);
    } catch {
      setError('Failed to load resources.');
    } finally {
      setIsLoading(false);
    }
  }, [tenantSlug]);

  const create = useCallback(async (request: CreateResourceRequest): Promise<boolean> => {
    setIsLoading(true);
    setError(null);
    try {
      const payload = {
        name: request.name,
        resourceType: resourceTypeToInt[request.resourceType],
        capacity: request.capacity,
        description: request.description,
      };
      const result = await axiosInstance.post('/resource', payload, { headers });
      if (result && !result.isSuccess) {
        setError(result.errors?.[0]?.message ?? 'Failed to create resource.');
        return false;
      }
      return true;
    } catch {
      setError('Failed to create resource.');
      return false;
    } finally {
      setIsLoading(false);
    }
  }, [tenantSlug]);

  const update = useCallback(async (resourceId: string, request: UpdateResourceRequest): Promise<boolean> => {
    setIsLoading(true);
    setError(null);
    try {
      const result = await axiosInstance.patch(`/resource/${resourceId}`, request, { headers });
      if (result && !result.isSuccess) {
        setError(result.errors?.[0]?.message ?? 'Failed to update resource.');
        return false;
      }
      return true;
    } catch {
      setError('Failed to update resource.');
      return false;
    } finally {
      setIsLoading(false);
    }
  }, [tenantSlug]);

  const deactivate = useCallback(async (resourceId: string): Promise<boolean> => {
    setIsLoading(true);
    setError(null);
    try {
      const result = await axiosInstance.patch(`/resource/${resourceId}/status`, {}, { headers });
      if (result && !result.isSuccess) {
        setError(result.errors?.[0]?.message ?? 'Failed to deactivate resource.');
        return false;
      }
      return true;
    } catch {
      setError('Failed to deactivate resource.');
      return false;
    } finally {
      setIsLoading(false);
    }
  }, [tenantSlug]);

  return {
    resources,
    total,
    isLoading,
    error,
    clearError: () => setError(null),
    getAll,
    create,
    update,
    deactivate,
  };
};
// import { useState, useCallback } from 'react';
// import axiosInstance from '../../../services/axiosInstance';
// import { useAuth } from '../../../context/AuthContext';
// import type {
//   Resource,
//   CreateResourceRequest,
//   UpdateResourceRequest,
//   ResourceType,
// } from '../types';

// const resourceTypeToInt: Record<ResourceType, number> = {
//   Person: 0,
//   Room: 1,
//   Equipment: 2,
//   SlotBased: 3,
// };

// export const useResources = () => {
//   const { tenantSlug } = useAuth();
//   const [resources, setResources] = useState<Resource[]>([]);
//   const [isLoading, setIsLoading] = useState(false);
//   const [error, setError] = useState<string | null>(null);

//   const headers = { 'X-Tenant': tenantSlug };


//   const getAll = useCallback(async (pageNumber = 1, pageSize = 10) => {
//     setIsLoading(true);
//     setError(null);
//     try {
//         const result = await axiosInstance.get('/resource', { 
//             headers,
//             params: { pageNumber, pageSize }
//         });
//         setResources(result.data ?? []);
//     } catch {
//         setError('Failed to load resources.');
//     } finally {
//         setIsLoading(false);
//     }
// }, [tenantSlug]);

//   // const getAll = useCallback(async () => {
//   //   setIsLoading(true);
//   //   setError(null);
//   //   try {
//   //     const result = await axiosInstance.get('/resource', { headers });
//   //     if (!result.isSuccess) {
//   //       setError(result.errors?.[0]?.message ?? 'Failed to load resources.');
//   //       return;
//   //     }
//   //     setResources(result.data ?? []);
//   //   } catch {
//   //     setError('Failed to load resources.');
//   //   } finally {
//   //     setIsLoading(false);
//   //   }
//   // }, [tenantSlug]);

//   const create = useCallback(async (request: CreateResourceRequest): Promise<boolean> => {
//     setIsLoading(true);
//     setError(null);
//     try {
//       const payload = {
//         name: request.name,
//         resourceType: resourceTypeToInt[request.resourceType],
//         capacity: request.capacity,
//         description: request.description,
//       };
//       const result = await axiosInstance.post('/resource', payload, { headers });

//       if (result && !result.isSuccess) {
//         setError(result.errors?.[0]?.message ?? 'Failed to create resource.');
//         return false;
//       }
//       await getAll();
//       return true;
//     } catch {
//       setError('Failed to create resource.');
//       return false;
//     } finally {
//       setIsLoading(false);
//     }
//   }, [tenantSlug, getAll]);

//     const update = useCallback(async (resourceId: string, request: UpdateResourceRequest): Promise<boolean> => {
//       setIsLoading(true);
//       setError(null);
//       try {
//         const result = await axiosInstance.patch(`/resource/${resourceId}`, request, { headers });

//         if (result && !result.isSuccess) {
//           setError(result.errors?.[0]?.message ?? 'Failed to update resource.');
//           return false;
//         }
//         await getAll();
//         return true;
//       } catch {
//         setError('Failed to update resource.');
//         return false;
//       } finally {
//         setIsLoading(false);
//       }
//     }, [tenantSlug, getAll]);

//   const deactivate = useCallback(async (resourceId: string): Promise<boolean> => {
//     setIsLoading(true);
//     setError(null);
//     try {
//       const result = await axiosInstance.patch(`/resource/${resourceId}/status`, {}, { headers });

//       if (result && !result.isSuccess) {
//         setError(result.errors?.[0]?.message ?? 'Failed to deactivate resource.');
//         return false;
//       }
//       await getAll();
//       return true;
//     } catch {
//       setError('Failed to deactivate resource.');
//       return false;
//     } finally {
//       setIsLoading(false);
//     }
//   }, [tenantSlug, getAll]);

//   return {
//     resources,
//     isLoading,
//     error,
//     clearError: () => setError(null),
//     getAll,
//     create,
//     update,
//     deactivate,
//   };
// };