# Profile Customization Per Role — Design Spec

**Date:** 2026-08-15
**Status:** Approved design, ready for implementation planning

## Background

An audit (2026-08-15) found that **no role in ApexBooking has self-service profile
settings**, and **no photo/avatar upload capability exists anywhere** in the codebase:

- No role — including the account owner — has a `GET`/`PUT` for their own
  `ApplicationUser` record. The closest things are (a) an Owner/Admin editing *another*
  team member's name/phone/job-title via `UpdateTeamMemberCommand`, and (b) the
  anonymous token-based forgot-password reset. Neither is "my own profile" self-service.
- `TenantMember.PhotoUrl` + `TenantMember.UpdatePhoto()` exist as unwired scaffolding —
  displayed read-only in a few staff-list DTOs, but nothing ever calls `UpdatePhoto()`.
  `ApplicationUser` has no photo field of any kind. No upload endpoint, no
  `IFormFile`/multipart handling, no file-storage abstraction exists anywhere in the
  solution.
- The frontend (LocalFlow, `C:\Users\Wyrlo\projects\LocalFlow`) mirrors this gap exactly:
  no settings/profile page, no upload UI, no upload library installed.

This spec designs the missing capability: self-service profile settings (name, contact
number, password, photo) for every authenticated role.

## Scope

**In scope:**
- Self-service view/edit of first name, last name, phone number for all roles
  (SuperAdmin, Owner, Admin, Staff)
- Authenticated "change my password" (current + new password), distinct from the
  existing anonymous forgot-password flow
- Profile photo upload/removal, stored on local disk behind a storage abstraction
- Backend: new entity behavior, new commands/queries/controller, one EF Core migration
- Frontend: TypeScript interfaces + a service module matching the new API's request/response
  shapes only — **no page or component UI**, which stays with the user's in-progress
  frontend refactor

**Out of scope (explicit):**
- Email editing (stays immutable, matching the existing `UpdateTeamMemberCommand`
  precedent — email is the login identifier and copied from `ApplicationUser` at
  invite time with nothing keeping the two in sync)
- Cloud blob storage (Azure Blob/S3) — deferred; the storage abstraction is built so this
  is a later one-file swap, not a rewrite, once/if the deployment grows past a single
  instance
- `CustomJobTitle` and `Role` — stay admin-controlled via the existing
  `UpdateTeamMemberCommand`; self-service never touches them
- Any frontend page/component construction
- Customer profile settings — customers have no login identity (`Customer` is a
  guest contact record, not an `ApplicationUser`), so there is nothing to self-serve

## Architecture

**One shared "MyProfile" capability across all roles**, rather than separate
implementations per identity type. `ApplicationUser` is the single source of truth for
name/contact/photo/password, since every role (SuperAdmin, Owner, Admin, Staff) is an
`ApplicationUser`. For roles that also have a `TenantMember` row (Owner/Admin/Staff),
the existing `TenantMember.UpdateProfile`/`UpdatePhoto` domain methods are called
alongside the `ApplicationUser` update, scoped to the **current tenant only** (via
`IUserContextService.GetCurrentTenantId()`) — keeping team lists, idle-staff lists, and
booking pickers in sync without introducing a new sync mechanism. SuperAdmin has no
`TenantMember`, so that branch is skipped for them
(`IUserContextService.IsPlatformAdmin()`).

This was chosen over building two parallel features (one for `ApplicationUser`, one for
`TenantMember`) because it gives every role the same code path, fixes a latent
duplication risk rather than adding to it, and needs exactly one frontend service file.

## Data model

### `ApplicationUser` (`ApexBooking.Core.Persistence/Identity/ApplicationUser.cs`)

- New column: `PhotoUrl` (`string?`, nullable `nvarchar(2048)` — mirrors
  `TenantMember.PhotoUrl`'s existing column definition)
- No new phone column — `PhoneNumber` is already inherited from `IdentityUser<Guid>`
  and is completely unused today (verified: zero references anywhere in the codebase).
  It becomes the canonical self-service contact number for every role, including
  SuperAdmin.
- Two new intention-revealing methods, matching the entity's existing style
  (`MarkAsActive`, `RecordSuccessfulLogin`, etc. — private setters, no public mutation):
  ```csharp
  public void UpdateProfile(string firstName, string lastName, string? phoneNumber, DateTime utcNow)
  public void UpdatePhoto(string? photoUrl, DateTime utcNow)
  ```

### `TenantMember` (`ApexBooking.Core.Domain/Entities/TenantMember.cs`)

No schema change. `UpdateProfile(firstName, lastName, contactNumber, customJobTitle)`
and `UpdatePhoto(photoUrl)` already exist and already do the right thing — the new
command handlers call them with the incoming self-service values (keeping
`CustomJobTitle` unchanged by passing through the existing value).

### Password change

New `IApplicationUserService.ChangePasswordAsync(userId, currentPassword, newPassword, CancellationToken)`,
implemented in `ApplicationUserService` following the exact pattern already used by
`ResetPasswordAsync`:
1. Load the user with `.Include(x => x.RefreshTokens)`
2. `UserManager.ChangePasswordAsync(user, currentPassword, newPassword)` — verifies the
   current password internally; on failure, throw `BusinessRuleBrokenException` with the
   joined `IdentityResult.Errors`
3. `user.RevokeAllRefreshTokens(utcNow)` — every other session is logged out, a
   deliberate security default on password change
4. Issue a fresh access token (`ITokenService.GenerateAccessToken`) + refresh token for
   *this* session, via the same `_context.Add(newRefreshToken)` +
   `_userManager.UpdateAsync(user)` sequence `ResetPasswordAsync` already uses (the
   comment there explains why the explicit `_context.Add` is required — `UpdateAsync`'s
   internal `Context.Update(user)` would otherwise mis-track the new `RefreshToken` as
   `Modified` instead of `Added`, since its Id is a client-generated Guid)
5. Return the same `PasswordResetResult(UserId, Email, FullName, AccessToken, RefreshToken)`
   record type `ResetPasswordAsync` already returns — no new result shape needed

### Photo storage

New `IFileStorageService` in `ApexBooking.Core.Domain/Services` (alongside
`IApplicationUserService`):
```csharp
public interface IFileStorageService
{
    Task<string> SaveAsync(Stream content, string fileName, string contentType, CancellationToken ct);
    Task DeleteAsync(string url, CancellationToken ct);
}
```
Implemented as `LocalDiskFileStorageService` in `ApexBooking.Infrastructure`, writing
under `ApexBooking.WebApi/wwwroot/uploads/profile-photos/{userId}/{guid}.{ext}` and
returning a URL served by the already-configured `app.UseStaticFiles()`. The old photo
file is deleted best-effort (swallow failure, log it) when a photo is replaced or
removed — this must never fail the request.

**Why local disk, not Azure Blob Storage:** at the current scale (single tenant, single
persistent API instance), local disk has no real downside — the risk with local disk is
about *hosting environment* (multiple instances or ephemeral containers each having a
separate filesystem), not data volume. The `IFileStorageService` interface exists
specifically so swapping in an Azure Blob implementation later, if the deployment
outgrows single-instance hosting, is a one-file change behind an existing seam, not a
rewrite.

## Backend API surface

New `AccountController` (`ApexBooking.WebApi/Controllers/AccountController.cs`), route
`api/account`, class-level `[Authorize]` only — **no policy restriction**, any
authenticated role. This is deliberate: unlike `TenantController`
(`[Authorize(Policy = "ManagementOnly")]`, requires `TenantMiddleware` to have resolved
a tenant), `AccountController` must also serve SuperAdmin, who belongs to no tenant.

| Route | Command/Query | Behavior |
|---|---|---|
| `GET /api/account/me` | `GetMyProfileQuery : IQuery<MyProfileDto>` | Returns `{ Id, Email, FirstName, LastName, FullName, PhoneNumber, PhotoUrl, IsPlatformAdmin }` |
| `PUT /api/account/me` | `UpdateMyProfileCommand(FirstName, LastName, PhoneNumber) : ICommand` | Updates `ApplicationUser` via `IApplicationUserService.UpdateProfileAsync`; if the caller has a `TenantMember` in the current tenant, also calls `TenantMember.UpdateProfile(...)`. `CustomJobTitle`/`Role` untouched |
| `POST /api/account/me/photo` (multipart/form-data) | `UpdateMyProfilePhotoCommand(Stream, ContentType, Extension) : ICommand<string>` | Controller validates content-type (`image/jpeg`, `image/png`, `image/webp`) and size (≤5MB) before dispatch; handler saves via `IFileStorageService`, updates `ApplicationUser.PhotoUrl`, syncs `TenantMember.UpdatePhoto` if applicable, deletes the old file best-effort. Returns the new URL |
| `DELETE /api/account/me/photo` | `RemoveMyProfilePhotoCommand : ICommand` | Clears `PhotoUrl` on both records, deletes the file best-effort |
| `POST /api/account/me/change-password` | `ChangeMyPasswordCommand(CurrentPassword, NewPassword, ConfirmPassword) : ICommand<PasswordResetResult>` | See "Password change" above |

**New `IApplicationUserService` methods** (implemented in `ApplicationUserService` via
`UserManager`, never raw `DbContext`, per existing convention): `UpdateProfileAsync`,
`UpdatePhotoAsync`, `ChangePasswordAsync`, `GetProfileAsync` (richer than the existing
`ApplicationUserSummary` record — adds `PhoneNumber`/`PhotoUrl`).

**Validators** (existing `Common/Validators/` folder, FluentValidation — this is the
live pattern; the folder's recent churn in git status is old commands being renamed, not
the pattern being abandoned):
- `UpdateMyProfileCommandValidator` — `FirstName`/`LastName` required, `PhoneNumber`
  optional with a format check
- `ChangeMyPasswordCommandValidator` — reuses the exact password rule the deleted
  `ResetPasswordCommandValidator` used (recovered from git history): `NewPassword`
  required, 8–256 chars, at least one uppercase letter, at least one digit;
  `ConfirmPassword` must equal `NewPassword`

**DTO**: `MyProfileDto` in `Dtos/Response/`, parameterless constructor, mapped via
AutoMapper `ForMember` from the service's summary record — matches the existing
zero-manual-mapping convention (see `ForMember` + parameterless DTO ctor + global VO
converters).

**Error handling**: wrong current password, invalid file type/size, and any other
invariant break all go through the existing `BusinessRuleBrokenException` → global
exception middleware path. No new error-handling mechanism is introduced.

## Frontend (LocalFlow)

Scoped strictly to request/response shape files — **no page or component UI**, per the
user's explicit instruction to only match data shapes during their in-progress frontend
refactor:

- `src/interfaces/IMyProfile.ts` — `IMyProfile { id, email, firstName, lastName,
  fullName, phoneNumber: string | null, photoUrl: string | null, isPlatformAdmin }`,
  plus `IUpdateMyProfileValues` and `IChangePasswordValues` form-value shapes
- `src/services/accountService.ts` — new file, following `teamService.ts`'s existing
  pattern exactly: a wire-shape interface (documenting the raw camelCase JSON shape) +
  a mapper function + one exported function per endpoint:
  - `getMyProfile(): Promise<IMyProfile>`
  - `updateMyProfile(values: IUpdateMyProfileValues): Promise<void>`
  - `uploadMyProfilePhoto(file: File): Promise<string>` — builds `FormData`, posts
    multipart
  - `removeMyProfilePhoto(): Promise<void>`
  - `changeMyPassword(values: IChangePasswordValues): Promise<void>` — calls
    `setAccessToken(...)` from `authClient` on success, mirroring `resetPassword`'s
    existing token-rotation handling in `authService.ts`

**Deliberately untouched**: `IUser.ts`, `AuthContext`, and the JWT/`TokenDescriptor`.
Avatars do not belong in the JWT — they would go stale between token refreshes and
bloat the token. The profile page (whenever it's built, outside this spec's scope)
fetches `GET /api/account/me` independently of session claims.

## Migration

One new EF Core migration adding `PhotoUrl` (nullable `nvarchar(2048)`) to the Identity
users table. Generated, not applied — matches the existing convention in this codebase
of leaving `dotnet ef database update` to the developer.

## Testing

This repo has exactly one test project, `ApexBooking.Core.Domain.UnitTests`
(Domain-layer only — no Application/handler test project exists to extend). Scope:
unit tests for `ApplicationUser.UpdateProfile`/`UpdatePhoto`, following whatever pattern
the existing `TenantMember`/`ApplicationUser` entity tests already use in that project.
Handler/controller behavior gets manual verification (e.g. via `/run`), consistent with
how the rest of this codebase is verified today.

## Rollout note

`ChangeMyPasswordCommand` revoking all other sessions is a real, user-visible behavior
change the first time it ships — worth a one-line mention in release notes, nothing
more. No other rollout considerations (no data backfill, no feature flag needed — every
new field is nullable and every new endpoint is additive).
