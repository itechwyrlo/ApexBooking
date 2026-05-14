import React, { useState, useEffect, useMemo } from "react";
import { useParams } from "react-router-dom";
import axiosInstance from "../../../services/axiosInstance";
import type {
  PublicService,
  PublicResource,
  CreateBookingRequest,
  BookingResult,
  PublicTenant,
} from "../types";
import { useSlots } from "../hooks/useSlots";
import { useMonthlyAvailability } from "../hooks/useMonthlyAvailability";
import { useInitiatePayment } from "../hooks/useInitiatePayment";
import { formatTo12Hour } from "../../../utils/timeFormat";
import BookingCalendar from "../components/BookingCalendar";
import TimeSlotGrid from "../components/TimeSlotGrid";
import StaffCard from "../components/StaffCard";
import StaffInfoDrawer from "../components/StaffInfoDrawer";
import WizardProgressBar from "../components/WizardProgressBar";
import WizardSelectionChips from "../components/WizardSelectionChips";
import { Alert } from "../../../components/ui/Alert";
import { Button } from "../../../components/ui/Button";

type WizardStep = 1 | 2 | 3 | 4 | 5;
type SelectedResource = PublicResource | null | "unset";

const CustomerBookingWizardPage: React.FC = () => {
  const { tenant } = useParams<{ tenant: string }>();
  // const navigate = useNavigate();
  const [step, setStep] = useState<WizardStep>(1);

  // Data
  const [tenantData, setTenantData] = useState<PublicTenant | null>(null);
  const [tenantLoading, setTenantLoading] = useState(true);
  const [services, setServices] = useState<PublicService[]>([]);
  const [servicesLoading, setServicesLoading] = useState(false);
  const [resources, setResources] = useState<PublicResource[]>([]);

  // Selections
  const [selectedService, setSelectedService] = useState<PublicService | null>(
    null,
  );
  const [selectedDate, setSelectedDate] = useState<string>("");
  const [selectedSlot, setSelectedSlot] = useState<string | null>(null);
  const [selectedResource, setSelectedResource] =
    useState<SelectedResource>("unset");

  // Details
  const [guestFirstName, setGuestFirstName] = useState("");
  const [guestLastName, setGuestLastName] = useState("");
  const [guestEmail, setGuestEmail] = useState("");
  const [guestPhone, setGuestPhone] = useState("");
  const [customerNotes, setCustomerNotes] = useState("");
  const [detailsErrors, setDetailsErrors] = useState<
    Partial<Record<string, string>>
  >({});

  // Result & Payment
  const [bookingResult, setBookingResult] = useState<BookingResult | null>(
    null,
  );
  const [submitLoading, setSubmitLoading] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [drawerResource, setDrawerResource] = useState<PublicResource | null>(
    null,
  );

  // Hooks
  const { slots, isLoading: slotsLoading, fetchSlots, clearSlots } = useSlots();
  const {
    availableDays,
    isLoading: isLoadingMonthly,
    fetchMonth,
  } = useMonthlyAvailability(tenant || "", selectedService?.serviceId || null);
  const {
    initiate,
    isLoading: paymentLoading,
    error: paymentError,
    clearError: clearPaymentError,
  } = useInitiatePayment();

  // Load Tenant & Services
  useEffect(() => {
    const loadTenant = async () => {
      setTenantLoading(true);
      try {
        const res = await axiosInstance.get(`/public/${tenant}`);
        setTenantData(res.data);
      } catch (err) {
        console.error("Failed to load tenant", err);
      } finally {
        setTenantLoading(false);
      }
    };
    const loadServices = async () => {
      setServicesLoading(true);
      try {
        const res = await axiosInstance.get(`/public/${tenant}/services`);
        setServices(res.data || []);
      } catch (err) {
        console.error("Failed to load services", err);
      } finally {
        setServicesLoading(false);
      }
    };
    if (tenant) {
      loadTenant();
      loadServices();
    }
  }, [tenant]);

  // Load resources when date/time/service are set
  useEffect(() => {
    if (step === 3 && selectedService && selectedDate && selectedSlot) {
      const loadResources = async () => {
        try {
          const params = { date: selectedDate, time: selectedSlot };
          const res = await axiosInstance.get(
            `/public/${tenant}/services/${selectedService.serviceId}/resources`,
            { params },
          );
          setResources(res.data || []);
        } catch (err) {
          console.error("Failed to load resources", err);
        }
      };
      loadResources();
    }
  }, [step, tenant, selectedService, selectedDate, selectedSlot]);

  // Fetch slots when date changes
  useEffect(() => {
    if (step === 2 && selectedDate && selectedService) {
      setSelectedSlot(null);
      fetchSlots(selectedService.serviceId, null, selectedDate, tenant!);
    }
  }, [selectedDate, selectedService, tenant, fetchSlots, step]);

  // Compute min/max dates
  // AFTER:
  const { minDate, maxDate } = useMemo(() => {
    if (!tenantData) return { minDate: "", maxDate: "" };

    // Get current time in tenant's timezone
    const nowUtc = new Date();
    const tenantNow = new Date(
      nowUtc.toLocaleString("en-US", { timeZone: tenantData.timezone }),
    );

    // Calculate boundaries in tenant's local time
    const minDateTime = new Date(
      tenantNow.getTime() + tenantData.minAdvanceBookingHours * 60 * 60 * 1000,
    );
    const maxDateTime = new Date(
      tenantNow.getTime() +
        tenantData.maxAdvanceBookingDays * 24 * 60 * 60 * 1000,
    );

    // Format as YYYY-MM-DD in tenant's timezone
    const formatInTenantTz = (d: Date) => {
      const year = d.getFullYear();
      const month = String(d.getMonth() + 1).padStart(2, "0");
      const day = String(d.getDate()).padStart(2, "0");
      return `${year}-${month}-${day}`;
    };

    return {
      minDate: formatInTenantTz(minDateTime),
      maxDate: formatInTenantTz(maxDateTime),
    };
  }, [tenantData]);
  // const { minDate, maxDate } = useMemo(() => {
  //   if (!tenantData) return { minDate: '', maxDate: '' };
  //   const now = new Date();
  //   const min = new Date(now.getTime() + tenantData.minAdvanceBookingHours * 60 * 60 * 1000);
  //   const max = new Date(now.getTime() + tenantData.maxAdvanceBookingDays * 24 * 60 * 60 * 1000);
  //   const formatDate = (d: Date) => d.toISOString().split('T')[0];
  //   return { minDate: formatDate(min), maxDate: formatDate(max) };
  // }, [tenantData]);

  const handleSelectService = (service: PublicService) => {
    setSelectedService(service);
    setSelectedDate("");
    setSelectedSlot(null);
    setSelectedResource("unset");
    clearSlots();
    setStep(2);
  };

  const handleSelectResource = (resource: SelectedResource) => {
    setSelectedResource(resource);
    setStep(4);
  };

  const validateDetails = () => {
    const errors: Partial<Record<string, string>> = {};
    if (!guestFirstName.trim())
      errors.guestFirstName = "First name is required.";
    if (!guestLastName.trim()) errors.guestLastName = "Last name is required.";
    if (!guestEmail.trim()) errors.guestEmail = "Email is required.";
    else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(guestEmail))
      errors.guestEmail = "Enter a valid email address.";
    setDetailsErrors(errors);
    return Object.keys(errors).length === 0;
  };

  const submitBooking = async () => {
    if (
      !selectedService ||
      !selectedDate ||
      !selectedSlot ||
      selectedResource === "unset"
    )
      return;
    setSubmitLoading(true);
    setSubmitError(null);
    try {
      const resourceId =
        selectedResource === null ? null : selectedResource.staffId;
      const request: CreateBookingRequest = {
        tenantSlug: tenant!,
        serviceId: selectedService.serviceId,
        resourceId,
        scheduledDate: selectedDate,
        scheduledStartTime: selectedSlot,
        guestFirstName: guestFirstName.trim(),
        guestLastName: guestLastName.trim(),
        guestEmail: guestEmail.trim(),
        guestPhone: guestPhone.trim() || undefined,
        customerNotes: customerNotes.trim() || undefined,
      };

      const res = await axiosInstance.post("/booking", request);
      if (!res.isSuccess) {
        setSubmitError(res.errors?.[0]?.message ?? "Failed to create booking.");
        return;
      }

      setBookingResult({
        bookingId: res.data.bookingId,
        bookingReference: res.data.bookingReference,
        status: res.data.status,
        priceSnapshot: res.data.priceSnapshot,
        currencyCode: res.data.currencyCode,
        serviceName: selectedService.name,
        resourceName:
          selectedResource === null ? "Any Available" : selectedResource.name,
        scheduledDate: selectedDate,
        scheduledStartTime: selectedSlot,
      });
    } catch {
      setSubmitError("Failed to create booking.");
    } finally {
      setSubmitLoading(false);
    }
  };

  const handleProceedToPayment = async () => {
    if (!bookingResult) return;
    const approvalUrl = await initiate(bookingResult.bookingId);
    if (approvalUrl) window.location.href = approvalUrl;
  };

  if (tenantLoading || servicesLoading)
    return <div className="p-5 text-center">Loading...</div>;
  if (!tenantData)
    return <div className="p-5 text-center">Business not found.</div>;

  // Render Confirmation
  if (bookingResult && bookingResult.status === "Confirmed") {
    return (
      <div className="min-vh-100 bg-light d-flex align-items-center justify-content-center p-3">
        <div
          className="card border-0 shadow-sm p-4 text-center"
          style={{ maxWidth: 480, width: "100%" }}
        >
          <div
            className="rounded-circle bg-success bg-opacity-10 d-flex align-items-center justify-content-center mx-auto mb-3"
            style={{ width: 64, height: 64 }}
          >
            <i className="fas fa-check fa-xl text-success" />
          </div>
          <h5 className="fw-bold mb-1">You're all set!</h5>
          <p className="text-muted small mb-3">
            A confirmation email has been sent to {guestEmail}.
          </p>
          <div className="bg-light rounded p-3 mb-3">
            <div className="text-muted small mb-1">Booking Reference</div>
            <div className="fw-bold fs-5">{bookingResult.bookingReference}</div>
          </div>
          <div className="text-muted small">
            <div>{bookingResult.serviceName}</div>
            <div>{bookingResult.resourceName}</div>
            <div>
              {bookingResult.scheduledDate} at{" "}
              {formatTo12Hour(bookingResult.scheduledStartTime)}
            </div>
          </div>
        </div>
      </div>
    );
  }

  // Render Payment Pending
  if (bookingResult && bookingResult.status === "PendingPayment") {
    return (
      <div className="min-vh-100 bg-light d-flex align-items-center justify-content-center p-3">
        <div
          className="card border-0 shadow-sm p-4 text-center"
          style={{ maxWidth: 480, width: "100%" }}
        >
          <div
            className="rounded-circle bg-warning bg-opacity-10 d-flex align-items-center justify-content-center mx-auto mb-3"
            style={{ width: 64, height: 64 }}
          >
            <i className="fas fa-credit-card fa-xl text-warning" />
          </div>
          <h5 className="fw-bold mb-2">Payment Required</h5>
          <p className="text-muted small mb-3">
            Your booking has been reserved. Complete payment to confirm your
            appointment.
          </p>
          <div className="bg-light rounded p-3 mb-3">
            <div className="text-muted small mb-1">Booking Reference</div>
            <div className="fw-bold fs-5">{bookingResult.bookingReference}</div>
            <div className="text-muted small mt-2">Amount Due</div>
            <div className="fw-bold fs-5">
              {bookingResult.currencyCode}{" "}
              {bookingResult.priceSnapshot.toFixed(2)}
            </div>
          </div>
          <div className="text-muted small mb-4">
            <div>{bookingResult.serviceName}</div>
            <div>{bookingResult.resourceName}</div>
            <div>
              {bookingResult.scheduledDate} at{" "}
              {formatTo12Hour(bookingResult.scheduledStartTime)}
            </div>
          </div>
          {paymentError && (
            <Alert
              variant="error"
              dismissible
              onDismiss={clearPaymentError}
              className="mb-3"
            >
              {paymentError}
            </Alert>
          )}
          <Button
            variant="primary"
            loading={paymentLoading}
            onClick={handleProceedToPayment}
            className="w-100"
          >
            Proceed to Payment
          </Button>
        </div>
      </div>
    );
  }

  const chipDate = selectedDate
    ? new Date(selectedDate + "T00:00:00").toLocaleDateString("en-US", {
        weekday: "short",
        month: "short",
        day: "numeric",
      })
    : undefined;
  const chipStaff =
    selectedResource === null
      ? "Any Available"
      : selectedResource !== "unset"
        ? selectedResource.name
        : undefined;

  return (
    <div className="min-vh-100 bg-light py-4 px-3">
      <div style={{ maxWidth: 680, margin: "0 auto" }}>
        <div className="text-center mb-4">
          {tenantData.logoUrl && (
            <img
              src={tenantData.logoUrl}
              alt={tenantData.businessName}
              className="mb-2"
              style={{ maxHeight: 60 }}
            />
          )}
          <h4 className="mb-1">{tenantData.businessName}</h4>
          <p className="text-muted small">
            Book your appointment in few easy steps
          </p>
        </div>

        <WizardProgressBar currentStep={step} />

        {(selectedService || selectedDate || selectedSlot || chipStaff) && (
          <WizardSelectionChips
            service={
              selectedService
                ? `${selectedService.name} — ${selectedService.currencyCode} ${selectedService.price.toFixed(2)}`
                : undefined
            }
            date={chipDate}
            time={selectedSlot ? formatTo12Hour(selectedSlot) : undefined}
            staffName={chipStaff}
          />
        )}

        {step === 1 && (
          <div className="card border-0 shadow-sm">
            <div className="card-header bg-white border-bottom py-3">
              <div className="fw-semibold">Select a Service</div>
              <div className="text-muted small">
                Choose what you'd like to book.
              </div>
            </div>
            <div className="card-body p-0">
              {services.map((service) => (
                <div
                  key={service.serviceId}
                  onClick={() => handleSelectService(service)}
                  className="px-4 py-3 border-bottom d-flex justify-content-between align-items-center clickable-row"
                  style={{ cursor: "pointer" }}
                >
                  <div>
                    <div className="fw-medium">{service.name}</div>
                    <div className="text-muted small">
                      {service.durationMinutes} min
                    </div>
                  </div>
                  <div className="fw-bold text-primary">
                    {service.currencyCode} {service.price.toFixed(2)}
                  </div>
                </div>
              ))}
            </div>
          </div>
        )}

        {step === 2 && selectedService && (
          <div className="card border-0 shadow-sm">
            <div className="card-header bg-white border-bottom py-3">
              <div className="fw-semibold">Pick a date and time</div>
            </div>
            <div className="card-body">
              <div className="row g-4">
                <div className="col-md-6">
                  <BookingCalendar
                    selectedDate={selectedDate}
                    onDateSelect={setSelectedDate}
                    minDate={minDate}
                    maxDate={maxDate}
                    availableDays={availableDays}
                    isLoadingAvailability={isLoadingMonthly}
                    onMonthChange={fetchMonth}
                  />
                </div>
                <div className="col-md-6">
                  {selectedDate ? (
                    <TimeSlotGrid
                      slots={slots?.availableSlots || []}
                      selectedSlot={selectedSlot}
                      onSelect={setSelectedSlot}
                      isLoading={slotsLoading}
                    />
                  ) : (
                    <div className="text-center py-5 text-muted">
                      <i className="bi bi-calendar-event display-4 mb-3 d-block"></i>
                      <p className="small">Please select a date first</p>
                    </div>
                  )}
                </div>
              </div>
            </div>
            <div className="card-footer bg-white border-top d-flex justify-content-between py-3">
              <button
                className="btn btn-outline-secondary"
                onClick={() => setStep(1)}
              >
                Back
              </button>
              <Button
                variant="primary"
                disabled={!selectedDate || !selectedSlot}
                onClick={() => setStep(3)}
              >
                Continue
              </Button>
            </div>
          </div>
        )}

        {step === 3 && selectedService && (
          <div className="card border-0 shadow-sm">
            <div className="card-header bg-white border-bottom py-3">
              <div className="fw-semibold">Choose your specialist</div>
            </div>
            <div className="card-body">
              <StaffCard
                resource={null}
                isSelected={selectedResource === null}
                onSelect={() => handleSelectResource(null)}
                onInfoClick={null}
              />
              {resources.map((resource) => (
                <StaffCard
                  key={resource.staffId}
                  resource={resource}
                  isSelected={
                    selectedResource !== "unset" &&
                    selectedResource !== null &&
                    selectedResource.staffId === resource.staffId
                  }
                  onSelect={() => handleSelectResource(resource)}
                  onInfoClick={() => setDrawerResource(resource)}
                />
              ))}
            </div>
            <div className="card-footer bg-white border-top d-flex py-3">
              <button
                className="btn btn-outline-secondary"
                onClick={() => setStep(2)}
              >
                Back
              </button>
            </div>
          </div>
        )}

        {step === 4 && selectedService && (
          <div className="card border-0 shadow-sm">
            <div className="card-header bg-white border-bottom py-3">
              <div className="fw-semibold">Your details</div>
            </div>
            <div className="card-body">
              <div className="row g-3">
                <div className="col-sm-6">
                  <label className="form-label small fw-medium">
                    First Name *
                  </label>
                  <input
                    type="text"
                    className={`form-control ${detailsErrors.guestFirstName ? "is-invalid" : ""}`}
                    value={guestFirstName}
                    onChange={(e) => setGuestFirstName(e.target.value)}
                  />
                </div>
                <div className="col-sm-6">
                  <label className="form-label small fw-medium">
                    Last Name *
                  </label>
                  <input
                    type="text"
                    className={`form-control ${detailsErrors.guestLastName ? "is-invalid" : ""}`}
                    value={guestLastName}
                    onChange={(e) => setGuestLastName(e.target.value)}
                  />
                </div>
                <div className="col-12">
                  <label className="form-label small fw-medium">Email *</label>
                  <input
                    type="email"
                    className={`form-control ${detailsErrors.guestEmail ? "is-invalid" : ""}`}
                    value={guestEmail}
                    onChange={(e) => setGuestEmail(e.target.value)}
                  />
                </div>
                <div className="col-12">
                  <label className="form-label small fw-medium">
                    Phone (optional)
                  </label>
                  <input
                    type="tel"
                    className="form-control"
                    value={guestPhone}
                    onChange={(e) => setGuestPhone(e.target.value)}
                  />
                </div>
                <div className="col-12">
                  <label className="form-label small fw-medium">
                    Notes (optional)
                  </label>
                  <textarea
                    className="form-control"
                    rows={3}
                    value={customerNotes}
                    onChange={(e) => setCustomerNotes(e.target.value)}
                  />
                </div>
              </div>
            </div>
            <div className="card-footer bg-white border-top d-flex justify-content-between py-3">
              <button
                className="btn btn-outline-secondary"
                onClick={() => setStep(3)}
              >
                Back
              </button>
              <Button
                variant="primary"
                onClick={() => validateDetails() && setStep(5)}
              >
                Continue
              </Button>
            </div>
          </div>
        )}

        {step === 5 &&
          selectedService &&
          selectedSlot &&
          selectedResource !== "unset" && (
            <div className="card border-0 shadow-sm">
              <div className="card-header bg-white border-bottom py-3">
                <div className="fw-semibold">Confirm Booking</div>
              </div>
              <div className="card-body">
                {submitError && (
                  <Alert variant="error" className="mb-3">
                    {submitError}
                  </Alert>
                )}
                <div className="bg-light rounded p-3 mb-4">
                  {[
                    ["Service", selectedService.name],
                    [
                      "Staff",
                      selectedResource === null
                        ? "Any Available"
                        : selectedResource.name,
                    ],
                    ["Date", chipDate],
                    ["Time", formatTo12Hour(selectedSlot)],
                    ["Name", `${guestFirstName} ${guestLastName}`],
                    ["Email", guestEmail],
                  ].map(([label, value]) => (
                    <div
                      key={label}
                      className="d-flex justify-content-between py-2 border-bottom"
                    >
                      <span className="text-muted small">{label}</span>
                      <span className="fw-medium small">{value}</span>
                    </div>
                  ))}
                </div>
                <Button
                  variant="primary"
                  loading={submitLoading}
                  onClick={submitBooking}
                  className="w-100"
                >
                  Confirm Booking
                </Button>
              </div>
              <div className="card-footer bg-white border-top d-flex py-3">
                <button
                  className="btn btn-outline-secondary"
                  onClick={() => setStep(4)}
                >
                  Back
                </button>
              </div>
            </div>
          )}
      </div>

      {drawerResource && (
        <StaffInfoDrawer
          resource={drawerResource}
          open={!!drawerResource}
          onClose={() => setDrawerResource(null)}
          onSelect={() => {
            handleSelectResource(drawerResource);
            setDrawerResource(null);
          }}
        />
      )}
    </div>
  );
};

export default CustomerBookingWizardPage;
