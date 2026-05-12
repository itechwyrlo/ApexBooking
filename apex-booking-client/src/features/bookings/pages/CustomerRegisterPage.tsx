import React, { useState, useEffect } from "react";
import { useParams, useSearchParams, useNavigate } from "react-router-dom";
import { Input } from "../../../components/ui/Input";
import { Button } from "../../../components/ui/Button";
import { Alert } from "../../../components/ui/Alert";
import { PhoneField } from "../../../components/ui/PhoneField";
import { useAuth } from "../../../context/AuthContext";
import axiosInstance from "../../../services/axiosInstance";
import type { AuthResponse } from "../../auth/types";

interface CustomerRegisterFormData {
  fullName: string;
  email: string;
  phone: string;
  password: string;
}

const CustomerRegisterPage: React.FC = () => {
  const { tenant } = useParams<{ tenant: string }>();
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const { setAccessToken, setTenantSlug } = useAuth();

  const [formData, setFormData] = useState<CustomerRegisterFormData>({
    fullName: "",
    email: "",
    phone: "",
    password: "",
  });

  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleInputChange = (
    field: keyof CustomerRegisterFormData,
    value: string,
  ) => {
    setError(null);
    setFormData((prev) => ({ ...prev, [field]: value }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsLoading(true);
    setError(null);

    try {
      const result = await axiosInstance.post<AuthResponse>(
        `/auth/register/customer/${tenant}`,
        {
          fullName: formData.fullName,
          email: formData.email,
          phone: formData.phone,
          password: formData.password,
          returnTo: searchParams.get("returnTo") ?? undefined,
        },
      );

      if (!result.isSuccess) {
        setError(result.errors?.[0]?.message ?? "Registration failed.");
        return;
      }

      const accessToken = result.data?.accessToken;
      const tenantSlug = result.data?.tenantSlug;

      if (!accessToken || !tenantSlug) {
        setError("Registration succeeded but response was incomplete.");
        return;
      }

      sessionStorage.setItem("access_token", accessToken);
      sessionStorage.setItem("isAuthenticated", "true");

      setAccessToken(accessToken);
      setTenantSlug(tenantSlug);

      const returnTo = searchParams.get("returnTo");
      const noticePath = returnTo
        ? `/verify-email-notice?returnTo=${encodeURIComponent(returnTo)}`
        : "/verify-email-notice";
      navigate(noticePath);
    } catch {
      setError("Registration failed.");
    } finally {
      setIsLoading(false);
    }
  };

  // Clear password on error
  useEffect(() => {
    if (error) {
      setFormData((prev) => ({ ...prev, password: "" }));
    }
  }, [error]);

  return (
    <div className="min-vh-100 d-flex align-items-center justify-content-center bg-light">
      <div
        className="card shadow-sm border-0 p-4"
        style={{ width: "100%", maxWidth: "400px" }}
      >
        <div className="text-center mb-4">
          <h3 className="fw-bold">Create an account</h3>
          <p className="text-muted small">
            Already have an account?{" "}
            <a
              href={`/book/${tenant}/customer/login${
                searchParams.get("returnTo")
                  ? `?returnTo=${encodeURIComponent(searchParams.get("returnTo")!)}`
                  : ""
              }`}
              className="text-decoration-none"
            >
              Sign in
            </a>
          </p>
        </div>

        {error && (
          <div className="mb-3">
            <Alert variant="error" dismissible onDismiss={() => setError(null)}>
              {error}
            </Alert>
          </div>
        )}

        <form onSubmit={handleSubmit}>
          <div className="mb-3">
            <Input
              label="Full Name"
              type="text"
              placeholder="Your full name"
              value={formData.fullName}
              onChange={(val) => handleInputChange("fullName", val)}
              required
            />
          </div>
          <div className="mb-3">
            <Input
              label="Email address"
              type="email"
              placeholder="name@example.com"
              value={formData.email}
              onChange={(val) => handleInputChange("email", val)}
              required
            />
          </div>
          <div className="mb-3">
            <PhoneField
              label="Phone"
              value={formData.phone}
              onChange={(val) => handleInputChange("phone", val)}
              required
            />
          </div>
          <div className="mb-4">
            <Input
              label="Password"
              type="password"
              placeholder="Create a password"
              value={formData.password}
              onChange={(val) => handleInputChange("password", val)}
              required
            />
          </div>

          <Button
            type="submit"
            className="btn btn-primary w-100 py-2 fw-semibold"
            disabled={isLoading}
          >
            {isLoading ? "Creating account..." : "Create account"}
          </Button>
        </form>
      </div>
    </div>
  );
};

export default CustomerRegisterPage;
