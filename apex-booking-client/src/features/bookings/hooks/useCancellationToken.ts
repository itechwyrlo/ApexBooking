import { useEffect, useState } from 'react';
import axiosInstance from '../../../services/axiosInstance';
import type { BaseResponse } from '../../../types';
import type { CancellationTokenValidation } from '../types';

export type CancellationPageState =
  | 'loading'
  | 'valid_refund'
  | 'valid_no_refund'
  | 'expired'
  | 'used'
  | 'invalid';

interface UseCancellationTokenResult {
  state: CancellationPageState;
  validationData: CancellationTokenValidation | null;
  isCancelling: boolean;
  cancelError: string | null;
  cancel: () => Promise<boolean>;
  clearCancelError: () => void;
}

export const useCancellationToken = (token: string): UseCancellationTokenResult => {
  const [state, setState] = useState<CancellationPageState>(token ? 'loading' : 'invalid');
  const [validationData, setValidationData] = useState<CancellationTokenValidation | null>(null);
  const [isCancelling, setIsCancelling] = useState(false);
  const [cancelError, setCancelError] = useState<string | null>(null);

  useEffect(() => {
    if (!token) return;

    let active = true;

    const doValidate = async () => {
      try {
        const result = await axiosInstance.get<BaseResponse<CancellationTokenValidation>>(
          `/public/cancellation/validate?token=${encodeURIComponent(token)}`
        );
        if (!active) return;

        if (result.isSuccess && result.data) {
          setValidationData(result.data);
          setState(result.data.refundPercent > 0 ? 'valid_refund' : 'valid_no_refund');
          return;
        }

        const message = result.errors?.[0]?.message ?? '';
        if (message.includes('already been used')) {
          setState('used');
        } else if (message.includes('expired')) {
          setState('expired');
        } else {
          setState('invalid');
        }
      } catch {
        if (active) setState('invalid');
      }
    };

    doValidate();
    return () => {
      active = false;
    };
  }, [token]);

  const cancel = async (): Promise<boolean> => {
    setIsCancelling(true);
    setCancelError(null);
    try {
      const result = await axiosInstance.post<BaseResponse<boolean>>(
        '/public/cancellation/cancel',
        { token }
      );
      if (!result.isSuccess) {
        setCancelError(result.errors?.[0]?.message ?? 'Failed to cancel booking.');
        return false;
      }
      return true;
    } catch {
      setCancelError('Failed to cancel booking. Please try again.');
      return false;
    } finally {
      setIsCancelling(false);
    }
  };

  return {
    state,
    validationData,
    isCancelling,
    cancelError,
    cancel,
    clearCancelError: () => setCancelError(null),
  };
};
