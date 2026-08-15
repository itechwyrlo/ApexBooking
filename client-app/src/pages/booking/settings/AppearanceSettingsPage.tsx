import { useEffect, useState, type FormEvent } from 'react'
import axios from 'axios'
import { PageHeader } from '../../../components/common/PageHeader'
import { Card } from '../../../components/common/Card'
import { Button } from '../../../components/common/Button'
import { Skeleton } from '../../../components/common/Skeleton'
import { useAppearance } from '../../../hooks/useAppearance'
import { useToast } from '../../../hooks/useToast'
import { updateAppearance } from '../../../services/appearanceService'
import { THEME_PALETTES } from '../../../config/themePalettes'
import type { IAppearanceValues } from '../../../interfaces/IAppearance'

const DEFAULT_VALUES: IAppearanceValues = {
  themePaletteId: 'indigo',
  // The public booking page always renders light — dark mode is a dashboard-only staff
  // preference, never a tenant brand setting. Still sent because the update endpoint's
  // contract expects the field; it's just never exposed as a choice in this form anymore.
  publicPageDarkMode: false,
}

export function AppearanceSettingsPage() {
  const { appearance, isLoading, refetch } = useAppearance()
  const { showToast } = useToast()
  const [values, setValues] = useState<IAppearanceValues>(DEFAULT_VALUES)
  const [isSubmitting, setIsSubmitting] = useState(false)

  useEffect(() => {
    if (appearance) {
      setValues({ themePaletteId: appearance.themePaletteId, publicPageDarkMode: false })
    }
  }, [appearance])

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()

    setIsSubmitting(true)
    try {
      await updateAppearance(values)
      showToast('success', 'Appearance settings updated.')
      refetch()
    } catch (error) {
      const detail = axios.isAxiosError(error) ? (error.response?.data as { detail?: string } | undefined)?.detail : undefined
      showToast('error', detail ?? 'Failed to update appearance settings. Please try again.')
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
        title="Appearance"
        description="Choose the brand color shown on your dashboard and public booking page."
      />
      <Card>
        <form noValidate onSubmit={handleSubmit}>
          <div className="mb-4">
            <label className="form-label fw-semibold">Theme Color</label>
            <div className="d-flex flex-wrap gap-3">
              {THEME_PALETTES.map((palette) => (
                <button
                  key={palette.id}
                  type="button"
                  className="btn p-0 border-0 bg-transparent d-flex flex-column align-items-center gap-1"
                  onClick={() => setValues((prev) => ({ ...prev, themePaletteId: palette.id }))}
                  aria-pressed={values.themePaletteId === palette.id}
                  aria-label={palette.name}
                  title={palette.name}
                >
                  <span
                    className="rounded-circle d-inline-block"
                    style={{
                      width: 40,
                      height: 40,
                      backgroundColor: palette.swatchColor,
                      border: values.themePaletteId === palette.id ? '3px solid var(--color-ink)' : '3px solid transparent',
                      boxShadow: '0 0 0 1px var(--color-border)',
                    }}
                  />
                  <span className="small text-muted">{palette.name}</span>
                </button>
              ))}
            </div>
            <div className="form-text mt-2">
              Applies to both your dashboard and your public booking page. Your dashboard also has its own light/dark switch in the top
              bar — that's a personal working preference and doesn't affect what customers see.
            </div>
          </div>

          <div className="d-flex justify-content-end">
            <Button type="submit" isLoading={isSubmitting}>
              {isSubmitting ? 'Saving...' : 'Save Changes'}
            </Button>
          </div>
        </form>
      </Card>
    </div>
  )
}
