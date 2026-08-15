import { useCallback, useEffect, useState } from 'react'
import {
  getPublicBranches,
  getPublicServices,
  getPublicStaff,
  getPublicAvailability,
  initiateBooking,
  getPublicTheme,
} from '../services/publicBookingService'
import type { IPublicBranch } from '../interfaces/publicBooking/IPublicBranch'
import type { IPublicService } from '../interfaces/publicBooking/IPublicService'
import type { IPublicStaff } from '../interfaces/publicBooking/IPublicStaff'
import type { IPublicSlot } from '../interfaces/publicBooking/IPublicSlot'
import type { IBookingContactValues } from '../interfaces/publicBooking/IInitiateBookingValues'
import type { IBookingInitiationResult } from '../interfaces/publicBooking/IBookingInitiationResult'

export type WizardStep = 'branch' | 'service' | 'staff' | 'schedule' | 'customerInfo' | 'review' | 'payment' | 'success'
export type WizardDirection = 'forward' | 'backward'

const VISIBLE_STEP_ORDER: WizardStep[] = ['branch', 'service', 'staff', 'schedule', 'customerInfo', 'review']

export const STEP_LABELS: Record<WizardStep, string> = {
  branch: 'Branch',
  service: 'Service',
  staff: 'Staff',
  schedule: 'Schedule',
  customerInfo: 'Your Info',
  review: 'Review',
  payment: 'Payment',
  success: 'Confirmed',
}

function todayIsoDate(): string {
  return new Date().toISOString().slice(0, 10)
}

interface IWizardState {
  step: WizardStep
  history: WizardStep[]
  direction: WizardDirection
  editReturnTarget: WizardStep | null

  isInitializing: boolean
  pageError: string | null

  branches: IPublicBranch[]
  branch: IPublicBranch | null
  branchStepSkipped: boolean

  services: IPublicService[]
  isLoadingServices: boolean
  service: IPublicService | null

  staff: IPublicStaff[]
  isLoadingStaff: boolean
  staffMember: IPublicStaff | null
  staffStepSkipped: boolean

  date: string
  slots: IPublicSlot[]
  isLoadingSlots: boolean
  slotsError: string | null
  unavailableReason: string | null
  slot: IPublicSlot | null

  contact: IBookingContactValues | null

  isSubmitting: boolean
  submitError: string | null
  result: IBookingInitiationResult | null
}

const INITIAL_STATE: IWizardState = {
  step: 'branch',
  history: [],
  direction: 'forward',
  editReturnTarget: null,
  isInitializing: true,
  pageError: null,
  branches: [],
  branch: null,
  branchStepSkipped: false,
  services: [],
  isLoadingServices: false,
  service: null,
  staff: [],
  isLoadingStaff: false,
  staffMember: null,
  staffStepSkipped: false,
  date: todayIsoDate(),
  slots: [],
  isLoadingSlots: false,
  slotsError: null,
  unavailableReason: null,
  slot: null,
  contact: null,
  isSubmitting: false,
  submitError: null,
  result: null,
}

export function usePublicBookingWizard(slug: string) {
  const [state, setState] = useState<IWizardState>(INITIAL_STATE)

  const fetchServices = useCallback(
    async (branch: IPublicBranch) => {
      setState((prev) => ({ ...prev, isLoadingServices: true }))
      try {
        const services = await getPublicServices(slug, branch.branchId)
        setState((prev) => ({ ...prev, services, isLoadingServices: false }))
      } catch {
        setState((prev) => ({ ...prev, isLoadingServices: false, pageError: 'Failed to load services for this location.' }))
      }
    },
    [slug],
  )

  useEffect(() => {
    let isMounted = true

    getPublicBranches(slug)
      .then((branches) => {
        if (!isMounted) return

        if (branches.length === 1) {
          const [onlyBranch] = branches
          setState((prev) => ({
            ...prev,
            branches,
            branch: onlyBranch,
            branchStepSkipped: true,
            step: 'service',
            history: [],
            direction: 'forward',
            isInitializing: false,
          }))
          void fetchServices(onlyBranch)
          return
        }

        setState((prev) => ({ ...prev, branches, isInitializing: false }))
      })
      .catch(() => {
        if (isMounted) setState((prev) => ({ ...prev, isInitializing: false, pageError: "This booking page isn't available." }))
      })

    return () => {
      isMounted = false
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [slug])

  // Deliberately separate from the branches fetch above — paints the page's colors as soon as
  // it resolves, but a failure here shouldn't block the wizard itself (falls back to the
  // default indigo/light CSS already baked into :root).
  useEffect(() => {
    // The public booking page always renders light, regardless of any tenant dark-mode setting —
    // dark mode is a dashboard-only staff preference, never a customer-facing brand choice. Set
    // this explicitly (not just "default to light") in case the same browser tab previously had a
    // dashboard session with dark mode on: data-bs-theme lives on the shared <html> element, so a
    // stale value could otherwise leak into this page.
    document.documentElement.setAttribute('data-bs-theme', 'light')

    // Apply a cached palette immediately so a repeat visitor doesn't see a flash of the default
    // indigo before this tenant's real color loads — getPublicTheme is still an async call.
    const paletteStorageKey = `apexbooking.publicTheme.${slug}`
    const cachedPaletteId = localStorage.getItem(paletteStorageKey)
    if (cachedPaletteId) {
      document.documentElement.setAttribute('data-palette', cachedPaletteId)
    }

    let isMounted = true

    getPublicTheme(slug)
      .then((theme) => {
        if (!isMounted) return
        document.documentElement.setAttribute('data-palette', theme.themePaletteId)
        localStorage.setItem(paletteStorageKey, theme.themePaletteId)
      })
      .catch(() => {
        // Silent — the default :root values (or the cached palette above) already render a sane page.
      })

    return () => {
      isMounted = false
    }
  }, [slug])

  const selectBranch = useCallback(
    (branch: IPublicBranch) => {
      setState((prev) => ({ ...prev, branch, step: 'service', history: [...prev.history, 'branch'], direction: 'forward' }))
      void fetchServices(branch)
    },
    [fetchServices],
  )

  const selectService = useCallback(
    async (service: IPublicService) => {
      if (!state.branch) return
      setState((prev) => ({ ...prev, service, isLoadingStaff: true }))

      try {
        const staff = await getPublicStaff(slug, state.branch.branchId, service.serviceId)

        if (staff.length === 1) {
          const [onlyStaff] = staff
          setState((prev) => ({
            ...prev,
            staff,
            staffMember: onlyStaff,
            staffStepSkipped: true,
            isLoadingStaff: false,
            step: 'schedule',
            history: [...prev.history, 'service'],
            direction: 'forward',
          }))
          return
        }

        setState((prev) => ({
          ...prev,
          staff,
          isLoadingStaff: false,
          step: 'staff',
          history: [...prev.history, 'service'],
          direction: 'forward',
        }))
      } catch {
        setState((prev) => ({ ...prev, isLoadingStaff: false, pageError: 'Failed to load available staff for this service.' }))
      }
    },
    [slug, state.branch],
  )

  const fetchSlots = useCallback(
    async (staffMember: IPublicStaff, service: IPublicService, branch: IPublicBranch, date: string) => {
      setState((prev) => ({ ...prev, isLoadingSlots: true, slotsError: null, unavailableReason: null, slot: null }))
      try {
        const result = await getPublicAvailability(slug, branch.branchId, staffMember.tenantMemberId, service.serviceId, date)
        setState((prev) => ({ ...prev, slots: result.slots, unavailableReason: result.unavailableReason, isLoadingSlots: false }))
      } catch {
        setState((prev) => ({ ...prev, isLoadingSlots: false, slotsError: 'Failed to load open times for this date.' }))
      }
    },
    [slug],
  )

  const selectStaff = useCallback(
    (staffMember: IPublicStaff) => {
      setState((prev) => ({ ...prev, staffMember, step: 'schedule', history: [...prev.history, 'staff'], direction: 'forward' }))
      if (state.service && state.branch) {
        void fetchSlots(staffMember, state.service, state.branch, state.date)
      }
    },
    [fetchSlots, state.service, state.branch, state.date],
  )

  const setDate = useCallback(
    (date: string) => {
      setState((prev) => ({ ...prev, date }))
      if (state.staffMember && state.service && state.branch) {
        void fetchSlots(state.staffMember, state.service, state.branch, date)
      }
    },
    [fetchSlots, state.staffMember, state.service, state.branch],
  )

  const selectSlot = useCallback((slot: IPublicSlot) => {
    setState((prev) => {
      // Editing Schedule (or Staff/Service, which force a fresh slot pick downstream)
      // from Review: contact is already known, so skip re-asking for it. 'customerInfo'
      // still goes into history even though we're skipping past it — goToStep/
      // editFromReview look targets up by scanning history, so dropping it here would
      // make "Edit" on Your Information silently fail on the next trip through Review.
      const returnToReview = prev.editReturnTarget === 'review'
      return {
        ...prev,
        slot,
        step: returnToReview ? 'review' : 'customerInfo',
        history: returnToReview ? [...prev.history, 'schedule', 'customerInfo'] : [...prev.history, 'schedule'],
        direction: 'forward',
        editReturnTarget: returnToReview ? null : prev.editReturnTarget,
      }
    })
  }, [])

  const saveContact = useCallback((contact: IBookingContactValues) => {
    setState((prev) => ({
      ...prev,
      contact,
      step: 'review',
      history: [...prev.history, 'customerInfo'],
      direction: 'forward',
      editReturnTarget: null,
    }))
  }, [])

  const goBack = useCallback(() => {
    setState((prev) => {
      if (prev.history.length === 0) return prev
      const history = prev.history.slice(0, -1)
      const previousStep = prev.history[prev.history.length - 1]
      return { ...prev, step: previousStep, history, direction: 'backward' }
    })
  }, [])

  const goToStep = useCallback((target: WizardStep) => {
    setState((prev) => {
      const index = prev.history.indexOf(target)
      if (index === -1) return prev
      return { ...prev, step: target, history: prev.history.slice(0, index), direction: 'backward' }
    })
  }, [])

  // Same jump as goToStep, but remembers to return straight to Review once the
  // edited selection (and anything it forces the user to re-pick) is resolved,
  // instead of walking through every intermediate step again.
  const editFromReview = useCallback((target: WizardStep) => {
    setState((prev) => {
      const index = prev.history.indexOf(target)
      if (index === -1) return prev
      return { ...prev, step: target, history: prev.history.slice(0, index), direction: 'backward', editReturnTarget: 'review' }
    })
  }, [])

  const submit = useCallback(async () => {
    if (!state.branch || !state.service || !state.staffMember || !state.slot || !state.contact) return

    setState((prev) => ({ ...prev, isSubmitting: true, submitError: null }))
    try {
      const result = await initiateBooking(slug, {
        branchId: state.branch.branchId,
        staffId: state.staffMember.tenantMemberId,
        serviceId: state.service.serviceId,
        scheduledDate: state.date,
        scheduledStartTime: state.slot.rawTime,
        customerFirstName: state.contact.customerFirstName,
        customerLastName: state.contact.customerLastName,
        customerEmail: state.contact.customerEmail,
        customerPhone: state.contact.customerPhone,
        customerNotes: state.contact.customerNotes || null,
      })

      setState((prev) => ({
        ...prev,
        isSubmitting: false,
        result,
        step: result.requiresPayment ? 'payment' : 'success',
        history: [...prev.history, 'review'],
        direction: 'forward',
      }))
    } catch {
      setState((prev) => ({
        ...prev,
        isSubmitting: false,
        submitError: 'That time may no longer be available. Please choose another.',
      }))
    }
  }, [slug, state.branch, state.service, state.staffMember, state.date, state.slot, state.contact])

  const markPaymentConfirmed = useCallback(() => {
    setState((prev) => ({ ...prev, step: 'success', direction: 'forward' }))
  }, [])

  const visibleSteps = VISIBLE_STEP_ORDER.filter(
    (step) => (step !== 'branch' || !state.branchStepSkipped) && (step !== 'staff' || !state.staffStepSkipped),
  )

  return {
    ...state,
    visibleSteps,
    selectBranch,
    selectService,
    selectStaff,
    setDate,
    selectSlot,
    saveContact,
    goBack,
    goToStep,
    editFromReview,
    submit,
    markPaymentConfirmed,
  }
}
