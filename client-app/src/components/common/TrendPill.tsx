import { Icon } from './Icon'

type TrendSentiment = 'positive' | 'negative' | 'neutral'
type TrendIcon = 'up' | 'down' | 'none'
type TrendPillSize = 'sm' | 'md'

const SENTIMENT_TONE_CLASS: Record<TrendSentiment, string> = {
  positive: 'badge-tone-success',
  negative: 'badge-tone-danger',
  neutral: 'badge-tone-neutral',
}

interface ITrendPillProps {
  /** Drives color — independent of `icon`, since a rising value isn't always good (e.g. cancellations). */
  sentiment: TrendSentiment
  /** Pre-formatted comparison value, e.g. "+8%", "-2%", "No change". */
  value: string
  /** e.g. "from last week". Rendered muted, after the value. */
  label?: string
  icon?: TrendIcon
  size?: TrendPillSize
}

export function TrendPill({ sentiment, value, label, icon = 'none', size = 'md' }: ITrendPillProps) {
  const classes = ['trend-pill', SENTIMENT_TONE_CLASS[sentiment], size === 'sm' ? 'trend-pill-sm' : '']
    .filter(Boolean)
    .join(' ')

  return (
    <span className={classes}>
      {icon !== 'none' && (
        <Icon
          name="trend-up"
          size={size === 'sm' ? 12 : 14}
          className={`trend-pill-icon${icon === 'down' ? ' trend-pill-icon-down' : ''}`}
        />
      )}
      <span>{value}</span>
      {label && <span className="trend-pill-label">{label}</span>}
    </span>
  )
}
