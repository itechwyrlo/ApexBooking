// Every status enum in this app mirrors a backend C# enum's PascalCase `.ToString()` value
// (e.g. BookingStatus.NoShow = 'NoShow', RefundRequestStatus.AwaitingOwnerApproval =
// 'AwaitingOwnerApproval') — readable for single-word values, but unreadable run together for
// multi-word ones. Three call sites had already solved this, each a different way: an explicit
// label map (CustomerBookingsModal), the same regex as below (RefundRequestTable), or not at all
// (BookingStatusBadge rendered the raw enum value). This is that one regex, shared, so every
// status badge formats consistently — and stays covered if a future single-word value gains a
// second word, which an explicit label map wouldn't unless someone remembered to update it.
export function formatStatusLabel(value: string): string {
  return value.replace(/([a-z])([A-Z])/g, '$1 $2')
}
