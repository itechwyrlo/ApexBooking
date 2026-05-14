import React from "react";

export type FieldType =
  | "string"
  | "number"
  | "boolean"
  | "textarea"
  | "select"
  | "multiselect"
  | "time"
  | "date"
  | "phone"
  | "custom";

export type Option = {
  label: string;
  value: string;
};

/**
 * NEW: DataSource for dynamic options
 */
export type DataSource =
  | {
      mode: "static";
      options: Option[];
    }
  | {
      mode: "local";
      options: Option[];
      filter: (options: Option[], query: string) => Option[];
    }
  | {
      mode: "remote";
      fetch: (query: string) => Promise<Option[]>;
      debounceMs?: number;
    };

export type ModelSchema<T> = {
  key: keyof T;
  label: string;
  type: FieldType;

  readonly?: boolean;
  required?: boolean;
  placeholder?: string;

  validation?: {
    min?: number;
    max?: number;
  };

  table?: boolean;

  form?: boolean;

  render?: (value: any, row: T) => React.ReactNode;

  /**
   * Keep for backward compatibility
   */
  options?: Option[];

  /**
   * NEW: dynamic data source
   */
  dataSource?: DataSource;
};

/**
 * TABLE
 */

export type DataColumn<T> = {
  key: keyof T | (string & {}); 
  header: string;
  render?: (value: any, row: T) => React.ReactNode;
};


export type ActionColumn<T> = {
  type: "button";
  header: string;
  label: string;
  onClick: (row: T) => void;
};

export type SelectionColumn = {
  key: string;
  header: string;
  type: "selection";
};

export type Column<T> = DataColumn<T> | ActionColumn<T> | SelectionColumn;

export type SelectionModel = {
  selectedIds: Set<string>;
  toggleRow: (id: string) => void;
  selectAll: (ids: string[]) => void;
  clear: () => void;
};

export type TableProps<T> = {
  data: T[];
  columns: Column<T>[];
  getRowId: (row: T) => string;

  schema?: ModelSchema<T>[];

  selection?: SelectionModel;

  onRowClick?: (row: T) => void;
  onRowHover?: (row: T) => void;
};
