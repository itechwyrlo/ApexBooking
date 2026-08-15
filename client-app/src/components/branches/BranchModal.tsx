import { useEffect, useState, type FormEvent } from 'react'
import axios from 'axios'
import { Modal } from '../common/Modal'
import { FormGroup } from '../common/FormGroup'
import { FormSection } from '../common/FormSection'
import { TimeSelect } from '../common/TimeSelect'
import { Button } from '../common/Button'
import { ModalTabs } from '../common/ModalTabs'
import { useToast } from '../../hooks/useToast'
import { useBranchDetail } from '../../hooks/useBranchDetail'
import { usePsgcBarangays, usePsgcCities, usePsgcProvinces } from '../../hooks/usePsgc'
import { addBranch, updateBranchOperatingHours, updateBranchProfile } from '../../services/branchService'
import { DayOfWeek, DAYS_OF_WEEK_ORDER } from '../../types/DayOfWeek'
import type { IBranchFormValues } from '../../interfaces/IBranchDetail'
import type { IOperatingHoursEntry } from '../../interfaces/IOperatingHoursEntry'

const DEFAULT_TIME_ZONE = 'Asia/Manila'
const DEFAULT_OPEN_TIME = '09:00'
const DEFAULT_CLOSE_TIME = '17:00'

const EMPTY_PROFILE_VALUES: IBranchFormValues = {
  branchName: '',
  timeZoneId: DEFAULT_TIME_ZONE,
  street: '',
  barangay: '',
  city: '',
  province: '',
  zipCode: '',
}

function defaultOperatingHours(): IOperatingHoursEntry[] {
  return DAYS_OF_WEEK_ORDER.map((dayOfWeek) => ({
    dayOfWeek,
    startTime: `${DEFAULT_OPEN_TIME}:00`,
    endTime: `${DEFAULT_CLOSE_TIME}:00`,
    isOff: true,
  }))
}

function toTimeInputValue(wireTime: string): string {
  return wireTime.slice(0, 5)
}

function toWireTime(inputValue: string): string {
  return `${inputValue}:00`
}

function namesMatch(a: string, b: string): boolean {
  return a.trim().toUpperCase() === b.trim().toUpperCase()
}

type ProfileField = keyof IBranchFormValues
type IProfileErrors = Partial<Record<ProfileField, string>>
type IProfileTouched = Partial<Record<ProfileField, boolean>>

const ZIP_CODE_PATTERN = /^\d{4}$/

function validateProfile(values: IBranchFormValues): IProfileErrors {
  const errors: IProfileErrors = {}

  if (!values.branchName.trim()) errors.branchName = 'Branch name is required.'
  if (!values.timeZoneId.trim()) errors.timeZoneId = 'Time zone is required.'
  if (!values.street.trim()) errors.street = 'Street is required.'
  if (!values.city.trim()) errors.city = 'City or municipality is required.'
  if (!values.province.trim()) errors.province = 'Province is required.'
  if (!values.zipCode.trim()) {
    errors.zipCode = 'ZIP code is required.'
  } else if (!ZIP_CODE_PATTERN.test(values.zipCode.trim())) {
    errors.zipCode = 'A Philippine ZIP code must be exactly four digits.'
  }

  return errors
}

function validateHours(hours: IOperatingHoursEntry[]): string | null {
  const invalidDay = hours.find((entry) => !entry.isOff && entry.startTime >= entry.endTime)
  return invalidDay ? `Start time must be earlier than end time for ${invalidDay.dayOfWeek}.` : null
}

const ALL_PROFILE_FIELDS_TOUCHED: IProfileTouched = {
  branchName: true,
  timeZoneId: true,
  street: true,
  city: true,
  province: true,
  zipCode: true,
}

interface IBranchModalProps {
  isOpen: boolean
  branchId: string | null
  onClose: () => void
  onSaved: () => void
}

export function BranchModal({ isOpen, branchId, onClose, onSaved }: IBranchModalProps) {
  const [activeBranchId, setActiveBranchId] = useState<string | null>(branchId)
  const [activeTab, setActiveTab] = useState<'profile' | 'hours'>('profile')
  const { branch, isLoading: isLoadingBranch, refetch: refetchBranch } = useBranchDetail(activeBranchId)
  const { showToast } = useToast()

  const [profileValues, setProfileValues] = useState<IBranchFormValues>(EMPTY_PROFILE_VALUES)
  const [profileErrors, setProfileErrors] = useState<IProfileErrors>({})
  const [profileTouched, setProfileTouched] = useState<IProfileTouched>({})
  const [isSubmittingProfile, setIsSubmittingProfile] = useState(false)

  const [selectedProvCode, setSelectedProvCode] = useState('')
  const [selectedMunCityCode, setSelectedMunCityCode] = useState('')
  const { items: provinces } = usePsgcProvinces()
  const { items: cities, isLoading: isLoadingCities } = usePsgcCities(selectedProvCode || null)
  const { items: barangays, isLoading: isLoadingBarangays } = usePsgcBarangays(selectedMunCityCode || null)

  const [hours, setHours] = useState<IOperatingHoursEntry[]>(defaultOperatingHours())
  const [hoursError, setHoursError] = useState<string | null>(null)
  const [isSubmittingHours, setIsSubmittingHours] = useState(false)
  const [copySourceDay, setCopySourceDay] = useState<DayOfWeek>(DayOfWeek.Monday)

  const isEditMode = activeBranchId !== null

  useEffect(() => {
    if (isOpen) {
      setActiveBranchId(branchId)
      setActiveTab('profile')
      if (!branchId) {
        setProfileValues(EMPTY_PROFILE_VALUES)
        setProfileErrors({})
        setProfileTouched({})
        setHours(defaultOperatingHours())
        setSelectedProvCode('')
        setSelectedMunCityCode('')
      }
    }
  }, [isOpen, branchId])

  useEffect(() => {
    if (branch) {
      setProfileValues({
        branchName: branch.name,
        timeZoneId: branch.timeZoneId,
        street: branch.street,
        barangay: branch.barangay ?? '',
        city: branch.city,
        province: branch.province,
        zipCode: branch.zipCode,
      })
      setHours(branch.operatingHours)
    }
  }, [branch])

  // Resolve the loaded branch's province/city names back to PSGC codes so the cascading
  // dropdowns can pre-select the right options (the branch itself only stores display names).
  useEffect(() => {
    if (branch && provinces.length > 0) {
      const match = provinces.find((p) => namesMatch(p.provName, branch.province))
      setSelectedProvCode(match?.provCode ?? '')
    }
  }, [branch, provinces])

  useEffect(() => {
    if (branch && cities.length > 0) {
      const match = cities.find((c) => namesMatch(c.munCityName, branch.city))
      setSelectedMunCityCode(match?.munCityCode ?? '')
    }
  }, [branch, cities])

  const handleProfileFieldChange = (field: ProfileField, value: string) => {
    const nextValues = { ...profileValues, [field]: value }
    setProfileValues(nextValues)
    setProfileErrors(validateProfile(nextValues))
  }

  const handleProfileBlur = (field: ProfileField) => {
    setProfileTouched((prev) => ({ ...prev, [field]: true }))
  }

  const handleProvinceChange = (provCode: string) => {
    const match = provinces.find((p) => p.provCode === provCode)
    const provinceName = match?.provName.trim() ?? ''
    setProfileValues((prev) => ({ ...prev, province: provinceName, city: '', barangay: '' }))
    setProfileErrors(validateProfile({ ...profileValues, province: provinceName, city: '', barangay: '' }))
    setSelectedProvCode(provCode)
    setSelectedMunCityCode('')
  }

  const handleCityChange = (munCityCode: string) => {
    const match = cities.find((c) => c.munCityCode === munCityCode)
    const cityName = match?.munCityName.trim() ?? ''
    setProfileValues((prev) => ({ ...prev, city: cityName, barangay: '' }))
    setProfileErrors(validateProfile({ ...profileValues, city: cityName, barangay: '' }))
    setSelectedMunCityCode(munCityCode)
  }

  const handleBarangayChange = (barangayName: string) => {
    handleProfileFieldChange('barangay', barangayName)
  }

  const handleProfileSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    const validationErrors = validateProfile(profileValues)
    setProfileErrors(validationErrors)
    setProfileTouched(ALL_PROFILE_FIELDS_TOUCHED)

    if (Object.keys(validationErrors).length > 0) {
      return
    }

    setIsSubmittingProfile(true)
    try {
      if (isEditMode) {
        await updateBranchProfile(activeBranchId!, profileValues)
        showToast('success', 'Branch updated.')
        refetchBranch()
      } else {
        const newBranchId = await addBranch(profileValues)
        showToast('success', "Branch added. You can now set its operating hours.")
        setActiveBranchId(newBranchId)
        setActiveTab('hours')
      }
      onSaved()
    } catch (error) {
      const detail = axios.isAxiosError(error) ? (error.response?.data as { detail?: string } | undefined)?.detail : undefined
      showToast('error', detail ?? 'Failed to save branch. Please try again.')
    } finally {
      setIsSubmittingProfile(false)
    }
  }

  const handleDayChange = (dayOfWeek: DayOfWeek, patch: Partial<IOperatingHoursEntry>) => {
    const nextHours = hours.map((entry) => (entry.dayOfWeek === dayOfWeek ? { ...entry, ...patch } : entry))
    setHours(nextHours)
    setHoursError(validateHours(nextHours))
  }

  const handleCopyToAllDays = (dayOfWeek: DayOfWeek) => {
    const source = hours.find((entry) => entry.dayOfWeek === dayOfWeek)
    if (!source) return

    const nextHours = hours.map((entry) => ({ ...entry, isOff: source.isOff, startTime: source.startTime, endTime: source.endTime }))
    setHours(nextHours)
    setHoursError(validateHours(nextHours))
  }

  const handleHoursSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    const validationError = validateHours(hours)
    setHoursError(validationError)

    if (validationError) {
      return
    }

    setIsSubmittingHours(true)
    try {
      await updateBranchOperatingHours(activeBranchId!, hours)
      showToast('success', 'Operating hours updated.')
      onSaved()
    } catch (error) {
      const detail = axios.isAxiosError(error) ? (error.response?.data as { detail?: string } | undefined)?.detail : undefined
      showToast('error', detail ?? 'Failed to update operating hours. Please try again.')
    } finally {
      setIsSubmittingHours(false)
    }
  }

  return (
    <Modal
      isOpen={isOpen}
      title={isEditMode ? 'Edit Branch' : 'Add Branch'}
      description={isEditMode ? "Update this branch's details and operating hours." : 'Add a new location so customers know where to find you.'}
      onClose={onClose}
    >
      {isEditMode && (
        <ModalTabs
          tabs={[
            { id: 'profile', label: 'Profile' },
            { id: 'hours', label: 'Operating Hours' },
          ]}
          activeTab={activeTab}
          onChange={(id) => setActiveTab(id as 'profile' | 'hours')}
        />
      )}

      {activeTab === 'profile' && (
        <form key="profile" className="list-fade-in" noValidate onSubmit={handleProfileSubmit}>
          <FormSection title="Branch Details">
            <FormGroup label="Branch Name" htmlFor="branchName" required error={profileTouched.branchName ? profileErrors.branchName : undefined}>
              <input
                type="text"
                id="branchName"
                className={`form-control ${profileTouched.branchName && profileErrors.branchName ? 'is-invalid' : ''}`}
                value={profileValues.branchName}
                onChange={(e) => handleProfileFieldChange('branchName', e.target.value)}
                onBlur={() => handleProfileBlur('branchName')}
                disabled={isLoadingBranch}
                autoComplete="off"
              />
            </FormGroup>

            <FormGroup label="Time Zone" htmlFor="timeZoneId" required error={profileTouched.timeZoneId ? profileErrors.timeZoneId : undefined}>
              <input
                type="text"
                id="timeZoneId"
                className={`form-control ${profileTouched.timeZoneId && profileErrors.timeZoneId ? 'is-invalid' : ''}`}
                value={profileValues.timeZoneId}
                onChange={(e) => handleProfileFieldChange('timeZoneId', e.target.value)}
                onBlur={() => handleProfileBlur('timeZoneId')}
                disabled={isLoadingBranch}
              />
            </FormGroup>
          </FormSection>

          <hr className="my-4" />

          <FormSection title="Address">
            <FormGroup label="Street" htmlFor="street" required error={profileTouched.street ? profileErrors.street : undefined}>
              <input
                type="text"
                id="street"
                className={`form-control ${profileTouched.street && profileErrors.street ? 'is-invalid' : ''}`}
                value={profileValues.street}
                onChange={(e) => handleProfileFieldChange('street', e.target.value)}
                onBlur={() => handleProfileBlur('street')}
                disabled={isLoadingBranch}
              />
            </FormGroup>

            <FormGroup label="Province" htmlFor="province" required error={profileTouched.province ? profileErrors.province : undefined}>
              <select
                id="province"
                className={`form-select ${profileTouched.province && profileErrors.province ? 'is-invalid' : ''}`}
                value={selectedProvCode}
                onChange={(e) => handleProvinceChange(e.target.value)}
                onBlur={() => handleProfileBlur('province')}
                disabled={isLoadingBranch}
              >
                <option value="">Select a province</option>
                {provinces.map((province) => (
                  <option key={province.provCode} value={province.provCode}>
                    {province.provName}
                    {province.cityClass === 'HUC' ? ' (Highly Urbanized City)' : ''}
                  </option>
                ))}
              </select>
            </FormGroup>

            <FormGroup label="City / Municipality" htmlFor="city" required error={profileTouched.city ? profileErrors.city : undefined}>
              <select
                id="city"
                className={`form-select ${profileTouched.city && profileErrors.city ? 'is-invalid' : ''}`}
                value={selectedMunCityCode}
                onChange={(e) => handleCityChange(e.target.value)}
                onBlur={() => handleProfileBlur('city')}
                disabled={isLoadingBranch || !selectedProvCode || isLoadingCities}
              >
                <option value="">{selectedProvCode ? 'Select a city or municipality' : 'Select a province first'}</option>
                {cities.map((city) => (
                  <option key={city.munCityCode} value={city.munCityCode}>
                    {city.munCityName}
                  </option>
                ))}
              </select>
            </FormGroup>

            <FormGroup label="Barangay" htmlFor="barangay">
              <select
                id="barangay"
                className="form-select"
                value={profileValues.barangay}
                onChange={(e) => handleBarangayChange(e.target.value)}
                disabled={isLoadingBranch || !selectedMunCityCode || isLoadingBarangays}
              >
                <option value="">{selectedMunCityCode ? 'Select a barangay (optional)' : 'Select a city first'}</option>
                {barangays.map((barangay) => (
                  <option key={barangay.brgyCode} value={barangay.brgyName.trim()}>
                    {barangay.brgyName.trim()}
                  </option>
                ))}
              </select>
            </FormGroup>

            <FormGroup label="ZIP Code" htmlFor="zipCode" required error={profileTouched.zipCode ? profileErrors.zipCode : undefined}>
              <input
                type="text"
                id="zipCode"
                inputMode="numeric"
                maxLength={4}
                className={`form-control ${profileTouched.zipCode && profileErrors.zipCode ? 'is-invalid' : ''}`}
                value={profileValues.zipCode}
                onChange={(e) => handleProfileFieldChange('zipCode', e.target.value)}
                onBlur={() => handleProfileBlur('zipCode')}
                disabled={isLoadingBranch}
              />
            </FormGroup>
          </FormSection>

          <div className="modal-form-actions">
            <Button type="button" variant="outline-secondary" onClick={onClose} disabled={isSubmittingProfile}>
              Cancel
            </Button>
            <Button type="submit" isLoading={isSubmittingProfile} disabled={isLoadingBranch}>
              {isSubmittingProfile ? 'Saving...' : isEditMode ? 'Save Changes' : 'Add Branch'}
            </Button>
          </div>
        </form>
      )}

      {activeTab === 'hours' && isEditMode && (
        <form key="hours" className="list-fade-in" noValidate onSubmit={handleHoursSubmit}>
          {hoursError && <div className="alert alert-danger py-2">{hoursError}</div>}

          <div className="d-flex flex-wrap align-items-center gap-2 p-2 mb-3 rounded-3" style={{ backgroundColor: 'var(--color-canvas)' }}>
            <span className="small fw-semibold text-nowrap me-1">Copy hours from</span>
            <div className="btn-group" role="group" aria-label="Day to copy hours from">
              {DAYS_OF_WEEK_ORDER.map((day) => (
                <button
                  key={day}
                  type="button"
                  className={`btn btn-sm ${copySourceDay === day ? 'btn-primary' : 'btn-outline-secondary'}`}
                  aria-pressed={copySourceDay === day}
                  onClick={() => setCopySourceDay(day)}
                >
                  {day.slice(0, 3)}
                </button>
              ))}
            </div>
            <Button type="button" variant="outline-primary" size="sm" onClick={() => handleCopyToAllDays(copySourceDay)}>
              Apply to All Days
            </Button>
          </div>

          {hours.map((entry) => (
            <div key={entry.dayOfWeek} className="d-flex flex-column gap-2 py-2 border-bottom">
              <div className="d-flex align-items-center gap-3">
                <div style={{ width: '5.5rem' }} className="fw-semibold small">
                  {entry.dayOfWeek}
                </div>
                <div className="form-check form-switch mb-0">
                  <input
                    type="checkbox"
                    className="form-check-input"
                    id={`open-${entry.dayOfWeek}`}
                    checked={!entry.isOff}
                    onChange={(e) => handleDayChange(entry.dayOfWeek, { isOff: !e.target.checked })}
                  />
                  <label className="form-check-label small" htmlFor={`open-${entry.dayOfWeek}`}>
                    Open
                  </label>
                </div>
              </div>
              {!entry.isOff && (
                <div className="d-flex flex-wrap align-items-center gap-2">
                  <TimeSelect
                    id={`start-${entry.dayOfWeek}`}
                    className="form-select-sm"
                    style={{ minWidth: '8rem', flex: '1 1 8rem' }}
                    value={toTimeInputValue(entry.startTime)}
                    onChange={(value) => handleDayChange(entry.dayOfWeek, { startTime: toWireTime(value) })}
                  />
                  <span className="text-muted small">to</span>
                  <TimeSelect
                    id={`end-${entry.dayOfWeek}`}
                    className="form-select-sm"
                    style={{ minWidth: '8rem', flex: '1 1 8rem' }}
                    value={toTimeInputValue(entry.endTime)}
                    onChange={(value) => handleDayChange(entry.dayOfWeek, { endTime: toWireTime(value) })}
                  />
                </div>
              )}
            </div>
          ))}

          <div className="modal-form-actions">
            <Button type="button" variant="outline-secondary" onClick={onClose} disabled={isSubmittingHours}>
              Cancel
            </Button>
            <Button type="submit" isLoading={isSubmittingHours}>
              {isSubmittingHours ? 'Saving...' : 'Save Hours'}
            </Button>
          </div>
        </form>
      )}
    </Modal>
  )
}
