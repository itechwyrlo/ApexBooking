// Mirrors the backend's BranchTimeZoneConverter — every branch converts UTC "now" using its own
// configured IANA time zone (Branch.TimeZoneId) for lead-time and walk-in availability calculations.
export function getBranchLocalNow(timeZoneId: string): { date: string; time: string } {
  const parts = new Intl.DateTimeFormat('en-CA', {
    timeZone: timeZoneId,
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
    hour12: false,
  }).formatToParts(new Date())

  const get = (type: string) => parts.find((part) => part.type === type)?.value ?? '00'

  return {
    date: `${get('year')}-${get('month')}-${get('day')}`,
    time: `${get('hour')}:${get('minute')}`,
  }
}
