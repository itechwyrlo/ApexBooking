# Staff Dashboard Phase 1 — Backend (TenantMemberId Claim) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a `tenant_member_id` JWT claim to tenant-session access tokens, so a logged-in Staff/Admin/Owner user can be filtered against `Booking.StaffId` (a `TenantMemberId`) without an extra lookup. This unblocks the frontend's "My Daily Lineup" widget (companion plan in the LocalFlow repo: `docs/superpowers/plans/2026-08-12-staff-dashboard-phase1-frontend.md`) and, later, Block My Time.

**Architecture:** `TenantMemberId? TenantMemberId` is added as a new, optional, trailing field on the existing `TokenDescriptor`/`TokenPrincipal` records (`ApexBooking.Core.Domain.Services.Auth`), following the exact optional-claim pattern already used for `TenantId`/`Role`/`Slug` — present for tenant sessions, absent for platform-admin sessions. The three real token-issuing paths (tenant login, refresh, password reset) each already resolve the caller's `TenantMember` to get `Role`; they now also read `.TenantMemberId` off that same object and pass it through.

**Tech Stack:** .NET / C#, MediatR command handlers, ASP.NET Identity, JWT (RS256 via `JwtTokenService`).

## Global Constraints

- No test project exists in this solution (verified: no `*.Tests.csproj`). Verification per task is `dotnet build` (compiles/type-checks), run manually by the user — do not run it yourself per the standing instruction for this session.
- `TenantMemberId` is appended as the **last** parameter on both `TokenDescriptor` and `TokenPrincipal`, with a `= null` default, specifically so the existing positional-argument call in `RefreshTokenHandler.cs` and `ApplicationUserService.cs` (`ResetPasswordAsync`'s `TokenDescriptor` construction) don't shift any existing argument's position — only `PlatformAdminLoginHandler.cs` is deliberately left unchanged (it never sets `Slug` either; platform admins have no tenant membership).
- Two dead-code items were found during design research — do **not** touch either as part of this plan: an orphaned duplicate `TokenDescriptor` in `ApexBooking.Core.Application\Dtos\Descriptor\TokenDescriptor.cs` (unreferenced anywhere), and the unused legacy `UserRole` enum in `ApexBooking.Core.Persistence\Identity\Enums\UserRole.cs`.

---

### Task 1: Token claim plumbing

**Files:**
- Modify: `ApexBooking.Core.Persistence\CustomClaimTypes\JwtClaimTypes.cs`
- Modify: `ApexBooking.Core.Domain\Services\Auth\ITokenService.cs`
- Modify: `ApexBooking.Core.Persistence\Services\JwtTokenService.cs`

**Interfaces:**
- Consumes: `TenantMemberId` value object (`ApexBooking.Core.Domain.ValueObjects`, `public record TenantMemberId(Guid Value)`).
- Produces: `TokenDescriptor.TenantMemberId` / `TokenPrincipal.TenantMemberId` (both `TenantMemberId?`, default `null`) — consumed by Tasks 2–4.

- [ ] **Step 1: Add the claim key**

In `ApexBooking.Core.Persistence\CustomClaimTypes\JwtClaimTypes.cs`, add a fourth constant:

```csharp
namespace ApexBooking.Core.Persistence.CustomClaimTypes
{
    public static class JwtClaimTypes
    {
        public const string PlatformAdmin = "platform_admin";

        public const string TenantId = "tenant_id";

        public const string TenantRole = "tenant_role";

        public const string TenantSlug = "tenant_slug";

        public const string TenantMemberId = "tenant_member_id";
    }
}
```

- [ ] **Step 2: Extend the descriptor/principal records**

In `ApexBooking.Core.Domain\Services\Auth\ITokenService.cs`, add the `using` and the new trailing field on both records:

```csharp
using ApexBooking.Core.Domain.Enums;
using ApexBooking.Core.Domain.ValueObjects;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Domain.Services.Auth
{
    public interface ITokenService
    {
        string GenerateAccessToken(TokenDescriptor descriptor);

        TokenPrincipal? ValidateExpiredAccessToken(string accessToken);
    }

    public sealed record TokenDescriptor(
    Guid UserId,
    string Email,
    string FullName,
    bool IsPlatformAdmin,
    TenantId? TenantId,
    SystemRole? Role,
    string? Slug = null,
    TenantMemberId? TenantMemberId = null);

    public sealed record TokenPrincipal(
    Guid UserId,
    string Email,
    string FullName,
    bool IsPlatformAdmin,
    TenantId? TenantId,
    SystemRole? Role,
    string? Slug = null,
    TenantMemberId? TenantMemberId = null);
}
```

- [ ] **Step 3: Write and read the claim in JwtTokenService**

In `ApexBooking.Core.Persistence\Services\JwtTokenService.cs`, add the `using`:

```csharp
using ApexBooking.Core.Domain.ValueObjects;
```

In `BuildClaims`, add this block right after the existing `descriptor.Slug` block:

```csharp
            if (descriptor.Slug is not null)
            {
                claims.Add(new Claim(JwtClaimTypes.TenantSlug, descriptor.Slug));
            }

            if (descriptor.TenantMemberId is not null)
            {
                claims.Add(new Claim(
                    JwtClaimTypes.TenantMemberId,
                    descriptor.TenantMemberId.Value.ToString())); // Extracts raw Guid out of TenantMemberId value object wrapper
            }

            return claims;
```

In `MapPrincipal`, add this block right after the existing `slug` line and before the `return new TokenPrincipal(...)`:

```csharp
            var slug = principal.FindFirst(JwtClaimTypes.TenantSlug)?.Value;

            TenantMemberId? tenantMemberId = null;
            var tenantMemberIdClaim = principal.FindFirst(JwtClaimTypes.TenantMemberId)?.Value;
            if (!string.IsNullOrWhiteSpace(tenantMemberIdClaim) && Guid.TryParse(tenantMemberIdClaim, out var memberGuid))
            {
                tenantMemberId = new TenantMemberId(memberGuid);
            }

            return new TokenPrincipal(
                userId,
                email,
                fullName,
                isPlatformAdmin,
                tenantId,
                tenantRole,
                slug,
                tenantMemberId);
```

- [ ] **Step 4: Compile check**

Run: `dotnet build`
Expected: no errors. (User runs this manually.)

---

### Task 2: Tenant login issues the claim

**Files:**
- Modify: `ApexBooking.Core.Application\Features\Authentication\Login\TenantLoginHandler.cs`

**Interfaces:**
- Consumes: `TokenDescriptor` from Task 1, `membership.TenantMemberId` (`TenantMember.TenantMemberId`, already resolved at line 65 of this file to get `Role`).

- [ ] **Step 1: Pass the member id through**

In `TenantLoginHandler.cs`, change the `TokenDescriptor` construction (currently lines 74-81):

```csharp
            var tenantDescriptor = new TokenDescriptor(
                UserId: user.Id,
                Email: user.Email,
                FullName: fullName,
                IsPlatformAdmin: false,
                TenantId: tenant.TenantId,
                Role: membership.Role,
                Slug: tenant.Slug,
                TenantMemberId: membership.TenantMemberId);
```

- [ ] **Step 2: Compile check**

Run: `dotnet build`
Expected: no errors. (User runs this manually.)

---

### Task 3: Refresh token re-issues the claim

**Files:**
- Modify: `ApexBooking.Core.Application\Features\Authentication\RefreshToken\RefreshTokenHandler.cs`

**Interfaces:**
- Consumes: `TokenDescriptor` from Task 1, `membership.TenantMemberId` (same `TenantMember` this handler already looks up to resolve `role`).

- [ ] **Step 1: Resolve and pass the member id**

In `RefreshTokenHandler.cs`, add the `using`:

```csharp
using ApexBooking.Core.Domain.ValueObjects;
```

Change the local-variable block and the membership-resolution block (currently lines 57-78):

```csharp
            TenantId? tenantId = null;
            SystemRole? role = null;
            string? slug = null;
            TenantMemberId? tenantMemberId = null;

            if (!rotation.IsPlatformAdmin)
            {
                var tenant = await _unitOfWork.TenantRepository.GetByUserIdAsync(
                    rotation.UserId,
                    cancellationToken);

                if (tenant is null || !tenant.IsActive)
                    throw Reject(command.IsPlatformAdmin);

                var membership = tenant.Members.FirstOrDefault(m => m.UserId == rotation.UserId);

                if (membership is null)
                    throw Reject(command.IsPlatformAdmin);

                tenantId = tenant.TenantId;
                role = membership.Role;
                slug = tenant.Slug;
                tenantMemberId = membership.TenantMemberId;
            }
```

Change the `TokenDescriptor` construction (currently lines 80-88) to pass it as an 8th positional argument:

```csharp
            var accessToken = _tokenService.GenerateAccessToken(
                new TokenDescriptor(
                    rotation.UserId,
                    rotation.Email,
                    rotation.FullName,
                    rotation.IsPlatformAdmin,
                    tenantId,
                    role,
                    slug,
                    tenantMemberId));
```

- [ ] **Step 2: Compile check**

Run: `dotnet build`
Expected: no errors. (User runs this manually.)

---

### Task 4: Password reset re-issues the claim

**Files:**
- Modify: `ApexBooking.Core.Domain\Services\IApplicationUserService.cs`
- Modify: `ApexBooking.Core.Persistence\Services\ApplicationUserService.cs`
- Modify: `ApexBooking.Core.Application\Features\Authentication\ResetPassword\ResetPasswordHandler.cs`

**Interfaces:**
- Consumes: `TokenDescriptor` from Task 1.
- Produces: `IApplicationUserService.ResetPasswordAsync` gains a `TenantMemberId? tenantMemberId` parameter, inserted between the existing `slug` and `cancellationToken` parameters.

- [ ] **Step 1: Extend the interface**

In `ApexBooking.Core.Domain\Services\IApplicationUserService.cs`, add the `using`:

```csharp
using ApexBooking.Core.Domain.ValueObjects;
```

Change the `ResetPasswordAsync` signature (currently lines 22-29):

```csharp
        Task<PasswordResetResult> ResetPasswordAsync(
            Guid userId,
            string resetToken,
            string newPassword,
            TenantId? tenantId,
            SystemRole? role,
            string? slug,
            TenantMemberId? tenantMemberId,
            CancellationToken cancellationToken = default);
```

- [ ] **Step 2: Extend the implementation**

In `ApexBooking.Core.Persistence\Services\ApplicationUserService.cs`, add the `using`:

```csharp
using ApexBooking.Core.Domain.ValueObjects;
```

Change the `ResetPasswordAsync` signature and its `TokenDescriptor` construction (currently lines 99-136):

```csharp
        public async Task<PasswordResetResult> ResetPasswordAsync(
            Guid userId,
            string resetToken,
            string newPassword,
            TenantId? tenantId,
            SystemRole? role,
            string? slug,
            TenantMemberId? tenantMemberId,
            CancellationToken cancellationToken = default)
        {
            var utcNow = DateTime.UtcNow;

            var user = await _userManager.Users
                .Include(x => x.RefreshTokens)
                .SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);

            if (user is null)
                throw new BusinessRuleBrokenException("Failed to reset password, User doesn't exist");

            var result = await _userManager.ResetPasswordAsync(user, resetToken, newPassword);

            if (!result.Succeeded)
                throw new BusinessRuleBrokenException(
                    string.Join(" ", result.Errors.Select(e => e.Description)));

            user.MarkAsActive(utcNow);
            user.MarkEmailAsConfirmed(utcNow);

            user.RevokeAllRefreshTokens(utcNow);

            var accessToken = _tokenService.GenerateAccessToken(
                new TokenDescriptor(
                    user.Id,
                    user.Email!,
                    user.FullName,
                    user.IsPlatformAdmin,
                    tenantId,
                    role,
                    slug,
                    tenantMemberId));
```

(The rest of the method, from `var refreshTokenRaw = GenerateRefreshTokenRaw();` onward, is unchanged.)

- [ ] **Step 3: Resolve the member id once and pass it through**

In `ApexBooking.Core.Application\Features\Authentication\ResetPassword\ResetPasswordHandler.cs`, replace the `role` resolution and the `ResetPasswordAsync` call (currently lines 41-51):

```csharp
            var tenant = await _unitOfWork.TenantRepository.GetByUserIdAsync(user.UserId, cancellationToken);

            var membership = tenant?.Members
                .FirstOrDefault(m => m.UserId == user.UserId);

            var session = await _applicationUserService.ResetPasswordAsync(
                command.UserId,
                command.Token,
                command.NewPassword,
                tenant?.TenantId,
                membership?.Role,
                tenant?.Slug,
                membership?.TenantMemberId,
                cancellationToken);
```

- [ ] **Step 4: Compile check**

Run: `dotnet build`
Expected: no errors — in particular, no leftover call site still passing the old 6-parameter (pre-`tenantMemberId`) shape to `ResetPasswordAsync`. (User runs this manually.)

---

## Self-Review Notes

- **Spec coverage**: all four token-issuing call sites named in the design doc are covered (`TenantLoginHandler`, `RefreshTokenHandler`, `ResetPasswordHandler`/`ApplicationUserService`); `PlatformAdminLoginHandler` is deliberately left untouched per the design.
- **Placeholder scan**: no TBDs — every step has literal, complete code with exact surrounding context.
- **Type consistency**: `TenantMemberId` (from `ApexBooking.Core.Domain.ValueObjects`) is used identically across all four tasks; the record's new field name (`TenantMemberId`) matches the JWT claim constant's name (`JwtClaimTypes.TenantMemberId`) and the claim string value (`"tenant_member_id"`) matches what the companion frontend plan expects to decode.
