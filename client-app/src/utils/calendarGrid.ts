export function toIsoDate(date: Date): string {
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')
  return `${date.getFullYear()}-${month}-${day}`
}

// A month grid always renders 6 full weeks (42 days) so the layout height never shifts between
// months, which means the visible range can spill into the adjacent month on either end.
export function buildMonthGridDays(year: number, month: number): Date[] {
  const firstOfMonth = new Date(year, month, 1)
  const gridStart = new Date(year, month, 1 - firstOfMonth.getDay())

  return Array.from({ length: 42 }, (_, index) => {
    const date = new Date(gridStart)
    date.setDate(gridStart.getDate() + index)
    return date
  })
}

export function getMonthGridRange(year: number, month: number): { fromDate: string; toDate: string } {
  const gridDays = buildMonthGridDays(year, month)
  return { fromDate: toIsoDate(gridDays[0]), toDate: toIsoDate(gridDays[gridDays.length - 1]) }
}
