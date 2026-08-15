interface IEwalletFieldsProps {
  provider: string
  number: string
  name: string
  disabled?: boolean
  onProviderChange: (provider: string) => void
  onNumberChange: (number: string) => void
  onNameChange: (name: string) => void
}

// Shared between CancelBookingPage (public, customer-facing) and CancelBookingModal (staff,
// cancelling on a customer's behalf) — both now collect e-wallet details up front, as part of the
// same submit as the cancellation itself, rather than as a separate follow-up step. Fields only —
// the parent form owns validation and the actual submit action.
export function EwalletFields({ provider, number, name, disabled, onProviderChange, onNumberChange, onNameChange }: IEwalletFieldsProps) {
  return (
    <>
      <p className="pb-muted mb-3">Tell us where to send your refund.</p>
      <div className="mb-3">
        <label className="form-label small" htmlFor="ewalletProvider">
          E-wallet
        </label>
        <select
          id="ewalletProvider"
          className="form-select"
          value={provider}
          onChange={(e) => onProviderChange(e.target.value)}
          disabled={disabled}
        >
          <option value="GCash">GCash</option>
          <option value="Maya">Maya</option>
        </select>
      </div>
      <div className="mb-3">
        <label className="form-label small" htmlFor="ewalletNumber">
          Account Number
        </label>
        <input
          type="tel"
          id="ewalletNumber"
          className="form-control"
          value={number}
          onChange={(e) => onNumberChange(e.target.value)}
          disabled={disabled}
        />
      </div>
      <div className="mb-3">
        <label className="form-label small" htmlFor="ewalletName">
          Account Name
        </label>
        <input
          type="text"
          id="ewalletName"
          className="form-control"
          value={name}
          onChange={(e) => onNameChange(e.target.value)}
          disabled={disabled}
        />
      </div>
    </>
  )
}
