import { toIsoDate } from './calendarGrid'

export type RevenuePeriod = 'today' | 'week' | 'month'

export interface IDateRange {
  fromDate: string
  toDate: string
}

// Calendar week (Monday–Sunday) and calendar month (1st–last day), both anchored to the browser's
// local "now" — same convention every other dashboard widget already uses for "today".
export function getPeriodRange(period: RevenuePeriod, now: Date = new Date()): IDateRange {
  if (period === 'today') {
    const today = toIsoDate(now)
    return { fromDate: today, toDate: today }
  }

  if (period === 'week') {
    const dayOfWeek = now.getDay() // 0 = Sunday
    const mondayOffset = dayOfWeek === 0 ? -6 : 1 - dayOfWeek
    const monday = new Date(now.getFullYear(), now.getMonth(), now.getDate() + mondayOffset)
    const sunday = new Date(monday.getFullYear(), monday.getMonth(), monday.getDate() + 6)
    return { fromDate: toIsoDate(monday), toDate: toIsoDate(sunday) }
  }

  const firstOfMonth = new Date(now.getFullYear(), now.getMonth(), 1)
  const lastOfMonth = new Date(now.getFullYear(), now.getMonth() + 1, 0)
  return { fromDate: toIsoDate(firstOfMonth), toDate: toIsoDate(lastOfMonth) }
}
