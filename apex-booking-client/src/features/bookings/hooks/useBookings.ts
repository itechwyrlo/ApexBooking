import { useState, useCallback } from 'react';
import axiosInstance from '../../../services/axiosInstance';
import { useAuth } from '../../../context/AuthContext';
import type {
  Booking,
  CustomerBooking,
  // CreateBookingRequest,
  UpdateBookingRequest,
} from '../types';

export interface AdminBookingForm {
  serviceId: string;
  resourceId: string;
  scheduledDate: string;
  scheduledStartTime: string;
  customerNotes: string;
}

export const useBookings = () => {
  const { tenantSlug, user } = useAuth();

  const [bookings, setBookings] = useState<Booking[]>([]);
  const [customerBookings, setCustomerBookings] = useState<CustomerBooking[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const headers = { 'X-Tenant': tenantSlug };

  const getAll = useCallback(async (pageNumber = 1, pageSize = 10) => {
    setIsLoading(true);
    setError(null);
    try {
      const result = await axiosInstance.get('/booking', { 
        headers,
        params: { pageNumber, pageSize }
      });
      setBookings(result.data ?? []);
    } catch(err: any) {
      setError(`Failed to load bookings. ${err}`);
    } finally {
      setIsLoading(false);
    }
  }, [tenantSlug]);

  const getCustomerBooking = useCallback(async () => {
    setIsLoading(true);
    setError(null);
    try {
      const result = await axiosInstance.get(`/booking/customer/${user?.id}`, { headers });
      if (!result.isSuccess) {
        setError(result.errors?.[0]?.message ?? 'Failed to load bookings.');
        return;
      }
      setCustomerBookings(result.data ?? []);
    } catch {
      setError('Failed to load bookings.');
    } finally {
      setIsLoading(false);
    }
  }, [tenantSlug, user?.id]);

  const create = useCallback(
    async (request: AdminBookingForm): Promise<boolean> => {
      setIsLoading(true);
      setError(null);
      try {
        const result = await axiosInstance.post('/booking', request, { headers });
        if (result && !result.isSuccess) {
          setError(result.errors?.[0]?.message ?? 'Failed to create booking.');
          return false;
        }
        await getAll();
        return true;
      } catch {
        setError('Failed to create booking.');
        return false;
      } finally {
        setIsLoading(false);
      }
    },
    [tenantSlug, getAll]
  );

  const update = useCallback(
    async (bookingId: string, request: UpdateBookingRequest): Promise<boolean> => {
      setIsLoading(true);
      setError(null);
      try {
        const result = await axiosInstance.patch(
          `/booking/${bookingId}`,
          request,
          { headers }
        );
        if (result && !result.isSuccess) {
          setError(result.errors?.[0]?.message ?? 'Failed to update booking.');
          return false;
        }
        await getAll();
        return true;
      } catch {
        setError('Failed to update booking.');
        return false;
      } finally {
        setIsLoading(false);
      }
    },
    [tenantSlug, getAll]
  );

  const cancel = useCallback(
    async (bookingId: string, onSuccess?: () => Promise<void>): Promise<boolean> => {
      setIsLoading(true);
      setError(null);
      try {
        const result = await axiosInstance.post(
          `/booking/${bookingId}/cancel`,
          { reason: null },
          { headers }
        );
        if (result && !result.isSuccess) {
          setError(result.errors?.[0]?.message ?? 'Failed to cancel booking.');
          return false;
        }
        if (onSuccess) {
          await onSuccess();
        } else {
          await getAll();
        }
        return true;
      } catch {
        setError('Failed to cancel booking.');
        return false;
      } finally {
        setIsLoading(false);
      }
    },
    [tenantSlug, getAll]
  );

  return {
    bookings,
    customerBookings,
    isLoading,
    error,
    clearError: () => setError(null),
    getCustomerBooking,
    getAll,
    create,
    update,
    cancel,
  };
};
// import { useState, useCallback } from 'react';
// import axiosInstance from '../../../services/axiosInstance';
// import { useAuth } from '../../../context/AuthContext';
// import type {
//   Booking,
//   CreateBookingRequest,
//   UpdateBookingRequest,
// } from '../types';

// export const useBookings = () => {
//   const { tenantSlug, user } = useAuth();

//   const [bookings, setBookings] = useState<Booking[]>([]);
//   const [isLoading, setIsLoading] = useState(false);
//   const [error, setError] = useState<string | null>(null);

//   const headers = { 'X-Tenant': tenantSlug };

//   const getAll = useCallback(async () => {
//     setIsLoading(true);
//     setError(null);

//     try {
//       const result = await axiosInstance.get('/booking', { headers });

//       if (!result.isSuccess) {
//         setError(result.errors?.[0]?.message ?? 'Failed to load bookings.');
//         return;
//       }

//       setBookings(result.data ?? []);
//     } catch {
//       setError('Failed to load bookings.');
//     } finally {
//       setIsLoading(false);
//     }
//   }, [tenantSlug]);

//   const getCustomerBooking = useCallback(async () => {
//     setIsLoading(true);
//     setError(null);
//     try {
//       const result = await axiosInstance.get(`/booking/customer/${user?.id}`, { headers });
//       if (!result.isSuccess) {
//         setError(result.errors?.[0]?.message ?? 'Failed to load bookings.');
//         return;
//       }
//       setBookings(result.data ?? []);
//     } catch (err: any) {
//       setError(err.message);
//     } finally {
//       setIsLoading(false);
//     }
// }, [tenantSlug, user?.id]);

//   const create = useCallback(
//     async (request: CreateBookingRequest): Promise<boolean> => {
//       setIsLoading(true);
//       setError(null);

//       try {
//         const result = await axiosInstance.post('/booking', request, {
//           headers,
//         });

//         if (result && !result.isSuccess) {
//           setError(result.errors?.[0]?.message ?? 'Failed to create booking.');
//           return false;
//         }

//         await getAll();
//         return true;
//       } catch {
//         setError('Failed to create booking.');
//         return false;
//       } finally {
//         setIsLoading(false);
//       }
//     },
//     [tenantSlug, getAll]
//   );

//   const update = useCallback(
//     async (bookingId: string, request: UpdateBookingRequest): Promise<boolean> => {
//       setIsLoading(true);
//       setError(null);

//       try {
//         const result = await axiosInstance.patch(
//           `/booking/${bookingId}`,
//           request,
//           { headers }
//         );

//         if (result && !result.isSuccess) {
//           setError(result.errors?.[0]?.message ?? 'Failed to update booking.');
//           return false;
//         }

//         await getAll();
//         return true;
//       } catch {
//         setError('Failed to update booking.');
//         return false;
//       } finally {
//         setIsLoading(false);
//       }
//     },
//     [tenantSlug, getAll]
//   );

//   const cancel = useCallback(
//     async (bookingId: string): Promise<boolean> => {
//       setIsLoading(true);
//       setError(null);

//       try {
//         const result = await axiosInstance.patch(
//           `/booking/${bookingId}/cancel`,
//           {},
//           { headers }
//         );

//         if (result && !result.isSuccess) {
//           setError(result.errors?.[0]?.message ?? 'Failed to cancel booking.');
//           return false;
//         }

//         await getAll();
//         return true;
//       } catch {
//         setError('Failed to cancel booking.');
//         return false;
//       } finally {
//         setIsLoading(false);
//       }
//     },
//     [tenantSlug, getAll]
//   );

//   return {
//     bookings,
//     isLoading,
//     error,
//     clearError: () => setError(null),
//     getCustomerBooking,
//     getAll,
//     create,
//     update,
//     cancel,
//   };
// };