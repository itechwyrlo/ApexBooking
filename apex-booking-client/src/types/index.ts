export interface SidebarItem {
  id: string;
  label: string;
  icon: string; // FontAwesome icon name
  path?: string;
  children?: SidebarItem[];
  isGroup?: boolean;
}


export interface PagedResult<T> {
  data: T[];
  total: number;
}