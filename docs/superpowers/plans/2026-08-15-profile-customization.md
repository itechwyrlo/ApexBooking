# Profile Customization Per Role Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Give every authenticated role (SuperAdmin, Owner, Admin, Staff) self-service profile settings — name, phone, password, and photo — where today none exists.

**Architecture:** One shared `MyProfile` capability backed by `ApplicationUser` as the single source of truth for every role. Roles that also have a `TenantMember` row (Owner/Admin/Staff) get that row synced on every self-service edit, scoped to the current tenant only, using the same `TenantMember.UpdateProfile`/`UpdatePhoto` methods the existing admin-edits-others flow already calls. Photos are saved to local disk behind a small `IFileStorageService` seam.

**Tech Stack:** ASP.NET Core 10 / EF Core 10 / MediatR / FluentValidation / AutoMapper (backend), React + TypeScript + axios (frontend, interfaces/service layer only).

**Spec:** `docs/superpowers/specs/2026-08-15-profile-customization-design.md`

## Global Constraints

- `ApplicationUser.PhotoUrl`: nullable, `nvarchar(2048)` (matches `TenantMember.PhotoUrl`'s existing column)
- Contact number reuses `ApplicationUser.PhoneNumber` (inherited from `IdentityUser<Guid>`, currently unused anywhere) — no new phone column
- Email is immutable — no self-service endpoint ever accepts it
- `CustomJobTitle` and `Role` are never touched by self-service — still admin-only via `UpdateTeamMemberCommand`
- Photo storage: local disk under `ApexBooking.WebApi/wwwroot/uploads/profile-photos/{userId}/{guid}.{ext}`, served via the existing `app.UseStaticFiles()`, behind `IFileStorageService` so a cloud implementation can replace it later without touching callers
- Photo upload limits: `image/jpeg`, `image/png`, `image/webp` only; 5MB max
- Password rule (reused from the deleted `ResetPasswordCommandValidator`, recovered via git history): 8–256 chars, at least one uppercase letter, at least one digit
- `ChangeMyPasswordCommand` revokes **every** refresh token, including the caller's own current session — full logout on password change, no new access/refresh token is issued (see spec's "Revised from the original draft" note)
- EF Core migration is generated, never hand-authored, and never applied by the executor — `dotnet ef database update` is left to the user (file-lock risk from their running debug session, matching prior plans in this repo)
- No handler/controller-level automated tests — this repo has exactly one test project (`ApexBooking.Core.Domain.UnitTests`, Domain-layer only, no mocking library referenced) and no precedent for testing MediatR handlers. Automated tests are scoped to the new `ApplicationUser` domain methods only; everything above that layer is verified by `dotnet build` succeeding and the manual end-to-end pass in the final task
- Frontend scope is strictly TypeScript interfaces + one service module matching the new API's request/response shapes — no page or component UI, per explicit instruction (the user is mid-refactor on that side)

---

### Task 1: `ApplicationUser` domain changes

**Files:**
- Modify: `ApexBooking.Core.Persistence/Identity/ApplicationUser.cs`
- Test: `ApexBooking.Core.Domain.UnitTests/Identity/ApplicationUserTests.cs`

**Interfaces:**
- Produces: `ApplicationUser.PhotoUrl` (`string?`, public getter, private setter), `ApplicationUser.UpdateProfile(string firstName, string lastName, string? phoneNumber, DateTime utcNow)`, `ApplicationUser.UpdatePhoto(string? photoUrl, DateTime utcNow)` — consumed by Task 4's `ApplicationUserService`

- [ ] **Step 1: Write the failing tests**

Create `ApexBooking.Core.Domain.UnitTests/Identity/ApplicationUserTests.cs`:

```csharp
using ApexBooking.Core.Persistence.Identity;
using ApexBooking.SharedKernel.Exceptions;
using Xunit;

namespace ApexBooking.Core.Domain.UnitTests.Identity;

public class ApplicationUserTests
{
    private static ApplicationUser CreateUser() =>
        ApplicationUser.Create("owner@example.com", "Ada", "Lovelace");

    [Fact]
    public void UpdateProfile_SetsNameAndPhoneNumber()
    {
        var user = CreateUser();
        var utcNow = DateTime.UtcNow;

        user.UpdateProfile("Grace", "Hopper", "+15551234567", utcNow);

        Assert.Equal("Grace", user.FirstName);
        Assert.Equal("Hopper", user.LastName);
        Assert.Equal("+15551234567", user.PhoneNumber);
        Assert.Equal(utcNow, user.UpdatedAt);
    }

    [Fact]
    public void UpdateProfile_WithBlankPhoneNumber_ClearsIt()
    {
        var user = CreateUser();
        user.UpdateProfile("Grace", "Hopper", "+15551234567", DateTime.UtcNow);

        user.UpdateProfile("Grace", "Hopper", "  ", DateTime.UtcNow);

        Assert.Null(user.PhoneNumber);
    }

    [Fact]
    public void UpdateProfile_WithEmptyLastName_Throws()
    {
        var user = CreateUser();

        Assert.Throws<BusinessRuleBrokenException>(() =>
            user.UpdateProfile("Grace", "  ", null, DateTime.UtcNow));
    }

    [Fact]
    public void UpdatePhoto_SetsPhotoUrl()
    {
        var user = CreateUser();
        var utcNow = DateTime.UtcNow;

        user.UpdatePhoto("https://cdn.example.com/photo.jpg", utcNow);

        Assert.Equal("https://cdn.example.com/photo.jpg", user.PhotoUrl);
        Assert.Equal(utcNow, user.UpdatedAt);
    }

    [Fact]
    public void UpdatePhoto_WithNull_ClearsPhotoUrl()
    {
        var user = CreateUser();
        user.UpdatePhoto("https://cdn.example.com/photo.jpg", DateTime.UtcNow);

        user.UpdatePhoto(null, DateTime.UtcNow);

        Assert.Null(user.PhotoUrl);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test ApexBooking.Core.Domain.UnitTests --filter ApplicationUserTests`
Expected: build error — `ApplicationUser` has no `PhotoUrl`, `UpdateProfile`, or `UpdatePhoto` members yet.

- [ ] **Step 3: Add `PhotoUrl` and the two methods to `ApplicationUser`**

In `ApexBooking.Core.Persistence/Identity/ApplicationUser.cs`, add the property next to the other `--- Core Properties ---` (after `LastLoginAt`):

```csharp
    public string? PhotoUrl { get; private set; }
```

Add the two methods, placed after `RecordSuccessfulLogin` and before the `--- Refresh tokens ---` section:

```csharp
    public void UpdateProfile(string firstName, string lastName, string? phoneNumber, DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
            throw new BusinessRuleBrokenException("Name cannot be empty.");

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim();
        UpdatedAt = utcNow;
    }

    public void UpdatePhoto(string? photoUrl, DateTime utcNow)
    {
        PhotoUrl = photoUrl;
        UpdatedAt = utcNow;
    }
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test ApexBooking.Core.Domain.UnitTests --filter ApplicationUserTests`
Expected: 5 tests pass.

- [ ] **Step 5: Commit**

```bash
git add ApexBooking.Core.Persistence/Identity/ApplicationUser.cs ApexBooking.Core.Domain.UnitTests/Identity/ApplicationUserTests.cs
git commit -m "feat: add profile/photo self-mutation to ApplicationUser"
```

---

### Task 2: EF Core mapping + migration

**Files:**
- Modify: `ApexBooking.Core.Persistence/Data/ApexBookingDbContext.cs:72-75`

**Interfaces:**
- Consumes: `ApplicationUser.PhotoUrl` (Task 1)
- Produces: `PhotoUrl` column on the `ApplicationUsers` table

- [ ] **Step 1: Add the column mapping**

In `ApexBookingDbContext.cs`, change:

```csharp
             builder.Entity<ApplicationUser>(entity =>
            {
                entity.ToTable("ApplicationUsers");
            });
```

to:

```csharp
             builder.Entity<ApplicationUser>(entity =>
            {
                entity.ToTable("ApplicationUsers");
                entity.Property(u => u.PhotoUrl).HasMaxLength(2048);
            });
```

- [ ] **Step 2: Build to confirm it compiles**

Run: `dotnet build ApexBooking.Core.Persistence/ApexBooking.Core.Persistence.csproj`
Expected: `Build succeeded.`

- [ ] **Step 3: Generate the migration**

Run: `dotnet ef migrations add AddApplicationUserPhotoUrl --project ApexBooking.Core.Persistence --startup-project ApexBooking.WebApi`

Open the generated `Migrations/<timestamp>_AddApplicationUserPhotoUrl.cs` and confirm it contains exactly one `AddColumn` call for `PhotoUrl` on table `ApplicationUsers` (nvarchar(2048), nullable) in `Up()`, and the matching `DropColumn` in `Down()`. Do not run `dotnet ef database update` — leave that to the user.

- [ ] **Step 4: Commit**

```bash
git add ApexBooking.Core.Persistence/Data/ApexBookingDbContext.cs ApexBooking.Core.Persistence/Migrations/
git commit -m "feat: add PhotoUrl column to ApplicationUsers"
```

---

### Task 3: `IFileStorageService` + local disk implementation

**Files:**
- Create: `ApexBooking.Core.Domain/Services/IFileStorageService.cs`
- Create: `ApexBooking.Infrastructure/ExternalServices/Storage/LocalDiskFileStorageService.cs`
- Modify: `ApexBooking.Infrastructure/Dependency/InfrastructureDependencies.cs`

**Interfaces:**
- Produces: `IFileStorageService.SaveAsync(Stream content, string fileName, string contentType, CancellationToken ct) : Task<string>` (returns the public URL), `IFileStorageService.DeleteAsync(string url, CancellationToken ct) : Task` — consumed by Task 8 and Task 9
- Consumes: `ApplicationUrlsSettings.BaseUrl` (existing, `ApexBooking.Infrastructure.Configuration`), `IWebHostEnvironment.WebRootPath` (built-in)

- [ ] **Step 1: Create the interface**

Create `ApexBooking.Core.Domain/Services/IFileStorageService.cs`:

```csharp
namespace ApexBooking.Core.Domain.Services
{
    /// <summary>
    /// Stores arbitrary binary content (currently: profile photos) and returns a URL the browser
    /// can load directly. <paramref name="fileName"/> may include subdirectories (e.g.
    /// "{userId}/{guid}.jpg") — it is the full relative path under the storage root, not just a
    /// leaf name. See LocalDiskFileStorageService (Infrastructure) for the current implementation
    /// — local disk under wwwroot, chosen for a single-instance deployment (see the profile
    /// customization design spec); swap in a cloud-backed implementation behind this interface if
    /// the deployment ever needs to scale to multiple instances.
    /// </summary>
    public interface IFileStorageService
    {
        Task<string> SaveAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken = default);

        /// <summary>Best-effort delete — callers must not let a missing/already-deleted file fail
        /// the request that triggered the delete.</summary>
        Task DeleteAsync(string url, CancellationToken cancellationToken = default);
    }
}
```

- [ ] **Step 2: Create the local-disk implementation**

Create `ApexBooking.Infrastructure/ExternalServices/Storage/LocalDiskFileStorageService.cs`:

```csharp
using ApexBooking.Core.Domain.Services;
using ApexBooking.Infrastructure.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;

namespace ApexBooking.Infrastructure.ExternalServices.Storage
{
    /// <summary>
    /// Writes uploaded content to the API server's own wwwroot, served back out by the existing
    /// app.UseStaticFiles() middleware. Fine for a single persistent instance; swap for a
    /// cloud-backed implementation behind IFileStorageService if the deployment ever needs to
    /// scale to multiple instances (see the profile customization design spec).
    /// </summary>
    public class LocalDiskFileStorageService : IFileStorageService
    {
        private const string RelativeRoot = "uploads/profile-photos";

        private readonly IWebHostEnvironment _env;
        private readonly ApplicationUrlsSettings _urls;

        public LocalDiskFileStorageService(IWebHostEnvironment env, IOptions<ApplicationUrlsSettings> urls)
        {
            _env = env;
            _urls = urls.Value;
        }

        public async Task<string> SaveAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken = default)
        {
            var relativePath = $"{RelativeRoot}/{fileName}";
            var physicalPath = Path.Combine(_env.WebRootPath, relativePath.Replace('/', Path.DirectorySeparatorChar));

            Directory.CreateDirectory(Path.GetDirectoryName(physicalPath)!);

            await using (var fileStream = new FileStream(physicalPath, FileMode.Create, FileAccess.Write))
            {
                await content.CopyToAsync(fileStream, cancellationToken);
            }

            return $"{_urls.BaseUrl.TrimEnd('/')}/{relativePath}";
        }

        public Task DeleteAsync(string url, CancellationToken cancellationToken = default)
        {
            try
            {
                var relativePath = GetRelativePathFromUrl(url);
                if (relativePath is null)
                    return Task.CompletedTask;

                var physicalPath = Path.Combine(_env.WebRootPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(physicalPath))
                    File.Delete(physicalPath);
            }
            catch
            {
                // Best-effort: a delete failure must never fail the request that triggered it
                // (e.g. replacing a photo, or removing a stale file already gone from disk).
            }

            return Task.CompletedTask;
        }

        private static string? GetRelativePathFromUrl(string url)
        {
            var marker = $"/{RelativeRoot}/";
            var index = url.IndexOf(marker, StringComparison.Ordinal);
            return index < 0 ? null : url[(index + 1)..];
        }
    }
}
```

- [ ] **Step 3: Register it in DI**

In `ApexBooking.Infrastructure/Dependency/InfrastructureDependencies.cs`, add to the `using` list:

```csharp
using ApexBooking.Infrastructure.ExternalServices.Storage;
```

and inside `AddInfrastructureService`, add near the other `AddScoped` calls:

```csharp
            service.AddScoped<IFileStorageService, LocalDiskFileStorageService>();
```

- [ ] **Step 4: Build to confirm it compiles**

Run: `dotnet build ApexBooking.Infrastructure/ApexBooking.Infrastructure.csproj`
Expected: `Build succeeded.`

- [ ] **Step 5: Commit**

```bash
git add ApexBooking.Core.Domain/Services/IFileStorageService.cs ApexBooking.Infrastructure/ExternalServices/Storage/LocalDiskFileStorageService.cs ApexBooking.Infrastructure/Dependency/InfrastructureDependencies.cs
git commit -m "feat: add local-disk file storage service"
```

---

### Task 4: `IApplicationUserService` profile/password methods

**Files:**
- Modify: `ApexBooking.Core.Domain/Services/IApplicationUserService.cs`
- Modify: `ApexBooking.Core.Persistence/Services/ApplicationUserService.cs`

**Interfaces:**
- Consumes: `ApplicationUser.UpdateProfile`/`UpdatePhoto` (Task 1), existing `PasswordResetResult`-adjacent infrastructure (`UserManager`, `ITokenService`... not needed here, see below)
- Produces: `ApplicationUserProfile` record (`Id, Email, FirstName, LastName, FullName, PhoneNumber, PhotoUrl, IsPlatformAdmin`), `IApplicationUserService.GetProfileAsync(Guid userId, CancellationToken) : Task<ApplicationUserProfile?>`, `.UpdateProfileAsync(Guid userId, string firstName, string lastName, string? phoneNumber, CancellationToken) : Task`, `.UpdatePhotoAsync(Guid userId, string? photoUrl, CancellationToken) : Task`, `.ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken) : Task` — consumed by Tasks 6, 7, 8, 9, 10

- [ ] **Step 1: Add the new record and interface methods**

In `ApexBooking.Core.Domain/Services/IApplicationUserService.cs`, add inside the `interface IApplicationUserService` block (after `GetPlatformAdminIdsAsync`):

```csharp
        Task<ApplicationUserProfile?> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default);

        Task UpdateProfileAsync(Guid userId, string firstName, string lastName, string? phoneNumber, CancellationToken cancellationToken = default);

        Task UpdatePhotoAsync(Guid userId, string? photoUrl, CancellationToken cancellationToken = default);

        /// <summary>
        /// Verifies <paramref name="currentPassword"/> and sets <paramref name="newPassword"/>,
        /// then revokes every refresh token for this user — including the one behind the request
        /// making this call. No new tokens are issued; the caller is expected to log in again.
        /// </summary>
        Task ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default);
```

and add this record alongside the other records at the bottom of the file (after `ApplicationUserSummary`):

```csharp
    public sealed record ApplicationUserProfile(
        Guid Id,
        string Email,
        string FirstName,
        string LastName,
        string FullName,
        string? PhoneNumber,
        string? PhotoUrl,
        bool IsPlatformAdmin);
```

- [ ] **Step 2: Implement the four methods**

In `ApexBooking.Core.Persistence/Services/ApplicationUserService.cs`, add these methods (placed after `GetPlatformAdminIdsAsync`, before the `private static` helpers):

```csharp
        public async Task<ApplicationUserProfile?> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user is null) return null;

            return new ApplicationUserProfile(
                user.Id,
                user.Email!,
                user.FirstName,
                user.LastName,
                user.FullName,
                user.PhoneNumber,
                user.PhotoUrl,
                user.IsPlatformAdmin);
        }

        public async Task UpdateProfileAsync(Guid userId, string firstName, string lastName, string? phoneNumber, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString())
                ?? throw new BusinessRuleBrokenException("Failed to update profile, user doesn't exist");

            user.UpdateProfile(firstName, lastName, phoneNumber, DateTime.UtcNow);

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                throw new BusinessRuleBrokenException(string.Join(" ", result.Errors.Select(e => e.Description)));
        }

        public async Task UpdatePhotoAsync(Guid userId, string? photoUrl, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString())
                ?? throw new BusinessRuleBrokenException("Failed to update photo, user doesn't exist");

            user.UpdatePhoto(photoUrl, DateTime.UtcNow);

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                throw new BusinessRuleBrokenException(string.Join(" ", result.Errors.Select(e => e.Description)));
        }

        public async Task ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default)
        {
            var utcNow = DateTime.UtcNow;

            var user = await _userManager.Users
                .Include(x => x.RefreshTokens)
                .SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);

            if (user is null)
                throw new BusinessRuleBrokenException("Failed to change password, user doesn't exist");

            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);

            if (!result.Succeeded)
                throw new BusinessRuleBrokenException(string.Join(" ", result.Errors.Select(e => e.Description)));

            user.RevokeAllRefreshTokens(utcNow);
            await _userManager.UpdateAsync(user);
        }
```

- [ ] **Step 3: Build to confirm it compiles**

Run: `dotnet build ApexBooking.Core.Persistence/ApexBooking.Core.Persistence.csproj`
Expected: `Build succeeded.`

- [ ] **Step 4: Commit**

```bash
git add ApexBooking.Core.Domain/Services/IApplicationUserService.cs ApexBooking.Core.Persistence/Services/ApplicationUserService.cs
git commit -m "feat: add profile/photo/password self-service methods to ApplicationUserService"
```

---

### Task 5: `MyProfileDto` + AutoMapper registration

**Files:**
- Create: `ApexBooking.Core.Application/Dtos/Response/MyProfileDto.cs`
- Create: `ApexBooking.Core.Application/Features/Account/Queries/GetMyProfile/MyProfileMappingProfile.cs`
- Modify: `ApexBooking.Core.Application/Dependency/ApplicationServices.cs`

**Interfaces:**
- Consumes: `ApplicationUserProfile` (Task 4)
- Produces: `MyProfileDto` — consumed by Task 6

- [ ] **Step 1: Create the DTO**

Create `ApexBooking.Core.Application/Dtos/Response/MyProfileDto.cs`:

```csharp
namespace ApexBooking.Core.Application.Dtos.Response
{
    public record MyProfileDto(
        Guid Id,
        string Email,
        string FirstName,
        string LastName,
        string FullName,
        string? PhoneNumber,
        string? PhotoUrl,
        bool IsPlatformAdmin
    )
    {
        public MyProfileDto() : this(default, string.Empty, string.Empty, string.Empty, string.Empty, default, default, default) { }
    }
}
```

- [ ] **Step 2: Create the mapping profile**

Create `ApexBooking.Core.Application/Features/Account/Queries/GetMyProfile/MyProfileMappingProfile.cs`:

```csharp
using ApexBooking.Core.Application.Dtos.Response;
using ApexBooking.Core.Domain.Services;
using AutoMapper;

namespace ApexBooking.Core.Application.Features.Account.Queries.GetMyProfile
{
    public static class MyProfileMappingProfile
    {
        public static void AddMappingConfigs(IMapperConfigurationExpression cfg)
        {
            cfg.CreateMap<ApplicationUserProfile, MyProfileDto>();
        }
    }
}
```

- [ ] **Step 3: Register it**

In `ApexBooking.Core.Application/Dependency/ApplicationServices.cs`, add to the `using` list:

```csharp
using ApexBooking.Core.Application.Features.Account.Queries.GetMyProfile;
```

and inside the `AddAutoMapper` block, add a line next to the other three:

```csharp
            services.AddAutoMapper(cfg =>
            {
                TenantRequestMappingProfile.AddMappingConfigs(cfg);
                TeamMemberMappingProfile.AddMappingConfigs(cfg);
                ServiceCatalogMappingProfile.AddMappingConfigs(cfg);
                MyProfileMappingProfile.AddMappingConfigs(cfg);
            }, assembly);
```

- [ ] **Step 4: Build to confirm it compiles**

Run: `dotnet build ApexBooking.Core.Application/ApexBooking.Core.Application.csproj`
Expected: `Build succeeded.`

- [ ] **Step 5: Commit**

```bash
git add ApexBooking.Core.Application/Dtos/Response/MyProfileDto.cs ApexBooking.Core.Application/Features/Account/Queries/GetMyProfile/MyProfileMappingProfile.cs ApexBooking.Core.Application/Dependency/ApplicationServices.cs
git commit -m "feat: add MyProfileDto and its AutoMapper config"
```

---

### Task 6: `GetMyProfileQuery`

**Files:**
- Create: `ApexBooking.Core.Application/Features/Account/Queries/GetMyProfile/GetMyProfileQuery.cs`
- Create: `ApexBooking.Core.Application/Features/Account/Queries/GetMyProfile/GetMyProfileHandler.cs`

**Interfaces:**
- Consumes: `IApplicationUserService.GetProfileAsync` (Task 4), `MyProfileDto` + AutoMapper config (Task 5), `IUserContextService.GetCurrentUserId()` (existing)
- Produces: `GetMyProfileQuery : IQuery<MyProfileDto>` — consumed by Task 11

- [ ] **Step 1: Create the query**

Create `ApexBooking.Core.Application/Features/Account/Queries/GetMyProfile/GetMyProfileQuery.cs`:

```csharp
using ApexBooking.Core.Application.Dtos.Response;
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Account.Queries.GetMyProfile
{
    public record GetMyProfileQuery : IQuery<MyProfileDto>;
}
```

- [ ] **Step 2: Create the handler**

Create `ApexBooking.Core.Application/Features/Account/Queries/GetMyProfile/GetMyProfileHandler.cs`:

```csharp
using ApexBooking.Core.Application.Dtos.Response;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services;
using ApexBooking.SharedKernel.Exceptions;
using AutoMapper;

namespace ApexBooking.Core.Application.Features.Account.Queries.GetMyProfile
{
    public class GetMyProfileHandler : IQueryHandler<GetMyProfileQuery, MyProfileDto>
    {
        private readonly IApplicationUserService _applicationUserService;
        private readonly IUserContextService _userContext;
        private readonly IMapper _mapper;

        public GetMyProfileHandler(IApplicationUserService applicationUserService, IUserContextService userContext, IMapper mapper)
        {
            _applicationUserService = applicationUserService;
            _userContext = userContext;
            _mapper = mapper;
        }

        public async Task<MyProfileDto> Handle(GetMyProfileQuery query, CancellationToken cancellationToken)
        {
            var userId = _userContext.GetCurrentUserId();
            var profile = await _applicationUserService.GetProfileAsync(userId, cancellationToken)
                ?? throw new BusinessRuleBrokenException("Failed to load profile. User not found.");

            return _mapper.Map<MyProfileDto>(profile);
        }
    }
}
```

- [ ] **Step 3: Build to confirm it compiles**

Run: `dotnet build ApexBooking.Core.Application/ApexBooking.Core.Application.csproj`
Expected: `Build succeeded.`

- [ ] **Step 4: Commit**

```bash
git add ApexBooking.Core.Application/Features/Account/Queries/GetMyProfile/
git commit -m "feat: add GetMyProfileQuery"
```

---

### Task 7: `UpdateMyProfileCommand`

**Files:**
- Create: `ApexBooking.Core.Application/Features/Account/Commands/UpdateMyProfile/UpdateMyProfileCommand.cs`
- Create: `ApexBooking.Core.Application/Features/Account/Commands/UpdateMyProfile/UpdateMyProfileHandler.cs`
- Create: `ApexBooking.Core.Application/Common/Validators/UpdateMyProfileCommandValidator.cs`

**Interfaces:**
- Consumes: `IApplicationUserService.UpdateProfileAsync` (Task 4), `TenantMember.UpdateProfile` (existing, unchanged), `IUserContextService`, `ITenantEntity`, `IUnitOfWork.TenantRepository` (existing)
- Produces: `UpdateMyProfileCommand : ICommand` — consumed by Task 11

- [ ] **Step 1: Create the command**

Create `ApexBooking.Core.Application/Features/Account/Commands/UpdateMyProfile/UpdateMyProfileCommand.cs`:

```csharp
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Account.Commands.UpdateMyProfile
{
    // Email is intentionally absent — it stays immutable for every role, matching
    // UpdateTeamMemberCommand's existing precedent.
    public record UpdateMyProfileCommand(
        string FirstName,
        string LastName,
        string? PhoneNumber
    ) : ICommand;
}
```

- [ ] **Step 2: Create the validator**

Create `ApexBooking.Core.Application/Common/Validators/UpdateMyProfileCommandValidator.cs`:

```csharp
using ApexBooking.Core.Application.Features.Account.Commands.UpdateMyProfile;
using FluentValidation;

namespace ApexBooking.Core.Application.Common.Validators;

public class UpdateMyProfileCommandValidator : AbstractValidator<UpdateMyProfileCommand>
{
    public UpdateMyProfileCommandValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required")
            .MaximumLength(100).WithMessage("First name cannot exceed 100 characters");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required")
            .MaximumLength(100).WithMessage("Last name cannot exceed 100 characters");

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(30).WithMessage("Phone number cannot exceed 30 characters")
            .When(x => !string.IsNullOrEmpty(x.PhoneNumber));
    }
}
```

- [ ] **Step 3: Create the handler**

Create `ApexBooking.Core.Application/Features/Account/Commands/UpdateMyProfile/UpdateMyProfileHandler.cs`:

```csharp
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services;
using ApexBooking.SharedKernel.Services;

namespace ApexBooking.Core.Application.Features.Account.Commands.UpdateMyProfile
{
    public class UpdateMyProfileHandler : ICommandHandler<UpdateMyProfileCommand>
    {
        private readonly IApplicationUserService _applicationUserService;
        private readonly IUserContextService _userContext;
        private readonly ITenantEntity _tenantEntity;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateMyProfileHandler(
            IApplicationUserService applicationUserService,
            IUserContextService userContext,
            ITenantEntity tenantEntity,
            IUnitOfWork unitOfWork)
        {
            _applicationUserService = applicationUserService;
            _userContext = userContext;
            _tenantEntity = tenantEntity;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(UpdateMyProfileCommand command, CancellationToken cancellationToken)
        {
            var userId = _userContext.GetCurrentUserId();

            await _applicationUserService.UpdateProfileAsync(
                userId, command.FirstName, command.LastName, command.PhoneNumber, cancellationToken);

            // Keep the tenant-facing copy (team lists, idle-staff lists, booking pickers) in
            // sync. SuperAdmin has no tenant context and no TenantMember row, so this is a no-op
            // for them; a deactivated/removed member is also a silent no-op — the ApplicationUser
            // update above already succeeded and must not be rolled back over a stale membership.
            var tenantId = _tenantEntity.TenantId;
            if (tenantId is null)
                return;

            var tenant = await _unitOfWork.TenantRepository.GetAsync(
                predicate: t => t.TenantId == tenantId.Value,
                includes: t => t.Members);

            var currentMember = tenant?.Members.FirstOrDefault(m => m.UserId == userId);
            if (currentMember is null)
                return;

            currentMember.UpdateProfile(command.FirstName, command.LastName, command.PhoneNumber ?? string.Empty, currentMember.CustomJobTitle);

            _unitOfWork.TenantRepository.Update(tenant!);
            await _unitOfWork.CompleteAsync(cancellationToken);
        }
    }
}
```

- [ ] **Step 4: Build to confirm it compiles**

Run: `dotnet build ApexBooking.Core.Application/ApexBooking.Core.Application.csproj`
Expected: `Build succeeded.`

- [ ] **Step 5: Commit**

```bash
git add ApexBooking.Core.Application/Features/Account/Commands/UpdateMyProfile/ ApexBooking.Core.Application/Common/Validators/UpdateMyProfileCommandValidator.cs
git commit -m "feat: add UpdateMyProfileCommand"
```

---

### Task 8: `UpdateMyProfilePhotoCommand`

**Files:**
- Create: `ApexBooking.Core.Application/Features/Account/Commands/UpdateMyProfilePhoto/UpdateMyProfilePhotoCommand.cs`
- Create: `ApexBooking.Core.Application/Features/Account/Commands/UpdateMyProfilePhoto/UpdateMyProfilePhotoHandler.cs`

**Interfaces:**
- Consumes: `IFileStorageService.SaveAsync`/`.DeleteAsync` (Task 3), `IApplicationUserService.GetProfileAsync`/`.UpdatePhotoAsync` (Task 4), `TenantMember.UpdatePhoto` (existing, unchanged)
- Produces: `UpdateMyProfilePhotoCommand : ICommand<string>` — consumed by Task 11

- [ ] **Step 1: Create the command**

Create `ApexBooking.Core.Application/Features/Account/Commands/UpdateMyProfilePhoto/UpdateMyProfilePhotoCommand.cs`:

```csharp
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Account.Commands.UpdateMyProfilePhoto
{
    // Content-type and size are validated by the controller before this command is ever
    // dispatched (see AccountController.UploadMyProfilePhoto) — FluentValidation has no natural
    // way to inspect a raw Stream, so that check happens at the edge instead of here.
    public record UpdateMyProfilePhotoCommand(
        Stream Content,
        string ContentType,
        string FileExtension
    ) : ICommand<string>;
}
```

- [ ] **Step 2: Create the handler**

Create `ApexBooking.Core.Application/Features/Account/Commands/UpdateMyProfilePhoto/UpdateMyProfilePhotoHandler.cs`:

```csharp
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services;
using ApexBooking.SharedKernel.Services;

namespace ApexBooking.Core.Application.Features.Account.Commands.UpdateMyProfilePhoto
{
    public class UpdateMyProfilePhotoHandler : ICommandHandler<UpdateMyProfilePhotoCommand, string>
    {
        private readonly IApplicationUserService _applicationUserService;
        private readonly IFileStorageService _fileStorage;
        private readonly IUserContextService _userContext;
        private readonly ITenantEntity _tenantEntity;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateMyProfilePhotoHandler(
            IApplicationUserService applicationUserService,
            IFileStorageService fileStorage,
            IUserContextService userContext,
            ITenantEntity tenantEntity,
            IUnitOfWork unitOfWork)
        {
            _applicationUserService = applicationUserService;
            _fileStorage = fileStorage;
            _userContext = userContext;
            _tenantEntity = tenantEntity;
            _unitOfWork = unitOfWork;
        }

        public async Task<string> Handle(UpdateMyProfilePhotoCommand command, CancellationToken cancellationToken)
        {
            var userId = _userContext.GetCurrentUserId();

            var existingProfile = await _applicationUserService.GetProfileAsync(userId, cancellationToken);
            var oldPhotoUrl = existingProfile?.PhotoUrl;

            var fileName = $"{userId}/{Guid.NewGuid()}{command.FileExtension}";
            var newPhotoUrl = await _fileStorage.SaveAsync(command.Content, fileName, command.ContentType, cancellationToken);

            await _applicationUserService.UpdatePhotoAsync(userId, newPhotoUrl, cancellationToken);

            var tenantId = _tenantEntity.TenantId;
            if (tenantId is not null)
            {
                var tenant = await _unitOfWork.TenantRepository.GetAsync(
                    predicate: t => t.TenantId == tenantId.Value,
                    includes: t => t.Members);

                var currentMember = tenant?.Members.FirstOrDefault(m => m.UserId == userId);
                if (currentMember is not null)
                {
                    currentMember.UpdatePhoto(newPhotoUrl);
                    _unitOfWork.TenantRepository.Update(tenant!);
                    await _unitOfWork.CompleteAsync(cancellationToken);
                }
            }

            if (!string.IsNullOrEmpty(oldPhotoUrl))
                await _fileStorage.DeleteAsync(oldPhotoUrl, cancellationToken);

            return newPhotoUrl;
        }
    }
}
```

- [ ] **Step 3: Build to confirm it compiles**

Run: `dotnet build ApexBooking.Core.Application/ApexBooking.Core.Application.csproj`
Expected: `Build succeeded.`

- [ ] **Step 4: Commit**

```bash
git add ApexBooking.Core.Application/Features/Account/Commands/UpdateMyProfilePhoto/
git commit -m "feat: add UpdateMyProfilePhotoCommand"
```

---

### Task 9: `RemoveMyProfilePhotoCommand`

**Files:**
- Create: `ApexBooking.Core.Application/Features/Account/Commands/RemoveMyProfilePhoto/RemoveMyProfilePhotoCommand.cs`
- Create: `ApexBooking.Core.Application/Features/Account/Commands/RemoveMyProfilePhoto/RemoveMyProfilePhotoHandler.cs`

**Interfaces:**
- Consumes: `IFileStorageService.DeleteAsync` (Task 3), `IApplicationUserService.GetProfileAsync`/`.UpdatePhotoAsync` (Task 4), `TenantMember.UpdatePhoto` (existing, unchanged)
- Produces: `RemoveMyProfilePhotoCommand : ICommand` — consumed by Task 11

- [ ] **Step 1: Create the command**

Create `ApexBooking.Core.Application/Features/Account/Commands/RemoveMyProfilePhoto/RemoveMyProfilePhotoCommand.cs`:

```csharp
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Account.Commands.RemoveMyProfilePhoto
{
    public record RemoveMyProfilePhotoCommand : ICommand;
}
```

- [ ] **Step 2: Create the handler**

Create `ApexBooking.Core.Application/Features/Account/Commands/RemoveMyProfilePhoto/RemoveMyProfilePhotoHandler.cs`:

```csharp
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services;
using ApexBooking.SharedKernel.Services;

namespace ApexBooking.Core.Application.Features.Account.Commands.RemoveMyProfilePhoto
{
    public class RemoveMyProfilePhotoHandler : ICommandHandler<RemoveMyProfilePhotoCommand>
    {
        private readonly IApplicationUserService _applicationUserService;
        private readonly IFileStorageService _fileStorage;
        private readonly IUserContextService _userContext;
        private readonly ITenantEntity _tenantEntity;
        private readonly IUnitOfWork _unitOfWork;

        public RemoveMyProfilePhotoHandler(
            IApplicationUserService applicationUserService,
            IFileStorageService fileStorage,
            IUserContextService userContext,
            ITenantEntity tenantEntity,
            IUnitOfWork unitOfWork)
        {
            _applicationUserService = applicationUserService;
            _fileStorage = fileStorage;
            _userContext = userContext;
            _tenantEntity = tenantEntity;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(RemoveMyProfilePhotoCommand command, CancellationToken cancellationToken)
        {
            var userId = _userContext.GetCurrentUserId();

            var existingProfile = await _applicationUserService.GetProfileAsync(userId, cancellationToken);
            var oldPhotoUrl = existingProfile?.PhotoUrl;

            await _applicationUserService.UpdatePhotoAsync(userId, null, cancellationToken);

            var tenantId = _tenantEntity.TenantId;
            if (tenantId is not null)
            {
                var tenant = await _unitOfWork.TenantRepository.GetAsync(
                    predicate: t => t.TenantId == tenantId.Value,
                    includes: t => t.Members);

                var currentMember = tenant?.Members.FirstOrDefault(m => m.UserId == userId);
                if (currentMember is not null)
                {
                    currentMember.UpdatePhoto(null);
                    _unitOfWork.TenantRepository.Update(tenant!);
                    await _unitOfWork.CompleteAsync(cancellationToken);
                }
            }

            if (!string.IsNullOrEmpty(oldPhotoUrl))
                await _fileStorage.DeleteAsync(oldPhotoUrl, cancellationToken);
        }
    }
}
```

- [ ] **Step 3: Build to confirm it compiles**

Run: `dotnet build ApexBooking.Core.Application/ApexBooking.Core.Application.csproj`
Expected: `Build succeeded.`

- [ ] **Step 4: Commit**

```bash
git add ApexBooking.Core.Application/Features/Account/Commands/RemoveMyProfilePhoto/
git commit -m "feat: add RemoveMyProfilePhotoCommand"
```

---

### Task 10: `ChangeMyPasswordCommand`

**Files:**
- Create: `ApexBooking.Core.Application/Features/Account/Commands/ChangeMyPassword/ChangeMyPasswordCommand.cs`
- Create: `ApexBooking.Core.Application/Features/Account/Commands/ChangeMyPassword/ChangeMyPasswordHandler.cs`
- Create: `ApexBooking.Core.Application/Common/Validators/ChangeMyPasswordCommandValidator.cs`

**Interfaces:**
- Consumes: `IApplicationUserService.ChangePasswordAsync` (Task 4), `IUserContextService.GetCurrentUserId()` (existing)
- Produces: `ChangeMyPasswordCommand : ICommand` — consumed by Task 11

- [ ] **Step 1: Create the command**

Create `ApexBooking.Core.Application/Features/Account/Commands/ChangeMyPassword/ChangeMyPasswordCommand.cs`:

```csharp
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Account.Commands.ChangeMyPassword
{
    public record ChangeMyPasswordCommand(
        string CurrentPassword,
        string NewPassword,
        string ConfirmPassword
    ) : ICommand;
}
```

- [ ] **Step 2: Create the validator**

Create `ApexBooking.Core.Application/Common/Validators/ChangeMyPasswordCommandValidator.cs`:

```csharp
using ApexBooking.Core.Application.Features.Account.Commands.ChangeMyPassword;
using FluentValidation;

namespace ApexBooking.Core.Application.Common.Validators;

public class ChangeMyPasswordCommandValidator : AbstractValidator<ChangeMyPasswordCommand>
{
    public ChangeMyPasswordCommandValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("Current password is required");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required")
            .MinimumLength(8).WithMessage("New password must be at least 8 characters")
            .MaximumLength(256).WithMessage("New password cannot exceed 256 characters")
            .Matches(@"[A-Z]").WithMessage("New password must contain at least one uppercase letter")
            .Matches(@"[0-9]").WithMessage("New password must contain at least one digit");

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.NewPassword).WithMessage("Confirm password must match the new password");
    }
}
```

- [ ] **Step 3: Create the handler**

Create `ApexBooking.Core.Application/Features/Account/Commands/ChangeMyPassword/ChangeMyPasswordHandler.cs`:

```csharp
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services;

namespace ApexBooking.Core.Application.Features.Account.Commands.ChangeMyPassword
{
    public class ChangeMyPasswordHandler : ICommandHandler<ChangeMyPasswordCommand>
    {
        private readonly IApplicationUserService _applicationUserService;
        private readonly IUserContextService _userContext;

        public ChangeMyPasswordHandler(IApplicationUserService applicationUserService, IUserContextService userContext)
        {
            _applicationUserService = applicationUserService;
            _userContext = userContext;
        }

        public async Task Handle(ChangeMyPasswordCommand command, CancellationToken cancellationToken)
        {
            var userId = _userContext.GetCurrentUserId();

            await _applicationUserService.ChangePasswordAsync(
                userId, command.CurrentPassword, command.NewPassword, cancellationToken);
        }
    }
}
```

- [ ] **Step 4: Build to confirm it compiles**

Run: `dotnet build ApexBooking.Core.Application/ApexBooking.Core.Application.csproj`
Expected: `Build succeeded.`

- [ ] **Step 5: Commit**

```bash
git add ApexBooking.Core.Application/Features/Account/Commands/ChangeMyPassword/ ApexBooking.Core.Application/Common/Validators/ChangeMyPasswordCommandValidator.cs
git commit -m "feat: add ChangeMyPasswordCommand"
```

---

### Task 11: `AccountController`

**Files:**
- Create: `ApexBooking.WebApi/Controllers/AccountController.cs`

**Interfaces:**
- Consumes: `GetMyProfileQuery` (Task 6), `UpdateMyProfileCommand` (Task 7), `UpdateMyProfilePhotoCommand` (Task 8), `RemoveMyProfilePhotoCommand` (Task 9), `ChangeMyPasswordCommand` (Task 10)
- Produces: `GET/PUT /api/account/me`, `POST/DELETE /api/account/me/photo`, `POST /api/account/me/change-password` — consumed by Task 12 (frontend)

- [ ] **Step 1: Create the controller**

Create `ApexBooking.WebApi/Controllers/AccountController.cs`:

```csharp
using ApexBooking.Core.Application.Dtos.Response;
using ApexBooking.Core.Application.Features.Account.Commands.ChangeMyPassword;
using ApexBooking.Core.Application.Features.Account.Commands.RemoveMyProfilePhoto;
using ApexBooking.Core.Application.Features.Account.Commands.UpdateMyProfile;
using ApexBooking.Core.Application.Features.Account.Commands.UpdateMyProfilePhoto;
using ApexBooking.Core.Application.Features.Account.Queries.GetMyProfile;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApexBooking.WebApi.Controllers
{
    // No policy restriction beyond being authenticated at all — unlike TenantController
    // ([Authorize(Policy = "ManagementOnly")], which sits behind TenantMiddleware's tenant
    // resolution), this controller must also serve SuperAdmin, who belongs to no tenant.
    [ApiController]
    [Route("api/account")]
    [Authorize]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public class AccountController : ControllerBase
    {
        private static readonly string[] AllowedPhotoContentTypes = { "image/jpeg", "image/png", "image/webp" };
        private const long MaxPhotoSizeBytes = 5 * 1024 * 1024;

        private readonly IMediator _mediator;

        public AccountController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("me")]
        [ProducesResponseType(typeof(MyProfileDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyProfile(CancellationToken ct)
        {
            var result = await _mediator.Send(new GetMyProfileQuery(), ct);
            return Ok(result);
        }

        [HttpPut("me")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateMyProfileCommand command, CancellationToken ct)
        {
            await _mediator.Send(command, ct);
            return NoContent();
        }

        [HttpPost("me/photo")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)] // { photoUrl: string }
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UploadMyProfilePhoto([FromForm] IFormFile photo, CancellationToken ct)
        {
            if (photo is null || photo.Length == 0)
                return Problem(title: "Validation Error", detail: "A photo file is required.", statusCode: StatusCodes.Status400BadRequest);

            if (photo.Length > MaxPhotoSizeBytes)
                return Problem(title: "Validation Error", detail: "Photo must be 5MB or smaller.", statusCode: StatusCodes.Status400BadRequest);

            if (Array.IndexOf(AllowedPhotoContentTypes, photo.ContentType) < 0)
                return Problem(title: "Validation Error", detail: "Photo must be a JPEG, PNG, or WebP image.", statusCode: StatusCodes.Status400BadRequest);

            var extension = photo.ContentType switch
            {
                "image/jpeg" => ".jpg",
                "image/png" => ".png",
                "image/webp" => ".webp",
                _ => ".jpg",
            };

            await using var stream = photo.OpenReadStream();
            var photoUrl = await _mediator.Send(new UpdateMyProfilePhotoCommand(stream, photo.ContentType, extension), ct);

            return Ok(new { photoUrl });
        }

        [HttpDelete("me/photo")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> RemoveMyProfilePhoto(CancellationToken ct)
        {
            await _mediator.Send(new RemoveMyProfilePhotoCommand(), ct);
            return NoContent();
        }

        [HttpPost("me/change-password")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ChangeMyPassword([FromBody] ChangeMyPasswordCommand command, CancellationToken ct)
        {
            await _mediator.Send(command, ct);
            return NoContent();
        }
    }
}
```

- [ ] **Step 2: Build the full solution**

Run: `dotnet build ApexBooking.sln`
Expected: `Build succeeded.` (If it fails with `MSB3021`/`MSB3027` file-lock errors and no `error CS` lines, that's a locked DLL from a running debug session, not a real failure — stop the debug session and rebuild, or trust the earlier per-project builds in Tasks 1–10, which already isolate real compile errors from this.)

- [ ] **Step 3: Commit**

```bash
git add ApexBooking.WebApi/Controllers/AccountController.cs
git commit -m "feat: add AccountController for self-service profile settings"
```

---

### Task 12: Frontend request/response shapes (LocalFlow)

**Files:**
- Create: `C:\Users\Wyrlo\projects\LocalFlow\src\interfaces\IMyProfile.ts`
- Create: `C:\Users\Wyrlo\projects\LocalFlow\src\services\accountService.ts`

**Interfaces:**
- Consumes: `AccountController`'s routes and `MyProfileDto`'s shape (Task 11), existing `authClient` (`src/api/clients/authClient.ts`)
- Produces: `IMyProfile`, `IUpdateMyProfileValues`, `IChangePasswordValues`, and `getMyProfile`/`updateMyProfile`/`uploadMyProfilePhoto`/`removeMyProfilePhoto`/`changeMyPassword` — for whatever UI is built later, outside this plan's scope

- [ ] **Step 1: Create the interfaces**

Create `C:\Users\Wyrlo\projects\LocalFlow\src\interfaces\IMyProfile.ts`:

```typescript
export interface IMyProfile {
  id: string
  email: string
  firstName: string
  lastName: string
  fullName: string
  phoneNumber: string | null
  photoUrl: string | null
  isPlatformAdmin: boolean
}

export interface IUpdateMyProfileValues {
  firstName: string
  lastName: string
  phoneNumber: string
}

export interface IChangePasswordValues {
  currentPassword: string
  newPassword: string
  confirmPassword: string
}
```

- [ ] **Step 2: Create the service module**

Create `C:\Users\Wyrlo\projects\LocalFlow\src\services\accountService.ts`:

```typescript
import { authClient } from '../api/clients/authClient'
import type { IChangePasswordValues, IMyProfile, IUpdateMyProfileValues } from '../interfaces/IMyProfile'

// Raw wire shape from ApexBooking.Core.Application.Dtos.Response.MyProfileDto
// (camelCase property names, ASP.NET Core's default JSON naming policy).
interface IMyProfileWire {
  id: string
  email: string
  firstName: string
  lastName: string
  fullName: string
  phoneNumber: string | null
  photoUrl: string | null
  isPlatformAdmin: boolean
}

function toMyProfile(wire: IMyProfileWire): IMyProfile {
  return {
    id: wire.id,
    email: wire.email,
    firstName: wire.firstName,
    lastName: wire.lastName,
    fullName: wire.fullName,
    phoneNumber: wire.phoneNumber,
    photoUrl: wire.photoUrl,
    isPlatformAdmin: wire.isPlatformAdmin,
  }
}

export async function getMyProfile(): Promise<IMyProfile> {
  const response = await authClient.get<IMyProfileWire>('/api/account/me')
  return toMyProfile(response.data)
}

export async function updateMyProfile(values: IUpdateMyProfileValues): Promise<void> {
  await authClient.put('/api/account/me', {
    firstName: values.firstName,
    lastName: values.lastName,
    phoneNumber: values.phoneNumber || null,
  })
}

export async function uploadMyProfilePhoto(file: File): Promise<string> {
  const formData = new FormData()
  formData.append('photo', file)
  const response = await authClient.post<{ photoUrl: string }>('/api/account/me/photo', formData, {
    headers: { 'Content-Type': 'multipart/form-data' },
  })
  return response.data.photoUrl
}

export async function removeMyProfilePhoto(): Promise<void> {
  await authClient.delete('/api/account/me/photo')
}

// Every session is revoked server-side on a successful call, including this one — the caller
// (outside this module's scope) is expected to treat the resolved promise as "log out now,"
// the same handling as any other session-expiry path. No token is returned to keep alive.
export async function changeMyPassword(values: IChangePasswordValues): Promise<void> {
  await authClient.post('/api/account/me/change-password', {
    currentPassword: values.currentPassword,
    newPassword: values.newPassword,
    confirmPassword: values.confirmPassword,
  })
}
```

- [ ] **Step 3: Typecheck**

Run (from `C:\Users\Wyrlo\projects\LocalFlow`): `npm run build`
Expected: completes with no TypeScript errors. (This also runs `vite build`; a failure there unrelated to the two new files — e.g. a pre-existing issue elsewhere in the mid-refactor codebase — is not this task's concern, but confirm the two new files themselves report no errors in the `tsc -b` portion of the output.)

- [ ] **Step 4: Commit**

```bash
cd "C:\Users\Wyrlo\projects\LocalFlow"
git add src/interfaces/IMyProfile.ts src/services/accountService.ts
git commit -m "feat: add MyProfile request/response shapes and service"
```

---

### Task 13: Manual end-to-end verification

**Files:** none — verification only.

- [ ] **Step 1: Apply the migration**

Run: `dotnet ef database update --project ApexBooking.Core.Persistence --startup-project ApexBooking.WebApi`

- [ ] **Step 2: Run the API and exercise every endpoint**

Use `/run` (or `dotnet run --project ApexBooking.WebApi`) to start the API, then, authenticated as a Staff/Owner/Admin user and separately as a SuperAdmin:

1. `GET /api/account/me` — returns the caller's current profile (verify `photoUrl` is `null` for a fresh account)
2. `PUT /api/account/me` with a new first/last name and phone — returns 204; a follow-up `GET` reflects the change. For a tenant-role user, also `GET /api/Tenant/team` and confirm that member's row shows the same updated name/contact
3. `POST /api/account/me/photo` with a small JPEG via multipart form — returns `{ photoUrl }`; open that URL directly in a browser and confirm the image loads; re-run `GET /api/account/me` and confirm `photoUrl` matches
4. `POST /api/account/me/photo` again with a second image — confirm the first file was deleted from `ApexBooking.WebApi/wwwroot/uploads/profile-photos/{userId}/` and only the new one remains
5. `DELETE /api/account/me/photo` — returns 204; `GET /api/account/me` shows `photoUrl: null`; the file is gone from disk
6. `POST /api/account/me/photo` with a `.txt` file — returns 400
7. `POST /api/account/me/change-password` with the wrong current password — returns 400; with the correct current password and a valid new one — returns 204, and confirm the access token used to make that call is rejected on the next request (or its refresh token, once it needs to refresh)

- [ ] **Step 3: Confirm the recovered password rule matches**

Try a new password with no uppercase letter, then one with no digit, then one under 8 characters — each should return 400 with the corresponding FluentValidation message.
