import React, {
  createContext,
  useContext,
  useEffect,
  useState,
  useCallback,
} from "react";
import { jwtDecode } from "jwt-decode";

interface User {
  id: string;
  email: string;
  fullName: string;
  tenantId: string;
  role: "TenantAdmin" | "Manager" | "Staff" | "customer" | "superadmin";
}

interface JWTPayload {
  sub?: string;
  email?: string;
  tenant_id?: string;
  role?: string;
  email_verified?: boolean;
}

interface AuthContextType {
  accessToken: string | null;
  setAccessToken: (token: string) => void;
  user: User | null;
  isAuthenticated: boolean;
  emailVerified: boolean;
  isInitializing: boolean;
  tenantSlug: string | null;
  setTenantSlug: (slug: string) => void;
  clearAllAuthData: () => void;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({
  children,
}) => {
  const [accessToken, setAccessTokenState] = useState<string | null>(null);
  const [user, setUser] = useState<User | null>(null);
  const [tenantSlug, setTenantSlugState] = useState<string | null>(null);
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [emailVerified, setEmailVerified] = useState(false);
  const [isInitializing, setIsInitializing] = useState(true);

  const setAccessToken = useCallback((token: string) => {
    const claims = jwtDecode<JWTPayload>(token);

    // Superadmin tokens carry no email_verified claim — treat them as verified.
    const verified = claims.role === "superadmin" || claims.email_verified === true;
    setEmailVerified(verified);

    if (claims.sub && claims.email && claims.role) {
      setUser({
        id: claims.sub,
        email: claims.email,
        fullName: "User",
        tenantId: claims.tenant_id ?? "",
        role: claims.role as User["role"],
      });
    }

    setAccessTokenState(token);
    setIsAuthenticated(true);

    sessionStorage.setItem("access_token", token);
  }, []);

  const setTenantSlug = (slug: string) => {
    setTenantSlugState(slug);
    sessionStorage.setItem("tenant_slug", slug);
  };

  const clearAllAuthData = () => {
    sessionStorage.clear();
    setAccessTokenState(null);
    setUser(null);
    setIsAuthenticated(false);
    setEmailVerified(false);
    setTenantSlugState(null);
  };

  useEffect(() => {
    const token = sessionStorage.getItem("access_token");
    const slug = sessionStorage.getItem("tenant_slug");

    if (token) setAccessToken(token);
    if (slug) setTenantSlugState(slug);

    setIsInitializing(false);
  }, [setAccessToken]);

  return (
    <AuthContext.Provider
      value={{
        accessToken,
        setAccessToken,
        user,
        isAuthenticated,
        emailVerified,
        isInitializing,
        tenantSlug,
        setTenantSlug,
        clearAllAuthData,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = () => {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used within AuthProvider");
  return ctx;
};
