import React, { useState, useEffect } from "react";
import { useParams, useSearchParams, useNavigate } from "react-router-dom";
import { Input } from "../../../components/ui/Input";
import { Button } from "../../../components/ui/Button";
import { PhoneField } from "../../../components/ui/PhoneField";
import { useAuth } from "../../../context/AuthContext";
import axiosInstance from "../../../services/axiosInstance";
import type { AuthResponseData } from "../../auth/types";

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
      const result = await axiosInstance.post<AuthResponseData>(
        `/auth/register/customer/${tenant}`,
        {
          fullName: formData.fullName,
          email: formData.email,
          phone: formData.phone,
          password: formData.password,
          returnTo: searchParams.get("returnTo") ?? undefined,
        },
      );

      const accessToken = result.accessToken;
      const tenantSlug = result.tenantSlug;

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

  useEffect(() => {
    if (error) {
      setFormData((prev) => ({ ...prev, password: "" }));
    }
  }, [error]);

  return (
    <div className="min-vh-100 d-flex align-items-center justify-content-center bg-light">
      <div className="container">
        <div className="row justify-content-center">
          <div className="col-12 col-sm-10 col-md-6 col-lg-5">
            <div className="card border-0 shadow-sm p-4 p-md-5">
              <div className="text-center mb-4">
                <h4 className="fw-bold">Create an account</h4>
                <p className="text-muted small">
                  Already have an account?{" "}
                  <a
                    href={`/book/${tenant}/customer/login${
                      searchParams.get("returnTo")
                        ? `?returnTo=${encodeURIComponent(searchParams.get("returnTo")!)}`
                        : ""
                    }`}
                    className="link-primary"
                  >
                    Sign in
                  </a>
                </p>
              </div>

              {error && (
                <div className="alert alert-danger alert-dismissible d-flex align-items-center mb-3" role="alert">
                  {error}
                  <button
                    type="button"
                    className="btn-close ms-auto"
                    onClick={() => setError(null)}
                    aria-label="Dismiss"
                  />
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
                  variant="primary"
                  loading={isLoading}
                  className="w-100 py-2 fw-semibold"
                >
                  Create account
                </Button>
              </form>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default CustomerRegisterPage;
