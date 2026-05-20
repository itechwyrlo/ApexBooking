import React, { useState, useEffect } from 'react';
import { Alert } from '../../../components/ui/Alert';
import { Button } from '../../../components/ui/Button';
import { usePlatformPaymentGateway } from '../hooks/usePlatformPaymentGateway';
import type { ConfigurePlatformPaymentGatewayRequest, GatewayProvider, GatewayMode } from '../types';

interface GatewayFormState {
  gatewayProvider: GatewayProvider;
  clientId: string;
  secretKey: string;
  webhookId: string;
  mode: GatewayMode;
}

const EMPTY_FORM: GatewayFormState = {
  gatewayProvider: 'PayPal',
  clientId: '',
  secretKey: '',
  webhookId: '',
  mode: 'Test',
};

const SuperAdminPaymentGatewayPage: React.FC = () => {
  const { loadGateway, configure, gateway, prefill, isLoading, error, clearError } =
    usePlatformPaymentGateway();

  const [form, setForm] = useState<GatewayFormState>(EMPTY_FORM);
  const [showSecret, setShowSecret] = useState(false);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  useEffect(() => {
    loadGateway();
  }, [loadGateway]);

  // When the initial load completes, pre-populate provider and mode from existing config.
  // Credential fields always start empty regardless.
  useEffect(() => {
    if (gateway !== undefined) {
      setForm(prev => ({
        ...prev,
        gatewayProvider: prefill.gatewayProvider,
        mode: prefill.mode,
      }));
    }
  }, [gateway, prefill]);

  const handleChange = (field: keyof GatewayFormState, value: string) => {
    setForm(prev => ({ ...prev, [field]: value }));
    setSuccessMessage(null);
  };

  const handleSubmit = async () => {
    const request: ConfigurePlatformPaymentGatewayRequest = {
      gatewayProvider: form.gatewayProvider,
      clientId: form.clientId,
      secretKey: form.secretKey,
      webhookId: form.webhookId,
      mode: form.mode,
    };
    const ok = await configure(request);
    if (ok) {
      setSuccessMessage('Payment gateway configured and validated successfully.');
      setForm(prev => ({ ...prev, clientId: '', secretKey: '', webhookId: '' }));
    }
  };

  const isSubmitDisabled =
    !form.clientId.trim() || !form.secretKey.trim() || !form.webhookId.trim();

  if (gateway === undefined) {
    return (
      <div className="container-fluid">
        <div className="mb-4">
          <h4 className="fw-bold mb-1">Platform Payment Gateway</h4>
          <div className="text-muted small">Configure the platform-level payment gateway credentials.</div>
        </div>
        <div className="card border-0 shadow-sm p-4">
          <div className="text-muted small">Loading gateway settings...</div>
        </div>
      </div>
    );
  }

  return (
    <div className="container-fluid">
      <div className="mb-4">
        <h4 className="fw-bold mb-1">Platform Payment Gateway</h4>
        <div className="text-muted small">Configure the platform-level payment gateway credentials.</div>
      </div>

      <div className="card border-0 shadow-sm">
        <div className="card-body p-4 sa-gateway-form">
          {successMessage && (
            <Alert variant="success" dismissible onDismiss={() => setSuccessMessage(null)} className="mb-3">
              {successMessage}
            </Alert>
          )}

          {error && (
            <Alert variant="error" dismissible onDismiss={clearError} className="mb-3">
              {error}
            </Alert>
          )}

          {gateway === null && (
            <Alert variant="info" className="mb-4">
              No gateway configured yet. Enter credentials below to configure the platform payment gateway.
            </Alert>
          )}

          <div className="mb-3">
            <label className="form-label small fw-medium">Gateway Provider</label>
            <select
              className="form-select"
              value={form.gatewayProvider}
              onChange={e => handleChange('gatewayProvider', e.target.value as GatewayProvider)}
            >
              <option value="PayPal">PayPal</option>
              <option value="Stripe">Stripe</option>
              <option value="PayMongo">PayMongo</option>
            </select>
          </div>

          <div className="mb-3">
            <label className="form-label small fw-medium">Client ID</label>
            <input
              type="text"
              className="form-control"
              value={form.clientId}
              onChange={e => handleChange('clientId', e.target.value)}
              placeholder="Enter client ID"
            />
          </div>

          <div className="mb-3">
            <label className="form-label small fw-medium">Secret Key</label>
            <div className="input-group">
              <input
                type={showSecret ? 'text' : 'password'}
                className="form-control"
                value={form.secretKey}
                onChange={e => handleChange('secretKey', e.target.value)}
                placeholder="Enter secret key"
              />
              <button
                className="btn btn-outline-secondary"
                type="button"
                onClick={() => setShowSecret(prev => !prev)}
              >
                <i className={`fas ${showSecret ? 'fa-eye-slash' : 'fa-eye'}`} />
              </button>
            </div>
          </div>

          <div className="mb-3">
            <label className="form-label small fw-medium">Webhook ID</label>
            <input
              type="text"
              className="form-control"
              value={form.webhookId}
              onChange={e => handleChange('webhookId', e.target.value)}
              placeholder="Enter webhook ID"
            />
          </div>

          <div className="mb-4">
            <label className="form-label small fw-medium">Mode</label>
            <select
              className="form-select"
              value={form.mode}
              onChange={e => handleChange('mode', e.target.value as GatewayMode)}
            >
              <option value="Test">Test</option>
              <option value="Live">Live</option>
            </select>
          </div>

          <Button
            variant="primary"
            onClick={handleSubmit}
            loading={isLoading}
            disabled={isSubmitDisabled}
          >
            Save and Validate
          </Button>
        </div>
      </div>
    </div>
  );
};

export default SuperAdminPaymentGatewayPage;
