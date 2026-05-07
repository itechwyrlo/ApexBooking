type ConfirmModalProps = {
    isOpen: boolean;
    title: string;
    message: string;
    onConfirm: () => void;
    onCancel: () => void;
  };
  
  export function ConfirmModal({
    isOpen,
    title,
    message,
    onConfirm,
    onCancel,
  }: ConfirmModalProps) {
    if (!isOpen) return null;
  
    return (
      <>
        <div className="modal-backdrop show" style={{ backgroundColor: 'rgba(0,0,0,0.5)' }} />
        <div className="modal show d-block" tabIndex={-1}>
          <div className="modal-dialog modal-dialog-centered">
            <div className="modal-content">
              <div className="modal-header">
                <h5 className="modal-title">{title}</h5>
                <button type="button" className="btn-close" onClick={onCancel} aria-label="Close" />
              </div>
              <div className="modal-body">
                <p className="text-danger">{message}</p>
              </div>
              <div className="modal-footer">
                <button className="btn btn-light" onClick={onCancel}>
                  Cancel
                </button>
                <button className="btn btn-danger" onClick={onConfirm}>
                  Delete
                </button>
              </div>
            </div>
          </div>
        </div>
      </>
    );
  }