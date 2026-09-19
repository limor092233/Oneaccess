# OneAccess

Centralized authentication and authorization system. A user logs in once and can then access every registered sub-system (System 1, System 2, System 3, ...) without having to log in again.

---

## 1. Goals

| Item | Detail |
|---|---|
| Single sign-on | One credential, many sub-systems |
| Central user store | Users, roles, and permissions live in one place |
| Central permission management | The System Administrator controls all access |
| Auditability | Every auth event is logged through Serilog |

---

## 2. Architecture Overview

Onion Architecture. Dependencies point inward — no inner layer knows anything about an outer layer.

```
        ┌──────────────────────────────────────────┐
        │        OneAccess.API (BFF Host)          │  ← Serves OneAccess.Client (Blazor WASM) as static files + hosts the BFF (frontend.md Section 2)
        │   Middleware • Endpoints • Composition   │
        │  ┌────────────────────────────────────┐  │
        │  │     OneAccess.Infrastructure       │  │  ← ALL implementations
        │  │  EF Core • Repos • UoW • JWT • Log │  │
        │  │  ┌──────────────────────────────┐  │  │
        │  │  │    OneAccess.Application     │  │  │  ← INTERFACES + CQRS only
        │  │  │  Interfaces • Commands •     │  │  │
        │  │  │  Queries • DTOs • Validators │  │  │
        │  │  │  ┌────────────────────────┐  │  │  │
        │  │  │  │   OneAccess.Domain     │  │  │  │  ← No dependencies
        │  │  │  │  Entities • Enums •    │  │  │  │
        │  │  │  │  Domain Exceptions     │  │  │  │
        │  │  │  └────────────────────────┘  │  │  │
        │  │  └──────────────────────────────┘  │  │
        │  └────────────────────────────────────┘  │
        └──────────────────────────────────────────┘
```

### Dependency Rules

| Layer | May reference |
|---|---|
| Domain | nothing |
| Application | Domain only |
| Infrastructure | Application + Domain |
| API (BFF) | Application (for contracts) + Infrastructure (for DI registration only) |

**Not allowed:**
- No `DbContext`, EF Core, or SQL in the Application layer.
- No concrete service classes in Application — interfaces only.
- The Domain calls nothing.
- The API never calls a repository directly — it goes through the Application layer via MediatR.

---

## 3. Project Layout

```
OneAccess/
├── OneAccess.sln
├── OneAccess.md
├── agent.md
├── frontend.md                          ← Blazor WebAssembly client's own source of truth (Section 3 there for its internal layout)
├── ROADMAP.md                           ← build order + progress tracker; read before either spec doc (agent.md Section 1, Rule 0)
│
├── src/
│   ├── OneAccess.Client/                ← Blazor WebAssembly SPA; see frontend.md — hosted by OneAccess.API, not a separate deployable (Section 2, frontend.md)
│   │
│   ├── OneAccess.Domain/
│   │   ├── Common/
│   │   │   ├── BaseEntity.cs
│   │   │   └── IAuditableEntity.cs
│   │   ├── Entities/
│   │   │   ├── User.cs
│   │   │   ├── Role.cs
│   │   │   ├── Permission.cs
│   │   │   ├── UserRole.cs
│   │   │   ├── RolePermission.cs
│   │   │   ├── SubSystem.cs
│   │   │   ├── UserSubSystemAccess.cs
│   │   │   ├── RoleSubSystemAccess.cs
│   │   │   ├── RefreshToken.cs
│   │   │   ├── SetupCode.cs
│   │   │   ├── AuditLog.cs
│   │   │   ├── Division.cs
│   │   │   ├── Section.cs
│   │   │   └── UserDivisionAssignment.cs
│   │   ├── Enums/
│   │   │   ├── UserStatus.cs
│   │   │   └── SystemRole.cs
│   │   └── Exceptions/
│   │       ├── DomainException.cs
│   │       └── NotFoundException.cs
│   │
│   ├── OneAccess.Application/
│   │   ├── Common/
│   │   │   ├── Interfaces/
│   │   │   │   ├── IRepository.cs
│   │   │   │   ├── IUnitOfWork.cs
│   │   │   │   ├── IReadDbContext.cs
│   │   │   │   ├── IJwtTokenService.cs
│   │   │   │   ├── IPasswordHasher.cs
│   │   │   │   ├── ICurrentUserService.cs
│   │   │   │   ├── ISetupCodeService.cs
│   │   │   │   ├── ICacheService.cs
│   │   │   │   ├── ITokenRevocationService.cs
│   │   │   │   ├── ISubSystemAccessService.cs
│   │   │   │   ├── IDivisionScopedRequest.cs
│   │   │   │   └── IDateTimeProvider.cs
│   │   │   ├── Behaviors/
│   │   │   │   ├── ValidationBehavior.cs
│   │   │   │   ├── LoggingBehavior.cs
│   │   │   │   ├── DivisionScopeBehavior.cs
│   │   │   │   └── PerformanceBehavior.cs
│   │   │   ├── Models/
│   │   │   │   ├── Result.cs
│   │   │   │   └── PagedResult.cs
│   │   │   └── Mappings/
│   │   │       └── MapsterConfiguration.cs
│   │   ├── Features/
│   │   │   ├── Setup/
│   │   │   │   ├── Queries/GetSetupStatus/
│   │   │   │   └── Commands/InitializeSystemAdmin/
│   │   │   ├── Auth/
│   │   │   │   ├── Commands/Login/
│   │   │   │   ├── Commands/RefreshToken/
│   │   │   │   ├── Commands/Logout/
│   │   │   │   └── Queries/GetCurrentUser/
│   │   │   ├── Users/
│   │   │   │   ├── Commands/CreateUser/
│   │   │   │   ├── Commands/UpdateUser/
│   │   │   │   ├── Commands/DeleteUser/
│   │   │   │   ├── Commands/AssignRole/
│   │   │   │   ├── Queries/GetUserById/
│   │   │   │   └── Queries/GetUsers/
│   │   │   ├── Roles/
│   │   │   │   ├── Commands/CreateRole/
│   │   │   │   ├── Commands/UpdateRole/
│   │   │   │   ├── Commands/DeleteRole/
│   │   │   │   └── Queries/GetRoles/
│   │   │   ├── Permissions/
│   │   │   │   └── Queries/GetPermissions/
│   │   │   ├── Audit/
│   │   │   │   └── Queries/GetAuditLogs/
│   │   │   ├── RolePermissions/
│   │   │   │   ├── Commands/AssignPermission/
│   │   │   │   └── Commands/RevokePermission/
│   │   │   ├── SubSystems/
│   │   │   │   ├── Commands/RegisterSubSystem/
│   │   │   │   ├── Commands/AssignRoleSubSystemAccess/
│   │   │   │   ├── Commands/RevokeRoleSubSystemAccess/
│   │   │   │   ├── Commands/AssignUserSubSystemAccess/
│   │   │   │   ├── Commands/RevokeUserSubSystemAccess/
│   │   │   │   ├── Queries/GetSubSystems/
│   │   │   │   ├── Queries/GetMySubSystems/
│   │   │   │   ├── Queries/GetRoleSubSystems/
│   │   │   │   └── Queries/GetUserSubSystems/
│   │   │   ├── PinCode/                          ← TODO, see Section 17
│   │   │   │   ├── Commands/ResetPinCode/
│   │   │   │   └── Commands/VerifyPinCode/
│   │   │   ├── Divisions/
│   │   │   │   ├── Commands/CreateDivision/
│   │   │   │   ├── Commands/UpdateDivision/
│   │   │   │   ├── Commands/DeleteDivision/
│   │   │   │   ├── Commands/AssignUserDivision/
│   │   │   │   ├── Commands/RevokeUserDivision/
│   │   │   │   ├── Queries/GetDivisions/
│   │   │   │   └── Queries/GetDivisionAdministrators/
│   │   │   └── Sections/
│   │   │       ├── Commands/CreateSection/
│   │   │       ├── Commands/UpdateSection/
│   │   │       ├── Commands/DeleteSection/
│   │   │       └── Queries/GetSectionsByDivision/
│   │   └── DependencyInjection.cs
│   │
│   ├── OneAccess.Infrastructure/
│   │   ├── Persistence/
│   │   │   ├── OneAccessDbContext.cs
│   │   │   ├── Configurations/
│   │   │   │   ├── UserConfiguration.cs
│   │   │   │   ├── RoleConfiguration.cs
│   │   │   │   ├── PermissionConfiguration.cs
│   │   │   │   ├── RolePermissionConfiguration.cs
│   │   │   │   ├── SubSystemConfiguration.cs
│   │   │   │   ├── RoleSubSystemAccessConfiguration.cs
│   │   │   │   ├── DivisionConfiguration.cs
│   │   │   │   ├── SectionConfiguration.cs
│   │   │   │   └── UserDivisionAssignmentConfiguration.cs
│   │   │   ├── Repositories/
│   │   │   │   ├── Repository.cs
│   │   │   │   └── UnitOfWork.cs
│   │   │   ├── Interceptors/
│   │   │   │   └── AuditableEntityInterceptor.cs
│   │   │   ├── Seed/
│   │   │   │   └── PermissionSeeder.cs
│   │   │   └── Migrations/
│   │   ├── Identity/
│   │   │   ├── JwtTokenService.cs
│   │   │   ├── RsaKeyProvider.cs
│   │   │   ├── PasswordHasher.cs
│   │   │   ├── CurrentUserService.cs
│   │   │   └── SubSystemClientAuthHandler.cs   ← TODO, see Section 17
│   │   ├── Services/
│   │   │   ├── SetupCodeService.cs
│   │   │   ├── RedisCacheService.cs
│   │   │   ├── RedisTokenRevocationService.cs
│   │   │   ├── SubSystemAccessService.cs
│   │   │   └── DateTimeProvider.cs
│   │   ├── BackgroundJobs/
│   │   │   ├── RefreshTokenCleanupJob.cs
│   │   │   └── PermissionCacheWarmupJob.cs
│   │   ├── Logging/
│   │   │   └── SerilogConfigurator.cs
│   │   ├── Options/
│   │   │   ├── JwtOptions.cs
│   │   │   ├── RedisOptions.cs
│   │   │   ├── BackgroundJobOptions.cs
│   │   │   ├── SetupOptions.cs
│   │   │   ├── CorsOptions.cs
│   │   │   ├── SubSystemOptions.cs
│   │   │   ├── PasswordPolicyOptions.cs
│   │   │   └── PinCodeOptions.cs               ← TODO, see Section 17
│   │   └── DependencyInjection.cs
│   │
│   └── OneAccess.API/                  ← BFF
│       ├── Endpoints/
│       │   ├── SetupEndpoints.cs
│       │   ├── AuthEndpoints.cs
│       │   ├── UserEndpoints.cs
│       │   ├── RoleEndpoints.cs
│       │   ├── SubSystemEndpoints.cs
│       │   ├── JwksEndpoints.cs
│       │   ├── DivisionEndpoints.cs
│       │   ├── SectionEndpoints.cs
│       │   ├── PermissionEndpoints.cs
│       │   └── AuditEndpoints.cs
│       ├── HealthChecks/
│       │   └── HealthCheckExtensions.cs
│       ├── Middleware/
│       │   ├── ExceptionHandlingMiddleware.cs
│       │   ├── CorrelationIdMiddleware.cs
│       │   └── SetupGuardMiddleware.cs
│       ├── Extensions/
│       │   └── WebApplicationExtensions.cs
│       ├── appsettings.json
│       ├── appsettings.Development.json
│       └── Program.cs
│
└── tests/
    ├── OneAccess.Domain.UnitTests/
    ├── OneAccess.Application.UnitTests/
    └── OneAccess.API.IntegrationTests/
```

---

## 4. Domain Model

```
User ──< UserRole >── Role ──< RolePermission >── Permission
 │                                │
 ├──< UserSubSystemAccess >── SubSystem ──< RoleSubSystemAccess ──┘
 │        (per-user override)              (per-role default restriction)
 │
 ├──< UserDivisionAssignment >── Division          (Administrator's managed Division(s))
 │
 └── DivisionId, SectionId (nullable FK)           (ordinary User's home placement)

Division ──< Section

User ──< RefreshToken
SetupCode (standalone, one-time use)
AuditLog (standalone, UserId nullable FK)
```

### Entities

**User** — Id, Username, Email, PasswordHash, FullName, Status, IsSystemAdministrator, DivisionId (nullable FK — home placement), SectionId (nullable FK — home placement), CreatedAt, UpdatedAt
*TODO (see Section 17):* PinCodeHash, PinCodeSetAt, PinCodeSetBy, IsPinCodeAutoGenerated

**Role** — Id, Name, Description, IsSystemRole, CreatedAt
Seeded roles: `System Administrator`, `Administrator`, `User`

**Permission** — Id, Code (e.g. `user.create`), Module, Description

**RolePermission** — RoleId, PermissionId

**UserRole** — UserId, RoleId

**SubSystem** — Id, Code (`SYS1`), Name, BaseUrl, Audience, IsActive
*TODO (see Section 17):* ClientSecretHash — for server-to-server auth into OneAccess (e.g. PIN verification), distinct from `Audience` which only governs outbound token validation

**UserSubSystemAccess** — UserId, SubSystemId, GrantedAt — a **per-user override** of default sub-system visibility. When rows exist for a User, they define exactly and only what that User can see, regardless of their Role's own setting (see Section 5, "Sub-System Visibility").

**RoleSubSystemAccess** — RoleId, SubSystemId, GrantedAt — a **per-role restriction** of default sub-system visibility, meaningful only for `Administrator` and `User` (never `System Administrator` — rejected at the command level, not just exempted at read time). No rows for a Role means every member of that Role sees every active SubSystem, including ones registered later.

**Division** — Id, Name, Description, CreatedAt

**Section** — Id, DivisionId, Name, Description, CreatedAt

**UserDivisionAssignment** — UserId, DivisionId, GrantedAt — an Administrator's managed Division(s); distinct from `User.DivisionId`, which is an ordinary user's own home placement, not a grant of management rights

**RefreshToken** — Id, UserId, TokenHash, ExpiresAt, RevokedAt, ReplacedByTokenId

**SetupCode** — Id, CodeHash, ExpiresAt, ConsumedAt

**AuditLog** — Id, UserId (nullable), Action, EntityType, EntityId, Details (JSON), IpAddress, CreatedAt — stored in its own table, isolated from the transactional tables it describes

---

## 5. Roles and Permissions

| Role | What it can do |
|---|---|
| **System Administrator** | Add/Edit/Delete User, Role, Role Permission, SubSystem, Division, Section. Full control. The first one is created through First Run Setup. Never Division-scoped — see below. |
| **Administrator** | Manage users within assigned Division(s) only, per `UserDivisionAssignment` — enforced by `DivisionScopeBehavior` (below). Cannot modify Role Permissions, and cannot create, delete, or reassign Divisions themselves (see "Division-Scoped Authorization"). OneAccess is a login portal (SSO) first — sub-system access itself is governed by `UserSubSystemAccess` grants on the end user, not by a separate Administrator-side sub-system scope. |
| **User** | Access only the sub-systems visible to them (see "Sub-System Visibility" below). Has a home `DivisionId`/`SectionId` but no management rights over either. |

### Sub-System Visibility

All three roles default to seeing **every active SubSystem** — this is a login portal for internal systems first, so the starting posture is open, not locked down. A System Administrator can narrow that default at two levels, resolved most-specific-first:

```
Effective sub-system access for User U (with Role R):
   1. UserSubSystemAccess rows exist for U?
        → YES: exactly those SubSystems. Nothing else. (per-user override)
   2. else, RoleSubSystemAccess rows exist for R?
        → YES: exactly those SubSystems, for every member of R. (per-role restriction)
   3. else:
        → every active SubSystem (system-wide default)

System Administrator: always every active SubSystem. Steps 1–3 never apply —
RoleSubSystemAccess cannot target the System Administrator role at all
(rejected by the command handler, not merely skipped at read time).
```

This is an **allow-list**, not a deny-list: once a restriction exists at either level, it defines the complete set of what's visible — there's no separate "also show these" on top of it.

**Where this is enforced — not just displayed.** `GetMySubSystems` (`GET /api/subsystems/mine`) uses this resolution to render the portal's list of accessible systems, but that's the read side. The write side matters more: before the BFF forwards or mints a JWT scoped to a specific sub-system audience, it calls `ISubSystemAccessService.HasAccessAsync(userId, subSystemId)` and rejects with `403` if the target isn't in that user's effective set — see Section 10. Hiding an icon in the portal is a UX nicety; the token-issuance check is what actually keeps a restricted user out.

**Caching.** Effective access is resolved through `ISubSystemAccessService`, backed by `ICacheService`/Redis with the same short TTL as the permission cache (Section 10) — a `RoleSubSystemAccess` or `UserSubSystemAccess` change invalidates the affected cache entries as part of the same handler (Section 7, "Cache Invalidation").

**Not delegable.** `rolesubsystem.assign`/`rolesubsystem.revoke` and `usersubsystem.assign`/`usersubsystem.revoke` are seeded to `System Administrator` only and, like `userdivision.assign`/`revoke`, are **never** assignable to `Administrator` through `RolePermission` — an Administrator who could grant sub-system access could simply grant themselves everything the System Administrator tried to restrict, defeating the whole mechanism.

### Role Assignment Guard

`AssignRoleCommand` (`POST /api/users/{id}/roles`) is gated by the ordinary `user.update` permission — a grant expected to be common among Administrators. Without an extra check, any Administrator holding `user.update` could assign the `System Administrator` role (or any other `IsSystemRole = true` role) to themselves or anyone else. The handler therefore blocks assigning a role with `IsSystemRole = true` unless the caller is already a System Administrator, regardless of what permissions the caller otherwise holds.

### Division-Scoped Authorization

Permission codes (`RequirePermission("user.update")`) answer "can this role do this *kind* of action at all," not "on *which* rows." `UserDivisionAssignment` narrows an Administrator to specific Divisions, so a second, resource-level check runs after the permission check:

**Mechanism: `DivisionScopeBehavior`**, a MediatR pipeline behavior (`Application/Common/Behaviors/`), inserted between `ValidationBehavior` and `PerformanceBehavior` — the same place cross-cutting authorization concerns already live in this pipeline (Section 7).

```
Request → LoggingBehavior → ValidationBehavior → DivisionScopeBehavior → PerformanceBehavior → Handler
```

- A command/query that targets a Division-associated resource implements a marker interface, `IDivisionScopedRequest`, exposing `Task<Guid?> GetTargetDivisionIdAsync(IReadDbContext db, CancellationToken ct)`. Requests that don't implement it (Auth, Setup, SubSystems, RolePermissions, etc.) pass through the behavior untouched.
- `DivisionScopeBehavior` logic:
  1. Request doesn't implement `IDivisionScopedRequest` → pass through.
  2. `ICurrentUserService.IsSystemAdministrator` → pass through unconditionally (System Administrators are never Division-scoped).
  3. `GetTargetDivisionIdAsync` returns `null` (resource has no Division association) → pass through.
  4. Otherwise, compare the resolved Division against `ICurrentUserService.GetAssignedDivisionIdsAsync()`. Not a member → `Result.Forbidden()`, short-circuited before the handler runs.
  5. **Fail-closed**, same philosophy as Section 10: if the assigned-divisions lookup itself cannot be resolved, reject (`503`) rather than assume unrestricted access.
- `ICurrentUserService.GetAssignedDivisionIdsAsync()` is cached the same way the permission set is (Section 10 point 2) — same `ICacheService`/Redis mechanism, same short TTL — and invalidated on the same trigger list as permissions (Section 7, "Cache Invalidation"), extended to include `AssignUserDivision` / `RevokeUserDivision`.
- `GetTargetDivisionIdAsync` implementations, by feature:
  - `UpdateUserCommand`, `DeleteUserCommand`, `AssignRoleCommand`, `GetUserById` → look up the target `User.DivisionId` via `IReadDbContext`.
  - `CreateUserCommand` → the target Division is the `DivisionId` supplied in the request body itself (no lookup needed) — an Administrator cannot create a user in a Division they don't manage.
  - `CreateSectionCommand`, `UpdateSectionCommand`, `DeleteSectionCommand`, `GetSectionsByDivision` → the target Section's (or supplied) `DivisionId`.
  - `GetUsers` (list query) does **not** implement `IDivisionScopedRequest` — a single target Division doesn't apply to a list. Instead, its handler filters results to `GetAssignedDivisionIdsAsync()` directly for non-SysAdmins, documented here rather than in the behavior.
- `CreateDivisionCommand`, `UpdateDivisionCommand`, `DeleteDivisionCommand`, `AssignUserDivisionCommand`, and `RevokeUserDivisionCommand` are **not** `IDivisionScopedRequest` — they are the mechanism that defines and alters scope itself, so they stay strictly System-Administrator-only. `division.create`, `division.update`, `division.delete`, `userdivision.assign`, and `userdivision.revoke` are seeded to `System Administrator` only and, unlike `user.pincode.reset`, are **not** delegable to `Administrator` through `RolePermission` — letting an Administrator create, modify, delete, or assign Divisions (including to themselves) would defeat the entire scoping and isolation model.

**Data integrity: Division/Section consistency.** `User.DivisionId` and `User.SectionId` are independent nullable FKs (a user may belong to a Division with no specific Section yet). `DivisionScopeBehavior` only inspects `DivisionId` when checking `CreateUserCommand`/`UpdateUserCommand` — it does not verify that a supplied `SectionId` actually belongs to the supplied `DivisionId`. Without a separate check, an Administrator scoped to Division A could pass a `DivisionId` they manage alongside a `SectionId` that actually belongs to an unmanaged Division B, producing an inconsistent row and, functionally, a scope bypass. `CreateUserCommandValidator` / `UpdateUserCommandValidator` (FluentValidation) therefore reject the request whenever both fields are supplied and `Section.DivisionId != DivisionId` — this runs in `ValidationBehavior`, before `DivisionScopeBehavior` ever sees the request.

**Reassignment across Divisions.** `DivisionScopeBehavior`'s check on `UpdateUserCommand` resolves the target Division from the user's *current* `DivisionId` — correct for deciding whether the caller may touch that row at all, but not sufficient when the update itself changes `DivisionId` to something else. Without an extra check, an Administrator scoped to Division A could move a user's `DivisionId` to Division B, a Division they don't manage, with no involvement from B's Administrator. `UpdateUserCommandHandler` therefore additionally requires — whenever `DivisionId` is part of the change — that the **new** `DivisionId` is also in the caller's `GetAssignedDivisionIdsAsync()` (System Administrators exempt, as always). Moving a user between Divisions the caller doesn't manage requires a System Administrator.

### Permission Codes (seeded)

```
user.view      user.create      user.update      user.delete
role.view      role.create      role.update      role.delete
permission.view
rolepermission.assign           rolepermission.revoke
subsystem.view subsystem.register subsystem.update
rolesubsystem.assign             rolesubsystem.revoke
usersubsystem.assign             usersubsystem.revoke
division.view  division.create  division.update  division.delete
section.view   section.create   section.update   section.delete
userdivision.assign              userdivision.revoke
audit.view
```

**Non-delegable permissions:** `division.create`, `division.update`, `division.delete`, `userdivision.assign`, `userdivision.revoke`, `rolesubsystem.assign`, `rolesubsystem.revoke`, `usersubsystem.assign`, `usersubsystem.revoke`, `subsystem.register`, `subsystem.update`, `role.create`, `role.update`, `role.delete`, `rolepermission.assign`, `rolepermission.revoke`, and `audit.view` are seeded to `System Administrator` only and are **never delegable** to any other role through `rolepermission.assign`. `AssignRolePermissionCommandHandler` explicitly rejects any attempt to assign these codes. `AuditLog` has no `DivisionId`, so there is no way to scope it to an Administrator's assigned Division(s). Granting it to an Administrator would expose every Division's audit trail, not just their own. `GetAuditLogsQuery` does **not** implement `IDivisionScopedRequest` and is not expected to — this is deliberate, not an oversight.

*TODO (see Section 17):* `user.pincode.reset` — seeded to `System Administrator` only; a System Administrator may assign it to `Administrator` through the existing `rolepermission.assign` endpoint. Not assignable to the `User` role.

Authorization is **permission-based**, not role-based, at the endpoint level. A role is simply a container of permissions. Endpoints use policies such as `RequirePermission("user.create")`.

---

## 6. First Run Setup

### Rule
- When **no** User has `IsSystemAdministrator = true` → setup is **OPEN**, and a one-time code is emitted to the **CLI / console** through Serilog.
- Once a System Administrator **exists** → setup is **CLOSED**. No code is emitted. All setup endpoints return `403 Forbidden`.

### Flow

```
App start
   │
   ├─ Does a System Administrator exist?
   │
   ├─ NO ──► Generate a one-time code (random, hashed before storing)
   │           │
   │           ├─ Emit to console/CLI:
   │           │     [SETUP] Setup code: 7F3K-92QD
   │           │     [SETUP] Expires in 15 minutes
   │           │
   │           └─ GET  /api/setup/status  ──► { isSetupRequired: true }
   │              POST /api/setup/initialize
   │                   { code, username, email, password, fullName }
   │                   │
   │                   ├─ Validate the code (hash match + not expired + not consumed)
   │                   ├─ Create the User with IsSystemAdministrator = true
   │                   ├─ Assign the "System Administrator" role
   │                   ├─ Mark the code as consumed
   │                   └─ SaveChanges through UnitOfWork (single transaction)
   │
   └─ YES ─► No code is emitted. Nothing is logged.
             GET  /api/setup/status      ──► { isSetupRequired: false }
             POST /api/setup/initialize  ──► 403 Forbidden
```

### Security Rules

1. The code is **never** returned in an HTTP response — console/CLI only.
2. The stored code is hashed, never plain text.
3. It expires (default 15 minutes, value comes from `appsettings.json`).
4. It is one-time use — once consumed it is dead.
5. `/api/setup/initialize` is rate-limited (default 5 attempts).
6. `SetupGuardMiddleware` performs the check before the handler ever runs.

---

## 7. CQRS

MediatR is the mediator. Each feature lives in its own folder together with its Command/Query, Handler, Validator, and Response.

| Type | Purpose | Data access |
|---|---|---|
| **Command** | Write (Create/Update/Delete) | `IRepository<T>` + `IUnitOfWork.SaveChangesAsync()` |
| **Query** | Read only | `IReadDbContext` + Mapster `ProjectToType<TDto>()` — no tracking |

### Example Command

```
Features/Users/Commands/CreateUser/
├── CreateUserCommand.cs          (IRequest<Result<Guid>>)
├── CreateUserCommandHandler.cs
├── CreateUserCommandValidator.cs (FluentValidation)
└── CreateUserResponse.cs
```

### Pipeline Behaviors (in order)

```
Request → LoggingBehavior → ValidationBehavior → DivisionScopeBehavior → PerformanceBehavior → Handler
```

`DivisionScopeBehavior` is documented in full in Section 5 ("Division-Scoped Authorization") since it's an authorization concern, not a CQRS mechanic — it's listed here only to fix its position in the pipeline.

### Cache Invalidation

Any command that changes `UserRole`, `RolePermission`, `UserDivisionAssignment`, `RoleSubSystemAccess`, `UserSubSystemAccess`, or a user's status (`AssignRole`, `AssignPermission`, `RevokePermission`, `UpdateUser`, `AssignUserDivision`, `RevokeUserDivision`, `AssignRoleSubSystemAccess`, `RevokeRoleSubSystemAccess`, `AssignUserSubSystemAccess`, `RevokeUserSubSystemAccess`) must call `ICacheService` to invalidate that user's (or, for a Role-level change, every affected member's) cached permission set, assigned-divisions set, and/or effective sub-system access as part of the same handler — not as an afterthought. This keeps permission, Division-scope, and sub-system-visibility checks accurate without needing the token to expire first.

---

## 8. Repository and Unit of Work

### Interfaces (Application layer)

```
IRepository<T>
├── GetByIdAsync(id, ct)
├── FindAsync(predicate, ct)
├── ListAsync(predicate, ct)
├── AddAsync(entity, ct)
├── Update(entity)
├── Remove(entity)
└── AnyAsync(predicate, ct)

IUnitOfWork
├── Repository<T>()
├── SaveChangesAsync(ct)
├── BeginTransactionAsync(ct)
├── CommitAsync(ct)
└── RollbackAsync(ct)
```

### Implementation (Infrastructure layer)

`Repository<T>` wraps `DbSet<T>`. `UnitOfWork` owns the `DbContext` and is the only thing that calls `SaveChangesAsync`. **Repositories never call SaveChanges** — the handler decides when to commit.

---

## 9. Mapster

- A single `MapsterConfiguration` class registers every mapping through `TypeAdapterConfig`.
- Queries use `ProjectToType<TDto>()` so projection happens at SQL level (the full entity is never loaded).
- The config is scanned from the assembly at startup — no manual mapping code inside handlers.
- **Sensitive fields are explicitly ignored, never implicitly excluded.** `PasswordHash`, `RefreshToken.TokenHash`, and `SetupCode.CodeHash` each get an explicit `.Ignore(dest => dest.X)` in `MapsterConfiguration` for every DTO mapped from their owning entity. Because Mapster maps by convention (matching property names), a new DTO added later that happens to declare a `PasswordHash`-shaped property would otherwise map it silently — the explicit ignore is what a code review checks for whenever a new User-derived DTO is added.

---

## 10. BFF and JWT

```
┌─────────┐  httpOnly cookie   ┌───────────────┐   JWT Bearer   ┌──────────┐
│ Browser │ ◄────────────────► │ OneAccess BFF │ ◄────────────► │ System 1 │
│  (SPA)  │                    │               │                ├──────────┤
└─────────┘                    │  Token store  │ ◄────────────► │ System 2 │
                               └───────────────┘                ├──────────┤
                                                 ◄────────────► │ System 3 │
                                                                └──────────┘
```
"Browser (SPA)" is `OneAccess.Client` (Blazor WebAssembly) — see `frontend.md`.

- **Browser ↔ BFF:** httpOnly + Secure + SameSite cookie. JavaScript never holds a token — this is the XSS protection.
- **BFF ↔ Sub-systems:** JWT Bearer tokens issued by OneAccess. Each sub-system has its own `Audience`.
- **Access token:** short-lived (default 15 minutes).
- **Refresh token:** long-lived (default 7 days), stored hashed, rotating — a new token is issued on each use and the old one is revoked.
- **Signing:** RS256 (asymmetric), not a shared symmetric key. OneAccess signs with the private key; sub-systems validate with the public key exposed at `/.well-known/jwks.json`. This means a sub-system can keep validating already-issued tokens on its own even if OneAccess is briefly unreachable — see Section 15.
- **Access gate before minting:** before the BFF issues or forwards a token with `aud` set to a given sub-system, it checks `ISubSystemAccessService.HasAccessAsync(userId, subSystemId)` and returns `403` if that sub-system isn't in the user's effective access set (Section 5, "Sub-System Visibility"). A sub-system audience is never handed out just because the user is authenticated — visibility is checked per target, per request.

> **Known gap (TODO, see Section 17):** everything above covers OneAccess → sub-system token validation (outbound). There is currently no mechanism for the reverse — a sub-system authenticating an inbound, server-to-server call *into* OneAccess (needed for central PIN verification). `Audience` does not cover this; it is not a credential, only a token-validation claim.

### JWT Claims (kept intentionally small)

```
sub          user id
name         username
email        email
role         array of role names   ← NOT the full permission list
aud          target sub-system audience
iss          OneAccess
kid          signing key id (for JWKS lookup)
exp / iat / jti
```

Permissions are **not** embedded in the token. A large permission set would bloat the token past comfortable header sizes and make it stale the moment a permission changes. Instead:

1. The token carries `role` only.
2. The BFF (and, if a sub-system needs it, the sub-system itself) resolves the current permission set for that role/user through `ICacheService`, backed by Redis, with a short TTL.
3. Any `RolePermission` or `UserRole` change invalidates that user's cache entry immediately, so a permission change takes effect on the next request — no waiting for token expiry.

### Revocation

Stateless JWTs can't be recalled once issued, so revocation is handled through a Redis-backed check rather than by mutating the token itself:

```
On: role removed, permission revoked, password changed, admin disables user, logout-all
   │
   └─► ITokenRevocationService.RevokeAsync(userId, DateTimeOffset.UtcNow)
          │
          └─► Redis: SET revoked-after:{userId} = <utc timestamp>

On every authenticated request:
   │
   └─► ITokenRevocationService.IsRevokedAsync(userId, token.iat)
          │
          └─► if token.iat < revoked-after:{userId} → reject, force re-authentication
```

This is a single Redis key per user (not a per-token blacklist), so it stays cheap even at high token volume.

**Fail-closed on Redis unavailability.** If `ITokenRevocationService.IsRevokedAsync` cannot reach Redis, the request is **rejected** (`503`), not allowed through. The same applies to `ICacheService` permission lookups — an unreachable cache means the request is denied rather than silently treated as "no permissions revoked." Availability is sacrificed for correctness here: a brief outage that blocks requests is preferable to one that silently serves stale authorization decisions. This is why Section 15 requires `/health` to check Redis connectivity and pull the instance out of rotation before this failure mode is hit by real traffic.

### Refresh Token Reuse Detection

Rotation alone (new token per use, old one revoked) does not detect theft — it only limits its window. If a refresh token that is already `RevokedAt`-marked or has a non-null `ReplacedByTokenId` is presented again, that is a signal the token was copied (attacker and legitimate user both have a copy, and one of them already rotated past it):

```
POST /api/auth/refresh with token T
   │
   ├─ T not found, or T.ExpiresAt < now            → 401, no further action
   ├─ T.RevokedAt is null and unexpired             → normal rotation (issue new token, revoke T)
   └─ T.RevokedAt is set OR T.ReplacedByTokenId set  → REUSE DETECTED:
          │
          ├─ ITokenRevocationService.RevokeAsync(userId, now)   ← kills every live access token
          ├─ Revoke every RefreshToken row for that user
          ├─ AuditLog entry: "refresh_token_reuse_detected"
          └─ 401 — force full re-authentication
```

No new schema is needed — the existing `RevokedAt` / `ReplacedByTokenId` chain on `RefreshToken` is enough to detect this.

### CSRF Protection

`SameSite=Strict` on the session cookie is the primary defense and blocks the large majority of CSRF vectors (the cookie is never sent on cross-site requests, including top-level navigations). As defense-in-depth, since the BFF only accepts JSON bodies:

- Every state-changing endpoint (`POST`/`PUT`/`DELETE`) requires `Content-Type: application/json`. A plain HTML `<form>` cannot set this header, so a classic cross-site form-post CSRF attack cannot produce a request the API will accept.
- This is enforced centrally, not per-endpoint — add a check in `ExceptionHandlingMiddleware`'s pipeline neighbor or a small dedicated middleware, not in individual handlers.

### JWT Signing Key Rotation

Keys are identified by `kid` in the token header. `/.well-known/jwks.json` publishes **all currently-valid public keys**, not just the latest one, so rotation is: (1) add the new key to the JWKS response alongside the old one, (2) start signing new tokens with the new key, (3) once every token signed with the old key has expired (≤ `AccessTokenExpiryMinutes` after step 2), remove the old key from JWKS. Sub-systems always validate against whichever `kid` the token declares, so no sub-system needs a deploy to pick up a rotated key.

## 11. Middleware Pipeline

Order matters:

```
1.  CorrelationIdMiddleware        ← trace id for every request
2.  ExceptionHandlingMiddleware    ← ProblemDetails output
3.  Serilog Request Logging
4.  HTTPS Redirection
5.  CORS
6.  Rate Limiting
7.  Authentication
8.  Authorization
9.  SetupGuardMiddleware           ← blocks setup routes once a SysAdmin exists
10. Endpoints
```

---

## 12. Serilog and Audit Logging

- Sinks: Console (this is where the CLI setup code appears), rolling File, and optionally Seq.
- Enrichers: CorrelationId, UserId, MachineName, Environment.
- Every level, path, retention, and sink comes from `appsettings.json` — no `.WriteTo.Console()` in `Program.cs`.
- Passwords, tokens, and setup codes are masked in structured log properties, except for the deliberate first-run console output.
- **The setup code must never reach a sink other than the raw console.** If Console output is ever piped into a centralized aggregator (Seq, ELK, Datadog, or the ops team simply forwards container stdout), the "CLI-only" guarantee in Section 6 is defeated the moment that happens — the code becomes visible to everyone with log access instead of only whoever has a shell on the box. The setup-code log event is emitted with a distinct Serilog property (e.g. `IsSetupCode = true`) and every non-console sink filters it out (`Filter.ByExcluding(le => le.Properties.ContainsKey("IsSetupCode"))`), configured in `appsettings.json` alongside the sink definitions — so a sink added later inherits the exclusion by default instead of requiring someone to remember it.
- `Setup.ConsoleOutputEnabled` defaults to `true` in `appsettings.Development.json` and must default to `false` in `appsettings.json` (production); an environment that truly needs first-run setup in production sets it explicitly.

**Serilog is for operational logs. `AuditLog` is a separate, business-facing trail** — who did what, to which record, and when. It lives in its own table so its growth (one row per user/role/permission/sub-system change) never competes with the hot transactional tables. If audit volume grows heavily, this table is the first candidate to move to its own database or an external sink (e.g. Seq, Elasticsearch) — the write path already goes through `IRepository<AuditLog>`, so that move doesn't touch any handler.

---

## 13. Configuration

**All settings live in `appsettings.json`.** No hardcoded values in `Program.cs`. See `agent.md` for the full rule and the sample `appsettings.json`.

*TODO (see Section 17):* `PinCode` section (`Length`, `MaxFailedAttempts`, `LockoutMinutes`) is not yet in the sample `appsettings.json` — add it alongside `PasswordPolicy` when Section 17 is implemented.

---

## 14. Endpoints

| Method | Route | Permission | Purpose |
|---|---|---|---|
| GET | `/api/setup/status` | anonymous | Is setup still required? |
| POST | `/api/setup/initialize` | anonymous + code | Create the first System Administrator |
| POST | `/api/auth/login` | anonymous | Log in, issue a session |
| GET | `/api/auth/me` | authenticated | The current session's user summary — id, username, fullName, role, resolved permission codes, and effective sub-system list. This is the *only* way the client learns who is logged in and what they can do: the cookie is `httpOnly` (unreadable by client script by design, Section 10) and the JWT itself carries `role` only, not permissions (Section 10, "JWT Claims"). Returns `401` if there is no valid session. |
| POST | `/api/auth/refresh` | cookie | Refresh the token |
| POST | `/api/auth/logout` | authenticated | Revoke the session |
| GET | `/api/users` | `user.view` | List users |
| POST | `/api/users` | `user.create` | Add a user |
| PUT | `/api/users/{id}` | `user.update` | Update a user |
| DELETE | `/api/users/{id}` | `user.delete` | Deactivate/delete a user (soft delete — `Status`, never a hard row delete, so audit history stays intact) |
| POST | `/api/users/{id}/roles` | `user.update` | Assign a role |
| GET | `/api/roles` | `role.view` | List roles |
| POST | `/api/roles` | `role.create` | Add a role |
| DELETE | `/api/roles/{id}` | `role.delete` | Delete a role (blocked with `409 Conflict` if any user still holds it, or if `IsSystemRole = true`) |
| POST | `/api/roles/{id}/permissions` | `rolepermission.assign` | Assign a permission |
| DELETE | `/api/roles/{id}/permissions/{pid}` | `rolepermission.revoke` | Revoke a permission |
| GET | `/api/permissions` | `permission.view` | List all seeded permissions (used to populate the role-permission assignment UI) |
| GET | `/api/subsystems` | `subsystem.view` | List all registered sub-systems (admin management view — distinct from `/mine` below) |
| GET | `/api/subsystems/mine` | authenticated | The caller's effective, resolved sub-system list (Section 5, "Sub-System Visibility") |
| POST | `/api/subsystems` | `subsystem.register` | Register a sub-system |
| GET | `/api/roles/{id}/subsystems` | `subsystem.view` | List a Role's configured sub-system restriction, if any |
| POST | `/api/roles/{id}/subsystems` | `rolesubsystem.assign` | Add a SubSystem to a Role's allow-list (rejected for the `System Administrator` role) |
| DELETE | `/api/roles/{id}/subsystems/{subSystemId}` | `rolesubsystem.revoke` | Remove a SubSystem from a Role's allow-list |
| GET | `/api/users/{id}/subsystems` | `user.view` | List a User's configured sub-system override, if any |
| POST | `/api/users/{id}/subsystems` | `usersubsystem.assign` | Add a SubSystem to a User's per-user override allow-list |
| DELETE | `/api/users/{id}/subsystems/{subSystemId}` | `usersubsystem.revoke` | Remove a SubSystem from a User's override allow-list |
| GET | `/api/divisions` | `division.view` | List divisions |
| POST | `/api/divisions` | `division.create` | Add a division |
| PUT | `/api/divisions/{id}` | `division.update` | Update a division |
| DELETE | `/api/divisions/{id}` | `division.delete` | Delete a division (blocked with `409 Conflict` if any `Section`, `User`, or `UserDivisionAssignment` still references it) |
| GET | `/api/divisions/{id}/users` | `division.view` | List Administrators currently assigned to manage a division (`UserDivisionAssignment` — populates `frontend.md`'s `DivisionUsers.razor`) |
| POST | `/api/divisions/{id}/users` | `userdivision.assign` | Assign an Administrator to manage a division |
| DELETE | `/api/divisions/{id}/users/{userId}` | `userdivision.revoke` | Revoke an Administrator's management of a division |
| GET | `/api/divisions/{id}/sections` | `section.view` | List sections under a division |
| POST | `/api/sections` | `section.create` | Add a section |
| PUT | `/api/sections/{id}` | `section.update` | Update a section |
| DELETE | `/api/sections/{id}` | `section.delete` | Delete a section (blocked with `409 Conflict` if any `User` still references it via `SectionId`) |
| GET | `/.well-known/jwks.json` | anonymous | Public key(s) for sub-systems to validate JWTs locally |
| GET | `/health` | anonymous, but only reachable from the internal network (enforced at the reverse proxy/load-balancer level, not in application code — see Section 15) | Load balancer health check, exposes DB/Redis connectivity |
| GET | `/api/health` | anonymous | Liveness/readiness probe for the load balancer (no dependency details in the response body) |
| POST | `/api/users/{id}/pincode/reset` **(TODO)** | `user.pincode.reset` | Reset/regenerate a user's 6-digit PIN — see Section 17 |
| POST | `/api/auth/verify-pin` **(TODO)** | sub-system client credential | Verify `{userId, pinCode}` for a document receive/release action — see Section 17 |
| POST | `/api/subsystems/{id}/rotate-secret` **(TODO)** | `subsystem.update` | Regenerate a sub-system's client secret, returned once — see Section 17 |
| GET | `/api/audit` | `audit.view` | List/search `AuditLog` entries (filterable by `UserId`, `EntityType`, `Action`, date range — see Section 12). **System Administrator only** — `audit.view` is not delegable (Section 5). |

Every state-changing endpoint above (`POST`/`PUT`/`DELETE`) is also subject to the CSRF mitigation in Section 10 and, where noted in `RateLimiting` config, to per-route rate limits (Section 13's sample now also rate-limits `/api/auth/refresh`, not just login/setup).

---

## 15. Scalability and Resilience

OneAccess is a single point of failure for every sub-system that depends on it, so these mitigations are part of the baseline design, not later optimizations.

| Risk | Mitigation |
|---|---|
| Every sub-system depends on OneAccess being up | RS256 signing + JWKS endpoint so sub-systems validate tokens locally without calling back to OneAccess for every request |
| Single instance = downtime on deploy or crash | Minimum 2 stateless API instances behind a load balancer; no sticky sessions needed since auth state lives in the JWT + Redis, not in memory |
| In-memory rate limiting breaks with multiple instances | Rate limiting counters live in Redis, shared across all instances |
| Token bloat from embedding permissions | Token carries `role` only; permissions resolved and cached per user in Redis with a short TTL (Section 10) |
| Can't revoke a stateless JWT | `revoked-after:{userId}` marker in Redis, checked on every authenticated request (Section 10) |
| Refresh token table grows unbounded | `RefreshTokenCleanupJob` (a `BackgroundService`) runs on an interval from `appsettings.json` and deletes expired/revoked rows past a configurable retention window |
| Audit log competes with transactional queries | Isolated `AuditLog` table/sink (Section 12) |
| Redis unreachable → revocation/permission checks can't run | Fail-closed: request rejected (`503`), never silently allowed through (Section 10). `/health` checks Redis connectivity so the load balancer pulls the instance out of rotation before this is hit by live traffic |
| Stolen refresh token used after the legitimate user already rotated it | Reuse detection on the existing `RevokedAt`/`ReplacedByTokenId` chain revokes the whole session, not just the one token (Section 10) |
| Setup code intended for console-only leaking into a centralized log sink | Distinct `IsSetupCode` log property, excluded from every non-console sink by default (Section 12) |

### Health Check Endpoint

Two endpoints, deliberately different depth:

- `GET /health` (default path, from `HealthChecks.DetailedPath`) — checks the database and Redis connections and returns the detail. Reachable only from the internal network (enforced at the reverse proxy/load balancer, not in application code) — a dependency-level report is useful to ops, not something to expose publicly.
- `GET /api/health` (default path, from `HealthChecks.LivenessPath`) — a shallow liveness/readiness probe with no dependency detail in the body, safe to expose to the public-facing load balancer.

Both are added via `HealthCheckExtensions` in the API project, mapped to the paths configured under `HealthChecks` in `appsettings.json` (`HealthChecks.DetailedPath`, `HealthChecks.LivenessPath`, `HealthChecks.Enabled` — see `agent.md` Section 2 for the sample) — never hardcoded as literal route strings. The `/health` and `/api/health` values used throughout this document are the shipped defaults, not fixed values.

### Background Job: Permission Cache Warmup

`PermissionCacheWarmupJob` (also a `BackgroundService`) is a **performance** optimization, not a correctness mechanism. On an interval from `appsettings.json` (`BackgroundJobs.PermissionCacheWarmup`), it re-warms the permission and assigned-Division cache entries for recently-active users shortly before their TTL expires, so an ordinary request doesn't pay a synchronous DB lookup on every cache miss. It has no bearing on the fail-closed rule in Section 10 — that rule triggers only when Redis itself is unreachable, not on a normal cache miss.

### Rough Scale Guidance

| Scale | What's needed |
|---|---|
| Up to ~5,000 users, a handful of sub-systems | Current design as-is |
| ~50,000 users | Redis (already in baseline above), a read replica for reporting queries |
| 500,000+ users | Partition the `AuditLog` and `RefreshToken` tables, consider splitting the read model out of the write model entirely, revisit whether auth and user-management should be separate deployables |

## 16. Tech Stack

| Concern | Choice |
|---|---|
| Framework | .NET 8 |
| ORM | EF Core |
| Mediator / CQRS | MediatR |
| Mapping | Mapster |
| Validation | FluentValidation |
| Logging | Serilog |
| Auth | JWT Bearer + Cookie (BFF) |
| Password hashing | BCrypt or the ASP.NET Core Identity hasher |
| Distributed cache | Redis — permission cache, token revocation, distributed rate limiting |
| Docs | Swagger / OpenAPI |
| Testing | xUnit, FluentAssertions, NSubstitute |

---

## 17. PIN Code (Document Receive/Release) — TODO

**Not yet implemented.** This section locks in the design agreed before implementation begins, per `agent.md` Section 8 ("stop and ask" before crossing layers). Nothing here should be treated as built until this TODO tag is removed.

### Rule

- Every `User` has a 6-digit PIN Code, separate from their password, used by sub-systems to authorize document receive/release actions.
- The PIN is generated at user-creation time, and the rule depends on **who creates the user**, not the new user's role:
  - Created by **System Administrator** → may optionally type the 6-digit PIN manually; if left blank, the system auto-generates it.
  - Created by **Administrator** → always auto-generated by the system. No manual entry.
- The PIN can be reset/regenerated after creation. Only **System Administrator** can do this by default. A System Administrator may grant this ability to a specific `Administrator` through the existing `RolePermission` mechanism (Section 5) — no new access-control mechanism is introduced.
- PIN verification is **central to OneAccess**. Sub-systems never store or verify the PIN themselves — they call `POST /api/auth/verify-pin` on OneAccess for every document receive/release action.

### New Domain fields

**User** (existing entity, new fields):
```
PinCodeHash             string    — hashed (BCrypt), never plain text — same rule as PasswordHash
PinCodeSetAt             DateTime
PinCodeSetBy             Guid?    — UserId who set/reset it, for audit
IsPinCodeAutoGenerated   bool
```

**SubSystem** (existing entity, new fields):
```
ClientSecretHash               string     — hashed (BCrypt). Required because /api/auth/verify-pin is a
                                             server-to-server call, not a browser-to-BFF call — it cannot
                                             rely on the httpOnly session cookie, and Audience alone is not
                                             a credential (Section 10).
PreviousClientSecretHash       string?    — the prior secret, kept only during a rotation's grace window
PreviousClientSecretExpiresAt  DateTime?  — when the previous secret stops being accepted
```

### New Permission code

```
user.pincode.reset
```
Seeded to `System Administrator` only. Not assignable to the `User` role. May be assigned to `Administrator` by a System Administrator through the existing `rolepermission.assign` endpoint.

### New Endpoints

| Method | Route | Auth | Purpose |
|---|---|---|---|
| POST | `/api/users/{id}/pincode/reset` | `user.pincode.reset` | Reset/regenerate a user's PIN. If the caller is a System Administrator, an optional `pinCode` field may be supplied for manual entry; otherwise the system generates one. |
| POST | `/api/auth/verify-pin` | Sub-system client credential (see "Sub-system → OneAccess Authentication" below) | Verify `{ userId, pinCode }` for a document receive/release action. First checks that the calling sub-system has a `UserSubSystemAccess` grant for that `userId` — a valid client credential authenticates *which sub-system* is calling, it does not by itself authorize that sub-system to verify *any* user's PIN. Returns success/fail only — never returns or echoes the PIN. |
| POST | `/api/subsystems/{id}/rotate-secret` | `subsystem.update` | Regenerate a sub-system's client secret. Returns the new plaintext secret exactly once. The old secret keeps validating for `ClientSecretRotationGraceMinutes` so the sub-system's own config can be updated without a hard-cutover outage. |

`/api/users/{id}/pincode/reset` (browser → BFF, cookie-authenticated) is subject to the CSRF / `Content-Type: application/json` rule (Section 10). `/api/auth/verify-pin` is a server-to-server call with no cookie involved, so CSRF does not apply to it — it still requires `Content-Type: application/json`, but as a request-format safeguard, not a CSRF mitigation. Both are subject to per-route rate limiting (`RateLimiting` config, Section 13).

### New Configuration (`PinCodeOptions`)

```json
"PinCode": {
  "Length": 6,
  "MaxFailedAttempts": 5,
  "LockoutMinutes": 15
}
```
A 6-digit PIN has only 1,000,000 combinations, so `MaxFailedAttempts` + `LockoutMinutes` are mandatory here, not optional — same reasoning as `PasswordPolicy` (`agent.md` Section 2).

Failed-attempt counts are tracked in **Redis**, not a DB column — key `pin-fail:{userId}:{subSystemId}`, TTL = `LockoutMinutes`. This is the same store already used for rate limiting (Section 13), so a lockout clears itself automatically and never needs a cleanup job.

The existing `SubSystemOptions` (Infrastructure/Options) gains one new key for the rotation mechanism below:
```json
"SubSystems": {
  "ClientSecretRotationGraceMinutes": 15
}
```

The existing `RateLimiting` section (`agent.md` sample `appsettings.json`) gains one new key, keyed **per sub-system client** rather than per-IP like `Login`/`Setup`/`Refresh` — this is what Security Rule 3 below requires:
```json
"RateLimiting": {
  "VerifyPin": { "PermitLimit": 20, "WindowSeconds": 60 }
}
```

### Sub-system → OneAccess Authentication

Today, `SubSystem.Audience` only supports OneAccess → sub-system token validation (outbound, Section 10). There is no existing mechanism for a sub-system to authenticate an inbound, server-to-server call to OneAccess itself — but `/api/auth/verify-pin` needs exactly that.

**Mechanism: a static hashed secret**, the same pattern already used for `RefreshToken`/`SetupCode` (BCrypt hash at rest, plaintext shown once) — chosen over mTLS or a short-lived service JWT because it needs no new infrastructure (no mutual-TLS termination, no second token-issuance flow) and stays consistent with "no custom crypto" (`agent.md` Section 5.3).

- `SubSystem.ClientSecretHash` is generated automatically (a random, high-entropy value — e.g. 32 bytes, base64url-encoded — not a human-chosen string) the moment a sub-system is registered (`POST /api/subsystems`). The plaintext secret is returned **exactly once**, in that response body, and is never retrievable again — the same one-time-reveal rule as the setup code (Section 6) and the PIN (Security Rule 5 below).
- Transport: sent as a request header (`X-Client-Secret`), not `Authorization: Bearer` — kept visually and mechanically distinct from the JWT Bearer scheme that flows in the *other* direction (BFF ↔ sub-system, Section 10), so a header from one direction is never mistakable for the other at a glance.
- Validated by a dedicated ASP.NET Core authentication handler, `SubSystemClientAuthHandler` (Section 3), registered under the scheme name `"SubSystemClient"` (`AddScheme<SubSystemClientAuthSchemeOptions, SubSystemClientAuthHandler>("SubSystemClient", ...)`), wired in `Program.cs` composition only (`agent.md` Section 3.9) — a distinct scheme from JWT Bearer and the httpOnly cookie. `RequireAuthorization("SubSystemClient")` (or an equivalent policy) is what `/api/auth/verify-pin` declares to opt into it — the only endpoint in the project that authenticates this way. `POST /api/subsystems/{id}/rotate-secret` stays ordinary cookie-authenticated (`subsystem.update`, a System-Administrator-facing action in the portal), not `SubSystemClient` — it's the caller *managing* a sub-system's credential, not the sub-system itself calling in.
- **Endpoint placement:** `POST /api/users/{id}/pincode/reset` lives in `UserEndpoints.cs` (it's a `/api/users/` route, cookie-authenticated like the rest of that file). `POST /api/auth/verify-pin` lives in `AuthEndpoints.cs` (it's a `/api/auth/` route, but `SubSystemClient`-authenticated instead of cookie-authenticated — the only route in that file that isn't). `POST /api/subsystems/{id}/rotate-secret` lives in `SubSystemEndpoints.cs`, alongside `POST /api/subsystems`. No new `Endpoints/` file is needed for Section 17.
- **Rotation:** `POST /api/subsystems/{id}/rotate-secret` (`subsystem.update`) generates a new secret and returns it once, exactly like registration. The *old* secret is not invalidated immediately — it is moved to `PreviousClientSecretHash` with `PreviousClientSecretExpiresAt = now + ClientSecretRotationGraceMinutes` (new `SubSystemOptions` key, default 15) — so `SubSystemClientAuthHandler` accepts either the current or the previous secret during that window. This mirrors the reasoning behind the JWT `kid`/JWKS overlap in Section 10 (avoid a hard-cutover outage) without needing a multi-key structure, since only one prior secret is ever live at a time.
- A valid `ClientSecretHash` only proves *which sub-system* is calling — it is not, by itself, authorization to verify any user's PIN. `POST /api/auth/verify-pin` additionally requires a `UserSubSystemAccess` grant between that sub-system and the `userId` in the request (see the endpoint note above); without this check a compromised or overly-broad sub-system credential could verify PINs for users who were never granted access to that sub-system.

### Security Rules

1. `PinCodeHash` is hashed (BCrypt), never stored or logged in plain text — same as `PasswordHash` (agent.md Section 5.3).
2. `PinCodeHash`, `ClientSecretHash`, and `PreviousClientSecretHash` must be explicitly `.Ignore()`-d in `MapsterConfiguration` for every DTO derived from `User` / `SubSystem` — extends Section 9 and `agent.md` Section 5.11.
3. `/api/auth/verify-pin` is rate-limited **per sub-system client**, not just per IP — a compromised sub-system credential should not be able to brute-force PINs at will.
4. Every PIN reset, every verify-pin attempt (success or failure), and every client-secret rotation is written to `AuditLog` — extends the "must be logged" list in `agent.md` Section 6.4.
5. The PIN is returned in an HTTP response body only at the moment of manual entry by a System Administrator, or once in the auto-generated response immediately after creation/reset — never retrievable afterward. This mirrors the setup-code rule in Section 6 and `agent.md` Section 5.1.
6. A manually-entered PIN (System Administrator only, per the Rule above) is validated against a blacklist of trivially-guessable values (`000000`, `111111`, ..., `123456`, `654321`, and other ascending/descending/repeating-digit sequences) before it is accepted — rejected with a validation error, same pipeline as `FluentValidation` for any other command (`agent.md` Section 4.6). An auto-generated PIN is exempt since it is drawn uniformly at random. This matters more here than for `PasswordPolicy`: with only 1,000,000 possible values, a human picking a memorable one is the weakest link in the whole PIN scheme.