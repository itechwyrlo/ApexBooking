import { useEffect, useState } from 'react'
import axios from 'axios'
import { Modal } from '../common/Modal'
import { Button } from '../common/Button'
import { Skeleton } from '../common/Skeleton'
import { useToast } from '../../hooks/useToast'
import { getTeamMemberRemovalImpact, removeTeamMember } from '../../services/teamService'
import type { ITeamMember } from '../../interfaces/ITeamMember'

interface IRemoveTeamMemberModalProps {
  member: ITeamMember | null
  onClose: () => void
  onRemoved: () => void
}

export function RemoveTeamMemberModal({ member, onClose, onRemoved }: IRemoveTeamMemberModalProps) {
  const { showToast } = useToast()
  const [hasHistoricalRecords, setHasHistoricalRecords] = useState<boolean | null>(null)
  const [isChecking, setIsChecking] = useState(false)
  const [isSubmitting, setIsSubmitting] = useState(false)

  useEffect(() => {
    if (!member) {
      setHasHistoricalRecords(null)
      return
    }

    let isMounted = true
    setIsChecking(true)
    getTeamMemberRemovalImpact(member.id)
      .then((impact) => {
        if (isMounted) setHasHistoricalRecords(impact.hasHistoricalRecords)
      })
      .catch(() => {
        if (isMounted) setHasHistoricalRecords(null)
      })
      .finally(() => {
        if (isMounted) setIsChecking(false)
      })

    return () => {
      isMounted = false
    }
  }, [member])

  const handleConfirm = async () => {
    if (!member) return

    setIsSubmitting(true)
    try {
      const result = await removeTeamMember(member.id)
      showToast(
        'success',
        result.wasSoftDeleted
          ? `${member.fullName} was deactivated and hidden from scheduling.`
          : `${member.fullName} was removed.`,
      )
      onRemoved()
      onClose()
    } catch (error) {
      const detail = axios.isAxiosError(error) ? (error.response?.data as { detail?: string } | undefined)?.detail : undefined
      showToast('error', detail ?? 'Failed to remove this team member. Please try again.')
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <Modal
      isOpen={member !== null}
      title="Remove Team Member"
      onClose={onClose}
      footer={
        <>
          <Button type="button" variant="outline-secondary" onClick={onClose} disabled={isSubmitting}>
            Cancel
          </Button>
          <Button type="button" variant="danger" isLoading={isSubmitting} disabled={isChecking} onClick={handleConfirm}>
            {isSubmitting ? 'Removing...' : hasHistoricalRecords ? 'Deactivate' : 'Remove Permanently'}
          </Button>
        </>
      }
    >
      {isChecking ? (
        <Skeleton height="4rem" />
      ) : hasHistoricalRecords ? (
        <p className="mb-0">
          <strong>{member?.fullName}</strong> can't be permanently deleted — they have historical records (e.g. past
          appointments) on file. Proceeding will instead <strong>deactivate</strong> this team member: they'll no
          longer be assignable to new bookings and will appear greyed out in your team list, while their history
          stays intact.
        </p>
      ) : (
        <p className="mb-0">
          Remove <strong>{member?.fullName}</strong>? They have no booking history, so this will permanently delete
          their record. This action cannot be undone.
        </p>
      )}
    </Modal>
  )
}
