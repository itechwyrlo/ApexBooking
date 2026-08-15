import { useParams } from 'react-router-dom'
import { PublicBookingLayout } from '../../layouts/PublicBookingLayout'
import { BranchStep } from '../../components/publicBooking/BranchStep'
import { ServiceStep } from '../../components/publicBooking/ServiceStep'
import { StaffStep } from '../../components/publicBooking/StaffStep'
import { ScheduleStep } from '../../components/publicBooking/ScheduleStep'
import { CustomerInfoStep } from '../../components/publicBooking/CustomerInfoStep'
import { ReviewStep } from '../../components/publicBooking/ReviewStep'
import { PaymentStep } from '../../components/publicBooking/PaymentStep'
import { SuccessStep } from '../../components/publicBooking/SuccessStep'
import { BookingSummaryPanel } from '../../components/publicBooking/BookingSummaryPanel'
import { usePublicBookingWizard, STEP_LABELS, type WizardStep } from '../../hooks/usePublicBookingWizard'

const BACK_ELIGIBLE_STEPS = new Set<WizardStep>(['service', 'staff', 'schedule', 'customerInfo', 'review'])
const SUMMARY_PANEL_STEPS = new Set<WizardStep>(['service', 'staff', 'schedule', 'customerInfo', 'review'])

export function PublicBookingPage() {
  const { slug = '' } = useParams<{ slug: string }>()
  const wizard = usePublicBookingWizard(slug)

  if (wizard.isInitializing) {
    return (
      <PublicBookingLayout currentIndex={0} total={1} stepLabels={[]} showProgress={false}>
        <p className="pb-muted">Loading...</p>
      </PublicBookingLayout>
    )
  }

  if (wizard.pageError) {
    return (
      <PublicBookingLayout currentIndex={0} total={1} stepLabels={[]} showProgress={false}>
        <h1 className="pb-display fs-3 mb-2">This booking page isn't available</h1>
        <p className="pb-muted">{wizard.pageError}</p>
      </PublicBookingLayout>
    )
  }

  const stepIndex = wizard.visibleSteps.indexOf(wizard.step)
  const showProgress = stepIndex !== -1
  const canGoBack = BACK_ELIGIBLE_STEPS.has(wizard.step) && wizard.history.length > 0
  const stepLabels = wizard.visibleSteps.map((step) => STEP_LABELS[step])
  // The schedule step shows a calendar beside the time list, which needs more
  // horizontal room than the single-column option cards used by every other step.
  const contentMaxWidth = wizard.step === 'schedule' ? 720 : 560
  const handleStepClick = (index: number) => {
    const target = wizard.visibleSteps[index]
    if (target) wizard.goToStep(target)
  }
  const sidePanel = SUMMARY_PANEL_STEPS.has(wizard.step) ? (
    <BookingSummaryPanel branch={wizard.branch} service={wizard.service} staffMember={wizard.staffMember} date={wizard.date} slot={wizard.slot} />
  ) : undefined

  return (
    <PublicBookingLayout
      currentIndex={Math.max(stepIndex, 0)}
      total={wizard.visibleSteps.length}
      stepLabels={stepLabels}
      showProgress={showProgress}
      contentMaxWidth={contentMaxWidth}
      onStepClick={handleStepClick}
      sidePanel={sidePanel}
    >
      {wizard.step === 'branch' && (
        <BranchStep
          branches={wizard.branches}
          selectedBranchId={wizard.branch?.branchId ?? null}
          direction={wizard.direction}
          onSelect={wizard.selectBranch}
        />
      )}

      {wizard.step === 'service' && (
        <ServiceStep
          services={wizard.services}
          isLoading={wizard.isLoadingServices}
          selectedServiceId={wizard.service?.serviceId ?? null}
          direction={wizard.direction}
          onSelect={wizard.selectService}
        />
      )}

      {wizard.step === 'staff' && (
        <StaffStep
          staff={wizard.staff}
          isLoading={wizard.isLoadingStaff}
          selectedStaffId={wizard.staffMember?.tenantMemberId ?? null}
          direction={wizard.direction}
          onSelect={wizard.selectStaff}
        />
      )}

      {wizard.step === 'schedule' && (
        <ScheduleStep
          date={wizard.date}
          onDateChange={wizard.setDate}
          slots={wizard.slots}
          isLoading={wizard.isLoadingSlots}
          error={wizard.slotsError}
          unavailableReason={wizard.unavailableReason}
          selectedSlotRawTime={wizard.slot?.rawTime ?? null}
          direction={wizard.direction}
          onSelectSlot={wizard.selectSlot}
        />
      )}

      {wizard.step === 'customerInfo' && (
        <CustomerInfoStep initialValues={wizard.contact} direction={wizard.direction} onContinue={wizard.saveContact} />
      )}

      {wizard.step === 'review' && wizard.branch && wizard.service && wizard.staffMember && wizard.slot && wizard.contact && (
        <ReviewStep
          branch={wizard.branch}
          service={wizard.service}
          staffMember={wizard.staffMember}
          date={wizard.date}
          slot={wizard.slot}
          contact={wizard.contact}
          branchStepSkipped={wizard.branchStepSkipped}
          staffStepSkipped={wizard.staffStepSkipped}
          isSubmitting={wizard.isSubmitting}
          submitError={wizard.submitError}
          direction={wizard.direction}
          onEdit={wizard.editFromReview}
          onSubmit={wizard.submit}
        />
      )}

      {wizard.step === 'payment' && wizard.result && wizard.service && (
        <PaymentStep
          result={wizard.result}
          currencyCode={wizard.service.currencyCode}
          direction={wizard.direction}
          onConfirmed={wizard.markPaymentConfirmed}
        />
      )}

      {wizard.step === 'success' && wizard.result && wizard.branch && wizard.service && wizard.staffMember && wizard.slot && (
        <SuccessStep
          result={wizard.result}
          branch={wizard.branch}
          service={wizard.service}
          staffMember={wizard.staffMember}
          date={wizard.date}
          slot={wizard.slot}
          direction={wizard.direction}
        />
      )}

      {canGoBack && (
        <button type="button" className="btn pb-btn-outline mt-4" onClick={wizard.goBack}>
          ← Back
        </button>
      )}
    </PublicBookingLayout>
  )
}
