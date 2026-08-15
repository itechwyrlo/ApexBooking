import type { IFeature } from '../interfaces/IFeature'

export const BOOKING_FEATURES: IFeature[] = [
  {
    id: 'online-booking',
    title: 'Online Booking',
    description: 'Customers can book appointments online, any time, without calling in.',
    icon: '/assets/icons/globe.svg',
    size: 'large',
  },
  {
    id: 'customer-booking-page',
    title: 'Customer Booking Page',
    description: 'A public booking page customers can use to pick a service, time, and staff member.',
    icon: '/assets/icons/appointments.svg',
  },
  {
    id: 'staff-management',
    title: 'Team Management',
    description: 'Manage team schedules, availability, and appointment assignments.',
    icon: '/assets/icons/staff.svg',
  },
  {
    id: 'service-management',
    title: 'Service Management',
    description: 'Create and organize the services customers can book.',
    icon: '/assets/icons/services.svg',
  },
  {
    id: 'booking-calendar',
    title: 'Booking Calendar',
    description: 'A shared calendar showing every appointment across your team.',
    icon: '/assets/icons/calendar.svg',
    size: 'large',
  },
  {
    id: 'dashboard-reports',
    title: 'Dashboard Reports',
    description: 'See booking volume, team activity, and business insights at a glance.',
    icon: '/assets/icons/chart.svg',
    size: 'large',
  },
  {
    id: 'email-notifications',
    title: 'Email Notifications',
    description: 'Automatic booking confirmations and reminders sent by email.',
    icon: '/assets/icons/mail.svg',
  },
  {
    id: 'sms-notifications',
    title: 'SMS Notifications',
    description: 'Booking reminders sent directly to customer phones.',
    icon: '/assets/icons/chat.svg',
    comingSoon: true,
  },
]
