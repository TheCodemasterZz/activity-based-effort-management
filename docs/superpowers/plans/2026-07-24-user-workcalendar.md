# WorkCalendarId on User (Phase 2) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a nullable `WorkCalendarId` to `User`, with single/bulk assignment endpoints, a sync-time notification when users are left without a calendar, and an admin UI filter — without auto-assigning a default calendar to anyone.

**Architecture:** Additive-only change across Domain → Persistence → Application → API → Frontend. No existing behavior changes; `WorkCalendarId` starts `null` for every user and is only set through an explicit admin action.

**Tech Stack:** .NET 8, EF Core 8 + Npgsql, MediatR, FluentValidation, React + TypeScript + Vite, TanStack Query.

## Global Constraints

- `User.WorkCalendarId` is nullable; AD sync and internal user creation never set it.
- The FK `Users.WorkCalendarId → WorkCalendars.Id` uses `ReferentialAction.Restrict` (same pattern as `Employee.WorkCalendarId`) — a work calendar cannot be deleted while users are assigned to it.
- No "block work log entry without a calendar" rule is added in this phase — that belongs to Phase 3 (see `docs/superpowers/specs/2026-07-24-user-workcalendar-design.md`).
- `Employee` entity is not touched.
- Migration must not lose data: it only adds a new nullable column + FK + index, nothing existing is renamed or dropped.
- Working directory for all commands below: `C:\Projects\activity-based-effort-management-main\.claude\worktrees\user-workcalendar` (worktree branch `worktree-user-workcalendar`).

---

### Task 1: Domain — add WorkCalendarId and AssignWorkCalendar to User

**Files:**
- Modify: `backend/src/EforTakip.Domain/Users/User.cs`
- Test: `backend/tests/EforTakip.Domain.Tests/Users/UserTests.cs`

**Interfaces:**
- Produces: `User.WorkCalendarId` (`Guid?`, public read-only), `User.AssignWorkCalendar(Guid workCalendarId)` (throws `BusinessRuleValidationException` if `workCalendarId == Guid.Empty`).
- Consumed by: Task 2 (Persistence config), Task 3 (Application commands).

- [ ] **Step 1: Write the failing tests**

Append to `backend/tests/EforTakip.Domain.Tests/Users/UserTests.cs` (inside the existing `UserTests` class, e.g. right after the `CreateFromActiveDirectory_WithValidData_CreatesUser` test):

```csharp
    [Fact]
    public void CreateFromActiveDirectory_NeverSetsWorkCalendarId()
    {
        var user = User.CreateFromActiveDirectory(
            Guid.NewGuid(), "serkan.gultepe", "Serkan", "Gültepe",
            "Serkan Gültepe", "serkan@kizilay.org.tr", "guid-123");

        user.WorkCalendarId.Should().BeNull();
    }

    [Fact]
    public void AssignWorkCalendar_WithValidId_SetsWorkCalendarId()
    {
        var user = User.CreateFromActiveDirectory(
            Guid.NewGuid(), "serkan.gultepe", "Serkan", "Gültepe",
            "Serkan Gültepe", "serkan@kizilay.org.tr", "guid-123");
        var workCalendarId = Guid.NewGuid();

        user.AssignWorkCalendar(workCalendarId);

        user.WorkCalendarId.Should().Be(workCalendarId);
    }

    [Fact]
    public void AssignWorkCalendar_WithEmptyId_Throws()
    {
        var user = User.CreateFromActiveDirectory(
            Guid.NewGuid(), "serkan.gultepe", "Serkan", "Gültepe",
            "Serkan Gültepe", "serkan@kizilay.org.tr", "guid-123");

        var act = () => user.AssignWorkCalendar(Guid.Empty);

        act.Should().Throw<BusinessRuleValidationException>();
    }
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `cd backend && dotnet test tests/EforTakip.Domain.Tests --filter "FullyQualifiedName~UserTests" -v q`
Expected: 3 new tests FAIL with `CS1061`/`CS0117`-style build errors (`WorkCalendarId`/`AssignWorkCalendar` don't exist yet) — the whole test project fails to build, which counts as the expected failing state for this step.

- [ ] **Step 3: Implement the minimal Domain change**

In `backend/src/EforTakip.Domain/Users/User.cs`, add the property after `LastSyncedUtc`:

```csharp
    public Guid? WorkCalendarId { get; private set; }
```

Add the method after `Deactivate`/`Activate` (right before `SetPassword` is a good spot):

```csharp
    /// <summary>
    /// Kullanıcının mesai takvimini atar. AD senkronu ve internal kullanıcı oluşturma bu alanı
    /// hep boş bırakır — sabit bir varsayılan atamak yanlış kapasite hesaplamalarına yol
    /// açabilir; bu yüzden atama her zaman ayrı, bilinçli bir admin eylemidir.
    /// </summary>
    public void AssignWorkCalendar(Guid workCalendarId)
    {
        if (workCalendarId == Guid.Empty)
            throw new BusinessRuleValidationException("Mesai takvimi seçilmelidir.");

        WorkCalendarId = workCalendarId;
    }
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `cd backend && dotnet test tests/EforTakip.Domain.Tests --filter "FullyQualifiedName~UserTests" -v q`
Expected: all `UserTests` PASS (existing tests + 3 new ones), 0 failures.

- [ ] **Step 5: Commit**

```bash
git add backend/src/EforTakip.Domain/Users/User.cs backend/tests/EforTakip.Domain.Tests/Users/UserTests.cs
git commit -m "$(cat <<'EOF'
feat: add nullable WorkCalendarId to User

Phase 2 of the Employee/User merge roadmap. WorkCalendarId starts
null for every user (AD sync and internal creation never set it) and
is only assigned through the explicit AssignWorkCalendar method —
never a default, since a wrong guess would corrupt capacity
calculations.
EOF
)"
```

---

### Task 2: Persistence — FK configuration and migration

**Files:**
- Modify: `backend/src/EforTakip.Persistence/Configurations/UserConfiguration.cs`
- Create: `backend/src/EforTakip.Persistence/Migrations/<timestamp>_AddWorkCalendarIdToUser.cs` (and paired `.Designer.cs`, auto-generated)
- Modify (auto-generated): `backend/src/EforTakip.Persistence/Migrations/EforTakipDbContextModelSnapshot.cs`

**Interfaces:**
- Consumes: `User.WorkCalendarId` (Task 1).
- Produces: `Users.WorkCalendarId` nullable `uuid` column with FK to `WorkCalendars.Id` (`Restrict`), index `IX_Users_WorkCalendarId`.

- [ ] **Step 1: Add the FK configuration**

In `backend/src/EforTakip.Persistence/Configurations/UserConfiguration.cs`, add `using EforTakip.Domain.WorkCalendars;` to the usings, then add this block inside `Configure` (after the `HasOne<Directory>()` block, before the `Roles` block):

```csharp
        builder.HasOne<WorkCalendar>()
            .WithMany()
            .HasForeignKey(u => u.WorkCalendarId)
            .OnDelete(DeleteBehavior.Restrict);
```

- [ ] **Step 2: Build to verify it compiles**

Run: `cd backend && dotnet build src/EforTakip.Persistence/EforTakip.Persistence.csproj`
Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`.

- [ ] **Step 3: Scaffold the migration**

```bash
dotnet ef migrations add AddWorkCalendarIdToUser --project src/EforTakip.Persistence --startup-project src/EforTakip.Api -- --environment RealDb
```

Expected: no data-loss warning this time (pure additive column). Open the generated
`<timestamp>_AddWorkCalendarIdToUser.cs` and confirm its `Up()` contains exactly one
`AddColumn<Guid>` (nullable, no default), one `CreateIndex` on `WorkCalendarId`, and one
`AddForeignKey` to `WorkCalendars`; `Down()` mirrors with `DropForeignKey`, `DropIndex`,
`DropColumn`. Do not hand-edit this file — the auto-generated content is already correct for
a pure additive change (unlike the Phase 1 rename migration, there is no rename to fix here).

- [ ] **Step 4: Build the full solution**

Run: `cd backend && dotnet build EforTakip.sln`
Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`.

- [ ] **Step 5: Commit**

```bash
git add backend/src/EforTakip.Persistence
git commit -m "$(cat <<'EOF'
feat: add WorkCalendarId column and FK to Users table

Nullable, Restrict on delete (matches Employee.WorkCalendarId's
pattern) — a work calendar can't be deleted while users are assigned
to it. Purely additive migration, no existing data touched.
EOF
)"
```

---

### Task 3: Application — assignment commands and query/DTO extensions

**Files:**
- Create: `backend/src/EforTakip.Application/Users/Commands/AssignWorkCalendar/AssignWorkCalendarCommand.cs`
- Create: `backend/src/EforTakip.Application/Users/Commands/AssignWorkCalendar/AssignWorkCalendarCommandHandler.cs`
- Create: `backend/src/EforTakip.Application/Users/Commands/AssignWorkCalendar/AssignWorkCalendarCommandValidator.cs`
- Create: `backend/src/EforTakip.Application/Users/Commands/BulkAssignWorkCalendar/BulkAssignWorkCalendarCommand.cs`
- Create: `backend/src/EforTakip.Application/Users/Commands/BulkAssignWorkCalendar/BulkAssignWorkCalendarCommandHandler.cs`
- Create: `backend/src/EforTakip.Application/Users/Commands/BulkAssignWorkCalendar/BulkAssignWorkCalendarCommandValidator.cs`
- Modify: `backend/src/EforTakip.Application/Users/Queries/GetUsers/GetUsersQuery.cs`
- Modify: `backend/src/EforTakip.Application/Users/Queries/GetUsers/GetUsersQueryHandler.cs`
- Modify: `backend/src/EforTakip.Application/Users/Dtos/UserDto.cs`
- Modify: `backend/src/EforTakip.Application/Users/Dtos/UserDetailDto.cs`
- Modify: `backend/src/EforTakip.Application/Users/Queries/GetUserById/GetUserByIdQueryHandler.cs`
- Test: `backend/tests/EforTakip.Application.Tests/Users/Commands/AssignWorkCalendarCommandHandlerTests.cs`
- Test: `backend/tests/EforTakip.Application.Tests/Users/Commands/BulkAssignWorkCalendarCommandHandlerTests.cs`

**Interfaces:**
- Consumes: `User.AssignWorkCalendar` (Task 1), `TestDbContext` (existing, at
  `EforTakip.Application.Tests.Directories.Commands.TestDbContext`).
- Produces: `AssignWorkCalendarCommand(Guid UserId, Guid WorkCalendarId) : IRequest`,
  `BulkAssignWorkCalendarCommand(IReadOnlyCollection<Guid> UserIds, Guid WorkCalendarId) : IRequest`,
  `UserDto.WorkCalendarId`/`WorkCalendarName`, `GetUsersQuery.OnlyMissingWorkCalendar`.
- Consumed by: Task 4 (API controller).

- [ ] **Step 1: Write the failing test for AssignWorkCalendarCommand**

Create `backend/tests/EforTakip.Application.Tests/Users/Commands/AssignWorkCalendarCommandHandlerTests.cs`:

```csharp
using EforTakip.Application.Tests.Directories.Commands;
using EforTakip.Application.Users.Commands.AssignWorkCalendar;
using EforTakip.Domain.Directories;
using EforTakip.Domain.Exceptions;
using EforTakip.Domain.Users;
using EforTakip.Domain.WorkCalendars;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace EforTakip.Application.Tests.Users.Commands;

public class AssignWorkCalendarCommandHandlerTests
{
    private static TestDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase($"assign-work-calendar-tests-{Guid.NewGuid()}")
            .Options;
        return new TestDbContext(options);
    }

    [Fact]
    public async Task Handle_WithValidUserAndCalendar_AssignsCalendar()
    {
        using var db = CreateDb();
        var directory = Domain.Directories.Directory.CreateInternal("Internal Users", 0);
        db.Directories.Add(directory);
        var user = User.CreateInternal(directory.Id, "serkan", "Serkan", "Gültepe", "Serkan Gültepe", "a@b.com", "hash");
        db.Users.Add(user);
        var workCalendar = WorkCalendar.Create("Standart");
        db.WorkCalendars.Add(workCalendar);
        await db.SaveChangesAsync(CancellationToken.None);

        var unitOfWork = Substitute.For<IUnitOfWork>();
        var handler = new AssignWorkCalendarCommandHandler(db, unitOfWork);
        var command = new AssignWorkCalendarCommand(user.Id, workCalendar.Id);

        await handler.Handle(command, CancellationToken.None);

        var updated = await db.Users.AsNoTracking().FirstAsync(u => u.Id == user.Id);
        updated.WorkCalendarId.Should().Be(workCalendar.Id);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithUnknownUser_ThrowsNotFoundException()
    {
        using var db = CreateDb();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var handler = new AssignWorkCalendarCommandHandler(db, unitOfWork);
        var command = new AssignWorkCalendarCommand(Guid.NewGuid(), Guid.NewGuid());

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
```

Note: `IUnitOfWork` needs `using EforTakip.Application.Common.Interfaces;` — add it to the usings above.

- [ ] **Step 2: Run tests to verify they fail**

Run: `cd backend && dotnet test tests/EforTakip.Application.Tests --filter "FullyQualifiedName~AssignWorkCalendarCommandHandlerTests" -v q`
Expected: FAILS to build (`AssignWorkCalendarCommand`/`AssignWorkCalendarCommandHandler` don't exist yet).

- [ ] **Step 3: Implement AssignWorkCalendarCommand**

Create `backend/src/EforTakip.Application/Users/Commands/AssignWorkCalendar/AssignWorkCalendarCommand.cs`:

```csharp
using MediatR;

namespace EforTakip.Application.Users.Commands.AssignWorkCalendar;

public sealed record AssignWorkCalendarCommand(Guid UserId, Guid WorkCalendarId) : IRequest;
```

Create `backend/src/EforTakip.Application/Users/Commands/AssignWorkCalendar/AssignWorkCalendarCommandHandler.cs`:

```csharp
using EforTakip.Application.Common.Interfaces;
using EforTakip.Domain.Exceptions;
using EforTakip.Domain.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EforTakip.Application.Users.Commands.AssignWorkCalendar;

public sealed class AssignWorkCalendarCommandHandler(
    IApplicationDbContext db,
    IUnitOfWork unitOfWork)
    : IRequestHandler<AssignWorkCalendarCommand>
{
    public async Task Handle(AssignWorkCalendarCommand request, CancellationToken cancellationToken)
    {
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), request.UserId);

        user.AssignWorkCalendar(request.WorkCalendarId);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
```

Create `backend/src/EforTakip.Application/Users/Commands/AssignWorkCalendar/AssignWorkCalendarCommandValidator.cs`:

```csharp
using FluentValidation;

namespace EforTakip.Application.Users.Commands.AssignWorkCalendar;

public sealed class AssignWorkCalendarCommandValidator : AbstractValidator<AssignWorkCalendarCommand>
{
    public AssignWorkCalendarCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("Kullanıcı seçilmelidir.");
        RuleFor(x => x.WorkCalendarId).NotEmpty().WithMessage("Mesai takvimi seçilmelidir.");
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `cd backend && dotnet test tests/EforTakip.Application.Tests --filter "FullyQualifiedName~AssignWorkCalendarCommandHandlerTests" -v q`
Expected: both tests PASS.

- [ ] **Step 5: Write the failing test for BulkAssignWorkCalendarCommand**

Create `backend/tests/EforTakip.Application.Tests/Users/Commands/BulkAssignWorkCalendarCommandHandlerTests.cs`:

```csharp
using EforTakip.Application.Tests.Directories.Commands;
using EforTakip.Application.Users.Commands.BulkAssignWorkCalendar;
using EforTakip.Domain.Users;
using EforTakip.Domain.WorkCalendars;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace EforTakip.Application.Tests.Users.Commands;

public class BulkAssignWorkCalendarCommandHandlerTests
{
    private static TestDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase($"bulk-assign-work-calendar-tests-{Guid.NewGuid()}")
            .Options;
        return new TestDbContext(options);
    }

    [Fact]
    public async Task Handle_WithMultipleUsers_AssignsCalendarToAll()
    {
        using var db = CreateDb();
        var directory = Domain.Directories.Directory.CreateInternal("Internal Users", 0);
        db.Directories.Add(directory);
        var user1 = User.CreateInternal(directory.Id, "serkan", null, null, "Serkan", null, "hash");
        var user2 = User.CreateInternal(directory.Id, "ayse", null, null, "Ayşe", null, "hash");
        db.Users.AddRange(user1, user2);
        var workCalendar = WorkCalendar.Create("Standart");
        db.WorkCalendars.Add(workCalendar);
        await db.SaveChangesAsync(CancellationToken.None);

        var unitOfWork = Substitute.For<IUnitOfWork>();
        var handler = new BulkAssignWorkCalendarCommandHandler(db, unitOfWork);
        var command = new BulkAssignWorkCalendarCommand([user1.Id, user2.Id], workCalendar.Id);

        await handler.Handle(command, CancellationToken.None);

        var updated = await db.Users.AsNoTracking().Where(u => u.WorkCalendarId == workCalendar.Id).CountAsync();
        updated.Should().Be(2);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
```

- [ ] **Step 6: Run to verify it fails**

Run: `cd backend && dotnet test tests/EforTakip.Application.Tests --filter "FullyQualifiedName~BulkAssignWorkCalendarCommandHandlerTests" -v q`
Expected: FAILS to build.

- [ ] **Step 7: Implement BulkAssignWorkCalendarCommand**

Create `backend/src/EforTakip.Application/Users/Commands/BulkAssignWorkCalendar/BulkAssignWorkCalendarCommand.cs`:

```csharp
using MediatR;

namespace EforTakip.Application.Users.Commands.BulkAssignWorkCalendar;

public sealed record BulkAssignWorkCalendarCommand(
    IReadOnlyCollection<Guid> UserIds, Guid WorkCalendarId) : IRequest;
```

Create `backend/src/EforTakip.Application/Users/Commands/BulkAssignWorkCalendar/BulkAssignWorkCalendarCommandHandler.cs`:

```csharp
using EforTakip.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EforTakip.Application.Users.Commands.BulkAssignWorkCalendar;

public sealed class BulkAssignWorkCalendarCommandHandler(
    IApplicationDbContext db,
    IUnitOfWork unitOfWork)
    : IRequestHandler<BulkAssignWorkCalendarCommand>
{
    public async Task Handle(BulkAssignWorkCalendarCommand request, CancellationToken cancellationToken)
    {
        var users = await db.Users
            .Where(u => request.UserIds.Contains(u.Id))
            .ToListAsync(cancellationToken);

        foreach (var user in users)
            user.AssignWorkCalendar(request.WorkCalendarId);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
```

Create `backend/src/EforTakip.Application/Users/Commands/BulkAssignWorkCalendar/BulkAssignWorkCalendarCommandValidator.cs`:

```csharp
using FluentValidation;

namespace EforTakip.Application.Users.Commands.BulkAssignWorkCalendar;

public sealed class BulkAssignWorkCalendarCommandValidator : AbstractValidator<BulkAssignWorkCalendarCommand>
{
    public BulkAssignWorkCalendarCommandValidator()
    {
        RuleFor(x => x.UserIds).NotEmpty().WithMessage("En az bir kullanıcı seçilmelidir.");
        RuleFor(x => x.WorkCalendarId).NotEmpty().WithMessage("Mesai takvimi seçilmelidir.");
    }
}
```

- [ ] **Step 8: Run tests to verify they pass**

Run: `cd backend && dotnet test tests/EforTakip.Application.Tests --filter "FullyQualifiedName~BulkAssignWorkCalendarCommandHandlerTests" -v q`
Expected: PASS.

- [ ] **Step 9: Extend GetUsersQuery, GetUsersQueryHandler, UserDto, UserDetailDto, GetUserByIdQueryHandler**

In `backend/src/EforTakip.Application/Users/Queries/GetUsers/GetUsersQuery.cs`, add a property:

```csharp
    public bool? OnlyMissingWorkCalendar { get; set; }
```//placed alongside OnlyActive

In `backend/src/EforTakip.Application/Users/Queries/GetUsers/GetUsersQueryHandler.cs`, add a
filter right after the existing `OnlyActive` block:

```csharp
        if (request.OnlyMissingWorkCalendar == true)
            query = query.Where(u => u.WorkCalendarId == null);
```

Change the projection's `.Join(db.Directories, ...)` to a `.GroupJoin`-free left join against
`WorkCalendars` by first joining `Directories` (unchanged), then adding a second left join.
Replace the whole projection block with:

```csharp
        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Join(db.Directories, u => u.DirectoryId, d => d.Id, (u, d) => new { u, DirectoryName = d.Name })
            .GroupJoin(db.WorkCalendars, x => x.u.WorkCalendarId, wc => wc.Id, (x, wcs) => new { x.u, x.DirectoryName, WorkCalendars = wcs })
            .SelectMany(x => x.WorkCalendars.DefaultIfEmpty(), (x, wc) => new UserDto
            {
                Id = x.u.Id,
                DirectoryId = x.u.DirectoryId,
                DirectoryName = x.DirectoryName,
                Source = x.u.Source,
                Username = x.u.Username,
                FirstName = x.u.FirstName,
                LastName = x.u.LastName,
                DisplayName = x.u.DisplayName,
                Email = x.u.Email,
                IsActive = x.u.IsActive,
                LastSyncedUtc = x.u.LastSyncedUtc,
                WorkCalendarId = x.u.WorkCalendarId,
                WorkCalendarName = wc != null ? wc.Name : null
            })
            .ToListAsync(cancellationToken);
```

In `backend/src/EforTakip.Application/Users/Dtos/UserDto.cs`, add two properties after
`LastSyncedUtc`:

```csharp
    public Guid? WorkCalendarId { get; init; }
    public string? WorkCalendarName { get; init; }
```

In `backend/src/EforTakip.Application/Users/Dtos/UserDetailDto.cs`, add the same two properties
to `UserDetailDto` (after `LastSyncedUtc`, before `Attributes`):

```csharp
    public Guid? WorkCalendarId { get; init; }
    public string? WorkCalendarName { get; init; }
```

In `backend/src/EforTakip.Application/Users/Queries/GetUserById/GetUserByIdQueryHandler.cs`,
add a lookup right after the `directoryName` lookup:

```csharp
        var workCalendarName = user.WorkCalendarId is { } workCalendarId
            ? await db.WorkCalendars
                .AsNoTracking()
                .Where(wc => wc.Id == workCalendarId)
                .Select(wc => wc.Name)
                .FirstOrDefaultAsync(cancellationToken)
            : null;
```

and add the two fields to the `UserDetailDto` object being returned:

```csharp
            WorkCalendarId = user.WorkCalendarId,
            WorkCalendarName = workCalendarName,
```

(placed right after `LastSyncedUtc = user.LastSyncedUtc,`).

- [ ] **Step 10: Run the full Application test suite**

Run: `cd backend && dotnet test tests/EforTakip.Application.Tests -v q`
Expected: `Failed: 0`.

- [ ] **Step 11: Commit**

```bash
git add backend/src/EforTakip.Application backend/tests/EforTakip.Application.Tests
git commit -m "$(cat <<'EOF'
feat: add work calendar assignment commands and expose it on User queries

AssignWorkCalendarCommand (single) and BulkAssignWorkCalendarCommand
(many), plus a WorkCalendarId/WorkCalendarName pair on UserDto and
UserDetailDto and an OnlyMissingWorkCalendar filter on GetUsersQuery,
for the admin "kullanıcılara takvim ata" flow.
EOF
)"
```

---

### Task 4: API — controller endpoints

**Files:**
- Modify: `backend/src/EforTakip.Api/Controllers/v1/UsersController.cs`

**Interfaces:**
- Consumes: `AssignWorkCalendarCommand`, `BulkAssignWorkCalendarCommand` (Task 3).
- Produces: `POST /api/v1/users/{id}/work-calendar`, `POST /api/v1/users/work-calendar/bulk`.

- [ ] **Step 1: Add the two endpoints**

In `backend/src/EforTakip.Api/Controllers/v1/UsersController.cs`, add two usings:

```csharp
using EforTakip.Application.Users.Commands.AssignWorkCalendar;
using EforTakip.Application.Users.Commands.BulkAssignWorkCalendar;
```

Add two actions at the end of the class, right before the closing `}`:

```csharp
    [RequirePermission(Permissions.User.Manage)]
    [HttpPost("{id:guid}/work-calendar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> AssignWorkCalendar(
        Guid id, AssignWorkCalendarCommand command, CancellationToken cancellationToken)
    {
        if (id != command.UserId)
            return BadRequest("Route ve gövde kimlikleri eşleşmiyor.");
        await mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [RequirePermission(Permissions.User.Manage)]
    [HttpPost("work-calendar/bulk")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> BulkAssignWorkCalendar(
        BulkAssignWorkCalendarCommand command, CancellationToken cancellationToken)
    {
        await mediator.Send(command, cancellationToken);
        return NoContent();
    }
```

- [ ] **Step 2: Build the solution**

Run: `cd backend && dotnet build EforTakip.sln`
Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`.

- [ ] **Step 3: Commit**

```bash
git add backend/src/EforTakip.Api/Controllers/v1/UsersController.cs
git commit -m "$(cat <<'EOF'
feat: expose work calendar assignment endpoints on UsersController

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

### Task 5: Notification on sync when users are left without a calendar

**Files:**
- Modify: `backend/src/EforTakip.Application/Directories/Commands/SyncDirectory/SyncDirectoryCommandHandler.cs`
- Test: `backend/tests/EforTakip.Application.Tests/Directories/Commands/SyncDirectoryCommandHandlerTests.cs`

**Interfaces:**
- Consumes: `Notification.Create(string message)` (existing, `EforTakip.Domain.Notifications`),
  `db.Notifications` (existing `IApplicationDbContext` member).

- [ ] **Step 1: Read the existing sync test file to find the right test fixture pattern**

Run: `cd backend && sed -n '1,50p' tests/EforTakip.Application.Tests/Directories/Commands/SyncDirectoryCommandHandlerTests.cs`

Use its existing `Handle_...` test setup (LDAP service substitute + `TestDbContext`) as the
template for the new test below — match its exact constructor/setup pattern rather than
guessing, since this file already has an established fixture for calling `Handle` with a
populated `TestDbContext` and a stubbed `ILdapService`.

- [ ] **Step 2: Write the failing test**

Add a new `[Fact]` to `SyncDirectoryCommandHandlerTests.cs` (mirror the existing tests' setup
exactly — the same `ILdapService` stub returning one or two `LdapUser` records, the same
`directory`/`db` setup already used by the neighboring tests in this file):

```csharp
    [Fact]
    public async Task Handle_WhenUsersHaveNoWorkCalendar_CreatesNotification()
    {
        // Arrange exactly like the neighboring "adds a new user" test in this file:
        // a directory + one LdapUser returned by the stubbed ILdapService, so the sync
        // creates at least one User with WorkCalendarId == null.

        await handler.Handle(new SyncDirectoryCommand(directory.Id), CancellationToken.None);

        var notifications = await db.Notifications.AsNoTracking().ToListAsync();
        notifications.Should().ContainSingle(n => n.Message.Contains("mesai takvimi atanmamış"));
    }
```

(Adjust variable names — `handler`, `directory`, `db` — to whatever the existing tests in this
file actually call them; read the file first per Step 1 before writing this.)

- [ ] **Step 3: Run to verify it fails**

Run: `cd backend && dotnet test tests/EforTakip.Application.Tests --filter "FullyQualifiedName~SyncDirectoryCommandHandlerTests" -v q`
Expected: the new test FAILS (no notification created yet); pre-existing tests in the same file still PASS.

- [ ] **Step 4: Implement the notification trigger**

In `backend/src/EforTakip.Application/Directories/Commands/SyncDirectory/SyncDirectoryCommandHandler.cs`,
add `using EforTakip.Domain.Notifications;` to the usings.

Insert this block right after the `deactivated` loop (right before `directory.MarkSynced(syncedAtUtc);`):

```csharp
        var missingCalendarCount = await db.Users
            .Where(u => u.DirectoryId == directory.Id && u.IsActive && u.WorkCalendarId == null)
            .CountAsync(cancellationToken);

        if (missingCalendarCount > 0)
        {
            db.Notifications.Add(Notification.Create(
                $"'{directory.Name}' dizininde {missingCalendarCount} kullanıcının mesai takvimi atanmamış."));
        }
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `cd backend && dotnet test tests/EforTakip.Application.Tests --filter "FullyQualifiedName~SyncDirectoryCommandHandlerTests" -v q`
Expected: `Failed: 0`.

- [ ] **Step 6: Run the full backend test suite**

Run: `cd backend && dotnet test EforTakip.sln -v q`
Expected: `Failed: 0` across `EforTakip.Domain.Tests` and `EforTakip.Application.Tests`.

- [ ] **Step 7: Commit**

```bash
git add backend/src/EforTakip.Application/Directories/Commands/SyncDirectory backend/tests/EforTakip.Application.Tests/Directories/Commands
git commit -m "$(cat <<'EOF'
feat: notify admins when a directory sync leaves users without a calendar

Reuses the existing generic Notification mechanism instead of building
a new one. Only triggers from directory sync (the bulk 200-300-user
case) — a single internal user creation doesn't need a notification
since the admin creating it can just assign the calendar right there.
EOF
)"
```

---

### Task 6: Frontend — types, API client, and Kullanıcılar UI

**Files:**
- Modify: `frontend/src/api/types.ts`
- Modify: `frontend/src/api/users.ts`
- Modify: `frontend/src/api/workCalendars.ts`
- Modify: `frontend/src/hooks/useWorkCalendar.ts`
- Modify: `frontend/src/components/admin/directory/UsersSection.tsx`

**Interfaces:**
- Consumes: `POST /api/v1/users/{id}/work-calendar`, `POST /api/v1/users/work-calendar/bulk`,
  `GET /api/v1/workcalendars` (existing, list not yet wired on frontend), `onlyMissingWorkCalendar`
  query param on `GET /api/v1/users` (Task 3/4).

- [ ] **Step 1: Extend types.ts**

In `frontend/src/api/types.ts`, add two fields to `UserDto` (after `lastSyncedUtc`):

```typescript
  workCalendarId: string | null;
  workCalendarName: string | null;
```

`UserDetailDto extends UserDto` already inherits them — no separate edit needed there.

Add a new interface right after `PagedResult` (or anywhere top-level, e.g. near other simple
list DTOs):

```typescript
export interface WorkCalendarDto {
  id: string;
  name: string;
}
```

- [ ] **Step 2: Add getWorkCalendars to the API client**

In `frontend/src/api/workCalendars.ts`, add:

```typescript
import type { PagedResult, WorkCalendarDetailDto, WorkCalendarDto } from './types';

export function getWorkCalendars(pageSize = 100) {
  return apiClient.get<PagedResult<WorkCalendarDto>>('/api/v1/workcalendars', { pageSize });
}
```

(Keep the existing `getWorkCalendarById` export; just add the `PagedResult`/`WorkCalendarDto`
import and this new function alongside it.)

- [ ] **Step 3: Add useWorkCalendars hook**

In `frontend/src/hooks/useWorkCalendar.ts`, add:

```typescript
import { getWorkCalendarById, getWorkCalendars } from '../api/workCalendars';

export function useWorkCalendars() {
  return useQuery({
    queryKey: ['workCalendars', 'list'],
    queryFn: () => getWorkCalendars(),
  });
}
```

(Merge the import with the existing `getWorkCalendarById` import line rather than duplicating
the `import ... from '../api/workCalendars'` line.)

- [ ] **Step 4: Add assignment functions to api/users.ts**

Append to `frontend/src/api/users.ts`:

```typescript
export function assignWorkCalendar(userId: string, workCalendarId: string) {
  return apiClient.post<void>(`/api/v1/users/${userId}/work-calendar`, {
    userId,
    workCalendarId,
  });
}

export function bulkAssignWorkCalendar(userIds: string[], workCalendarId: string) {
  return apiClient.post<void>('/api/v1/users/work-calendar/bulk', {
    userIds,
    workCalendarId,
  });
}
```

- [ ] **Step 5: Add mutation hooks**

In `frontend/src/hooks/useUsers.ts`, add `useMutation`/`useQueryClient` imports if missing
(the file already imports `useMutation` and `useQuery` from `@tanstack/react-query` — add
`useQueryClient` to that same import), then append:

```typescript
export function useAssignWorkCalendarMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ userId, workCalendarId }: { userId: string; workCalendarId: string }) =>
      assignWorkCalendar(userId, workCalendarId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['users'] });
    },
  });
}

export function useBulkAssignWorkCalendarMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ userIds, workCalendarId }: { userIds: string[]; workCalendarId: string }) =>
      bulkAssignWorkCalendar(userIds, workCalendarId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['users'] });
    },
  });
}
```

Add `assignWorkCalendar, bulkAssignWorkCalendar` to the existing `from '../api/users'` import
line at the top of the file.

- [ ] **Step 6: Update UsersSection.tsx**

Add these imports at the top of `frontend/src/components/admin/directory/UsersSection.tsx`:

```typescript
import { useWorkCalendars } from '../../../hooks/useWorkCalendar';
import { useAssignWorkCalendarMutation, useBulkAssignWorkCalendarMutation } from '../../../hooks/useUsers';
```

Add state right after the existing `pageSize` state:

```typescript
  const [onlyMissingWorkCalendar, setOnlyMissingWorkCalendar] = useState(false);
  const [selectedUserIds, setSelectedUserIds] = useState<string[]>([]);
  const [bulkWorkCalendarId, setBulkWorkCalendarId] = useState('');
  const workCalendars = useWorkCalendars();
  const assignWorkCalendarMutation = useAssignWorkCalendarMutation();
  const bulkAssignMutation = useBulkAssignWorkCalendarMutation();
```

Pass the new filter into the `useUsers` call:

```typescript
  const users = useUsers({
    directoryId: selectedDirectoryId || undefined,
    searchTerm,
    onlyMissingWorkCalendar: onlyMissingWorkCalendar || undefined,
    pageNumber,
    pageSize,
  });
```

(This requires `onlyMissingWorkCalendar` to be accepted by `getUsers`/`useUsers` — add
`onlyMissingWorkCalendar?: boolean` to both the `getUsers` options object type in
`frontend/src/api/users.ts` and the `useUsers` options type in `frontend/src/hooks/useUsers.ts`,
threading it through to the `apiClient.get` query object the same way the existing
`onlyActive` field is already threaded through in both files.)

Add a checkbox filter next to the existing "Dizin" filter (inside the same flex row div):

```tsx
        <label className="flex items-center gap-2 text-sm text-slate-500">
          <input
            type="checkbox"
            checked={onlyMissingWorkCalendar}
            onChange={(e) => {
              setOnlyMissingWorkCalendar(e.target.checked);
              setPageNumber(1);
            }}
          />
          Takvimsiz
        </label>
```

Add a "Mesai Takvimi" column to the table header (after "E-posta", before "Durum"):

```tsx
              <th className="py-2 pr-4 font-medium">Mesai Takvimi</th>
```

The corresponding table cell is added in the `<tbody>` row further below (see the
"per-row single-assign" cell later in this step) — don't add a separate simpler cell here,
that later block is the only work-calendar `<td>` this row gets.

Add a checkbox column header (as the first `<th>`):

```tsx
              <th className="py-2 pr-2 font-medium">
                <input
                  type="checkbox"
                  checked={items.length > 0 && selectedUserIds.length === items.length}
                  onChange={(e) =>
                    setSelectedUserIds(e.target.checked ? items.map((u) => u.id) : [])
                  }
                />
              </th>
```

In the `<tbody>` row, add a checkbox cell as the first `<td>` (stop click propagation so it
doesn't open the detail view) and a work-calendar cell after the e-mail cell:

```tsx
                <td className="py-2 pr-2" onClick={(e) => e.stopPropagation()}>
                  <input
                    type="checkbox"
                    checked={selectedUserIds.includes(user.id)}
                    onChange={(e) =>
                      setSelectedUserIds((prev) =>
                        e.target.checked ? [...prev, user.id] : prev.filter((id) => id !== user.id),
                      )
                    }
                  />
                </td>
```

Add a bulk-assign toolbar, shown right above the table when at least one row is selected
(place it right before the `{users.isLoading ? ... : ...}` block):

```tsx
      {selectedUserIds.length > 0 && (
        <div className="mb-3 flex items-center gap-2 rounded-md bg-indigo-50 px-3 py-2 text-sm">
          <span className="text-indigo-700">{selectedUserIds.length} kullanıcı seçildi</span>
          <select
            value={bulkWorkCalendarId}
            onChange={(e) => setBulkWorkCalendarId(e.target.value)}
            className="rounded-md border border-slate-300 px-2 py-1 text-sm"
          >
            <option value="">Takvim seç…</option>
            {(workCalendars.data?.items ?? []).map((wc) => (
              <option key={wc.id} value={wc.id}>
                {wc.name}
              </option>
            ))}
          </select>
          <button
            type="button"
            disabled={!bulkWorkCalendarId || bulkAssignMutation.isPending}
            onClick={() =>
              bulkAssignMutation.mutate(
                { userIds: selectedUserIds, workCalendarId: bulkWorkCalendarId },
                { onSuccess: () => setSelectedUserIds([]) },
              )
            }
            className="rounded-md bg-indigo-600 px-3 py-1 text-sm text-white disabled:cursor-not-allowed disabled:opacity-40"
          >
            Seçilenlere Takvim Ata
          </button>
        </div>
      )}
```

Add the work-calendar `<td>` in the `<tbody>` row, right after the e-mail cell — shows the
calendar name when assigned, or an inline select for immediate single-user assignment when not:

```tsx
                <td className="py-2 pr-4" onClick={(e) => e.stopPropagation()}>
                  {user.workCalendarName ? (
                    user.workCalendarName
                  ) : (
                    <select
                      defaultValue=""
                      onChange={(e) => {
                        if (e.target.value) {
                          assignWorkCalendarMutation.mutate({ userId: user.id, workCalendarId: e.target.value });
                        }
                      }}
                      className="rounded-md border border-amber-300 bg-amber-50 px-1.5 py-0.5 text-xs text-amber-700"
                    >
                      <option value="">Atanmamış</option>
                      {(workCalendars.data?.items ?? []).map((wc) => (
                        <option key={wc.id} value={wc.id}>
                          {wc.name}
                        </option>
                      ))}
                    </select>
                  )}
                </td>
```

- [ ] **Step 7: Build and lint**

Run: `cd frontend && npm run build`
Expected: exit code 0, no TypeScript errors.

Run: `cd frontend && npm run lint`
Expected: no new errors (pre-existing warnings in unrelated files are fine).

- [ ] **Step 8: Commit**

```bash
git add frontend
git commit -m "$(cat <<'EOF'
feat: add work calendar assignment UI to the Kullanıcılar admin screen

"Takvimsiz" filter, a Mesai Takvimi column with inline single-user
assignment for unassigned rows, and a checkbox-select + bulk-assign
toolbar for assigning many users to the same calendar at once.
EOF
)"
```

---

### Task 7: End-to-end verification against real PostgreSQL

**Files:** none (verification-only).

- [ ] **Step 1: Apply the migration**

```bash
cd backend
dotnet ef database update --project src/EforTakip.Persistence --startup-project src/EforTakip.Api -- --environment RealDb
```
Expected: ends with `Done.`, no errors.

- [ ] **Step 2: Verify the admin user's WorkCalendarId is null (no data assumption made)**

Ask the user to run in their own `psql` session (same pattern as Phase 1's verification):

```powershell
& "C:\Program Files\PostgreSQL\18\bin\psql.exe" -U efortakip_dev -h localhost -d efortakip_dev -f <path to a temp .sql file containing: SELECT username, "WorkCalendarId" FROM "Users";>
```

Expected: `admin` row shows `WorkCalendarId` as empty/NULL.

- [ ] **Step 3: Start backend and frontend, verify in browser**

```bash
dotnet run --project src/EforTakip.Api --launch-profile RealDb
```
```bash
cd frontend && npm run dev
```

Log in, go to Kullanıcı Yönetimi → Kullanıcılar, confirm:
- The "Takvimsiz" checkbox filters correctly.
- The `admin` row shows an "Atanmamış" badge with an inline select.
- Assigning a calendar to `admin` via the inline select updates the row without a page reload.
- Selecting 2+ rows shows the bulk toolbar; bulk-assigning works and clears the selection.

Take a screenshot as evidence, then stop both processes.

- [ ] **Step 4: Report results to the user**

Summarize what was verified (migration applied cleanly, UI flows work, no pre-existing data
touched) and hand off to `finishing-a-development-branch`.
