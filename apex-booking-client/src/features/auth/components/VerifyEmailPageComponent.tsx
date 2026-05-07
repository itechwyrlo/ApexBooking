import React, { useState, useEffect, useRef } from "react";
import { useSearchParams, useNavigate } from "react-router-dom";
import { Button } from "../../../components/ui/Button";
import { Alert } from "../../../components/ui/Alert";

const VerifyEmailPageComponent: React.FC = () => {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const token = searchParams.get("token");

  // 1. Initialize the redirectUrl state with a default login path
  const [redirectUrl, setRedirectUrl] = useState("/login");

  const [status, setStatus] = useState<"loading" | "success" | "error">("loading");
  const [message, setMessage] = useState("");
  const hasCalledApi = useRef(false);

  useEffect(() => {
    if (hasCalledApi.current) return;

    const verify = async () => {
      if (!token) {
        setStatus("error");
        setMessage("No verification token found in URL.");
        return;
      }

      try {
        const response = await fetch(`/auth/verify-email?token=${token}`, {
          method: "POST",
          headers: { "Content-Type": "application/json" },
        });

        const data = await response.json();

        // 2. Handle BaseResponse and DTO specific checks
        if (!response.ok || !data.isSuccess) {
          const errorMessage = data.errors?.[0]?.message || "Verification failed";
          throw new Error(errorMessage);
        }

        // 3. CORRECTED: Access the 'url' property from your DTO
        // .NET serializes 'Url' to 'url' by default in JSON
        if (data.url) {
          setRedirectUrl(data.url);
        }

        setStatus("success");
        setMessage("Email verified successfully!");

        // 4. Auto-redirect using the specific backend URL
        setTimeout(() => {
          navigate(data.url || "/login");
        }, 2000);

      } catch (error) {
        setStatus("error");
        setMessage(error instanceof Error ? error.message : "An error occurred");
      }
    };

    verify();
    hasCalledApi.current = true;
  }, [token, navigate]);

  return (
    <div className="min-vh-100 d-flex align-items-center justify-content-center bg-light">
      <div className="card shadow" style={{ width: "100%", maxWidth: "500px" }}>
        <div className="card-body p-4">
          <h2 className="card-title text-center mb-4">Email Verification</h2>
          
          {/* Status UI logic remains the same */}

          {status === "error" && (
            <div>
              <Alert variant="error" className="mb-3">{message}</Alert>
              <div className="d-grid gap-2">
                {/* 5. Button now uses the dynamic redirectUrl state */}
                <Button variant="primary" onClick={() => navigate(redirectUrl)}>
                  Go to Login
                </Button>
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  );
};


export default VerifyEmailPageComponent;
