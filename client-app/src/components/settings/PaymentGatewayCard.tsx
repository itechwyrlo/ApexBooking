import { useEffect, useState, type FormEvent } from 'react'
import axios from 'axios'
import { Card } from '../common/Card'
import { Button } from '../common/Button'
import { FormGroup } from '../common/FormGroup'
import { Skeleton } from '../common/Skeleton'
import { usePaymentGatewayCredential } from '../../hooks/usePaymentGatewayCredential'
import { useToast } from '../../hooks/useToast'
import { configurePaymentGateway, setPaymentGatewayWebhookSecret } from '../../services/paymentGatewayService'
import type { IPaymentGatewayCredentialValues, IWebhookSecretValues } from '../../interfaces/IPaymentGatewayCredential'

const INITIAL_VALUES: IPaymentGatewayCredentialValues = {
  secretKey: '',
  publicKey: '',
}

const INITIAL_WEBHOOK_VALUES: IWebhookSecretValues = {
  webhookSecret: '',
}

interface IFormErrors {
  publicKey?: string
  secretKey?: string
}

interface IWebhookFormErrors {
  webhookSecret?: string
}

function validate(values: IPaymentGatewayCredentialValues): IFormErrors {
  const errors: IFormErrors = {}

  if (!values.publicKey.trim()) {
    errors.publicKey = 'Public key is required.'
  } else if (!values.publicKey.startsWith('pk_test_') && !values.publicKey.startsWith('pk_live_')) {
    errors.publicKey = "Public key must start with 'pk_test_' or 'pk_live_'."
  }

  if (!values.secretKey.trim()) {
    errors.secretKey = 'Secret key is required.'
  } else if (!values.secretKey.startsWith('sk_test_') && !values.secretKey.startsWith('sk_live_')) {
    errors.secretKey = "Secret key must start with 'sk_test_' or 'sk_live_'."
  }

  return errors
}

function validateWebhookSecret(values: IWebhookSecretValues): IWebhookFormErrors {
  const errors: IWebhookFormErrors = {}

  if (!values.webhookSecret.trim()) {
    errors.webhookSecret = 'Webhook signing secret is required.'
  } else if (!values.webhookSecret.startsWith('whsk_')) {
    errors.webhookSecret = "Webhook signing secret must start with 'whsk_'."
  }

  return errors
}

export function PaymentGatewayCard() {
  const { credential, isLoading, refetch } = usePaymentGatewayCredential()
  const { showToast } = useToast()
  const [values, setValues] = useState<IPaymentGatewayCredentialValues>(INITIAL_VALUES)
  const [errors, setErrors] = useState<IFormErrors>({})
  const [isSubmitting, setIsSubmitting] = useState(false)

  const [webhookValues, setWebhookValues] = useState<IWebhookSecretValues>(INITIAL_WEBHOOK_VALUES)
  const [webhookErrors, setWebhookErrors] = useState<IWebhookFormErrors>({})
  const [isSubmittingWebhook, setIsSubmittingWebhook] = useState(false)

  useEffect(() => {
    // Secret key never round-trips from the server — only the public key is safe to prefill.
    if (credential) {
      setValues({ secretKey: '', publicKey: credential.publicKey ?? '' })
    }
  }, [credential])

  const handleChange = (nextValues: IPaymentGatewayCredentialValues) => {
    setValues(nextValues)
    setErrors(validate(nextValues))
  }

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    const validationErrors = validate(values)
    setErrors(validationErrors)

    if (Object.keys(validationErrors).length > 0) {
      return
    }

    setIsSubmitting(true)
    try {
      await configurePaymentGateway(values)
      showToast('success', 'Payment gateway connected.')
      refetch()
    } catch (error) {
      const detail = axios.isAxiosError(error) ? (error.response?.data as { detail?: string } | undefined)?.detail : undefined
      showToast('error', detail ?? 'Failed to save payment gateway credentials. Please try again.')
    } finally {
      setIsSubmitting(false)
    }
  }

  const handleWebhookChange = (nextValues: IWebhookSecretValues) => {
    setWebhookValues(nextValues)
    setWebhookErrors(validateWebhookSecret(nextValues))
  }

  const handleWebhookSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    const validationErrors = validateWebhookSecret(webhookValues)
    setWebhookErrors(validationErrors)

    if (Object.keys(validationErrors).length > 0) {
      return
    }

    setIsSubmittingWebhook(true)
    try {
      await setPaymentGatewayWebhookSecret(webhookValues)
      showToast('success', 'Webhook signing secret saved.')
      setWebhookValues(INITIAL_WEBHOOK_VALUES)
      refetch()
    } catch (error) {
      const detail = axios.isAxiosError(error) ? (error.response?.data as { detail?: string } | undefined)?.detail : undefined
      showToast('error', detail ?? 'Failed to save webhook signing secret. Please try again.')
    } finally {
      setIsSubmittingWebhook(false)
    }
  }

  if (isLoading) {
    return (
      <Card className="mb-3">
        <Skeleton height="2.4rem" className="mb-3" />
        <Skeleton height="2.4rem" />
      </Card>
    )
  }

  return (
    <Card className="mb-3">
      <h5 className="mb-1">Payment Gateway (PayMongo)</h5>
      <p className="text-muted small mb-3">
        {credential?.isConfigured ? (
          <>
            Connected in <span className="fw-semibold">{credential.mode}</span> mode — secret key{' '}
            <span className="font-monospace">{credential.maskedSecretKey}</span>
          </>
        ) : (
          'Not connected. Add your PayMongo keys to accept online payments.'
        )}
      </p>

      <form noValidate onSubmit={handleSubmit}>
        <FormGroup label="Public Key" htmlFor="gatewayPublicKey" required error={errors.publicKey}>
          <input
            type="text"
            id="gatewayPublicKey"
            className={`form-control ${errors.publicKey ? 'is-invalid' : ''}`}
            placeholder="pk_test_..."
            value={values.publicKey}
            onChange={(e) => handleChange({ ...values, publicKey: e.target.value })}
          />
        </FormGroup>

        <FormGroup label="Secret Key" htmlFor="gatewaySecretKey" required error={errors.secretKey}>
          <input
            type="password"
            id="gatewaySecretKey"
            className={`form-control ${errors.secretKey ? 'is-invalid' : ''}`}
            placeholder="sk_test_..."
            value={values.secretKey}
            onChange={(e) => handleChange({ ...values, secretKey: e.target.value })}
            autoComplete="off"
          />
          <div className="form-text">Re-enter your secret key to update either value.</div>
        </FormGroup>

        <div className="d-flex justify-content-end">
          <Button type="submit" isLoading={isSubmitting}>
            {isSubmitting ? 'Saving...' : 'Save Payment Gateway'}
          </Button>
        </div>
      </form>

      {credential?.isConfigured && (
        <>
          <hr className="my-4" />

          <h6 className="mb-1">Webhook Signing Secret</h6>
          <p className="text-muted small mb-3">
            {credential.isWebhookConfigured
              ? 'Webhook configured — payments confirm automatically once PayMongo notifies us.'
              : "Not connected. Add the signing secret from your webhook's PayMongo dashboard page so payments can confirm automatically."}
          </p>

          <form noValidate onSubmit={handleWebhookSubmit}>
            <FormGroup label="Webhook Signing Secret" htmlFor="gatewayWebhookSecret" required error={webhookErrors.webhookSecret}>
              <input
                type="password"
                id="gatewayWebhookSecret"
                className={`form-control ${webhookErrors.webhookSecret ? 'is-invalid' : ''}`}
                placeholder="whsk_..."
                value={webhookValues.webhookSecret}
                onChange={(e) => handleWebhookChange({ webhookSecret: e.target.value })}
                autoComplete="off"
              />
            </FormGroup>

            <div className="d-flex justify-content-end">
              <Button type="submit" isLoading={isSubmittingWebhook}>
                {isSubmittingWebhook ? 'Saving...' : 'Save Webhook Secret'}
              </Button>
            </div>
          </form>
        </>
      )}
    </Card>
  )
}
