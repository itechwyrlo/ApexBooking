import { useEffect, useState, type FormEvent } from 'react'
import axios from 'axios'
import { Modal } from '../common/Modal'
import { FormGroup } from '../common/FormGroup'
import { FormSection } from '../common/FormSection'
import { Button } from '../common/Button'
import { NumberInput } from '../common/NumberInput'
import { MultiSelectCombobox, type IMultiSelectOption } from '../common/MultiSelectCombobox'
import { CollapsibleField } from '../common/CollapsibleField'
import { useToast } from '../../hooks/useToast'
import { useBookingPolicy } from '../../hooks/useBookingPolicy'
import { useServiceStaff } from '../../hooks/useServiceStaff'
import { useTeamMembers } from '../../hooks/useTeamMembers'
import { createService, updateService, assignStaffToService, unassignStaffFromService } from '../../services/serviceService'
import { isRequired } from '../../utils/validators'
import { TenantMemberStatus } from '../../types/TenantMemberStatus'
import type { IService, IServiceFormValues } from '../../interfaces/IService'

const CURRENCY_OPTIONS = ['PHP', 'USD']

const EMPTY_VALUES: IServiceFormValues = {
  name: '',
  description: '',
  durationMinutes: 30,
  price: 0,
  currencyCode: 'PHP',
  bufferBeforeMinutes: 0,
  bufferAfterMinutes: 0,
  minAdvanceBookingHoursOverride: null,
}

type ServiceField = keyof IServiceFormValues
type IFormErrors = Partial<Record<ServiceField, string>>
type IFormTouched = Partial<Record<ServiceField, boolean>>

const ALL_FIELDS_TOUCHED: IFormTouched = { name: true, durationMinutes: true, price: true, currencyCode: true }

function validate(values: IServiceFormValues): IFormErrors {
  const errors: IFormErrors = {}

  if (!isRequired(values.name)) {
    errors.name = 'Service name is required.'
  }
  if (values.durationMinutes <= 0) {
    errors.durationMinutes = 'Duration must be greater than zero minutes.'
  }
  if (values.price < 0) {
    errors.price = 'Price cannot be negative.'
  }
  if (!values.currencyCode.trim() || values.currencyCode.trim().length !== 3) {
    errors.currencyCode = 'A valid 3-character currency code is required.'
  }
  if (values.bufferBeforeMinutes < 0 || values.bufferAfterMinutes < 0) {
    errors.bufferBeforeMinutes = 'Buffer time cannot be negative.'
  }
  if (values.minAdvanceBookingHoursOverride !== null && values.minAdvanceBookingHoursOverride < 0) {
    errors.minAdvanceBookingHoursOverride = 'Minimum advance booking hours cannot be negative.'
  }

  return errors
}

interface IAddServiceModalProps {
  isOpen: boolean
  service: IService | null
  onClose: () => void
  onSaved: () => void
}

export function AddServiceModal({ isOpen, service, onClose, onSaved }: IAddServiceModalProps) {
  const { showToast } = useToast()
  const { policy: bookingPolicy } = useBookingPolicy()
  const [values, setValues] = useState<IServiceFormValues>(EMPTY_VALUES)
  const [errors, setErrors] = useState<IFormErrors>({})
  const [touched, setTouched] = useState<IFormTouched>({})
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [selectedStaffIds, setSelectedStaffIds] = useState<string[]>([])
  const [initialStaffIds, setInitialStaffIds] = useState<string[]>([])

  const isEditMode = service !== null
  const isOverridden = values.minAdvanceBookingHoursOverride !== null

  const { staff: serviceStaff, isLoading: isServiceStaffLoading, refetch: refetchServiceStaff } = useServiceStaff(service?.id ?? null)
  const { members: teamMembers, isLoading: isTeamMembersLoading } = useTeamMembers({ pageSize: 200 })

  const staffOptions: IMultiSelectOption[] = isEditMode
    ? serviceStaff.map((member) => ({ value: member.tenantMemberId, label: member.fullName, sublabel: member.customJobTitle }))
    : teamMembers
        .filter((member) => member.status === TenantMemberStatus.Active)
        .map((member) => ({ value: member.id, label: member.fullName, sublabel: member.customJobTitle }))
  const isStaffOptionsLoading = isEditMode ? isServiceStaffLoading : isTeamMembersLoading

  useEffect(() => {
    if (isOpen) {
      setValues(
        service
          ? {
              name: service.name,
              description: service.description ?? '',
              durationMinutes: service.durationMinutes,
              price: service.price,
              currencyCode: service.currencyCode,
              bufferBeforeMinutes: service.bufferBeforeMinutes,
              bufferAfterMinutes: service.bufferAfterMinutes,
              minAdvanceBookingHoursOverride: service.minAdvanceBookingHoursOverride,
            }
          : EMPTY_VALUES,
      )
      setErrors({})
      setTouched({})
    }
  }, [isOpen, service])

  useEffect(() => {
    if (isOpen && service) {
      refetchServiceStaff()
    }
  }, [isOpen, service, refetchServiceStaff])

  useEffect(() => {
    if (!isOpen) return

    if (isEditMode) {
      const assignedIds = serviceStaff.filter((member) => member.isAssigned).map((member) => member.tenantMemberId)
      setSelectedStaffIds(assignedIds)
      setInitialStaffIds(assignedIds)
    } else {
      setSelectedStaffIds([])
      setInitialStaffIds([])
    }
  }, [isOpen, service, serviceStaff, isEditMode])

  const handleFieldChange = <K extends ServiceField>(field: K, value: IServiceFormValues[K]) => {
    const nextValues = { ...values, [field]: value }
    setValues(nextValues)
    setErrors(validate(nextValues))
  }

  const handleBlur = (field: ServiceField) => {
    setTouched((prev) => ({ ...prev, [field]: true }))
  }

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    const validationErrors = validate(values)
    setErrors(validationErrors)
    setTouched(ALL_FIELDS_TOUCHED)

    if (Object.keys(validationErrors).length > 0) {
      return
    }

    setIsSubmitting(true)
    try {
      const serviceId = isEditMode ? service.id : await createService(values)
      if (isEditMode) {
        await updateService(serviceId, values)
      }

      const addedStaffIds = isEditMode ? selectedStaffIds.filter((id) => !initialStaffIds.includes(id)) : selectedStaffIds
      const removedStaffIds = isEditMode ? initialStaffIds.filter((id) => !selectedStaffIds.includes(id)) : []

      let staffSyncFailed = false
      if (addedStaffIds.length > 0 || removedStaffIds.length > 0) {
        try {
          await Promise.all([
            ...addedStaffIds.map((id) => assignStaffToService(serviceId, id)),
            ...removedStaffIds.map((id) => unassignStaffFromService(serviceId, id)),
          ])
        } catch {
          staffSyncFailed = true
        }
      }

      if (staffSyncFailed) {
        showToast('error', `${values.name} was saved, but updating service providers failed. Please try again.`)
      } else {
        showToast('success', isEditMode ? `${values.name} was updated.` : `${values.name} was added to your service catalog.`)
      }

      onSaved()
      onClose()
    } catch (error) {
      const detail = axios.isAxiosError(error) ? (error.response?.data as { detail?: string } | undefined)?.detail : undefined
      showToast('error', detail ?? 'Failed to save this service. Please try again.')
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <Modal
      isOpen={isOpen}
      title={isEditMode ? 'Edit Service' : 'Add Service'}
      description="Define what customers can book, how long it takes, and what it costs."
      onClose={onClose}
    >
      <form noValidate onSubmit={handleSubmit}>
        <FormSection title="Service Details">
          <FormGroup label="Service Name" htmlFor="name" required error={touched.name ? errors.name : undefined}>
            <input
              type="text"
              id="name"
              className={`form-control ${touched.name && errors.name ? 'is-invalid' : ''}`}
              value={values.name}
              onChange={(e) => handleFieldChange('name', e.target.value)}
              onBlur={() => handleBlur('name')}
            />
          </FormGroup>

          <FormGroup label="Description" htmlFor="description">
            <textarea
              id="description"
              rows={3}
              className="form-control"
              value={values.description}
              onChange={(e) => handleFieldChange('description', e.target.value)}
            />
          </FormGroup>
        </FormSection>

        <hr className="my-4" />

        <FormSection title="Pricing & Duration">
          <div className="row">
            <div className="col-sm-6">
              <FormGroup
                label="Duration (minutes)"
                htmlFor="durationMinutes"
                required
                error={touched.durationMinutes ? errors.durationMinutes : undefined}
              >
                <NumberInput
                  id="durationMinutes"
                  min={1}
                  isInvalid={touched.durationMinutes && !!errors.durationMinutes}
                  value={values.durationMinutes}
                  onChange={(value) => handleFieldChange('durationMinutes', value)}
                />
              </FormGroup>
            </div>
            <div className="col-sm-6">
              <FormGroup label="Price" htmlFor="price" required error={touched.price ? errors.price : undefined}>
                <div className="input-group">
                  <select
                    className="form-select"
                    style={{ maxWidth: '6rem' }}
                    value={values.currencyCode}
                    onChange={(e) => handleFieldChange('currencyCode', e.target.value)}
                  >
                    {CURRENCY_OPTIONS.map((code) => (
                      <option key={code} value={code}>
                        {code}
                      </option>
                    ))}
                  </select>
                  <NumberInput
                    id="price"
                    min={0}
                    decimals={2}
                    isInvalid={touched.price && !!errors.price}
                    value={values.price}
                    onChange={(value) => handleFieldChange('price', value)}
                  />
                </div>
                {(touched.currencyCode && errors.currencyCode) && (
                  <div className="invalid-feedback d-block">{errors.currencyCode}</div>
                )}
              </FormGroup>
            </div>
          </div>

          <div className="row">
            <div className="col-sm-6">
              <FormGroup label="Buffer Before (minutes)" htmlFor="bufferBeforeMinutes">
                <NumberInput
                  id="bufferBeforeMinutes"
                  min={0}
                  value={values.bufferBeforeMinutes}
                  onChange={(value) => handleFieldChange('bufferBeforeMinutes', value)}
                />
              </FormGroup>
            </div>
            <div className="col-sm-6">
              <FormGroup label="Buffer After (minutes)" htmlFor="bufferAfterMinutes">
                <NumberInput
                  id="bufferAfterMinutes"
                  min={0}
                  value={values.bufferAfterMinutes}
                  onChange={(value) => handleFieldChange('bufferAfterMinutes', value)}
                />
              </FormGroup>
            </div>
          </div>
        </FormSection>

        <hr className="my-4" />

        <FormSection title="Booking Rules" description="Optionally override your business-wide booking settings for this service only.">
          <div className="form-check form-switch mb-2">
            <input
              type="checkbox"
              className="form-check-input"
              id="overrideMinAdvanceBookingHours"
              checked={isOverridden}
              onChange={() =>
                handleFieldChange('minAdvanceBookingHoursOverride', isOverridden ? null : (bookingPolicy?.minAdvanceBookingHours ?? 0))
              }
            />
            <label className="form-check-label" htmlFor="overrideMinAdvanceBookingHours">
              Override minimum advance booking hours for this service
            </label>
          </div>
          {!isOverridden && (
            <div className="form-text">
              Currently using your booking settings default: {bookingPolicy?.minAdvanceBookingHours ?? 0} hour(s).
            </div>
          )}

          <CollapsibleField isOpen={isOverridden}>
            <FormGroup
              label="Minimum Advance Booking (hours)"
              htmlFor="minAdvanceBookingHoursOverride"
              error={touched.minAdvanceBookingHoursOverride ? errors.minAdvanceBookingHoursOverride : undefined}
            >
              <NumberInput
                id="minAdvanceBookingHoursOverride"
                min={0}
                disabled={!isOverridden}
                isInvalid={touched.minAdvanceBookingHoursOverride && !!errors.minAdvanceBookingHoursOverride}
                value={values.minAdvanceBookingHoursOverride ?? bookingPolicy?.minAdvanceBookingHours ?? 0}
                onChange={(value) => handleFieldChange('minAdvanceBookingHoursOverride', value)}
              />
              <div className="form-text">This overrides the booking settings default just for this service.</div>
            </FormGroup>
          </CollapsibleField>
        </FormSection>

        <hr className="my-4" />

        <FormSection title="Service Providers" description="Choose which team members can perform this service.">
          <MultiSelectCombobox
            options={staffOptions}
            selectedValues={selectedStaffIds}
            onChange={setSelectedStaffIds}
            placeholder="Select team members..."
            searchPlaceholder="Search team members..."
            emptyMessage="No active team members yet."
            isLoading={isStaffOptionsLoading}
          />
        </FormSection>

        <div className="modal-form-actions">
          <Button type="button" variant="outline-secondary" onClick={onClose} disabled={isSubmitting}>
            Cancel
          </Button>
          <Button type="submit" isLoading={isSubmitting}>
            {isSubmitting ? 'Saving...' : isEditMode ? 'Save Changes' : 'Add Service'}
          </Button>
        </div>
      </form>
    </Modal>
  )
}
