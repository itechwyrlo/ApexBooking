import type { ModelSchema } from "../table/types";

export type FormModalProps<T> = {
  isOpen: boolean;
  title: string;
  fields: ModelSchema<T>[];
  value: T;
  onChange: (value: T) => void;
  onSubmit: (value: T) => Promise<void>;
  onClose: () => void;
};
