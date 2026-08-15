import type { INavItem } from '../interfaces/INavItem'
import { SECTION_IDS } from '../constants/sectionIds'

export const NAV_ITEMS: INavItem[] = [
  { label: 'Home', href: `#${SECTION_IDS.HOME}` },
  { label: 'Features', href: `#${SECTION_IDS.FEATURES}` },
  { label: 'Pricing', href: `#${SECTION_IDS.PRICING}` },
  { label: 'Contact', href: `#${SECTION_IDS.CONTACT}` },
]
