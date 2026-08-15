/*
  Backfill: pre-existing refund status data → post-"RefundManualTransferRedesign" enum shape
  ============================================================================================
  Migration: src/backend-api/ApexBooking.Core.Persistence/Migrations/20260815023630_RefundManualTransferRedesign.cs

  That migration changed the SCHEMA only (columns added/dropped) — EF Core migrations never
  rewrite existing row data. It landed alongside a C# enum collapse:

      RefundStatus:        None, Pending, Processing, Succeeded, Failed   →  None, Pending, Refunded, Rejected
      RefundRequestStatus:  PendingReview, AwaitingOwnerApproval, Approved, Rejected, Processing,
                             AwaitingManualTransfer, ManuallyRefunded, Succeeded, Failed
                             → PendingReview, Refunded, Rejected

  Both columns are mapped via `HasConversion<string>()`, i.e. the enum member's name is stored
  literally as text. Any row written before the collapse still holds one of the old, now-retired
  names. The moment EF tries to materialize such a row, it throws:

      System.InvalidOperationException: Cannot convert string value 'Failed' from the database
      to any value in the mapped 'RefundStatus' enum.

  WHEN TO RUN THIS
  ----------------
  Once, against any environment (a teammate's machine, staging, a restored backup) whose database
  has applied `RefundManualTransferRedesign` — or is about to — but still contains bookings/refund
  requests created before 2026-08-15. Safe to run at any time relative to that migration; it only
  ever touches rows holding a retired string value, so it is a harmless no-op everywhere else,
  including a fresh database that never had pre-redesign data. Safe to re-run.

  Run in SSMS (or `sqlcmd -i` / `Invoke-Sqlcmd`) against the target database. Not part of the
  `dotnet ef migrations` pipeline by design — see the repo cleanup/debugging session on
  2026-08-15 for why this stayed a standalone script instead of being folded into the (already-
  applied, should-stay-immutable) EF migration.

  MAPPING APPLIED
  ----------------
  bookings.refund_status          '' / NULL     → 'None'      (no refund process ever ran)
  bookings.refund_status          'Succeeded'   → 'Refunded'  (direct rename)
  bookings.refund_status          'Failed'      → 'Rejected'  (terminal, no money moved — closest surviving state)
  refund_requests.status          'ManuallyRefunded'      → 'Refunded'       (had actually completed — fact preserved)
  refund_requests.status          'AwaitingManualTransfer'→ 'PendingReview'  (never completed — reopened, not falsely closed)
  refund_requests.status          'Approved'              → 'PendingReview' (decided but never executed — same reasoning)
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRAN;

    UPDATE dbo.bookings
    SET refund_status = 'None'
    WHERE refund_status = '' OR refund_status IS NULL;

    UPDATE dbo.bookings
    SET refund_status = 'Refunded'
    WHERE refund_status = 'Succeeded';

    UPDATE dbo.bookings
    SET refund_status = 'Rejected'
    WHERE refund_status = 'Failed';

    UPDATE dbo.refund_requests
    SET status = 'Refunded'
    WHERE status = 'ManuallyRefunded';

    UPDATE dbo.refund_requests
    SET status = 'PendingReview'
    WHERE status IN ('AwaitingManualTransfer', 'Approved');

COMMIT TRAN;

-- Verification: every remaining value must be a member of the current enum. Both result sets
-- should only ever show None/Pending/Refunded/Rejected and PendingReview/Refunded/Rejected —
-- if anything else appears, a value this script doesn't know about slipped through and needs
-- its own UPDATE added above before re-running.
SELECT 'bookings.refund_status' AS column_name, refund_status AS value, COUNT(*) AS row_count
FROM dbo.bookings
GROUP BY refund_status
UNION ALL
SELECT 'refund_requests.status', status, COUNT(*)
FROM dbo.refund_requests
GROUP BY status
ORDER BY column_name, value;
