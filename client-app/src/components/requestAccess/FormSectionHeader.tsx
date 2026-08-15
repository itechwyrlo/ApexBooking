import { Icon } from '../common/Icon'

interface IFormSectionHeaderProps {
  icon: string
  label: string
}

export function FormSectionHeader({ icon, label }: IFormSectionHeaderProps) {
  return (
    <div className="form-section-header">
      <div className="form-section-header__title">
        <Icon name={icon} size={18} />
        <span className="text-eyebrow mb-0">{label}</span>
      </div>
      <hr className="form-section-header__divider" />
    </div>
  )
}
