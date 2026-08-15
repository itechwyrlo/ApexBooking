import { Skeleton } from './Skeleton'

interface ITableSkeletonProps {
  rows?: number
  columns?: number
}

export function TableSkeleton({ rows = 5, columns = 4 }: ITableSkeletonProps) {
  return (
    <div className="table-responsive">
      <span className="visually-hidden" role="status">
        Loading data
      </span>
      <table className="table align-middle mb-0" aria-hidden="true">
        <tbody>
          {Array.from({ length: rows }).map((_, rowIndex) => (
            <tr key={rowIndex}>
              {Array.from({ length: columns }).map((__, columnIndex) => (
                <td key={columnIndex}>
                  <Skeleton height="1rem" />
                </td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
