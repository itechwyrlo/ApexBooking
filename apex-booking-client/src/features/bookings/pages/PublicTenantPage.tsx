import React, { useEffect, useState } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { Alert } from "../../../components/ui/Alert";
import type { PublicTenant, PublicService } from "../types";
import axiosInstance from "../../../services/axiosInstance";

const PublicTenantPage: React.FC = () => {
  const { tenant } = useParams<{ tenant: string }>();
  const navigate = useNavigate();

  const [tenantProfile, setTenantProfile] = useState<PublicTenant | null>(null);
  const [services, setServices] = useState<PublicService[]>([]);
  const [tenantLoading, setTenantLoading] = useState(true);
  const [servicesLoading, setServicesLoading] = useState(true);
  const [tenantError, setTenantError] = useState<string | null>(null);
  const [servicesError, setServicesError] = useState<string | null>(null);

  useEffect(() => {
    const load = async () => {
      setTenantLoading(true);
      setTenantError(null);
      try {
        const result = await axiosInstance.get(`/public/${tenant}`);
        if (!result.isSuccess) {
          setTenantError(
            result.errors?.[0]?.message ?? "Failed to load business profile.",
          );
          return;
        }
        setTenantProfile(result.data ?? null);
      } catch {
        setTenantError("Failed to load business profile.");
      } finally {
        setTenantLoading(false);
      }
    };
    load();
  }, [tenant]);

  useEffect(() => {
    const load = async () => {
      setServicesLoading(true);
      setServicesError(null);
      try {
        const result = await axiosInstance.get(`/public/${tenant}/services`);
        if (!result.isSuccess) {
          setServicesError(
            result.errors?.[0]?.message ?? "Failed to load services.",
          );
          return;
        }
        setServices(result.data ?? []);
      } catch {
        setServicesError("Failed to load services.");
      } finally {
        setServicesLoading(false);
      }
    };
    load();
  }, [tenant]);

  const handleBookNow = (serviceId: string) => {
    navigate(`/book/${tenant}/new?serviceId=${serviceId}`);
  };

  return (
    <div className="min-vh-100 bg-light">
      {/* Business profile header */}
      <div className="bg-white border-bottom shadow-sm">
        <div className="container py-4" style={{ maxWidth: 900 }}>
          {tenantLoading ? (
            <div className="text-muted small">Loading...</div>
          ) : tenantError ? (
            <Alert variant="error">{tenantError}</Alert>
          ) : tenantProfile ? (
            <div className="d-flex align-items-center gap-4 flex-wrap">
              {tenantProfile.logoUrl && (
                <img
                  src={tenantProfile.logoUrl}
                  alt={tenantProfile.businessName}
                  style={{
                    width: 72,
                    height: 72,
                    objectFit: "contain",
                    borderRadius: 8,
                  }}
                />
              )}
              <div>
                <h4 className="fw-bold mb-1">{tenantProfile.businessName}</h4>
                <div className="d-flex flex-wrap gap-3 text-muted small">
                  {tenantProfile.city && tenantProfile.countryCode && (
                    <span>
                      <i className="fas fa-map-marker-alt me-1" />
                      {tenantProfile.city}, {tenantProfile.countryCode}
                    </span>
                  )}
                  {tenantProfile.contactEmail && (
                    <span>
                      <i className="fas fa-envelope me-1" />
                      {tenantProfile.contactEmail}
                    </span>
                  )}
                  {tenantProfile.contactPhone && (
                    <span>
                      <i className="fas fa-phone me-1" />
                      {tenantProfile.contactPhone}
                    </span>
                  )}
                  {tenantProfile.websiteUrl && (
                    <a
                      href={tenantProfile.websiteUrl}
                      target="_blank"
                      rel="noreferrer"
                      className="text-decoration-none"
                    >
                      <i className="fas fa-globe me-1" />
                      {tenantProfile.websiteUrl}
                    </a>
                  )}
                </div>
              </div>
            </div>
          ) : null}
        </div>
      </div>

      {/* Service catalog */}
      <div className="container py-4" style={{ maxWidth: 900 }}>
        <h5 className="fw-bold mb-4">Our Services</h5>

        {servicesError && (
          <Alert
            variant="error"
            dismissible
            onDismiss={() => setServicesError(null)}
          >
            {servicesError}
          </Alert>
        )}

        {servicesLoading ? (
          <div className="text-muted small">Loading services...</div>
        ) : services.length === 0 ? (
          <div className="text-muted small">
            No services are currently available.
          </div>
        ) : (
          <div className="row g-3">
            {services.map((service) => (
              <div key={service.serviceId} className="col-12 col-md-6">
                <div className="card border-0 shadow-sm h-100">
                  <div className="card-body d-flex flex-column">
                    <div className="fw-semibold mb-1">{service.name}</div>
                    {service.description && (
                      <div className="text-muted small mb-2">
                        {service.description}
                      </div>
                    )}
                    <div className="d-flex gap-3 text-muted small mb-3">
                      <span>
                        <i className="fas fa-clock me-1" />
                        {service.durationMinutes} min
                      </span>
                      <span className="fw-semibold text-primary">
                        {service.currencyCode} {service.price.toFixed(2)}
                      </span>
                    </div>
                    <div className="mt-auto">
                      <button
                        className="btn btn-primary btn-sm w-100"
                        onClick={() =>
                          handleBookNow(service.serviceId.toString())
                        }
                      >
                        Book Now
                      </button>
                    </div>
                  </div>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
};

export default PublicTenantPage;
