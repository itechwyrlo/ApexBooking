import { useEffect, useState } from "react";
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faXmark } from '@fortawesome/free-solid-svg-icons';
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
  notice,
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
    <>
      <div className="modal-backdrop show" />
      <div className="modal show d-block" tabIndex={-1}>
        <div className="modal-dialog modal-dialog-centered form-modal-dialog">
          <div className="modal-content">
            <div className="px-4 py-3 border-bottom d-flex justify-content-between">
              <div className="fw-semibold">{title}</div>
              <button type="button" onClick={onClose} className="btn btn-light btn-sm" aria-label="Close">
                <FontAwesomeIcon icon={faXmark} />
              </button>
            </div>

            <div className="px-4 py-4 form-modal-body">
              {notice && (
                <div className="alert alert-info py-2 px-3 mb-3 small d-flex align-items-center gap-2">
                  <i className="fas fa-envelope" />
                  {notice}
                </div>
              )}
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
      </div>
    </>
  );
}
