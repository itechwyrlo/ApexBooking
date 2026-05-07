import React, { useEffect, useState, useCallback } from "react";
import { useSearchParams, useNavigate } from "react-router-dom";
import axiosInstance from "../../../services/axiosInstance";
import type { EmailVerificationResponse } from "../types";

const EmailVerificationPage: React.FC = () => {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();

  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const token = searchParams.get("token");

  const verifyEmail = useCallback(async () => {
    if (!token) {
      setError("Missing token");
      return;
    }

    setIsLoading(true);
    setError(null);

    try {
      const result = await axiosInstance.get<EmailVerificationResponse>(
        `/auth/verify-account?token=${token}`
      );

      if (!result.isSuccess) {
        setError(result.errors?.[0]?.message || "Verification failed");
        return;
      }

      const url = result.data?.url;

      if (url) {
        navigate(url);
        return;
      }

      navigate("/login");
    } catch (err: any) {
      setError(
        err?.response?.data?.errors?.[0]?.message ||
          "Verification failed"
      );
    } finally {
      setIsLoading(false);
    }
  }, [token, navigate]);

  useEffect(() => {
    verifyEmail();
  }, [verifyEmail]);

  return (
    <div>
      {isLoading && <p>Verifying...</p>}
      {error && <p>{error}</p>}
    </div>
  );
};

export default EmailVerificationPage;