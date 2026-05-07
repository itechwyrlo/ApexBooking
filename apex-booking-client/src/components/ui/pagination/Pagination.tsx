import React from "react";

type PaginationProps = {
  currentPage: number;
  totalPages: number;
  pageSize: number;
  totalItems: number;

  onPageChange: (page: number) => void;
  onPageSizeChange: (size: number) => void;
};

export const Pagination: React.FC<PaginationProps> = ({
  currentPage,
  totalPages,
  pageSize,
  totalItems,
  onPageChange,
  onPageSizeChange,
}) => {
  const pageSizes = [10, 20, 50];

  return (
    <div className="d-flex justify-content-between align-items-center mt-3">
      {/* LEFT: info */}
      <div className="text-muted small">
        Showing page {currentPage} of {totalPages} • {totalItems} total items
      </div>

      {/* CENTER: page controls */}
      <div className="d-flex align-items-center gap-2">
        <button
          className="btn btn-sm btn-outline-primary"
          disabled={currentPage === 1}
          onClick={() => onPageChange(currentPage - 1)}
        >
          <i className="bi bi-chevron-left"></i>
          Previous
        </button>

        <div className="d-flex align-items-center gap-1">
          <span className="px-3 py-1 bg-primary text-white rounded">{currentPage}</span>
          <span className="text-muted">of {totalPages}</span>
        </div>

        <button
          className="btn btn-sm btn-outline-primary"
          disabled={currentPage === totalPages}
          onClick={() => onPageChange(currentPage + 1)}
        >
          Next
          <i className="bi bi-chevron-right"></i>
        </button>
      </div>

      {/* RIGHT: page size */}
      <div className="d-flex align-items-center gap-2">
        <label className="form-label mb-0 small text-muted">Show:</label>
        <select
          className="form-select form-select-sm"
          style={{ width: "auto" }}
          value={pageSize}
          onChange={(e) => onPageSizeChange(Number(e.target.value))}
        >
          {pageSizes.map((size) => (
            <option key={size} value={size}>
              {size}
            </option>
          ))}
        </select>
      </div>
    </div>
  );
};