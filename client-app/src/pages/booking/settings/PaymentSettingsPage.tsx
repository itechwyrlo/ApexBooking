import { useEffect, useState, type FormEvent } from 'react'
import axios from 'axios'
import { PageHeader } from '../../../components/common/PageHeader'
import { Card } from '../../../components/common/Card'
import { Button } from '../../../components/common/Button'
import { FormGroup } from '../../../components/common/FormGroup'
import { NumberInput } from '../../../components/common/NumberInput'
import { Skeleton } from '../../../components/common/Skeleton'
import { PaymentGatewayCard } from '../../../components/settings/PaymentGatewayCard'
import { usePaymentPolicy } from '../../../hooks/usePaymentPolicy'
import { useAuth } from '../../../hooks/useAuth'
import { useToast } from '../../../hooks/useToast'
import { updatePaymentPolicy } from '../../../services/paymentPolicyService'
import { PaymentRequirementType } from '../../../types/PaymentRequirementType'
import { DepositType } from '../../../types/DepositType'
import { Role } from '../../../types/Role'
import type { IPaymentPolicy } from '../../../interfaces/IPaymentPolicy'

const DEFAULT_VALUES: IPaymentPolicy = {
  requirementType: PaymentRequirementType.None,
  depositType: DepositType.Percentage,
  depositValue: 0,
  onTimeRefundPercent: 100,
  lateCancellationRefundPercent: 0,
  refundReviewDeadlineDays: 7,
  refundEnabled: false,
}

interface IFormErrors {
  depositValue?: string
  onTimeRefundPercent?: string
  lateCancellationRefundPercent?: string
  refundReviewDeadlineDays?: string
}

function validate(values: IPaymentPolicy): IFormErrors {
  const errors: IFormErrors = {}

  if (values.requirementType !== PaymentRequirementType.None) {
    if (values.depositValue < 0) {
      errors.depositValue = 'Deposit value cannot be a negative amount.'
    } else if (values.depositType === DepositType.Percentage && values.depositValue > 100) {
      errors.depositValue = 'A percentage-based deposit requirement cannot exceed 100%.'
    }
  }

  if (values.onTimeRefundPercent < 0 || values.onTimeRefundPercent > 100) {
    errors.onTimeRefundPercent = 'On-time refund percentage must be between 0% and 100%.'
  }

  if (values.lateCancellationRefundPercent < 0 || values.lateCancellationRefundPercent > 100) {
    errors.lateCancellationRefundPercent = 'Late cancellation refund percentage must be between 0% and 100%.'
  }

  if (values.refundReviewDeadlineDays < 1) {
    errors.refundReviewDeadlineDays = 'Refund review deadline must be at least 1 day.'
  }

  return errors
}

export function PaymentSettingsPage() {
  const { policy, isLoading, refetch } = usePaymentPolicy()
  const { user } = useAuth()
  const isOwner = user !== null && user.roles.includes(Role.Owner)
  const { showToast } = useToast()
  const [values, setValues] = useState<IPaymentPolicy>(DEFAULT_VALUES)
  const [errors, setErrors] = useState<IFormErrors>({})
  const [isSubmitting, setIsSubmitting] = useState(false)

  useEffect(() => {
    if (policy) {
      setValues(policy)
    }
  }, [policy])

  const requiresDeposit = values.requirementType !== PaymentRequirementType.None

  const handleChange = (nextValues: IPaymentPolicy) => {
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
      await updatePaymentPolicy(values)
      showToast('success', 'Payment settings updated.')
      refetch()
    } catch (error) {
      const detail = axios.isAxiosError(error) ? (error.response?.data as { detail?: string } | undefined)?.detail : undefined
      showToast('error', detail ?? 'Failed to update payment settings. Please try again.')
    } finally {
      setIsSubmitting(false)
    }
  }

  if (isLoading) {
    return (
      <Card>
        <Skeleton height="2.4rem" className="mb-3" />
        <Skeleton height="2.4rem" className="mb-3" />
        <Skeleton height="2.4rem" />
      </Card>
    )
  }

  return (
    <div>
      <PageHeader
        title="Payment Settings"
        description="Connect a payment method to accept deposits and online payments."
      />
      {isOwner && <PaymentGatewayCard />}
      <Card>
        <form noValidate onSubmit={handleSubmit}>
          <FormGroup label="Payment Requirement" htmlFor="requirementType">
            <select
              id="requirementType"
              className="form-select"
              value={values.requirementType}
              onChange={(e) => handleChange({ ...values, requirementType: e.target.value as IPaymentPolicy['requirementType'] })}
            >
              <option value={PaymentRequirementType.None}>No payment required at booking</option>
              <option value={PaymentRequirementType.DepositRequired}>Deposit required</option>
              <option value={PaymentRequirementType.FullPaymentRequired}>Full payment required</option>
            </select>
          </FormGroup>

          {requiresDeposit && (
            <div className="row">
              <div className="col-sm-6">
                <FormGroup label="Deposit Type" htmlFor="depositType">
                  <select
                    id="depositType"
                    className="form-select"
                    value={values.depositType}
                    onChange={(e) => handleChange({ ...values, depositType: e.target.value as IPaymentPolicy['depositType'] })}
                  >
                    <option value={DepositType.Percentage}>Percentage</option>
                    <option value={DepositType.FixedAmount}>Fixed Amount</option>
                  </select>
                </FormGroup>
              </div>
              <div className="col-sm-6">
                <FormGroup
                  label={values.depositType === DepositType.Percentage ? 'Deposit Value (%)' : 'Deposit Value'}
                  htmlFor="depositValue"
                  error={errors.depositValue}
                >
                  <NumberInput
                    id="depositValue"
                    min={0}
                    decimals={2}
                    isInvalid={!!errors.depositValue}
                    value={values.depositValue}
                    onChange={(value) => handleChange({ ...values, depositValue: value })}
                  />
                </FormGroup>
              </div>
            </div>
          )}

          <FormGroup label="Enable Refunds" htmlFor="refundEnabled">
            <div className="form-check form-switch">
              <input
                type="checkbox"
                role="switch"
                id="refundEnabled"
                className="form-check-input"
                checked={values.refundEnabled}
                onChange={(e) => handleChange({ ...values, refundEnabled: e.target.checked })}
              />
              <label className="form-check-label" htmlFor="refundEnabled">
                {values.refundEnabled ? 'On — cancelled online payments can be refunded' : 'Off — cancellations never trigger a refund'}
              </label>
            </div>
            <div className="form-text">
              When off (default), cancelling an online-paid booking never creates a refund request — no matter the timing or policy below.
            </div>
          </FormGroup>

          {values.refundEnabled && (
            <>
              <div className="row">
                <div className="col-sm-6">
                  <FormGroup label="On-Time Refund (%)" htmlFor="onTimeRefundPercent" error={errors.onTimeRefundPercent}>
                    <NumberInput
                      id="onTimeRefundPercent"
                      min={0}
                      max={100}
                      decimals={2}
                      isInvalid={!!errors.onTimeRefundPercent}
                      value={values.onTimeRefundPercent}
                      onChange={(value) => handleChange({ ...values, onTimeRefundPercent: value })}
                    />
                    <div className="form-text">How much is refunded for an on-time cancellation. Defaults to 100% — lower it to retain an admin fee.</div>
                  </FormGroup>
                </div>
                <div className="col-sm-6">
                  <FormGroup label="Late Cancellation Refund (%)" htmlFor="lateCancellationRefundPercent" error={errors.lateCancellationRefundPercent}>
                    <NumberInput
                      id="lateCancellationRefundPercent"
                      min={0}
                      max={100}
                      decimals={2}
                      isInvalid={!!errors.lateCancellationRefundPercent}
                      value={values.lateCancellationRefundPercent}
                      onChange={(value) => handleChange({ ...values, lateCancellationRefundPercent: value })}
                    />
                    <div className="form-text">
                      Only used when Late Cancellation Policy (Booking Settings) is "Partial Refund." Defaults to 0%.
                    </div>
                  </FormGroup>
                </div>
              </div>

              <FormGroup label="Refund Review Deadline (days)" htmlFor="refundReviewDeadlineDays" error={errors.refundReviewDeadlineDays}>
                <NumberInput
                  id="refundReviewDeadlineDays"
                  min={1}
                  decimals={0}
                  isInvalid={!!errors.refundReviewDeadlineDays}
                  value={values.refundReviewDeadlineDays}
                  onChange={(value) => handleChange({ ...values, refundReviewDeadlineDays: value })}
                />
                <div className="form-text">How many days staff have to review a pending refund before it's flagged as overdue.</div>
              </FormGroup>
            </>
          )}

          <div className="d-flex justify-content-end mt-4">
            <Button type="submit" isLoading={isSubmitting}>
              {isSubmitting ? 'Saving...' : 'Save Changes'}
            </Button>
          </div>
        </form>
      </Card>
    </div>
  )
}
