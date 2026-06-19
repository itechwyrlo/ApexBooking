import { useEffect, useState } from 'react';
import axiosInstance from '../../../services/axiosInstance';
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
        const result = await axiosInstance.get<CancellationTokenValidation>(
          `/public/cancellation/validate?token=${encodeURIComponent(token)}`
        );
        if (!active) return;
        setValidationData(result);
        setState(result.refundPercent > 0 ? 'valid_refund' : 'valid_no_refund');
      } catch (err: any) {
        if (!active) return;
        const message: string = err?.message ?? '';
        if (message.includes('already been used')) {
          setState('used');
        } else if (message.includes('expired')) {
          setState('expired');
        } else {
          setState('invalid');
        }
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
      await axiosInstance.post('/public/cancellation/cancel', { token });
      return true;
    } catch (err: any) {
      setCancelError(err?.message || 'Failed to cancel booking. Please try again.');
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
