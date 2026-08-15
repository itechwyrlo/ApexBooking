import { Icon } from './Icon'

interface IPaginationProps {
  currentPage: number
  totalPages: number
  onPageChange: (page: number) => void
  className?: string
}

/**
 * Builds an abbreviated page list, e.g. [1, '…', 4, 5, 6, '…', 12].
 * Always keeps the first page, last page, and a window around the current page.
 */
function buildPageList(currentPage: number, totalPages: number): (number | 'ellipsis')[] {
  const pages: (number | 'ellipsis')[] = []
  const windowStart = Math.max(2, currentPage - 1)
  const windowEnd = Math.min(totalPages - 1, currentPage + 1)

  pages.push(1)
  if (windowStart > 2) pages.push('ellipsis')
  for (let page = windowStart; page <= windowEnd; page++) pages.push(page)
  if (windowEnd < totalPages - 1) pages.push('ellipsis')
  if (totalPages > 1) pages.push(totalPages)

  return pages
}

export function Pagination({ currentPage, totalPages, onPageChange, className = '' }: IPaginationProps) {
  if (totalPages <= 1) return null

  const pages = buildPageList(currentPage, totalPages)

  return (
    <nav aria-label="Pagination" className={className}>
      <ul className="pagination mb-0 flex-wrap">
        <li className={`page-item${currentPage === 1 ? ' disabled' : ''}`}>
          <button
            type="button"
            className="page-link d-flex align-items-center justify-content-center"
            style={{ minHeight: '2.75rem', minWidth: '2.75rem' }}
            title="Previous page"
            aria-label="Previous page"
            disabled={currentPage === 1}
            onClick={() => onPageChange(currentPage - 1)}
          >
            <Icon name="chevron-left" size={14} />
          </button>
        </li>

        {pages.map((page, index) =>
          page === 'ellipsis' ? (
            <li key={`ellipsis-${index}`} className="page-item d-none d-sm-block">
              <span className="pagination-ellipsis">&hellip;</span>
            </li>
          ) : (
            <li key={page} className={`page-item d-none d-sm-block${page === currentPage ? ' active' : ''}`.trim()}>
              <button
                type="button"
                className="page-link"
                aria-current={page === currentPage ? 'page' : undefined}
                aria-label={`Page ${page}`}
                onClick={() => onPageChange(page)}
              >
                {page}
              </button>
            </li>
          ),
        )}

        <li className="page-item d-sm-none d-flex align-items-center">
          <span className="pagination-ellipsis" aria-hidden="true">
            {currentPage} of {totalPages}
          </span>
        </li>

        <li className={`page-item${currentPage === totalPages ? ' disabled' : ''}`}>
          <button
            type="button"
            className="page-link d-flex align-items-center justify-content-center"
            style={{ minHeight: '2.75rem', minWidth: '2.75rem' }}
            title="Next page"
            aria-label="Next page"
            disabled={currentPage === totalPages}
            onClick={() => onPageChange(currentPage + 1)}
          >
            <Icon name="chevron-right" size={14} />
          </button>
        </li>
      </ul>
    </nav>
  )
}
