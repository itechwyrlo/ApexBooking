import { useEffect, useState } from "react";
import type { FormModalProps } from "./types";
import { FieldRenderer } from "./renderField";

export function FormModal<T>({
  isOpen,
  title,
  fields,
  value,
  onChange,
  onSubmit,
  onClose,
}: FormModalProps<T>) {
  const [localValue, setLocalValue] = useState<T>(value);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [fieldContext, setFieldContext] = useState<Record<string, any>>({});

  useEffect(() => {
    setLocalValue(value);
  }, [value]);

  const updateField = (key: keyof T, fieldValue: any) => {
    const next = {
      ...localValue,
      [key]: fieldValue,
    };

    const nextContext = {
      ...fieldContext,
      [key as string]: fieldValue,
    };

    setLocalValue(next);
    setFieldContext(nextContext);
    onChange(next);
  };

  const handleSave = async () => {
    setIsSubmitting(true);
    try {
      await onSubmit(localValue);
    } finally {
      setIsSubmitting(false);
    }
  };

  if (!isOpen) return null;

  return (
    <div
      className="position-fixed top-0 start-0 w-100 h-100 d-flex align-items-center justify-content-center"
      style={{
        zIndex: 1050,
        background: "rgba(15, 23, 42, 0.45)",
        backdropFilter: "blur(6px)",
      }}
    >
      <div
        className="bg-white rounded-4 shadow-lg w-100"
        style={{ maxWidth: "520px" }}
      >
        <div className="px-4 py-3 border-bottom d-flex justify-content-between">
          <div className="fw-semibold">{title}</div>
          <button onClick={onClose} className="btn btn-light btn-sm">
            ×
          </button>
        </div>

        <div
          className="px-4 py-4"
          style={{ maxHeight: "65vh", overflowY: "auto" }}
        >
          <div className="d-flex flex-column gap-3">
            {fields.map((field) => {
              const v = (localValue as any)[field.key];
              return (
                <div
                  key={String(field.key)}
                  className="p-3 border rounded-3 bg-light-subtle"
                >
                  <FieldRenderer
                    field={field}
                    value={v}
                    formValue={localValue}
                    onChange={(val) => updateField(field.key, val)}
                  />
                </div>
              );
            })}
          </div>
        </div>

        <div className="px-4 py-3 border-top d-flex justify-content-end gap-2">
          <button
            className="btn btn-light"
            onClick={onClose}
            disabled={isSubmitting}
          >
            Cancel
          </button>
          <button
            className="btn btn-primary"
            onClick={handleSave}
            disabled={isSubmitting}
          >
            {isSubmitting ? "Saving..." : "Save"}
          </button>
        </div>
      </div>
    </div>
  );
}
