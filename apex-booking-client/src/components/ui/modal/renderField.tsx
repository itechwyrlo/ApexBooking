import { useEffect, useRef, useState } from "react";
import type { ModelSchema, Option } from "../table/types";

type Props<T> = {
  field: ModelSchema<T>;
  value: any;
  formValue?: T;
  onChange: (value: any) => void;
};

export function FieldRenderer<T>({
  field,
  value,
  onChange,
}: Props<T>) {
  const disabled = field.readonly === true;

  const baseInput =
    "form-control bg-white border rounded-3 px-3 py-2 shadow-none";

  const labelStyle: React.CSSProperties = {
    fontSize: "13px",
    fontWeight: 600,
    marginBottom: "6px",
  };

  const dataSource = field.dataSource;

  const [query, setQuery] = useState("");
  const [options, setOptions] = useState<Option[]>(field.options ?? []);
  const [showDropdown, setShowDropdown] = useState(false);

  const containerRef = useRef<HTMLDivElement | null>(null);

  useEffect(() => {
    let active = true;

    const delay =
      dataSource?.mode === "remote"
        ? dataSource.debounceMs ?? 300
        : 0;

    const handler = setTimeout(async () => {
      if (!dataSource) {
        if (field.options) setOptions(field.options);
        return;
      }

      if (dataSource.mode === "static") {
        setOptions(dataSource.options);
        return;
      }

      if (dataSource.mode === "local") {
        const filtered = dataSource.filter(dataSource.options, query);
        setOptions(filtered);
        return;
      }

      if (dataSource.mode === "remote") {
        const result = await dataSource.fetch(query);
        if (active) setOptions(result);
      }
    }, delay);

    return () => {
      active = false;
      clearTimeout(handler);
    };
  }, [query, field, dataSource]);

  useEffect(() => {
    const handleClickOutside = (e: MouseEvent) => {
      if (!containerRef.current) return;

      if (!containerRef.current.contains(e.target as Node)) {
        setShowDropdown(false);
        setQuery("");
      }
    };

    document.addEventListener("mousedown", handleClickOutside);

    return () => {
      document.removeEventListener("mousedown", handleClickOutside);
    };
  }, []);

  switch (field.type) {
    case "string":
      return (
        <div>
          <label style={labelStyle}>{field.label}</label>
          <input
            className={baseInput}
            value={value ?? ""}
            disabled={disabled}
            onChange={(e) => onChange(e.target.value)}
          />
        </div>
      );

    case "number":
      return (
        <div>
          <label style={labelStyle}>{field.label}</label>
          <input
            className={baseInput}
            type="number"
            value={value ?? 0}
            disabled={disabled}
            onChange={(e) => onChange(Number(e.target.value))}
          />
        </div>
      );

    case "textarea":
      return (
        <div>
          <label style={labelStyle}>{field.label}</label>
          <textarea
            className={baseInput}
            value={value ?? ""}
            disabled={disabled}
            rows={3}
            onChange={(e) => onChange(e.target.value)}
          />
        </div>
      );

    case "date":
      return (
        <div>
          <label style={labelStyle}>{field.label}</label>
          <input
            className={baseInput}
            type="date"
            value={value ?? ""}
            disabled={disabled}
            onChange={(e) => onChange(e.target.value)}
          />
        </div>
      );

    case "time":
      return (
        <div>
          <label style={labelStyle}>{field.label}</label>
          <input
            className={baseInput}
            type="time"
            value={value ?? ""}
            disabled={disabled}
            onChange={(e) => onChange(e.target.value)}
          />
        </div>
      );

    case "select": {
      const selectedOption = options.find((o) => o.value === value);

      const filteredOptions = options.filter(
        (o) =>
          query.trim() === "" ||
          o.label.toLowerCase().includes(query.toLowerCase())
      );

      const selectValue = (val: string) => {
        onChange(val);
        setQuery("");
        setShowDropdown(false);
      };

      return (
        <div>
          <label style={labelStyle}>{field.label}</label>

          <div className="position-relative" ref={containerRef}>
            <div
              className="form-control bg-white border rounded-3 d-flex align-items-center px-2"
              style={{ minHeight: "42px" }}
              onClick={() => setShowDropdown(true)}
            >
              <input
                className="border-0 bg-transparent flex-grow-1"
                style={{ outline: "none" }}
                placeholder={selectedOption ? selectedOption.label : "Select..."}
                value={query}
                disabled={disabled}
                onChange={(e) => {
                  setQuery(e.target.value);
                  setShowDropdown(true);
                }}
                onFocus={() => setShowDropdown(true)}
              />

              {selectedOption && !query && (
                <span className="text-muted small">
                  {selectedOption.label}
                </span>
              )}
            </div>

            {showDropdown && !disabled && (
              <div
                className="position-absolute w-100 border rounded-3 bg-white shadow-sm mt-1"
                style={{ maxHeight: 220, overflowY: "auto", zIndex: 1000 }}
              >
                {filteredOptions.map((o) => (
                  <div
                    key={o.value}
                    className="px-3 py-2 d-flex justify-content-between"
                    style={{ cursor: "pointer" }}
                    onMouseDown={(e) => {
                      e.preventDefault();
                      selectValue(o.value);
                    }}
                  >
                    <span>{o.label}</span>
                    {o.value === value && (
                      <span className="text-muted small">✓</span>
                    )}
                  </div>
                ))}

                {filteredOptions.length === 0 && (
                  <div className="px-3 py-2 text-muted small">
                    No results
                  </div>
                )}
              </div>
            )}
          </div>
        </div>
      );
    }

    case "multiselect": {
      const selected = Array.isArray(value) ? value : [];

      const selectedOptions = options.filter((o) =>
        selected.includes(o.value)
      );

      const filteredOptions = options.filter((o) => {
        const matchQuery =
          query.trim() === "" ||
          o.label.toLowerCase().includes(query.toLowerCase());

        const notSelected = !selected.includes(o.value);

        return matchQuery && notSelected;
      });

      const removeChip = (val: string) => {
        onChange(selected.filter((v) => v !== val));
      };

      const addValue = (val: string) => {
        if (selected.includes(val)) return;
        onChange([...selected, val]);
        setQuery("");
      };

      return (
        <div>
          <label style={labelStyle}>{field.label}</label>

          <div className="position-relative" ref={containerRef}>
            <div
              className="form-control bg-white border rounded-3 d-flex flex-wrap gap-2 align-items-center p-2"
              style={{ minHeight: "42px" }}
              onClick={() => setShowDropdown(true)}
            >
              {selectedOptions.map((o) => (
                <span
                  key={o.value}
                  className="d-flex align-items-center gap-1 bg-primary bg-opacity-10 text-primary px-2 py-1 rounded-pill"
                  style={{ fontSize: "12px" }}
                >
                  {o.label}

                  {!disabled && (
                    <button
                      type="button"
                      onClick={(e) => {
                        e.stopPropagation();
                        removeChip(o.value);
                      }}
                      style={{
                        border: 0,
                        background: "transparent",
                        fontSize: "12px",
                        cursor: "pointer",
                      }}
                    >
                      ×
                    </button>
                  )}
                </span>
              ))}

              <input
                className="border-0 bg-transparent flex-grow-1"
                style={{ minWidth: "120px", outline: "none" }}
                placeholder={selected.length === 0 ? "Search..." : ""}
                value={query}
                disabled={disabled}
                onChange={(e) => {
                  setQuery(e.target.value);
                  setShowDropdown(true);
                }}
                onFocus={() => setShowDropdown(true)}
              />
            </div>

            {showDropdown && !disabled && (
              <div
                className="position-absolute w-100 border rounded-3 bg-white shadow-sm mt-1"
                style={{ maxHeight: 220, overflowY: "auto", zIndex: 1000 }}
              >
                {filteredOptions.map((o) => (
                  <div
                    key={o.value}
                    className="px-3 py-2 d-flex justify-content-between"
                    style={{ cursor: "pointer" }}
                    onMouseDown={(e) => {
                      e.preventDefault();
                      addValue(o.value);
                    }}
                  >
                    <span>{o.label}</span>
                  </div>
                ))}

                {filteredOptions.length === 0 && (
                  <div className="px-3 py-2 text-muted small">
                    No results
                  </div>
                )}
              </div>
            )}
          </div>
        </div>
      );
    }

    case "boolean":
      return (
        <div className="d-flex justify-content-between align-items-center">
          <label style={labelStyle}>{field.label}</label>
          <input
            type="checkbox"
            className="form-check-input"
            checked={!!value}
            disabled={disabled}
            onChange={(e) => onChange(e.target.checked)}
          />
        </div>
      );

    default:
      return null;
  }
}