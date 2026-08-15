export interface IThemePalette {
  id: string
  name: string
  /** Swatch color shown in the picker UI — matches the palette's light-mode --color-primary. */
  swatchColor: string
}

// Mirrors ApexBooking.Core.Domain.Entities.BusinessProfile.KnownPaletteIds — keep in sync.
// Each id has a light AND dark CSS variant defined together in styles/theme.css, so every
// entry here is guaranteed to already have both rendered and checked for legibility.
export const THEME_PALETTES: IThemePalette[] = [
  { id: 'indigo', name: 'Indigo', swatchColor: '#4f46e5' },
  { id: 'teal', name: 'Teal', swatchColor: '#0d9488' },
  { id: 'rose', name: 'Rose', swatchColor: '#db2777' },
  { id: 'amber', name: 'Amber', swatchColor: '#d97706' },
  { id: 'forest', name: 'Forest', swatchColor: '#15803d' },
  { id: 'slate', name: 'Slate', swatchColor: '#475569' },
]
