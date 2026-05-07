import React, { useEffect, useState } from 'react';
import { useNavigate, useParams, useSearchParams } from 'react-router-dom';
import { Alert } from '../../../components/ui/Alert';
import { Button } from '../../../components/ui/Button';
import { useSlots } from '../hooks/useSlots';
import { useAuth } from '../../../context/AuthContext';
import type { CreateBookingRequest, PublicService, PublicResource } from '../types';
import axiosInstance from '../../../services/axiosInstance';

type WizardStep = 1 | 2 | 3 | 4 | 5;

const STEP_LABELS: Record<WizardStep, string> = {
  1: 'Select Service',
  2: 'Select Resource',
  3: 'Select Date',
  4: 'Select Slot',
  5: 'Confirm',
};

const CustomerBookingWizardPage: React.FC = () => {
  const { tenant } = useParams<{ tenant: string }>();
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const { isAuthenticated, isInitializing } = useAuth();

  const [step, setStep] = useState<WizardStep>(1);
  const [services, setServices] = useState<PublicService[]>([]);
  const [resources, setResources] = useState<PublicResource[]>([]);
  const [servicesLoading, setServicesLoading] = useState(false);
  const [servicesError, setServicesError] = useState<string | null>(null);
  const [resourcesLoading, setResourcesLoading] = useState(false);
  const [resourcesError, setResourcesError] = useState<string | null>(null);
  const [submitLoading, setSubmitLoading] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [bookingReference, setBookingReference] = useState<string | null>(null);

  const [selectedService, setSelectedService] = useState<PublicService | null>(null);
  const [selectedResource, setSelectedResource] = useState<PublicResource | null>(null);
  const [selectedDate, setSelectedDate] = useState('');
  const [selectedSlot, setSelectedSlot] = useState<string | null>(null);
  const [customerNotes, setCustomerNotes] = useState('');
  const [restoredFromLogin, setRestoredFromLogin] = useState(false);

  const { slots, isLoading: slotsLoading, error: slotsError, clearError: clearSlotsError, fetchSlots } = useSlots();

  const today = new Date().toISOString().split('T')[0];

  useEffect(() => {
    const load = async () => {
      setServicesLoading(true);
      setServicesError(null);
      try {
        const result = await axiosInstance.get(`/public/${tenant}/services`);
        if (!result.isSuccess) {
          setServicesError(result.errors?.[0]?.message ?? 'Failed to load services.');
          return;
        }
        const loaded: PublicService[] = result.data ?? [];
        setServices(loaded);

        const preselectedServiceId = searchParams.get('serviceId');
        const preselectedResourceId = searchParams.get('resourceId');
        const preselectedDate = searchParams.get('date');
        const preselectedSlot = searchParams.get('slot');

        if (preselectedServiceId) {
          const matchedService = loaded.find(s => s.serviceId === preselectedServiceId);
          if (matchedService) {
            setSelectedService(matchedService);
            if (preselectedDate) setSelectedDate(preselectedDate);
            if (preselectedSlot) setSelectedSlot(preselectedSlot);

            if (preselectedResourceId && preselectedDate && preselectedSlot) {
              setRestoredFromLogin(true);
              setStep(5);
            } else {
              setStep(2);
            }
          }
        }
      } catch {
        setServicesError('Failed to load services.');
      } finally {
        setServicesLoading(false);
      }
    };
    load();
  }, [tenant]);

  useEffect(() => {
    if (!selectedService) return;

    const load = async () => {
      setResourcesLoading(true);
      setResourcesError(null);
      setResources([]);
      try {
        const result = await axiosInstance.get(
          `/public/${tenant}/services/${selectedService.serviceId}/resources`
        );
        if (!result.isSuccess) {
          setResourcesError(result.errors?.[0]?.message ?? 'Failed to load resources.');
          return;
        }
        const loaded: PublicResource[] = result.data ?? [];
        setResources(loaded);

        const preselectedResourceId = searchParams.get('resourceId');
        if (preselectedResourceId) {
          const matchedResource = loaded.find(r => r.resourceId === preselectedResourceId);
          if (matchedResource) {
            setSelectedResource(matchedResource);
          }
        }
      } catch {
        setResourcesError('Failed to load resources.');
      } finally {
        setResourcesLoading(false);
      }
    };
    load();
  }, [selectedService]);

  useEffect(() => {
    if (isInitializing) return;
    if (!isAuthenticated) return;
    if (!restoredFromLogin) return;
    if (!selectedService || !selectedResource || !selectedDate || !selectedSlot) return;
    if (bookingReference) return;

    submitBooking();
  }, [isAuthenticated, isInitializing, restoredFromLogin, selectedResource]);

  const handleSelectService = (service: PublicService) => {
    setSelectedService(service);
    setSelectedResource(null);
    setSelectedDate('');
    setSelectedSlot(null);
    setStep(2);
  };

  const handleSelectResource = (resource: PublicResource) => {
    setSelectedResource(resource);
    setSelectedDate('');
    setSelectedSlot(null);
    setStep(3);
  };

  const handleDateSubmit = () => {
    if (!selectedDate) return;
    setSelectedSlot(null);
    setStep(4);
    fetchSlots(selectedService!.serviceId, selectedResource!.resourceId, selectedDate, tenant!);
  };

  const handleSelectSlot = (slot: string) => {
    setSelectedSlot(slot);
    setStep(5);
  };

  const handleConfirm = () => {
    if (!selectedService || !selectedResource || !selectedDate || !selectedSlot) return;

    if (!isInitializing && !isAuthenticated) {
      const params = new URLSearchParams();
      params.set('serviceId', selectedService.serviceId);
      params.set('resourceId', selectedResource.resourceId);
      params.set('date', selectedDate);
      params.set('slot', selectedSlot);
      navigate(`/book/${tenant}/login?${params.toString()}`);
      return;
    }

    submitBooking();
  };

  const submitBooking = async () => {
    if (!selectedService || !selectedResource || !selectedDate || !selectedSlot) return;
    setSubmitLoading(true);
    setSubmitError(null);
    try {
      const request: CreateBookingRequest = {
        serviceId: selectedService.serviceId,
        resourceId: selectedResource.resourceId,
        scheduledDate: selectedDate,
        scheduledStartTime: selectedSlot,
        customerNotes: customerNotes || undefined,
      };
      const result = await axiosInstance.post('/booking', request);
      if (!result.isSuccess) {
        setSubmitError(result.errors?.[0]?.message ?? 'Failed to create booking.');
        return;
      }
      setBookingReference(result.data?.bookingReference ?? null);
    } catch {
      setSubmitError('Failed to create booking.');
    } finally {
      setSubmitLoading(false);
    }
  };

  if (bookingReference) {
    return (
      <div className="min-vh-100 bg-light d-flex align-items-center justify-content-center p-3">
        <div className="card border-0 shadow-sm p-4 text-center" style={{ maxWidth: 480, width: '100%' }}>
          <div className="rounded-circle bg-success bg-opacity-10 d-flex align-items-center justify-content-center mx-auto mb-3" style={{ width: 64, height: 64 }}>
            <i className="fas fa-check fa-xl text-success" />
          </div>
          <h5 className="fw-bold mb-2">Booking Confirmed</h5>
          <p className="text-muted small mb-3">Your booking has been placed. A confirmation email has been sent.</p>
          <div className="bg-light rounded p-3 mb-3">
            <div className="text-muted small mb-1">Booking Reference</div>
            <div className="fw-bold fs-5">{bookingReference}</div>
          </div>
          <div className="text-muted small">
            <div>{selectedService?.name}</div>
            <div>{selectedResource?.name}</div>
            <div>{selectedDate} at {selectedSlot}</div>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="min-vh-100 bg-light d-flex align-items-center justify-content-center p-3">
      <div style={{ maxWidth: 600, width: '100%' }}>

        <div className="text-center mb-4">
          <h4 className="fw-bold mb-1">Book an Appointment</h4>
          <p className="text-muted small mb-0">{tenant}</p>
        </div>

        <div className="d-flex justify-content-center gap-2 mb-4 flex-wrap">
          {([1, 2, 3, 4, 5] as WizardStep[]).map(s => (
            <div key={s} className="d-flex align-items-center gap-1">
              <div
                className="rounded-circle d-flex align-items-center justify-content-center fw-semibold"
                style={{
                  width: 28,
                  height: 28,
                  fontSize: 12,
                  background: step === s ? '#0d6efd' : step > s ? '#198754' : '#e9ecef',
                  color: step >= s ? '#fff' : '#6c757d',
                }}
              >
                {step > s ? <i className="fas fa-check" style={{ fontSize: 10 }} /> : s}
              </div>
              <span className="small text-muted d-none d-sm-inline">{STEP_LABELS[s]}</span>
              {s < 5 && <span className="text-muted small mx-1">›</span>}
            </div>
          ))}
        </div>

        {step === 1 && (
          <div className="card border-0 shadow-sm">
            <div className="card-header bg-white border-bottom py-3">
              <div className="fw-semibold">Select a Service</div>
            </div>
            <div className="card-body p-0">
              {servicesError && (
                <div className="p-3">
                  <Alert variant="error" dismissible onDismiss={() => setServicesError(null)}>{servicesError}</Alert>
                </div>
              )}
              {servicesLoading ? (
                <div className="p-4 text-center text-muted small">Loading services...</div>
              ) : services.length === 0 ? (
                <div className="p-4 text-center text-muted small">No services available.</div>
              ) : (
                services.map(service => (
                  <div
                    key={service.serviceId}
                    onClick={() => handleSelectService(service)}
                    className="px-4 py-3 border-bottom d-flex justify-content-between align-items-center"
                    style={{ cursor: 'pointer' }}
                    onMouseEnter={e => (e.currentTarget.style.background = '#f8f9fa')}
                    onMouseLeave={e => (e.currentTarget.style.background = '')}
                  >
                    <div>
                      <div className="fw-medium">{service.name}</div>
                      {service.description && (
                        <div className="text-muted small">{service.description}</div>
                      )}
                      <div className="text-muted small">{service.durationMinutes} min</div>
                    </div>
                    <div className="text-end">
                      <div className="fw-semibold text-primary">
                        {service.currencyCode} {service.price.toFixed(2)}
                      </div>
                      <i className="fas fa-chevron-right text-muted small" />
                    </div>
                  </div>
                ))
              )}
            </div>
          </div>
        )}

        {step === 2 && selectedService && (
          <div className="card border-0 shadow-sm">
            <div className="card-header bg-white border-bottom py-3 d-flex justify-content-between align-items-center">
              <div className="fw-semibold">Select a Resource</div>
              <button className="btn btn-sm btn-outline-secondary" onClick={() => setStep(1)}>Back</button>
            </div>
            <div className="card-body p-0">
              {resourcesError && (
                <div className="p-3">
                  <Alert variant="error" dismissible onDismiss={() => setResourcesError(null)}>{resourcesError}</Alert>
                </div>
              )}
              {resourcesLoading ? (
                <div className="p-4 text-center text-muted small">Loading resources...</div>
              ) : resources.length === 0 ? (
                <div className="p-4 text-center text-muted small">No resources available for this service.</div>
              ) : (
                resources.map(resource => (
                  <div
                    key={resource.resourceId}
                    onClick={() => handleSelectResource(resource)}
                    className="px-4 py-3 border-bottom d-flex justify-content-between align-items-center"
                    style={{ cursor: 'pointer' }}
                    onMouseEnter={e => (e.currentTarget.style.background = '#f8f9fa')}
                    onMouseLeave={e => (e.currentTarget.style.background = '')}
                  >
                    <div>
                      <div className="fw-medium">{resource.name}</div>
                      {resource.description && (
                        <div className="text-muted small">{resource.description}</div>
                      )}
                    </div>
                    <i className="fas fa-chevron-right text-muted small" />
                  </div>
                ))
              )}
            </div>
          </div>
        )}

        {step === 3 && selectedService && selectedResource && (
          <div className="card border-0 shadow-sm">
            <div className="card-header bg-white border-bottom py-3 d-flex justify-content-between align-items-center">
              <div className="fw-semibold">Select a Date</div>
              <button className="btn btn-sm btn-outline-secondary" onClick={() => setStep(2)}>Back</button>
            </div>
            <div className="card-body">
              <div className="mb-3">
                <label className="form-label small fw-medium mb-1">
                  Date <span className="text-danger">*</span>
                </label>
                <input
                  type="date"
                  className="form-control"
                  min={today}
                  value={selectedDate}
                  onChange={e => setSelectedDate(e.target.value)}
                />
              </div>
              <Button
                variant="primary"
                onClick={handleDateSubmit}
                disabled={!selectedDate}
                className="w-100"
              >
                Check Availability
              </Button>
            </div>
          </div>
        )}

        {step === 4 && (
          <div className="card border-0 shadow-sm">
            <div className="card-header bg-white border-bottom py-3 d-flex justify-content-between align-items-center">
              <div>
                <div className="fw-semibold">Select a Time Slot</div>
                <div className="text-muted small">{selectedDate}</div>
              </div>
              <button className="btn btn-sm btn-outline-secondary" onClick={() => setStep(3)}>Back</button>
            </div>
            <div className="card-body">
              {slotsError && (
                <Alert variant="error" dismissible onDismiss={clearSlotsError} className="mb-3">
                  {slotsError}
                </Alert>
              )}
              {slotsLoading ? (
                <div className="text-center text-muted small py-3">Loading available slots...</div>
              ) : slots && slots.availableSlots.length === 0 ? (
                <div className="text-center text-muted small py-3">
                  No slots available on this date. Please go back and select a different date.
                </div>
              ) : slots ? (
                <>
                  <div className="text-muted small mb-3">
                    {slots.availableSlots.length} slot{slots.availableSlots.length !== 1 ? 's' : ''} available · {slots.durationMinutes} min each
                  </div>
                  <div className="d-flex flex-wrap gap-2">
                    {slots.availableSlots.map(slot => (
                      <button
                        key={slot}
                        onClick={() => handleSelectSlot(slot)}
                        className="btn btn-outline-primary btn-sm"
                        style={{ minWidth: 80 }}
                      >
                        {slot}
                      </button>
                    ))}
                  </div>
                </>
              ) : null}
            </div>
          </div>
        )}

        {step === 5 && selectedService && selectedResource && selectedSlot && (
          <div className="card border-0 shadow-sm">
            <div className="card-header bg-white border-bottom py-3 d-flex justify-content-between align-items-center">
              <div className="fw-semibold">Confirm Booking</div>
              <button className="btn btn-sm btn-outline-secondary" onClick={() => setStep(4)}>Back</button>
            </div>
            <div className="card-body">
              {submitLoading && (
                <div className="text-center text-muted small py-3">Processing your booking...</div>
              )}
              {!submitLoading && (
                <>
                  {submitError && (
                    <Alert variant="error" dismissible onDismiss={() => setSubmitError(null)} className="mb-3">
                      {submitError}
                    </Alert>
                  )}
                  <div className="bg-light rounded p-3 mb-4">
                    {[
                      ['Service', selectedService.name],
                      ['Resource', selectedResource.name],
                      ['Date', selectedDate],
                      ['Time', selectedSlot],
                      ['Duration', `${selectedService.durationMinutes} min`],
                      ['Price', `${selectedService.currencyCode} ${selectedService.price.toFixed(2)}`],
                    ].map(([label, value]) => (
                      <div key={label} className="d-flex justify-content-between py-2 border-bottom">
                        <span className="text-muted small">{label}</span>
                        <span className="fw-medium small">{value}</span>
                      </div>
                    ))}
                  </div>
                  <div className="mb-4">
                    <label className="form-label small fw-medium mb-1">Notes (optional)</label>
                    <textarea
                      className="form-control"
                      rows={3}
                      placeholder="Any additional information for the provider..."
                      value={customerNotes}
                      onChange={e => setCustomerNotes(e.target.value)}
                    />
                  </div>
                  <Button
                    variant="primary"
                    loading={submitLoading}
                    onClick={handleConfirm}
                    className="w-100"
                  >
                    Confirm Booking
                  </Button>
                </>
              )}
            </div>
          </div>
        )}

      </div>
    </div>
  );
};

export default CustomerBookingWizardPage;