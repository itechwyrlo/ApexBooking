import { Icon } from '../common/Icon'

interface ISlugValidationHintProps {
  slug: string
  isValid: boolean
}

function getSlugRuleMessage(slug: string): string {
  if (/[^a-z0-9-]/.test(slug)) {
    return 'Only lowercase letters, numbers, and hyphens are allowed.'
  }

  if (slug.startsWith('-') || slug.endsWith('-')) {
    return "Can't start or end with a hyphen."
  }

  return 'Use 2-63 characters.'
}

export function SlugValidationHint({ slug, isValid }: ISlugValidationHintProps) {
  if (!slug) {
    return (
      <div id="slug-help" className="form-text">
        Lowercase letters, numbers, and hyphens only.
      </div>
    )
  }

  if (isValid) {
    return (
      <div className="slug-hint slug-hint--valid" role="status">
        <Icon name="check-circle" size={14} />
        <span>Looks good</span>
      </div>
    )
  }

  return (
    <div className="slug-hint slug-hint--invalid" role="alert">
      <Icon name="x-circle" size={14} />
      <span>{getSlugRuleMessage(slug)}</span>
    </div>
  )
}
