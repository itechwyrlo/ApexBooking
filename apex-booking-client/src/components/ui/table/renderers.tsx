import type { ModelSchema } from "./types";

export function defaultRenderer(
  field: ModelSchema<any> | undefined,
  value: any
) {
  if (!field) return String(value ?? "");

  switch (field.type) {
    case "boolean":
      return (
        <span style={{ color: value ? "green" : "red" }}>
          {value ? "Yes" : "No"}
        </span>
      );

    case "number":
      return value?.toLocaleString?.() ?? value;

    case "textarea":
      return (
        <span title={value}>
          {String(value ?? "").slice(0, 40)}
          {value?.length > 40 && "..."}
        </span>
      );

    default:
      return String(value ?? "");
  }
}