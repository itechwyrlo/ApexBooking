export interface SidebarChild {
  id: string;
  label: string;
  icon: string;
  path?: string;
  children?: SidebarChild[];
}

export interface SidebarGroup {
  id: string;
  label: string;
  isGroup: boolean;
  children: SidebarChild[];
}

export const getSidebarConfig = (): SidebarGroup[] => [
  {
    id: 'main',
    label: 'Main',
    isGroup: true,
    children: [
      {
        id: 'dashboard',
        label: 'Dashboard',
        icon: 'fas fa-chart-pie',
        path: '/dashboard',
      },
    ],
  },
  {
    id: 'resources-group',
    label: 'Resources',
    isGroup: true,
    children: [
      {
        id: 'resources',
        label: 'Resources',
        icon: 'fas fa-layer-group',
        path: '/resources',
      },
      {
        id: 'services',
        label: 'Services',
        icon: 'fas fa-concierge-bell',
        path: '/services',
      },
      {
        id: "bookings",
        label: "Bookings",
        icon: "fa-calendar",
        path: "/bookings",
      }
    ],
  },
  {
    id: 'account',
    label: 'Account',
    isGroup: true,
    children: [
      {
        id: 'profile',
        label: 'Profile',
        icon: 'fas fa-user',
        path: '/profile',
      },
      {
        id: 'settings',
        label: 'Settings',
        icon: 'fas fa-cog',
        path: '/settings',
      },
    ],
  },
];