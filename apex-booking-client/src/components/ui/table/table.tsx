import type {
  TableProps,
  Column,
  SelectionColumn,
  DataColumn,
  ActionColumn,
} from "./types";

import { defaultRenderer } from "./renderers";

export function Table<T extends Record<string, any>>({
  data,
  columns,
  schema,
  selection,
  getRowId,
  onRowClick,
  onRowHover,
}: TableProps<T>) {
  const enhancedColumns: Column<T>[] = selection
    ? [
        {
          key: "__selection",
          header: "",
          type: "selection",
        } as SelectionColumn,
        ...columns,
      ]
    : columns;

  const getSchemaField = (key: keyof T) =>
    schema?.find((f) => f.key === key);

  const renderCellContent = (col: Column<T>, row: T) => {
    if ("type" in col && col.type === "selection") {
      const id = getRowId(row);
      const checked = selection?.selectedIds.has(id) ?? false;
      return (
        <div className="form-check">
          <input
            className="form-check-input"
            type="checkbox"
            checked={checked}
            onChange={() => selection?.toggleRow(id)}
          />
        </div>
      );
    }

    if ("type" in col && col.type === "button") {
      const actionCol = col as ActionColumn<T>;
      return (
        <button
          className="btn btn-sm btn-outline-primary"
          onClick={(e) => {
            e.stopPropagation();
            actionCol.onClick(row);
          }}
        >
          {actionCol.label}
        </button>
      );
    }

    const dataCol = col as DataColumn<T>;
    const value = row[dataCol.key];
    const field = getSchemaField(dataCol.key);

    return dataCol.render
      ? dataCol.render(value, row)
      : field?.render
      ? field.render(value, row)
      : defaultRenderer(field as any, value);
  };

  return (
    <>
      {/* Desktop: full table with header — hidden below 768px */}
      <div className="apex-table-desktop">
        <div className="table-responsive">
          <table className="apex-table">
            <thead>
              <tr>
                {enhancedColumns.map((col, index) => {
                  if ("type" in col && col.type === "selection") {
                    const ids = data.map((row) => getRowId(row));
                    const allSelected =
                      ids.length > 0 &&
                      ids.every((id) => selection?.selectedIds.has(id));

                    return (
                      <th key={index}>
                        <div className="form-check">
                          <input
                            className="form-check-input"
                            type="checkbox"
                            checked={allSelected}
                            onChange={() => {
                              if (!selection) return;
                              if (allSelected) {
                                selection.clear();
                              } else {
                                selection.selectAll(ids);
                              }
                            }}
                          />
                        </div>
                      </th>
                    );
                  }

                  return <th key={index}>{col.header}</th>;
                })}
              </tr>
            </thead>

            <tbody>
              {data.map((row, rowIndex) => (
                <tr
                  key={rowIndex}
                  onClick={() => onRowClick?.(row)}
                  onMouseEnter={() => onRowHover?.(row)}
                  className={onRowClick ? "apex-row-clickable" : ""}
                >
                  {enhancedColumns.map((col, colIndex) => (
                    <td key={colIndex}>{renderCellContent(col, row)}</td>
                  ))}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      {/* Mobile: card list with no header — hidden above 768px */}
      <div className="apex-table-mobile">
        {data.map((row, rowIndex) => (
          <div
            key={rowIndex}
            className={`apex-booking-card${onRowClick ? " apex-row-clickable" : ""}`}
            onClick={() => onRowClick?.(row)}
          >
            {enhancedColumns
              .filter((col) => !("type" in col && col.type === "selection"))
              .map((col, colIndex) => (
                <div key={colIndex} className="apex-booking-card-row">
                  {"type" in col && col.type === "button" ? (
                    renderCellContent(col, row)
                  ) : (
                    <>
                      <span className="apex-booking-card-label">
                        {col.header}
                      </span>
                      <span className="apex-booking-card-value">
                        {renderCellContent(col, row)}
                      </span>
                    </>
                  )}
                </div>
              ))}
          </div>
        ))}
      </div>
    </>
  );
}
