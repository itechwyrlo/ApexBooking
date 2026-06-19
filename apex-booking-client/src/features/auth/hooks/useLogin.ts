import { useState } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import { jwtDecode } from "jwt-decode";
import axiosInstance from "../../../services/axiosInstance";
import { useAuth } from "../../../context/AuthContext";
import type { AuthResponseData } from "../types";

interface JWTPayload {
  role?: string;
}

export const useLogin = () => {
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const { setAccessToken, setTenantSlug } = useAuth();
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();

  const login = async (email: string, password: string) => {
    setIsLoading(true);
    setError(null);

    try {
      const result = await axiosInstance.post<AuthResponseData>("/auth/login", {
        email,
        password,
      });

      const accessToken = result.accessToken;
      const tenantSlug = result.tenantSlug;

      if (!tenantSlug) {
        setError("Missing tenant slug from server");
        return;
      }

      if (!accessToken) {
        setError("Missing access token");
        return;
      }

      sessionStorage.setItem("access_token", accessToken);
      sessionStorage.setItem("isAuthenticated", "true");

      setAccessToken(accessToken);
      setTenantSlug(tenantSlug);

      const claims = jwtDecode<JWTPayload>(accessToken);
      const returnTo = searchParams.get("returnTo");

      if (returnTo && claims.role === "customer") {
        navigate(returnTo);
      } else if (claims.role === "customer") {
        navigate(`/book/${tenantSlug}/customer/bookings`);
      } else {
        navigate(`/t/${tenantSlug}/dashboard`);
      }
    } catch (err: any) {
      setError(err?.message ?? "Login failed");
    } finally {
      setIsLoading(false);
    }
  };

  return {
    login,
    isLoading,
    error,
    clearError: () => setError(null),
  };
};
