export interface SidebarItem {
  id: string;
  label: string;
  icon: string; // FontAwesome icon name
  path?: string;
  children?: SidebarItem[];
  isGroup?: boolean;
}


// Base Response Interface
export interface BaseResponse<T = any> {
  isSuccess: boolean;
  data: T | null;
  errors?: {
    code?: string;
    message: string;
    details?: any[];
  }[];
}

export interface Error {
  code?: string;
  message: string;
  details?: any[];
}


export interface PagedResult<T> {
  data: T[];
  total: number;
}