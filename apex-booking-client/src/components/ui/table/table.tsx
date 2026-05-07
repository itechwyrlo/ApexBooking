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

  return (
    <div className="table-responsive">
      <table className="table table-hover align-middle">
        <thead className="table-light">
          <tr>
            {enhancedColumns.map((col, index) => {
              if ("type" in col && col.type === "selection") {
                const ids = data.map((row) => getRowId(row));
                const allSelected =
                  ids.length > 0 &&
                  ids.every((id) => selection?.selectedIds.has(id));

                return (
                  <th key={index} className="border-0">
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

              return (
                <th
                  key={index}
                  className="border-0 text-secondary fw-semibold"
                >
                  {col.header}
                </th>
              );
            })}
          </tr>
        </thead>

        <tbody>
          {data.map((row, rowIndex) => (
            <tr
              key={rowIndex}
              onClick={() => onRowClick?.(row)}
              onMouseEnter={() => onRowHover?.(row)}
              className={onRowClick ? "cursor-pointer" : ""}
            >
              {enhancedColumns.map((col, colIndex) => {
                /**
                 * Selection column
                 */
                if ("type" in col && col.type === "selection") {
                  const id = getRowId(row);
                  const checked =
                    selection?.selectedIds.has(id) ?? false;

                  return (
                    <td key={colIndex}>
                      <div className="form-check">
                        <input
                          className="form-check-input"
                          type="checkbox"
                          checked={checked}
                          onChange={() => selection?.toggleRow(id)}
                        />
                      </div>
                    </td>
                  );
                }

                /**
                 * Action column
                 */
                if ("type" in col && col.type === "button") {
                  const actionCol = col as ActionColumn<T>;

                  return (
                    <td key={colIndex}>
                      <button
                        className="btn btn-sm btn-outline-primary"
                        onClick={(e) => {
                          e.stopPropagation();
                          actionCol.onClick(row);
                        }}
                      >
                        {actionCol.label}
                      </button>
                    </td>
                  );
                }

                /**
                 * Data column
                 */
                const dataCol = col as DataColumn<T>;
                const value = row[dataCol.key];

                const field = getSchemaField(dataCol.key);

                return (
                  <td key={colIndex}>
                    {dataCol.render
                      ? dataCol.render(value, row)
                      : field?.render
                      ? field.render(value, row)
                      : defaultRenderer(field as any, value)}
                  </td>
                );
              })}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}