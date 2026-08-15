import type { IProblem } from '../interfaces/IProblem'

export const PROBLEMS: IProblem[] = [
  {
    id: 'double-bookings',
    icon: 'double-booking',
    title: 'Double bookings',
    description: "Manual calendars and spreadsheets can't catch scheduling conflicts before they happen.",
  },
  {
    id: 'no-shows',
    icon: 'no-shows',
    title: 'No-shows',
    description: 'Without automatic reminders, missed appointments quietly eat into revenue every week.',
  },
  {
    id: 'back-and-forth-messaging',
    icon: 'messaging',
    title: 'Endless back-and-forth',
    description: 'Confirming a time over text or phone wastes time for you and your customers.',
  },
  {
    id: 'no-visibility',
    icon: 'no-visibility',
    title: 'No visibility',
    description: "Without a dashboard, you don't know your busiest hours or your best-performing staff.",
  },
]
