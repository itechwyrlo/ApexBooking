import type { IconDefinition } from '@fortawesome/fontawesome-svg-core';
import {
  faTachometerAlt,
  faCalendarAlt,
  faCalendarDays,
  faUsers,
  faBriefcase,
  faCog,
  faUser,
  faClock,
  faAddressBook,
  faBuilding,
  faCreditCard,
} from '@fortawesome/free-solid-svg-icons';

export interface SidebarChild {
  id: string;
  label: string;
  icon: IconDefinition;
  path?: string;
  children?: SidebarChild[];
}

export interface SidebarGroup {
  id: string;
  label: string;
  isGroup: boolean;
  children: SidebarChild[];
}

export const getSidebarConfig = (role: string): SidebarGroup[] => {
  if (role === 'superadmin') {
    return [
      {
        id: 'platform',
        label: 'Platform',
        isGroup: true,
        children: [
          { id: 'overview', label: 'Overview', icon: faTachometerAlt, path: '/superadmin' },
          { id: 'organizations', label: 'Organizations', icon: faBuilding, path: '/superadmin/organizations' },
          { id: 'payment-gateway', label: 'Payment Gateway', icon: faCreditCard, path: '/superadmin/payment-gateway' },
        ],
      },
    ];
  }

  if (role === 'staff') {
    return [
      {
        id: 'main',
        label: 'Main',
        isGroup: true,
        children: [
          { id: 'dashboard', label: 'Dashboard', icon: faTachometerAlt, path: '/dashboard' },
          { id: 'bookings', label: 'My Bookings', icon: faCalendarAlt, path: '/bookings' },
          { id: 'calendar', label: 'Calendar', icon: faCalendarDays, path: '/calendar' },
          { id: 'my-availability', label: 'My Availability', icon: faClock, path: '/my-availability' },
        ],
      },
      {
        id: 'account',
        label: 'Account',
        isGroup: true,
        children: [
          { id: 'my-profile', label: 'My Profile', icon: faUser, path: '/my-profile' },
        ],
      },
    ];
  }

  const adminGroups: SidebarGroup[] = [
    {
      id: 'main',
      label: 'Main',
      isGroup: true,
      children: [
        { id: 'dashboard', label: 'Dashboard', icon: faTachometerAlt, path: '/dashboard' },
        { id: 'bookings', label: 'Bookings', icon: faCalendarAlt, path: '/bookings' },
        { id: 'calendar', label: 'Calendar', icon: faCalendarDays, path: '/calendar' },
        { id: 'clients', label: 'Clients', icon: faAddressBook, path: '/clients' },
        { id: 'staff', label: 'My Team', icon: faUsers, path: '/staff' },
        { id: 'services', label: 'Services', icon: faBriefcase, path: '/services' },
      ],
    },
  ];

  if (role === 'tenantadmin') {
    adminGroups.push({
      id: 'system',
      label: 'System',
      isGroup: true,
      children: [
        { id: 'settings', label: 'Settings', icon: faCog, path: '/settings' },
      ],
    });
  }

  return adminGroups;
};
