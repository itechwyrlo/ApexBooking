import { useState } from "react";
import { useNavigate } from "react-router-dom";
import axiosInstance from "../../../services/axiosInstance";
import { useAuth } from "../../../context/AuthContext";
import type { AuthResponse } from "../types";

export const useLogin = () => {
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const { setAccessToken, setTenantSlug } = useAuth();
  const navigate = useNavigate();

  const login = async (email: string, password: string) => {
    setIsLoading(true);
    setError(null);

    try {
      const result = await axiosInstance.post<AuthResponse>("/auth/login", {
        email,
        password,
      });


      if (!result.isSuccess) {
        setError(result?.errors?.[0]?.message ?? "Login failed");
        return;
      }


      const accessToken = result.data?.accessToken;
      const tenantSlug = result.data?.tenantSlug;

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

      navigate(`/t/${tenantSlug}/dashboard`);
    } catch {
      setError("Login failed");
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
// import { useState } from "react";
// import { useNavigate } from "react-router-dom";
// import axiosInstance from "../../../services/axiosInstance";
// import { useAuth } from "../../../context/AuthContext";
// import type { LoginRequest, AuthResponse } from "../types";

// export const useLogin = () => {
//   const [isLoading, setIsLoading] = useState(false);
//   const [error, setError] = useState<string | null>(null);

//   const { setAccessToken } = useAuth();
//   const navigate = useNavigate();

//   const login = async (email: string, password: string) => {
//     setIsLoading(true);
//     setError(null);

//     try {
//       const result = await axiosInstance.post<AuthResponse>('/auth/login', {
//         email,
//         password,
//       } as LoginRequest);
      
//       if (!result.isSuccess || !result.data) {
//         setError(result.errors?.[0]?.message || 'Login failed.');
//         return;
//       }

//       const data = result.data;

//       sessionStorage.setItem("access_token", data.accessToken);

//       setAccessToken(data.accessToken);

//       const tenant = data.tenantId;

//       navigate(`/t/${tenant}/dashboard`);
//     } catch (err: any) {
//       setError(
//         err?.response?.data?.errors?.[0]?.message ||
//           "Login failed"
//       );
//     } finally {
//       setIsLoading(false);
//     }
//   };

//   return {
//     login,
//     isLoading,
//     error,
//     clearError: () => setError(null),
//   };
// };