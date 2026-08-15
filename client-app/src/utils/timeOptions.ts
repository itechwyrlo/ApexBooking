export interface ITimeOption {
  value: string
  label: string
}

function formatLabel(hour24: number, minute: number): string {
  const period = hour24 < 12 ? 'AM' : 'PM'
  const hour12 = hour24 % 12 === 0 ? 12 : hour24 % 12
  return `${hour12}:${minute.toString().padStart(2, '0')} ${period}`
}

export function generateTimeOptions(intervalMinutes = 30): ITimeOption[] {
  const options: ITimeOption[] = []

  for (let totalMinutes = 0; totalMinutes < 24 * 60; totalMinutes += intervalMinutes) {
    const hour24 = Math.floor(totalMinutes / 60)
    const minute = totalMinutes % 60
    const value = `${hour24.toString().padStart(2, '0')}:${minute.toString().padStart(2, '0')}`
    options.push({ value, label: formatLabel(hour24, minute) })
  }

  return options
}
