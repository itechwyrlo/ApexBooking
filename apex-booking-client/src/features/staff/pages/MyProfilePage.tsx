import React, { useEffect, useState } from 'react';
import { Alert } from '../../../components/ui/Alert';
import { Button } from '../../../components/ui/Button';
import { faSave } from '@fortawesome/free-solid-svg-icons';
import { useMyProfile } from '../hooks/useMyProfile';

const MyProfilePage: React.FC = () => {
  const { profile, isLoading, error, success, clearError, clearSuccess, getMyProfile, updatePhoto } = useMyProfile();
  const [photoUrl, setPhotoUrl] = useState('');

  useEffect(() => {
    getMyProfile();
  }, [getMyProfile]);

  useEffect(() => {
    if (profile?.photoUrl) setPhotoUrl(profile.photoUrl);
  }, [profile]);

  const handleSave = async () => {
    await updatePhoto(photoUrl.trim() || null);
  };

  if (isLoading && !profile) return <div className="p-4 text-center text-muted small">Loading…</div>;

  return (
    <div className="container-fluid px-3 px-md-4 py-4">
      <div className="row mb-4">
        <div className="col">
          <h5 className="fw-bold mb-0">My Profile</h5>
          <small className="text-muted">View your profile details and update your photo.</small>
        </div>
      </div>

      {error && (
        <div className="alert alert-danger alert-dismissible d-flex align-items-center mb-3" role="alert">
          {error}
          <button type="button" className="btn-close ms-auto" onClick={clearError} aria-label="Dismiss" />
        </div>
      )}

      {success && (
        <Alert variant="success" dismissible onDismiss={clearSuccess} className="mb-3">
          {success}
        </Alert>
      )}

      {profile && (
        <div className="row g-4">
          <div className="col-12 col-md-4 col-lg-3">
            <div className="card border-0 shadow-sm p-4 text-center">
              {photoUrl ? (
                <img
                  src={photoUrl}
                  alt="Profile"
                  className="rounded-circle mx-auto mb-3"
                  style={{ width: 96, height: 96, objectFit: 'cover' }}
                  onError={e => { (e.target as HTMLImageElement).style.display = 'none'; }}
                />
              ) : (
                <div
                  className="rounded-circle bg-light d-flex align-items-center justify-content-center mx-auto mb-3"
                  style={{ width: 96, height: 96 }}
                >
                  <i className="fas fa-user text-muted" style={{ fontSize: 36 }} />
                </div>
              )}
              <div className="fw-semibold">{profile.firstName} {profile.lastName}</div>
              <div className="text-muted small">{profile.email ?? '—'}</div>
            </div>
          </div>

          <div className="col-12 col-md-8 col-lg-9">
            <div className="card border-0 shadow-sm">
              <div className="card-header bg-white border-bottom py-3">
                <div className="fw-semibold">Profile Details</div>
              </div>
              <div className="card-body">
                <div className="row g-3 mb-3">
                  <div className="col-md-6">
                    <label className="form-label small fw-medium text-muted mb-1">First Name</label>
                    <div className="form-control form-control-sm bg-light border-0 text-muted">
                      {profile.firstName}
                    </div>
                  </div>
                  <div className="col-md-6">
                    <label className="form-label small fw-medium text-muted mb-1">Last Name</label>
                    <div className="form-control form-control-sm bg-light border-0 text-muted">
                      {profile.lastName}
                    </div>
                  </div>
                  <div className="col-md-6">
                    <label className="form-label small fw-medium text-muted mb-1">Email</label>
                    <div className="form-control form-control-sm bg-light border-0 text-muted">
                      {profile.email ?? '—'}
                    </div>
                  </div>
                  <div className="col-md-6">
                    <label className="form-label small fw-medium text-muted mb-1">Contact Number</label>
                    <div className="form-control form-control-sm bg-light border-0 text-muted">
                      {profile.contactNumber ?? '—'}
                    </div>
                  </div>
                </div>

                <hr className="my-4" />

                <div className="mb-3">
                  <label className="form-label small fw-medium mb-1">
                    Profile Photo URL
                  </label>
                  <input
                    type="url"
                    className="form-control form-control-sm"
                    placeholder="https://example.com/your-photo.jpg"
                    value={photoUrl}
                    onChange={e => setPhotoUrl(e.target.value)}
                  />
                  <div className="form-text">Paste a direct link to your profile image.</div>
                </div>

                <div className="d-flex justify-content-end">
                  <Button variant="primary" size="sm" icon={faSave} loading={isLoading} onClick={handleSave}>
                    Save Photo
                  </Button>
                </div>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default MyProfilePage;
