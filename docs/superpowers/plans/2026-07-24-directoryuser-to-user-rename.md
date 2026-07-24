# DirectoryUser → User Rename (Phase 1) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Rename the `DirectoryUser` entity (and everything attached to it) to `User`, moving it out of the `Directories` module into a new `Users` module, with zero data loss against the real PostgreSQL database already running in this worktree.

**Architecture:** Pure rename + module-move across Domain, Application, Persistence, API, and Frontend. No behavior changes. The physical PostgreSQL tables are renamed in place via a hand-written EF Core migration (`RenameTable`/`RenameColumn`), never dropped/recreated, so existing synced AD users survive.

**Tech Stack:** .NET 8, EF Core 8 + Npgsql, MediatR, FluentValidation, React + TypeScript + Vite.

## Global Constraints

- No data loss: existing rows in `DirectoryUsers`, `DirectoryUserAttributes`, `DirectoryUserRoles` (real PostgreSQL, this worktree) must survive as `Users`, `UserAttributes`, `UserRoles` with all values intact.
- No new "delete user" capability is introduced.
- `Users.DirectoryId → Directories.Id` foreign key stays `ReferentialAction.Restrict` (a Directory cannot be deleted while it has Users).
- `Employee` entity is not touched in this phase.
- `DirectorySource` enum stays in `Domain/Directories/` (shared by both `Directory.Source` and `User.Source`).
- API route changes from `/api/v1/directoryusers` to `/api/v1/users`; the frontend must be updated in the same phase (not deferred) so the app keeps working.
- Working directory for all commands below: `C:\Projects\activity-based-effort-management-main\.claude\worktrees\dev-real-postgres` (already-open worktree branch `worktree-dev-real-postgres`).

---

### Task 1: Domain + Application + Persistence rename

**Files:**
- Move: `backend/src/EforTakip.Domain/Directories/DirectoryUser.cs` → `backend/src/EforTakip.Domain/Users/User.cs`
- Move: `backend/src/EforTakip.Domain/Directories/DirectoryUserAttribute.cs` → `backend/src/EforTakip.Domain/Users/UserAttribute.cs`
- Move: `backend/src/EforTakip.Domain/Directories/DirectoryUserRole.cs` → `backend/src/EforTakip.Domain/Users/UserRole.cs`
- Move: `backend/src/EforTakip.Application/Directories/Commands/CreateInternalUser/*` → `backend/src/EforTakip.Application/Users/Commands/CreateInternalUser/*`
- Move: `backend/src/EforTakip.Application/Directories/Commands/ResetInternalUserPassword/*` → `backend/src/EforTakip.Application/Users/Commands/ResetInternalUserPassword/*`
- Move: `backend/src/EforTakip.Application/Directories/Queries/GetDirectoryUsers/*` → `backend/src/EforTakip.Application/Users/Queries/GetUsers/*`
- Move: `backend/src/EforTakip.Application/Directories/Queries/GetDirectoryUserById/*` → `backend/src/EforTakip.Application/Users/Queries/GetUserById/*`
- Move: `backend/src/EforTakip.Application/Directories/Queries/GetOrgChart/*` → `backend/src/EforTakip.Application/Users/Queries/GetOrgChart/*`
- Move: `backend/src/EforTakip.Application/Directories/Dtos/DirectoryUserDto.cs` → `backend/src/EforTakip.Application/Users/Dtos/UserDto.cs`
- Move: `backend/src/EforTakip.Application/Directories/Dtos/DirectoryUserDetailDto.cs` → `backend/src/EforTakip.Application/Users/Dtos/UserDetailDto.cs`
- Move: `backend/src/EforTakip.Application/Directories/Dtos/OrgChartResultDto.cs` → `backend/src/EforTakip.Application/Users/Dtos/OrgChartResultDto.cs`
- Modify: `backend/src/EforTakip.Persistence/Configurations/DirectoryUserConfiguration.cs` → rename to `UserConfiguration.cs`
- Modify: `backend/src/EforTakip.Persistence/Configurations/DirectoryUserAttributeConfiguration.cs` → rename to `UserAttributeConfiguration.cs`
- Modify: `backend/src/EforTakip.Persistence/Configurations/DirectoryUserRoleConfiguration.cs` → rename to `UserRoleConfiguration.cs`
- Modify: `backend/src/EforTakip.Persistence/EforTakipDbContext.cs`
- Modify: `backend/src/EforTakip.Application/Common/Interfaces/IApplicationDbContext.cs`
- Modify: `backend/src/EforTakip.Persistence/DependencyInjection.cs`
- Modify (type references only, no move): `backend/src/EforTakip.Application/Directories/Commands/SyncDirectory/SyncDirectoryCommandHandler.cs`
- Modify (type references only): `backend/src/EforTakip.Domain/Roles/Role.cs` (comment only)
- Modify (type references only): `backend/src/EforTakip.Application/Roles/Queries/GetRoleById/GetRoleByIdQueryHandler.cs`
- Modify (type references only): `backend/src/EforTakip.Application/Roles/Commands/AssignRoleToUser/AssignRoleToUserCommandHandler.cs`
- Modify (type references only): `backend/src/EforTakip.Application/Roles/Commands/RemoveRoleFromUser/RemoveRoleFromUserCommandHandler.cs`
- Modify (type references only): `backend/src/EforTakip.Application/Auth/Commands/Login/LoginCommandHandler.cs`
- Modify (type references only): `backend/src/EforTakip.Persistence/Seed/BootstrapAdminSeeder.cs`
- Create: `backend/src/EforTakip.Persistence/Migrations/<timestamp>_RenameDirectoryUserToUser.cs` (hand-authored Up/Down)

**Interfaces:**
- Produces: `EforTakip.Domain.Users.User` (was `EforTakip.Domain.Directories.DirectoryUser`) with the exact same public API (`CreateFromActiveDirectory`, `CreateInternal`, `UpdateFromSync`, `Deactivate`, `Activate`, `SetPassword`, `SetAttribute`, `ClearAttributes`, `AssignRole`, `RemoveRole`, and properties `DirectoryId`, `Source`, `Username`, `FirstName`, `LastName`, `DisplayName`, `Email`, `ObjectGuid`, `PasswordHash`, `IsActive`, `LastSyncedUtc`, `Attributes`, `Roles`).
- Produces: `EforTakip.Domain.Users.UserAttribute` (was `DirectoryUserAttribute`) with property `UserId` (was `DirectoryUserId`) and `ReferencedUserId` (was `ReferencedDirectoryUserId`).
- Produces: `EforTakip.Domain.Users.UserRole` (was `DirectoryUserRole`) with property `UserId` (was `DirectoryUserId`).
- Produces: `IApplicationDbContext.Users` (was `.DirectoryUsers`), `.UserAttributes` (was `.DirectoryUserAttributes`), `.UserRoles` (was `.DirectoryUserRoles`) — all `DbSet<T>` with the renamed types.
- Consumed by: Task 2 (API controller), Task 3 (backend tests), Task 4 (migration apply).

- [ ] **Step 1: Move and rewrite the three Domain files**

Run:
```bash
cd backend
mkdir -p src/EforTakip.Domain/Users
git mv src/EforTakip.Domain/Directories/DirectoryUser.cs src/EforTakip.Domain/Users/User.cs
git mv src/EforTakip.Domain/Directories/DirectoryUserAttribute.cs src/EforTakip.Domain/Users/UserAttribute.cs
git mv src/EforTakip.Domain/Directories/DirectoryUserRole.cs src/EforTakip.Domain/Users/UserRole.cs
```

Then, in each of the 3 moved files, change the `namespace` line from `EforTakip.Domain.Directories;` to `EforTakip.Domain.Users;`, and apply this exact global text substitution across the 3 files (order matters: do the longer pattern first so it isn't half-replaced by the shorter one):

```bash
for f in src/EforTakip.Domain/Users/User.cs src/EforTakip.Domain/Users/UserAttribute.cs src/EforTakip.Domain/Users/UserRole.cs; do
  sed -i 's/EforTakip\.Domain\.Directories;/EforTakip.Domain.Users;/' "$f"
  sed -i 's/DirectoryUser/User/g' "$f"
done
```

Since `sed 's/DirectoryUser/User/g'` replaces the substring `DirectoryUser` wherever it appears, this single pass correctly turns `DirectoryUserAttribute`→`UserAttribute`, `DirectoryUserRole`→`UserRole`, `DirectoryUserId`→`UserId`, `ReferencedDirectoryUserId`→`ReferencedUserId`, and the bare class name `DirectoryUser`→`User` everywhere in the same step — no separate pass is needed.

After this step, `User.cs` must start with:
```csharp
using EforTakip.Domain.Common;
using EforTakip.Domain.Exceptions;

namespace EforTakip.Domain.Users;

public sealed class User : Entity, IAggregateRoot
{
```

And `UserAttribute.cs` must contain a property `public Guid UserId { get; private set; }` and `public Guid? ReferencedUserId { get; private set; }` (verify with `grep -n "UserId\|ReferencedUserId" src/EforTakip.Domain/Users/UserAttribute.cs`).

- [ ] **Step 2: Move and rewrite the Application files into the new Users module**

Run:
```bash
mkdir -p src/EforTakip.Application/Users/Commands src/EforTakip.Application/Users/Queries src/EforTakip.Application/Users/Dtos

git mv src/EforTakip.Application/Directories/Commands/CreateInternalUser src/EforTakip.Application/Users/Commands/CreateInternalUser
git mv src/EforTakip.Application/Directories/Commands/ResetInternalUserPassword src/EforTakip.Application/Users/Commands/ResetInternalUserPassword
git mv src/EforTakip.Application/Directories/Queries/GetDirectoryUsers src/EforTakip.Application/Users/Queries/GetUsers
git mv src/EforTakip.Application/Directories/Queries/GetDirectoryUserById src/EforTakip.Application/Users/Queries/GetUserById
git mv src/EforTakip.Application/Directories/Queries/GetOrgChart src/EforTakip.Application/Users/Queries/GetOrgChart
git mv src/EforTakip.Application/Directories/Dtos/DirectoryUserDto.cs src/EforTakip.Application/Users/Dtos/UserDto.cs
git mv src/EforTakip.Application/Directories/Dtos/DirectoryUserDetailDto.cs src/EforTakip.Application/Users/Dtos/UserDetailDto.cs
git mv src/EforTakip.Application/Directories/Dtos/OrgChartResultDto.cs src/EforTakip.Application/Users/Dtos/OrgChartResultDto.cs

# GetDirectoryUsersQuery(Handler).cs -> GetUsersQuery(Handler).cs, same for GetDirectoryUserById -> GetUserById
git mv src/EforTakip.Application/Users/Queries/GetUsers/GetDirectoryUsersQuery.cs src/EforTakip.Application/Users/Queries/GetUsers/GetUsersQuery.cs
git mv src/EforTakip.Application/Users/Queries/GetUsers/GetDirectoryUsersQueryHandler.cs src/EforTakip.Application/Users/Queries/GetUsers/GetUsersQueryHandler.cs
git mv src/EforTakip.Application/Users/Queries/GetUserById/GetDirectoryUserByIdQuery.cs src/EforTakip.Application/Users/Queries/GetUserById/GetUserByIdQuery.cs
git mv src/EforTakip.Application/Users/Queries/GetUserById/GetDirectoryUserByIdQueryHandler.cs src/EforTakip.Application/Users/Queries/GetUserById/GetUserByIdQueryHandler.cs
```

Now fix namespaces and identifiers. The moved files' namespace lines change `EforTakip.Application.Directories.*` → `EforTakip.Application.Users.*` (keeping the sub-path after `Directories`), and `EforTakip.Domain.Directories` type-only usages of `DirectoryUser*` become `EforTakip.Domain.Users`:

```bash
for f in $(find src/EforTakip.Application/Users -name "*.cs"); do
  sed -i 's/EforTakip\.Application\.Directories\./EforTakip.Application.Users./g' "$f"
  sed -i 's/DirectoryUser/User/g' "$f"
done
```

This also correctly rewrites `GetDirectoryUsersQuery`→`GetUsersQuery`, `GetDirectoryUserByIdQuery`→`GetUserByIdQuery`, `DirectoryUserDto`→`UserDto`, `DirectoryUserDetailDto`→`UserDetailDto`, `DirectoryUserAttributeValueDto`→`UserAttributeValueDto`, `DirectoryUserId`→`UserId`, `ReferencedDirectoryUserId`→`ReferencedUserId` in the same pass (all are substrings containing `DirectoryUser`).

`GetUserByIdQuery.cs` must now read exactly:
```csharp
using EforTakip.Application.Users.Dtos;
using MediatR;

namespace EforTakip.Application.Users.Queries.GetUserById;

public sealed record GetUserByIdQuery(Guid UserId) : IRequest<UserDetailDto>;
```

`CreateInternalUserCommandHandler.cs` still needs `using EforTakip.Domain.Directories;` for `Directory`/`DirectorySource` (those types did NOT move) plus a new `using EforTakip.Domain.Users;` for `User`. Open the file and confirm it has both usings — the sed pass above does not add missing usings, only rewrites existing text, so verify by running:
```bash
dotnet build src/EforTakip.Application/EforTakip.Application.csproj 2>&1 | grep -i "error CS0246"
```
Expected: for each `CS0246: The type or namespace name 'User' could not be found`-style error, add `using EforTakip.Domain.Users;` to the top of the reported file. For each `CS0246` about `Directory`/`DirectorySource` not found in a file now under `Application/Users/`, add `using EforTakip.Domain.Directories;`.

- [ ] **Step 3: Update the Application files that stay in place but reference the renamed types**

These files are NOT moved — only their `DirectoryUser` references become `User` and they gain a `using EforTakip.Domain.Users;` where needed:

```bash
for f in \
  src/EforTakip.Application/Directories/Commands/SyncDirectory/SyncDirectoryCommandHandler.cs \
  src/EforTakip.Application/Roles/Queries/GetRoleById/GetRoleByIdQueryHandler.cs \
  src/EforTakip.Application/Roles/Commands/AssignRoleToUser/AssignRoleToUserCommandHandler.cs \
  src/EforTakip.Application/Roles/Commands/RemoveRoleFromUser/RemoveRoleFromUserCommandHandler.cs \
  src/EforTakip.Application/Auth/Commands/Login/LoginCommandHandler.cs \
  ; do
  sed -i 's/DirectoryUser/User/g' "$f"
done
sed -i 's/DirectoryUser/User/g' src/EforTakip.Domain/Roles/Role.cs
```

`SyncDirectoryCommandHandler.cs` uses `Directory = EforTakip.Domain.Directories.Directory;` (a type alias) and now also needs `using EforTakip.Domain.Users;` for the bare `User` type — after the sed pass, add that using manually if the build (Step 6) reports it missing.

`LoginCommandHandler.cs`: after the sed pass, `db.Users` (was `db.DirectoryUsers`) and `User user = ...` must compile; it already has no direct `EforTakip.Domain.Directories` type usage of `DirectoryUser`, but keeps using `Directory` (the AD config entity) — confirm the `using EforTakip.Domain.Directories;` for `Directory` is still present (it referenced `Directory = EforTakip.Domain.Directories.Directory;` alias already, unaffected by this rename), and add `using EforTakip.Domain.Users;`.

- [ ] **Step 4: Rename the Persistence configuration files and fix their content**

```bash
git mv src/EforTakip.Persistence/Configurations/DirectoryUserConfiguration.cs src/EforTakip.Persistence/Configurations/UserConfiguration.cs
git mv src/EforTakip.Persistence/Configurations/DirectoryUserAttributeConfiguration.cs src/EforTakip.Persistence/Configurations/UserAttributeConfiguration.cs
git mv src/EforTakip.Persistence/Configurations/DirectoryUserRoleConfiguration.cs src/EforTakip.Persistence/Configurations/UserRoleConfiguration.cs

for f in src/EforTakip.Persistence/Configurations/UserConfiguration.cs \
         src/EforTakip.Persistence/Configurations/UserAttributeConfiguration.cs \
         src/EforTakip.Persistence/Configurations/UserRoleConfiguration.cs; do
  sed -i 's/DirectoryUser/User/g' "$f"
  sed -i 's/using EforTakip\.Domain\.Directories;/using EforTakip.Domain.Directories;\nusing EforTakip.Domain.Users;/' "$f"
done
```

`UserConfiguration.cs` must now contain `builder.ToTable("Users");` and `public sealed class UserConfiguration : IEntityTypeConfiguration<User>`. `UserAttributeConfiguration.cs` must contain `builder.ToTable("UserAttributes");`. `UserRoleConfiguration.cs` must contain `builder.ToTable("UserRoles");`. Manually verify these three `ToTable` lines with:
```bash
grep -n "ToTable" src/EforTakip.Persistence/Configurations/User*.cs
```
Expected output:
```
src/EforTakip.Persistence/Configurations/UserAttributeConfiguration.cs:    builder.ToTable("UserAttributes");
src/EforTakip.Persistence/Configurations/UserConfiguration.cs:    builder.ToTable("Users");
src/EforTakip.Persistence/Configurations/UserRoleConfiguration.cs:    builder.ToTable("UserRoles");
```

- [ ] **Step 5: Update `EforTakipDbContext.cs`, `IApplicationDbContext.cs`, and `DependencyInjection.cs`**

In `backend/src/EforTakip.Persistence/EforTakipDbContext.cs`, add `using EforTakip.Domain.Users;` to the usings block, then replace these three lines:
```csharp
    public DbSet<DirectoryUser> DirectoryUsers => Set<DirectoryUser>();

    public DbSet<DirectoryAttributeMapping> DirectoryAttributeMappings => Set<DirectoryAttributeMapping>();

    public DbSet<DirectoryUserAttribute> DirectoryUserAttributes => Set<DirectoryUserAttribute>();
```
with:
```csharp
    public DbSet<User> Users => Set<User>();

    public DbSet<DirectoryAttributeMapping> DirectoryAttributeMappings => Set<DirectoryAttributeMapping>();

    public DbSet<UserAttribute> UserAttributes => Set<UserAttribute>();
```
and replace:
```csharp
    public DbSet<DirectoryUserRole> DirectoryUserRoles => Set<DirectoryUserRole>();
```
with:
```csharp
    public DbSet<UserRole> UserRoles => Set<UserRole>();
```

In `backend/src/EforTakip.Application/Common/Interfaces/IApplicationDbContext.cs`, add `using EforTakip.Domain.Users;`, then replace:
```csharp
    DbSet<DirectoryUser> DirectoryUsers { get; }

    DbSet<DirectoryAttributeMapping> DirectoryAttributeMappings { get; }

    DbSet<DirectoryUserAttribute> DirectoryUserAttributes { get; }
```
with:
```csharp
    DbSet<User> Users { get; }

    DbSet<DirectoryAttributeMapping> DirectoryAttributeMappings { get; }

    DbSet<UserAttribute> UserAttributes { get; }
```
and replace:
```csharp
    DbSet<DirectoryUserRole> DirectoryUserRoles { get; }
```
with:
```csharp
    DbSet<UserRole> UserRoles { get; }
```

In `backend/src/EforTakip.Persistence/DependencyInjection.cs`, add `using EforTakip.Domain.Users;`, then replace:
```csharp
        services.AddScoped<IRepository<DirectoryUser>, RepositoryBase<DirectoryUser>>();
```
with:
```csharp
        services.AddScoped<IRepository<User>, RepositoryBase<User>>();
```

- [ ] **Step 6: Build the three affected projects and fix any remaining reference errors**

Run:
```bash
dotnet build src/EforTakip.Domain/EforTakip.Domain.csproj
dotnet build src/EforTakip.Application/EforTakip.Application.csproj
dotnet build src/EforTakip.Persistence/EforTakip.Persistence.csproj
```
Expected: all three report `Build succeeded. 0 Warning(s) 0 Error(s)`.

If there are `CS0246` (type not found) errors, they are missing `using EforTakip.Domain.Users;` or `using EforTakip.Domain.Directories;` statements in specific files — add the missing using to the file named in the error and rebuild. Do not change any other logic to make these pass.

- [ ] **Step 7: Hand-write the rename migration**

Scaffold an empty migration (its auto-generated Up/Down will be wrong — a naive Drop+Create — because EF sees `EforTakip.Domain.Users.User` as an unrelated new entity type; it will be fully overwritten in the next step):
```bash
dotnet ef migrations add RenameDirectoryUserToUser --project src/EforTakip.Persistence --startup-project src/EforTakip.Api -- --environment RealDb
```

Open the newly created `backend/src/EforTakip.Persistence/Migrations/<timestamp>_RenameDirectoryUserToUser.cs` and replace its `Up`/`Down` method bodies entirely with:

```csharp
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(name: "DirectoryUsers", newName: "Users");
            migrationBuilder.RenameTable(name: "DirectoryUserAttributes", newName: "UserAttributes");
            migrationBuilder.RenameTable(name: "DirectoryUserRoles", newName: "UserRoles");

            migrationBuilder.RenameColumn(
                name: "DirectoryUserId", table: "UserAttributes", newName: "UserId");
            migrationBuilder.RenameColumn(
                name: "ReferencedDirectoryUserId", table: "UserAttributes", newName: "ReferencedUserId");
            migrationBuilder.RenameColumn(
                name: "DirectoryUserId", table: "UserRoles", newName: "UserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UserId", table: "UserRoles", newName: "DirectoryUserId");
            migrationBuilder.RenameColumn(
                name: "ReferencedUserId", table: "UserAttributes", newName: "ReferencedDirectoryUserId");
            migrationBuilder.RenameColumn(
                name: "UserId", table: "UserAttributes", newName: "DirectoryUserId");

            migrationBuilder.RenameTable(name: "UserRoles", newName: "DirectoryUserRoles");
            migrationBuilder.RenameTable(name: "UserAttributes", newName: "DirectoryUserAttributes");
            migrationBuilder.RenameTable(name: "Users", newName: "DirectoryUsers");
        }
```

Do **not** hand-edit the paired `<timestamp>_RenameDirectoryUserToUser.Designer.cs` or `EforTakipDbContextModelSnapshot.cs` — those are auto-generated from the current C# model by the `dotnet ef migrations add` command you already ran, and they already reflect the renamed `User`/`UserAttribute`/`UserRole` types correctly.

Verify no other migration accidentally got new `AddColumn`/`CreateTable` operations for `DirectoryUser*` alongside these renames — the design's explicit no-data-loss requirement depends on this migration being a pure rename:
```bash
grep -n "migrationBuilder\." "src/EforTakip.Persistence/Migrations/"*_RenameDirectoryUserToUser.cs
```
Expected: only the 6 `RenameTable`/`RenameColumn` calls listed above appear (3 in `Up`, mirrored 3 in `Down`) — no `AddColumn`, `CreateTable`, or `DropTable`.

- [ ] **Step 8: Commit**

```bash
git add backend/src/EforTakip.Domain/Users backend/src/EforTakip.Domain/Directories \
        backend/src/EforTakip.Application/Users backend/src/EforTakip.Application/Directories \
        backend/src/EforTakip.Application/Roles backend/src/EforTakip.Application/Auth \
        backend/src/EforTakip.Application/Common/Interfaces/IApplicationDbContext.cs \
        backend/src/EforTakip.Persistence \
        backend/src/EforTakip.Domain/Roles/Role.cs
git commit -m "$(cat <<'EOF'
refactor: rename DirectoryUser to User, move into dedicated Users module

Phase 1 of merging the Employee and DirectoryUser concepts. Domain,
Application, and Persistence now use User/UserAttribute/UserRole
instead of DirectoryUser/DirectoryUserAttribute/DirectoryUserRole,
living in a new Users module rather than under Directories (which now
only holds AD connection/sync concerns). Adds a hand-written migration
that renames the physical Postgres tables/columns in place so existing
synced AD users are preserved.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

### Task 2: API controller rename

**Files:**
- Move: `backend/src/EforTakip.Api/Controllers/v1/DirectoryUsersController.cs` → `backend/src/EforTakip.Api/Controllers/v1/UsersController.cs`

**Interfaces:**
- Consumes: `GetUsersQuery`, `GetUserByIdQuery`, `UserDto`, `UserDetailDto`, `CreateInternalUserCommand`, `ResetInternalUserPasswordCommand` from `EforTakip.Application.Users.*` (produced by Task 1).
- Produces: HTTP routes `GET /api/v1/users`, `GET /api/v1/users/{id}`, `POST /api/v1/users/internal`, `POST /api/v1/users/{id}/reset-password` (was `/api/v1/directoryusers/...`) — consumed by Task 5 (frontend).

- [ ] **Step 1: Move and rewrite the controller**

```bash
cd backend
git mv src/EforTakip.Api/Controllers/v1/DirectoryUsersController.cs src/EforTakip.Api/Controllers/v1/UsersController.cs
```

Replace the full content of `src/EforTakip.Api/Controllers/v1/UsersController.cs` with:

```csharp
using Asp.Versioning;
using EforTakip.Api.Authorization;
using EforTakip.Application.Common.Models;
using EforTakip.Application.Users.Commands.CreateInternalUser;
using EforTakip.Application.Users.Commands.ResetInternalUserPassword;
using EforTakip.Application.Users.Dtos;
using EforTakip.Application.Users.Queries.GetUserById;
using EforTakip.Application.Users.Queries.GetUsers;
using EforTakip.Domain.Authorization;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EforTakip.Api.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public sealed class UsersController(ISender mediator) : ControllerBase
{
    [RequirePermission(Permissions.User.Read)]
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<UserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<UserDto>>> GetAll(
        [FromQuery] GetUsersQuery query, CancellationToken cancellationToken)
        => Ok(await mediator.Send(query, cancellationToken));

    [RequirePermission(Permissions.User.Read)]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UserDetailDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserDetailDto>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await mediator.Send(new GetUserByIdQuery(id), cancellationToken));

    [RequirePermission(Permissions.User.Manage)]
    [HttpPost("internal")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateInternal(
        CreateInternalUserCommand command, CancellationToken cancellationToken)
    {
        var id = await mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id, version = "1.0" }, null);
    }

    [RequirePermission(Permissions.User.Manage)]
    [HttpPost("{id:guid}/reset-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ResetPassword(
        Guid id, ResetInternalUserPasswordCommand command, CancellationToken cancellationToken)
    {
        if (id != command.UserId)
            return BadRequest("Route ve gövde kimlikleri eşleşmiyor.");
        await mediator.Send(command, cancellationToken);
        return NoContent();
    }
}
```

Note `command.DirectoryUserId` became `command.UserId` — this matches `ResetInternalUserPasswordCommand(Guid UserId, string NewPassword)` after Task 1's rename.

- [ ] **Step 2: Build the whole solution**

```bash
dotnet build EforTakip.sln
```
Expected: `Build succeeded. 0 Warning(s) 0 Error(s)` for all projects, including `EforTakip.Api`. If there are still `CS0246`/`CS1061` errors anywhere outside `UsersController.cs`, they are leftover `DirectoryUser*` references Task 1 missed — grep for them and fix:
```bash
grep -rln "DirectoryUser" src --include="*.cs" | grep -v /Migrations/
```
Expected: no output (empty). Any file listed here (other than files under `Migrations/`, which intentionally keep historical `DirectoryUser*` names since they describe past schema states) needs the same `sed -i 's/DirectoryUser/User/g'` treatment applied in Task 1.

- [ ] **Step 3: Commit**

```bash
git add backend/src/EforTakip.Api/Controllers/v1/UsersController.cs
git commit -m "$(cat <<'EOF'
refactor: rename DirectoryUsersController to UsersController

Route changes from /api/v1/directoryusers to /api/v1/users, matching
the User rename from the previous commit.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

### Task 3: Backend tests rename

**Files:**
- Move: `backend/tests/EforTakip.Domain.Tests/Directories/DirectoryUserTests.cs` → `backend/tests/EforTakip.Domain.Tests/Users/UserTests.cs`
- Move: `backend/tests/EforTakip.Application.Tests/Directories/Commands/CreateInternalUserCommandHandlerTests.cs` → `backend/tests/EforTakip.Application.Tests/Users/Commands/CreateInternalUserCommandHandlerTests.cs`
- Move: `backend/tests/EforTakip.Application.Tests/Directories/Commands/ResetInternalUserPasswordCommandHandlerTests.cs` → `backend/tests/EforTakip.Application.Tests/Users/Commands/ResetInternalUserPasswordCommandHandlerTests.cs`
- Move: `backend/tests/EforTakip.Application.Tests/Directories/Queries/GetOrgChartQueryHandlerTests.cs` → `backend/tests/EforTakip.Application.Tests/Users/Queries/GetOrgChartQueryHandlerTests.cs`
- Modify (stay in place, update type references): `backend/tests/EforTakip.Application.Tests/Directories/Commands/TestDbContext.cs`
- Modify (stay in place): `backend/tests/EforTakip.Application.Tests/Directories/Commands/SyncDirectoryCommandHandlerTests.cs`
- Modify (stay in place): `backend/tests/EforTakip.Application.Tests/Directories/Commands/SyncDirectoryCommandHandlerRealDbContextTests.cs`
- Modify (stay in place): `backend/tests/EforTakip.Application.Tests/Roles/Queries/GetRoleByIdQueryHandlerTests.cs`
- Modify (stay in place): `backend/tests/EforTakip.Application.Tests/Roles/Commands/AssignRoleToUserCommandHandlerTests.cs`
- Modify (stay in place): `backend/tests/EforTakip.Application.Tests/Auth/LoginCommandHandlerTests.cs`

**Interfaces:**
- Consumes: `User`, `UserAttribute`, `UserRole` from `EforTakip.Domain.Users` and `IApplicationDbContext.Users`/`.UserAttributes`/`.UserRoles` (produced by Task 1).

- [ ] **Step 1: Move the test files**

```bash
cd backend
mkdir -p tests/EforTakip.Domain.Tests/Users
mkdir -p tests/EforTakip.Application.Tests/Users/Commands
mkdir -p tests/EforTakip.Application.Tests/Users/Queries

git mv tests/EforTakip.Domain.Tests/Directories/DirectoryUserTests.cs tests/EforTakip.Domain.Tests/Users/UserTests.cs
git mv tests/EforTakip.Application.Tests/Directories/Commands/CreateInternalUserCommandHandlerTests.cs tests/EforTakip.Application.Tests/Users/Commands/CreateInternalUserCommandHandlerTests.cs
git mv tests/EforTakip.Application.Tests/Directories/Commands/ResetInternalUserPasswordCommandHandlerTests.cs tests/EforTakip.Application.Tests/Users/Commands/ResetInternalUserPasswordCommandHandlerTests.cs
git mv tests/EforTakip.Application.Tests/Directories/Queries/GetOrgChartQueryHandlerTests.cs tests/EforTakip.Application.Tests/Users/Queries/GetOrgChartQueryHandlerTests.cs
```

- [ ] **Step 2: Apply the identifier rename across all affected test files**

```bash
for f in \
  tests/EforTakip.Domain.Tests/Users/UserTests.cs \
  tests/EforTakip.Application.Tests/Users/Commands/CreateInternalUserCommandHandlerTests.cs \
  tests/EforTakip.Application.Tests/Users/Commands/ResetInternalUserPasswordCommandHandlerTests.cs \
  tests/EforTakip.Application.Tests/Users/Queries/GetOrgChartQueryHandlerTests.cs \
  tests/EforTakip.Application.Tests/Directories/Commands/TestDbContext.cs \
  tests/EforTakip.Application.Tests/Directories/Commands/SyncDirectoryCommandHandlerTests.cs \
  tests/EforTakip.Application.Tests/Directories/Commands/SyncDirectoryCommandHandlerRealDbContextTests.cs \
  tests/EforTakip.Application.Tests/Roles/Queries/GetRoleByIdQueryHandlerTests.cs \
  tests/EforTakip.Application.Tests/Roles/Commands/AssignRoleToUserCommandHandlerTests.cs \
  tests/EforTakip.Application.Tests/Auth/LoginCommandHandlerTests.cs \
  ; do
  sed -i 's/EforTakip\.Application\.Directories\.Commands\.CreateInternalUser/EforTakip.Application.Users.Commands.CreateInternalUser/g' "$f"
  sed -i 's/EforTakip\.Application\.Directories\.Commands\.ResetInternalUserPassword/EforTakip.Application.Users.Commands.ResetInternalUserPassword/g' "$f"
  sed -i 's/EforTakip\.Application\.Directories\.Queries\.GetOrgChart/EforTakip.Application.Users.Queries.GetOrgChart/g' "$f"
  sed -i 's/EforTakip\.Application\.Directories\.Queries\.GetDirectoryUsers/EforTakip.Application.Users.Queries.GetUsers/g' "$f"
  sed -i 's/EforTakip\.Application\.Directories\.Queries\.GetDirectoryUserById/EforTakip.Application.Users.Queries.GetUserById/g' "$f"
  sed -i 's/EforTakip\.Application\.Directories\.Dtos/EforTakip.Application.Users.Dtos/g' "$f"
  sed -i 's/DirectoryUser/User/g' "$f"
done
```

Also fix the namespace declaration inside the two moved test files that changed folder (their own `namespace` line must match their new path):
```bash
sed -i 's/namespace EforTakip\.Domain\.Tests\.Directories;/namespace EforTakip.Domain.Tests.Users;/' tests/EforTakip.Domain.Tests/Users/UserTests.cs
sed -i 's/namespace EforTakip\.Application\.Tests\.Directories\.Commands;/namespace EforTakip.Application.Tests.Users.Commands;/' tests/EforTakip.Application.Tests/Users/Commands/CreateInternalUserCommandHandlerTests.cs tests/EforTakip.Application.Tests/Users/Commands/ResetInternalUserPasswordCommandHandlerTests.cs
sed -i 's/namespace EforTakip\.Application\.Tests\.Directories\.Queries;/namespace EforTakip.Application.Tests.Users.Queries;/' tests/EforTakip.Application.Tests/Users/Queries/GetOrgChartQueryHandlerTests.cs
```

- [ ] **Step 3: Build and run the full test suite**

```bash
dotnet build EforTakip.sln
dotnet test EforTakip.sln
```
Expected: build succeeds with 0 errors; test run ends with a summary line showing `Failed: 0` (some pre-existing skipped/real-DB-only tests such as `SyncDirectoryCommandHandlerRealDbContextTests` may report as skipped if no `RealDb`-configured connection is available in the test run environment — that is pre-existing behavior, not something this rename should change; do not investigate further unless the failure message mentions `DirectoryUser`, `UserId`, or a compile error).

If `CS0246`/`CS1061` errors remain, re-run the grep from Task 2 Step 2 across `tests/` as well:
```bash
grep -rln "DirectoryUser" tests --include="*.cs"
```
Expected: no output.

- [ ] **Step 4: Commit**

```bash
git add backend/tests
git commit -m "$(cat <<'EOF'
refactor: rename DirectoryUser test references to User

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

### Task 4: Apply the migration to the real PostgreSQL database and verify no data loss

**Files:** none (verification-only task; the migration itself was created in Task 1).

**Interfaces:**
- Consumes: the `RenameDirectoryUserToUser` migration (Task 1) and the running `efortakip_dev` PostgreSQL database in this worktree (already has real AD-synced users from the earlier session).

- [ ] **Step 1: Snapshot the current user count and usernames before migrating**

With the `RealDb`-configured connection string already in `dotnet user-secrets` (set earlier in this session), run:
```bash
cd backend
dotnet ef migrations list --project src/EforTakip.Persistence --startup-project src/EforTakip.Api -- --environment RealDb
```
Expected: the list ends with `RenameDirectoryUserToUser (Pending)`.

Ask the user to record the current row count via their own `psql` session before proceeding (they hold the DB password, not this session):
```powershell
& "C:\Program Files\PostgreSQL\18\bin\psql.exe" -U efortakip_dev -h localhost -d efortakip_dev -c "SELECT COUNT(*) FROM \"DirectoryUsers\";"
```

- [ ] **Step 2: Apply the migration**

```bash
dotnet ef database update --project src/EforTakip.Persistence --startup-project src/EforTakip.Api -- --environment RealDb
```
Expected: ends with `Done.` and no errors.

- [ ] **Step 3: Verify the row count survived the rename**

Ask the user to run, in their own `psql` session:
```powershell
& "C:\Program Files\PostgreSQL\18\bin\psql.exe" -U efortakip_dev -h localhost -d efortakip_dev -c "SELECT COUNT(*) FROM \"Users\";"
```
Expected: the same count recorded in Step 1. If the counts differ, STOP — do not proceed to Task 5 — and investigate the migration before continuing (this would mean the no-data-loss guarantee was violated).

- [ ] **Step 4: Start the backend against RealDb and smoke-test the renamed endpoint**

```bash
dotnet run --project src/EforTakip.Api --launch-profile RealDb
```
In a second terminal, once "Application started" appears in the log:
```bash
curl -s -X POST http://localhost:5298/api/v1/auth/login -H "Content-Type: application/json" -d '{"username":"admin","password":"Admin123!"}'
```
Expected: a JSON response containing a `token` field (200 OK), not a 401/500. Copy the `token` value, then:
```bash
curl -s http://localhost:5298/api/v1/users -H "Authorization: Bearer <token>"
```
Expected: a 200 OK JSON `PagedResult` containing the previously-synced AD users by username/displayName (same people seen before this migration).

Stop the backend (Ctrl+C in its terminal) once verified.

---

### Task 5: Frontend rename

**Files:**
- Move: `frontend/src/api/directoryUsers.ts` → `frontend/src/api/users.ts`
- Move: `frontend/src/hooks/useDirectoryUsers.ts` → `frontend/src/hooks/useUsers.ts`
- Move: `frontend/src/components/admin/directory/DirectoryUserCard.tsx` → `frontend/src/components/admin/directory/UserCard.tsx`
- Move: `frontend/src/components/admin/directory/DirectoryUserCardModal.tsx` → `frontend/src/components/admin/directory/UserCardModal.tsx`
- Modify: `frontend/src/api/types.ts`
- Modify: `frontend/src/hooks/useDirectoryMutations.ts`
- Modify: `frontend/src/components/admin/directory/OrgChartSection.tsx`
- Modify: `frontend/src/components/admin/directory/UsersSection.tsx`
- Modify: `frontend/src/components/admin/roles/RolesSection.tsx`

**Interfaces:**
- Consumes: `GET/POST /api/v1/users*` (produced by Task 2).

- [ ] **Step 1: Move the 4 files**

```bash
cd frontend
git mv src/api/directoryUsers.ts src/api/users.ts
git mv src/hooks/useDirectoryUsers.ts src/hooks/useUsers.ts
git mv src/components/admin/directory/DirectoryUserCard.tsx src/components/admin/directory/UserCard.tsx
git mv src/components/admin/directory/DirectoryUserCardModal.tsx src/components/admin/directory/UserCardModal.tsx
```

- [ ] **Step 2: Rewrite `src/api/users.ts`**

Replace its full content with:
```typescript
import { apiClient } from './client';
import type { UserDetailDto, UserDto, PagedResult } from './types';

export function getUsers(options?: {
  directoryId?: string;
  searchTerm?: string;
  onlyActive?: boolean;
  pageNumber?: number;
  pageSize?: number;
}) {
  return apiClient.get<PagedResult<UserDto>>('/api/v1/users', {
    directoryId: options?.directoryId,
    searchTerm: options?.searchTerm,
    onlyActive: options?.onlyActive,
    pageNumber: options?.pageNumber ?? 1,
    pageSize: options?.pageSize ?? 25,
  });
}

export function getUserById(id: string) {
  return apiClient.get<UserDetailDto>(`/api/v1/users/${id}`);
}

export interface CreateInternalUserPayload {
  directoryId: string;
  username: string;
  password: string;
  firstName?: string | null;
  lastName?: string | null;
  displayName?: string | null;
  email?: string | null;
}

export function createInternalUser(payload: CreateInternalUserPayload) {
  return apiClient.post<void>('/api/v1/users/internal', payload);
}

export function resetInternalUserPassword(userId: string, newPassword: string) {
  return apiClient.post<void>(`/api/v1/users/${userId}/reset-password`, {
    userId,
    newPassword,
  });
}
```

- [ ] **Step 3: Rewrite `src/hooks/useUsers.ts`**

Replace its full content (apply the same shape the old `useDirectoryUsers.ts` had, renamed):
```bash
cat src/hooks/useUsers.ts
```
Take the printed content and:
- rename the import from `'../api/directoryUsers'` to `'../api/users'`
- rename imported functions `getDirectoryUserById` → `getUserById`, `getDirectoryUsers` → `getUsers`
- rename exported hooks `useDirectoryUsers` → `useUsers`, `useDirectoryUser` → `useUser`
- rename the two React Query cache keys `'directoryUsers'` → `'users'`

- [ ] **Step 4: Update `src/api/types.ts`**

Rename the three interfaces:
```bash
sed -i 's/DirectoryUserAttributeValueDto/UserAttributeValueDto/g' src/api/types.ts
sed -i 's/DirectoryUserDetailDto/UserDetailDto/g' src/api/types.ts
sed -i 's/DirectoryUserDto/UserDto/g' src/api/types.ts
sed -i 's/referencedDirectoryUserId/referencedUserId/g' src/api/types.ts
```
(Order matters: the two `Detail`/plain `Dto` substitutions must happen before nothing else conflicts, and `DirectoryUserAttributeValueDto` must be replaced before the plainer `DirectoryUserDto` pattern could partially match it — since `sed` does exact substring matching this is already safe as written, but keep this exact order.)

- [ ] **Step 5: Update the remaining 3 consumer files**

```bash
sed -i \
  -e "s/getDirectoryUsers/getUsers/g" \
  -e "s/getDirectoryUserById/getUserById/g" \
  -e "s/useDirectoryUsers/useUsers/g" \
  -e "s/useDirectoryUser\b/useUser/g" \
  -e "s/DirectoryUserDto/UserDto/g" \
  -e "s/DirectoryUserDetailDto/UserDetailDto/g" \
  -e "s/'directoryUsers'/'users'/g" \
  -e "s#'\.\./api/directoryUsers'#'../api/users'#g" \
  -e "s#'\.\./\.\./\.\./api/directoryUsers'#'../../../api/users'#g" \
  src/hooks/useDirectoryMutations.ts \
  src/components/admin/directory/OrgChartSection.tsx \
  src/components/admin/directory/UsersSection.tsx \
  src/components/admin/roles/RolesSection.tsx
```

Also update any import of the two moved components:
```bash
grep -rln "DirectoryUserCard" src --include="*.tsx" --include="*.ts"
```
For every file listed, replace `DirectoryUserCardModal` → `UserCardModal` and `DirectoryUserCard` → `UserCard` (in that order, since `DirectoryUserCard` is a prefix of `DirectoryUserCardModal`):
```bash
for f in $(grep -rln "DirectoryUserCard" src --include="*.tsx" --include="*.ts"); do
  sed -i -e 's/DirectoryUserCardModal/UserCardModal/g' -e 's/DirectoryUserCard/UserCard/g' "$f"
done
```

- [ ] **Step 6: Verify no references remain, then build and lint**

```bash
grep -rln "DirectoryUser\|directoryUser" src --include="*.ts" --include="*.tsx"
```
Expected: no output (empty). If any file is listed, open it and fix the remaining reference by hand using the same renaming pattern as above before continuing.

```bash
npm run build
npm run lint
```
Expected: `build` completes with no TypeScript errors (exit code 0); `lint` reports no new errors (pre-existing warnings unrelated to this rename, if any, are out of scope).

- [ ] **Step 7: Manual browser smoke test**

With the backend running (`dotnet run --project src/EforTakip.Api --launch-profile RealDb` from Task 4) and frontend running (`npm run dev` from `frontend/`), open `http://localhost:5180`, log in as `admin` / `Admin123!`, navigate to the admin Users screen (previously "Directory Users"/"Kullanıcılar" section) and confirm the previously-synced AD users still list correctly, then open the Org Chart section and confirm it still renders. Take a screenshot as evidence.

- [ ] **Step 8: Commit**

```bash
cd ..
git add frontend
git commit -m "$(cat <<'EOF'
refactor: rename DirectoryUser references to User in frontend

Matches the backend User rename and the /api/v1/users route change.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

## Plan Self-Review Notes

- **Spec coverage:** Domain move (Task 1) ✓, Persistence table/column rename with no data loss (Task 1 + 4) ✓, Application module split (Task 1) ✓, API route rename (Task 2) ✓, tests (Task 3) ✓, frontend (Task 5) ✓, data-safety verification via row-count check (Task 4) ✓, `Restrict` FK behavior preserved (untouched by this rename — `UserConfiguration.cs` keeps the same `OnDelete(DeleteBehavior.Restrict)` call copied verbatim from `DirectoryUserConfiguration.cs`) ✓.
- **Out of scope reminders carried from the spec:** `Employee` entity untouched, no delete-user endpoint added, `WorkCalendarId` not added to `User` — none of these appear anywhere in this plan, confirmed.
- **Type consistency check:** `UserId` (property on `UserAttribute`/`UserRole`), `ReferencedUserId` (property on `UserAttribute`), `UserDto`/`UserDetailDto`/`UserAttributeValueDto` (Application DTOs), `Users`/`UserAttributes`/`UserRoles` (DbSet + table names) are used identically across Tasks 1, 2, 3, and 4 — verified no drift between tasks.


