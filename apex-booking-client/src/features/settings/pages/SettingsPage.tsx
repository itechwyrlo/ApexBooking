import React, { useEffect, useState } from 'react';
import {
  faCalendarCheck,
  faFileInvoiceDollar,
  faCreditCard,
  faBuilding,
} from '@fortawesome/free-solid-svg-icons';
import { Alert } from '../../../components/ui/Alert';
import { Button } from '../../../components/ui/Button';
import { Tabs } from '../../../components/ui/Tabs';
import { usePaymentGateway } from '../hooks/usePaymentGateway';
import { usePaymentPolicy } from '../hooks/usePaymentPolicy';
import { useTenantSettings } from '../hooks/useTenantSettings';
import { useTenantProfile } from '../hooks/useTenantProfile';
import { useAuth } from '../../../context/AuthContext';
import type {
  BookingConfirmationMode,
  CancellationPolicy,
  DepositType,
  TimeFormat,
  UpdateTenantPaymentPolicyRequest,
  UpdateTenantSettingsRequest,
  UpdateTenantProfileRequest,
} from '../types';

type SettingsTab = 'booking' | 'policy' | 'gateway' | 'profile';

const SETTINGS_TABS = [
  { id: 'booking' as SettingsTab, label: 'Booking Settings', icon: faCalendarCheck },
  { id: 'policy' as SettingsTab, label: 'Payment Policy', icon: faFileInvoiceDollar },
  { id: 'gateway' as SettingsTab, label: 'Payment Gateway', icon: faCreditCard },
  { id: 'profile' as SettingsTab, label: 'Organization Profile', icon: faBuilding },
];

const SettingsPage: React.FC = () => {
  const { tenantSlug } = useAuth();

  const {
    loadGateway,
    gatewayStatus,
    isLoading: gatewayLoading,
    error: gatewayError,
    clearError: clearGatewayError,
  } = usePaymentGateway();

  const {
    settings,
    isLoading: settingsLoading,
    error: settingsError,
    clearError: clearSettingsError,
    load: loadSettings,
    update: updateSettings,
  } = useTenantSettings();

  const {
    policy,
    isLoading: policyLoading,
    error: policyError,
    clearError: clearPolicyError,
    load: loadPolicy,
    update: updatePolicy,
  } = usePaymentPolicy();

  const {
    profile,
    isLoading: profileLoading,
    error: profileError,
    clearError: clearProfileError,
    load: loadProfile,
    update: updateProfile,
  } = useTenantProfile(tenantSlug);

  const [activeTab, setActiveTab] = useState<SettingsTab>('booking');
  const [settingsForm, setSettingsForm] = useState<UpdateTenantSettingsRequest>({});
  const [policyForm, setPolicyForm] = useState<UpdateTenantPaymentPolicyRequest>({});
  const [profileForm, setProfileForm] = useState<UpdateTenantProfileRequest>({});
  const [settingsSuccess, setSettingsSuccess] = useState<string | null>(null);
  const [policySuccess, setPolicySuccess] = useState<string | null>(null);
  const [profileSuccess, setProfileSuccess] = useState<string | null>(null);

  useEffect(() => { loadSettings(); }, [loadSettings]);

  useEffect(() => {
    if (activeTab === 'policy' && !policy) loadPolicy();
    if (activeTab === 'gateway') loadGateway();
    if (activeTab === 'profile' && !profile) loadProfile();
  }, [activeTab, policy, loadPolicy, loadGateway, profile, loadProfile]);

  useEffect(() => {
    if (!settings) return;
    setSettingsForm({
      bookingConfirmationMode: settings.bookingConfirmationMode,
      minAdvanceBookingHours: settings.minAdvanceBookingHours,
      maxAdvanceBookingDays: settings.maxAdvanceBookingDays,
      cancellationCutoffHours: settings.cancellationCutoffHours,
      lateCancellationPolicy: settings.lateCancellationPolicy,
      guestBookingEnabled: settings.guestBookingEnabled,
      notifyBookingConfirmed: settings.notifyBookingConfirmed,
      notifyBookingCancelled: settings.notifyBookingCancelled,
      notifyBookingReminder: settings.notifyBookingReminder,
      notifyNewCustomer: settings.notifyNewCustomer,
      reminderHoursBefore: settings.reminderHoursBefore,
    });
  }, [settings]);

  useEffect(() => {
    if (!policy) return;
    setPolicyForm({
      paymentRequired: policy.paymentRequired,
      depositOnly: policy.depositOnly,
      depositType: policy.depositType,
      depositValue: policy.depositValue,
      refundPercent: policy.refundPercent,
    });
  }, [policy]);

  useEffect(() => {
    if (!profile) return;
    setProfileForm({
      logoUrl: profile.logoUrl ?? undefined,
      addressLine1: profile.addressLine1 ?? undefined,
      addressLine2: profile.addressLine2 ?? undefined,
      city: profile.city ?? undefined,
      state: profile.state ?? undefined,
      postalCode: profile.postalCode ?? undefined,
      countryCode: profile.countryCode ?? undefined,
      timezone: profile.timezone,
      currencyCode: profile.currencyCode,
      websiteUrl: profile.websiteUrl ?? undefined,
      contactEmail: profile.contactEmail ?? undefined,
      contactPhone: profile.contactPhone ?? undefined,
      dateFormat: profile.dateFormat,
      timeFormat: profile.timeFormat,
      languageCode: profile.languageCode,
    });
  }, [profile]);

  const handleSettingsChange = (field: keyof UpdateTenantSettingsRequest, value: unknown) => {
    setSettingsForm(prev => ({ ...prev, [field]: value }));
  };

  const handlePolicyChange = (field: keyof UpdateTenantPaymentPolicyRequest, value: unknown) => {
    setPolicyForm(prev => ({ ...prev, [field]: value }));
  };

  const handleProfileChange = (field: keyof UpdateTenantProfileRequest, value: unknown) => {
    setProfileForm(prev => ({ ...prev, [field]: value }));
  };

  const handleSettingsSubmit = async () => {
    const ok = await updateSettings(settingsForm);
    if (ok) setSettingsSuccess('Booking settings saved.');
  };

  const handlePolicySubmit = async () => {
    const ok = await updatePolicy(policyForm);
    if (ok) setPolicySuccess('Payment policy saved.');
  };

  const handleProfileSubmit = async () => {
    const ok = await updateProfile(profileForm);
    if (ok) setProfileSuccess('Profile saved.');
  };

  return (
    <div className="container-fluid px-3 px-md-4 py-4">
      <div className="row mb-4 align-items-center">
        <div className="col">
          <h5 className="fw-bold mb-0">Settings</h5>
          <small className="text-muted">Manage your booking rules and payment configuration.</small>
        </div>
      </div>

      <div className="card border-0 shadow-sm">
        <div className="card-body p-0">
          <div className="px-4 pt-4">
            <Tabs tabs={SETTINGS_TABS} activeTab={activeTab} onChange={(id) => setActiveTab(id)} />
          </div>

          {activeTab === 'booking' && (
            <div className="p-4 apex-settings-panel">
              <h6 className="fw-semibold mb-3">Booking Settings</h6>

              {settingsSuccess && (
                <Alert variant="success" dismissible onDismiss={() => setSettingsSuccess(null)} className="mb-3">
                  {settingsSuccess}
                </Alert>
              )}
              {settingsError && (
                <div className="alert alert-danger alert-dismissible d-flex align-items-center mb-3" role="alert">
                  {settingsError}
                  <button type="button" className="btn-close ms-auto" onClick={clearSettingsError} aria-label="Dismiss" />
                </div>
              )}

              {settingsLoading && !settings ? (
                <div className="text-muted small">Loading settings...</div>
              ) : (
                <>
                  <div className="mb-3">
                    <label className="form-label small fw-medium">Booking Confirmation Mode</label>
                    <select
                      className="form-select"
                      value={settingsForm.bookingConfirmationMode ?? 'Automatic'}
                      onChange={e => handleSettingsChange('bookingConfirmationMode', e.target.value as BookingConfirmationMode)}
                    >
                      <option value="Automatic">Automatic — confirmed immediately</option>
                      <option value="Manual">Manual — requires staff approval</option>
                    </select>
                  </div>

                  <div className="mb-3">
                    <label className="form-label small fw-medium">Min Advance Booking (hours)</label>
                    <input
                      type="number"
                      className="form-control"
                      min={0}
                      value={settingsForm.minAdvanceBookingHours ?? ''}
                      onChange={e => handleSettingsChange('minAdvanceBookingHours', parseInt(e.target.value))}
                    />
                  </div>

                  <div className="mb-3">
                    <label className="form-label small fw-medium">Max Advance Booking (days)</label>
                    <input
                      type="number"
                      className="form-control"
                      min={1}
                      value={settingsForm.maxAdvanceBookingDays ?? ''}
                      onChange={e => handleSettingsChange('maxAdvanceBookingDays', parseInt(e.target.value))}
                    />
                  </div>

                  <div className="mb-3">
                    <label className="form-label small fw-medium">Cancellation Cutoff (hours)</label>
                    <input
                      type="number"
                      className="form-control"
                      min={0}
                      value={settingsForm.cancellationCutoffHours ?? ''}
                      onChange={e => handleSettingsChange('cancellationCutoffHours', parseInt(e.target.value))}
                    />
                  </div>

                  <div className="mb-3">
                    <label className="form-label small fw-medium">Late Cancellation Policy</label>
                    <select
                      className="form-select"
                      value={settingsForm.lateCancellationPolicy ?? ''}
                      onChange={e => handleSettingsChange('lateCancellationPolicy', e.target.value as CancellationPolicy)}
                    >
                      <option value="NoRefund">No Refund</option>
                      <option value="PartialRefund">Partial Refund</option>
                      <option value="FullRefund">Full Refund</option>
                    </select>
                  </div>

                  <div className="mb-3">
                    <label className="form-label small fw-medium">Reminder Hours Before</label>
                    <input
                      type="number"
                      className="form-control"
                      min={1}
                      value={settingsForm.reminderHoursBefore ?? ''}
                      onChange={e => handleSettingsChange('reminderHoursBefore', parseInt(e.target.value))}
                    />
                  </div>

                  <div className="mb-4">
                    <label className="form-label small fw-medium d-block mb-2">Notifications</label>
                    {([
                      ['notifyBookingConfirmed', 'Booking Confirmed'],
                      ['notifyBookingCancelled', 'Booking Cancelled'],
                      ['notifyBookingReminder', 'Booking Reminder'],
                      ['notifyNewCustomer', 'New Customer Registered'],
                    ] as [keyof UpdateTenantSettingsRequest, string][]).map(([field, label]) => (
                      <div className="form-check mb-2" key={field}>
                        <input
                          type="checkbox"
                          className="form-check-input"
                          id={field}
                          checked={(settingsForm[field] as boolean) ?? false}
                          onChange={e => handleSettingsChange(field, e.target.checked)}
                        />
                        <label className="form-check-label small" htmlFor={field}>{label}</label>
                      </div>
                    ))}
                  </div>

                  <Button variant="primary" onClick={handleSettingsSubmit} loading={settingsLoading}>
                    Save Settings
                  </Button>
                </>
              )}
            </div>
          )}

          {activeTab === 'policy' && (
            <div className="p-4 apex-settings-panel">
              <h6 className="fw-semibold mb-1">Payment Policy</h6>
              <p className="text-muted small mb-4">
                Define when and how much to charge. The platform handles gateway credentials separately.
              </p>

              {policySuccess && (
                <Alert variant="success" dismissible onDismiss={() => setPolicySuccess(null)} className="mb-3">
                  {policySuccess}
                </Alert>
              )}
              {policyError && (
                <div className="alert alert-danger alert-dismissible d-flex align-items-center mb-3" role="alert">
                  {policyError}
                  <button type="button" className="btn-close ms-auto" onClick={clearPolicyError} aria-label="Dismiss" />
                </div>
              )}

              {policyLoading && !policy ? (
                <div className="text-muted small">Loading policy...</div>
              ) : (
                <>
                  <div className="mb-4 p-3 border rounded">
                    <div className="form-check form-switch mb-0">
                      <input
                        type="checkbox"
                        className="form-check-input"
                        role="switch"
                        id="paymentRequired"
                        checked={policyForm.paymentRequired ?? false}
                        onChange={e => handlePolicyChange('paymentRequired', e.target.checked)}
                      />
                      <label className="form-check-label fw-medium" htmlFor="paymentRequired">
                        Require payment at booking
                      </label>
                    </div>
                    <div className="text-muted small mt-1">
                      When enabled, customers must pay online to confirm their booking.
                    </div>
                  </div>

                  {policyForm.paymentRequired && (
                    <div className="mb-4 p-3 border rounded">
                      <div className="form-check form-switch mb-2">
                        <input
                          type="checkbox"
                          className="form-check-input"
                          role="switch"
                          id="depositOnly"
                          checked={policyForm.depositOnly ?? false}
                          onChange={e => handlePolicyChange('depositOnly', e.target.checked)}
                        />
                        <label className="form-check-label fw-medium" htmlFor="depositOnly">
                          Charge deposit only
                        </label>
                      </div>
                      <div className="text-muted small mb-3">
                        Collect a partial amount at booking. The remainder is paid later.
                      </div>

                      {policyForm.depositOnly && (
                        <div className="row g-2">
                          <div className="col-12 col-sm-5">
                            <label className="form-label small fw-medium">Deposit Type</label>
                            <select
                              className="form-select form-select-sm"
                              value={policyForm.depositType ?? 'Percentage'}
                              onChange={e => handlePolicyChange('depositType', e.target.value as DepositType)}
                            >
                              <option value="Percentage">Percentage (%)</option>
                              <option value="FixedAmount">Fixed Amount</option>
                            </select>
                          </div>
                          <div className="col-12 col-sm-7">
                            <label className="form-label small fw-medium">
                              {policyForm.depositType === 'FixedAmount' ? 'Amount' : 'Percentage'}
                            </label>
                            <div className="input-group input-group-sm">
                              {policyForm.depositType === 'FixedAmount' && (
                                <span className="input-group-text">$</span>
                              )}
                              <input
                                type="number"
                                className="form-control"
                                min={0}
                                max={policyForm.depositType === 'Percentage' ? 100 : undefined}
                                step={policyForm.depositType === 'Percentage' ? 1 : 0.01}
                                value={policyForm.depositValue ?? ''}
                                onChange={e => handlePolicyChange('depositValue', parseFloat(e.target.value))}
                              />
                              {(policyForm.depositType ?? 'Percentage') === 'Percentage' && (
                                <span className="input-group-text">%</span>
                              )}
                            </div>
                          </div>
                        </div>
                      )}
                    </div>
                  )}

                  <div className="mb-4">
                    <label className="form-label small fw-medium">Partial Refund Percentage</label>
                    <div className="input-group apex-refund-input">
                      <input
                        type="number"
                        className="form-control"
                        min={0}
                        max={100}
                        value={policyForm.refundPercent ?? ''}
                        onChange={e => handlePolicyChange('refundPercent', parseFloat(e.target.value))}
                      />
                      <span className="input-group-text">%</span>
                    </div>
                    <div className="text-muted small mt-1">
                      Applied when Late Cancellation Policy is set to Partial Refund.
                    </div>
                  </div>

                  <Button variant="primary" onClick={handlePolicySubmit} loading={policyLoading}>
                    Save Policy
                  </Button>
                </>
              )}
            </div>
          )}

          {activeTab === 'gateway' && (
            <div className="p-4 apex-settings-panel">
              <h6 className="fw-semibold mb-3">Payment Gateway</h6>

              {gatewayError && (
                <div className="alert alert-danger alert-dismissible d-flex align-items-center mb-3" role="alert">
                  {gatewayError}
                  <button type="button" className="btn-close ms-auto" onClick={clearGatewayError} aria-label="Dismiss" />
                </div>
              )}

              {gatewayLoading && !gatewayStatus ? (
                <div className="text-muted small">Loading gateway settings...</div>
              ) : !gatewayStatus ? (
                <Alert variant="info">
                  No payment gateway has been configured. Contact your platform administrator.
                </Alert>
              ) : (
                <>
                  <Alert variant="info" className="mb-4">
                    Gateway credentials are managed by the platform administrator. Contact them to change providers or keys.
                  </Alert>

                  <div className="mb-3">
                    <div className="small fw-medium text-muted mb-1">Provider</div>
                    <div className="fw-semibold">{gatewayStatus.gatewayProvider ?? '—'}</div>
                  </div>

                  <div className="mb-3">
                    <div className="small fw-medium text-muted mb-1">Mode</div>
                    <span className={`badge ${gatewayStatus.mode === 'Live' ? 'bg-success' : 'bg-warning text-dark'}`}>
                      {gatewayStatus.mode ?? '—'}
                    </span>
                  </div>

                  <div className="mb-3">
                    <div className="small fw-medium text-muted mb-1">Status</div>
                    <span className={`badge ${gatewayStatus.isActive ? 'bg-success' : 'bg-secondary'}`}>
                      {gatewayStatus.isActive ? 'Active' : 'Inactive'}
                    </span>
                  </div>

                  <div className="mb-3">
                    <div className="small fw-medium text-muted mb-1">Validated</div>
                    <div className="fw-semibold">
                      {gatewayStatus.validatedAt
                        ? new Date(gatewayStatus.validatedAt).toLocaleString()
                        : '—'}
                    </div>
                  </div>
                </>
              )}
            </div>
          )}

          {activeTab === 'profile' && (
            <div className="p-4 apex-settings-panel">
              <h6 className="fw-semibold mb-3">Organization Profile</h6>

              {profileSuccess && (
                <Alert variant="success" dismissible onDismiss={() => setProfileSuccess(null)} className="mb-3">
                  {profileSuccess}
                </Alert>
              )}
              {profileError && (
                <div className="alert alert-danger alert-dismissible d-flex align-items-center mb-3" role="alert">
                  {profileError}
                  <button type="button" className="btn-close ms-auto" onClick={clearProfileError} aria-label="Dismiss" />
                </div>
              )}

              {profileLoading && !profile ? (
                <div className="text-muted small">Loading profile...</div>
              ) : (
                <>
                  <div className="mb-3">
                    <label className="form-label small fw-medium">Timezone</label>
                    <select
                      className="form-select"
                      value={profileForm.timezone ?? ''}
                      onChange={e => handleProfileChange('timezone', e.target.value)}
                    >
                      <option value="">Select timezone</option>
                      <option value="Asia/Manila">Asia/Manila (UTC+8)</option>
                      <option value="Asia/Singapore">Asia/Singapore (UTC+8)</option>
                      <option value="Asia/Tokyo">Asia/Tokyo (UTC+9)</option>
                      <option value="Asia/Shanghai">Asia/Shanghai (UTC+8)</option>
                      <option value="Asia/Kolkata">Asia/Kolkata (UTC+5:30)</option>
                      <option value="Asia/Dubai">Asia/Dubai (UTC+4)</option>
                      <option value="Australia/Sydney">Australia/Sydney (UTC+10/11)</option>
                      <option value="Europe/London">Europe/London (UTC+0/1)</option>
                      <option value="Europe/Paris">Europe/Paris (UTC+1/2)</option>
                      <option value="America/New_York">America/New_York (UTC-5/4)</option>
                      <option value="America/Chicago">America/Chicago (UTC-6/5)</option>
                      <option value="America/Los_Angeles">America/Los_Angeles (UTC-8/7)</option>
                      <option value="UTC">UTC</option>
                    </select>
                    <div className="text-muted small mt-1">
                      All booking times are calculated based on this timezone.
                    </div>
                  </div>

                  <div className="mb-3">
                    <label className="form-label small fw-medium">Currency</label>
                    <select
                      className="form-select"
                      value={profileForm.currencyCode ?? ''}
                      onChange={e => handleProfileChange('currencyCode', e.target.value)}
                    >
                      <option value="PHP">PHP - Philippine Peso</option>
                      <option value="USD">USD - US Dollar</option>
                      <option value="EUR">EUR - Euro</option>
                      <option value="GBP">GBP - British Pound</option>
                      <option value="SGD">SGD - Singapore Dollar</option>
                      <option value="AUD">AUD - Australian Dollar</option>
                      <option value="JPY">JPY - Japanese Yen</option>
                    </select>
                  </div>

                  <div className="mb-3">
                    <label className="form-label small fw-medium">Time Format</label>
                    <select
                      className="form-select"
                      value={profileForm.timeFormat ?? '12h'}
                      onChange={e => handleProfileChange('timeFormat', e.target.value as TimeFormat)}
                    >
                      <option value="12h">12-hour (1:00 PM)</option>
                      <option value="24h">24-hour (13:00)</option>
                    </select>
                  </div>

                  <div className="mb-3">
                    <label className="form-label small fw-medium">Address Line 1</label>
                    <input
                      type="text"
                      className="form-control"
                      value={profileForm.addressLine1 ?? ''}
                      onChange={e => handleProfileChange('addressLine1', e.target.value)}
                    />
                  </div>

                  <div className="mb-3">
                    <label className="form-label small fw-medium">Address Line 2</label>
                    <input
                      type="text"
                      className="form-control"
                      value={profileForm.addressLine2 ?? ''}
                      onChange={e => handleProfileChange('addressLine2', e.target.value)}
                    />
                  </div>

                  <div className="row g-2 mb-3">
                    <div className="col-12 col-sm-6">
                      <label className="form-label small fw-medium">City</label>
                      <input
                        type="text"
                        className="form-control"
                        value={profileForm.city ?? ''}
                        onChange={e => handleProfileChange('city', e.target.value)}
                      />
                    </div>
                    <div className="col-12 col-sm-6">
                      <label className="form-label small fw-medium">State</label>
                      <input
                        type="text"
                        className="form-control"
                        value={profileForm.state ?? ''}
                        onChange={e => handleProfileChange('state', e.target.value)}
                      />
                    </div>
                  </div>

                  <div className="row g-2 mb-3">
                    <div className="col-12 col-sm-6">
                      <label className="form-label small fw-medium">Postal Code</label>
                      <input
                        type="text"
                        className="form-control"
                        value={profileForm.postalCode ?? ''}
                        onChange={e => handleProfileChange('postalCode', e.target.value)}
                      />
                    </div>
                    <div className="col-12 col-sm-6">
                      <label className="form-label small fw-medium">Country Code</label>
                      <input
                        type="text"
                        className="form-control"
                        maxLength={2}
                        placeholder="PH"
                        value={profileForm.countryCode ?? ''}
                        onChange={e => handleProfileChange('countryCode', e.target.value.toUpperCase())}
                      />
                    </div>
                  </div>

                  <div className="mb-3">
                    <label className="form-label small fw-medium">Contact Email</label>
                    <input
                      type="email"
                      className="form-control"
                      value={profileForm.contactEmail ?? ''}
                      onChange={e => handleProfileChange('contactEmail', e.target.value)}
                    />
                  </div>

                  <div className="mb-3">
                    <label className="form-label small fw-medium">Contact Phone</label>
                    <input
                      type="text"
                      className="form-control"
                      value={profileForm.contactPhone ?? ''}
                      onChange={e => handleProfileChange('contactPhone', e.target.value)}
                    />
                  </div>

                  <div className="mb-4">
                    <label className="form-label small fw-medium">Website</label>
                    <input
                      type="text"
                      className="form-control"
                      value={profileForm.websiteUrl ?? ''}
                      onChange={e => handleProfileChange('websiteUrl', e.target.value)}
                    />
                  </div>

                  <Button variant="primary" onClick={handleProfileSubmit} loading={profileLoading}>
                    Save Profile
                  </Button>
                </>
              )}
            </div>
          )}

        </div>
      </div>
    </div>
  );
};

export default SettingsPage;
