# ROADMAP.md

Build sequence for OneAccess. This file is **state, not spec** — it tracks what's been built and what's next. `agent.md`, `OneAccess.md`, and `frontend.md` are the source of truth for *how* to build each piece; this file only tracks *order* and *progress*. Do not copy architecture, entity, or endpoint detail into this file — link to the section that has it instead.

---

## How to use this file (read this first, every session)

1. **Read this file before doing anything else.** It tells you which step is current — the first unchecked box, top to bottom. Do not skip ahead to a later step even if it looks easy, and do not restart a checked step without being asked.
2. **Then read `agent.md` Section 1** — it will point you to `OneAccess.md` and/or `frontend.md` depending on which layer the current step touches.
3. **Complete the current step fully** before checking its box. "Fully" means: it builds, and where the step says to verify something (e.g. "confirm via Swagger"), that verification actually happened — not just that the code was written.
4. **When a step is done**, check its box `[x]` and add one line to the **Progress Log** at the bottom: date, what was actually done, and anything you deferred or changed from the plan. The next session (possibly a different agent) depends on this log to pick up correctly — don't skip it.
5. **If a step needs to deviate from what's written here or in the other docs**, stop and ask first (`agent.md` Section 8). Don't silently change the plan and don't silently change this file's step list — only the checkboxes and the Progress Log get updated as work happens; if the *plan itself* needs to change, that's a conversation with the user, not a self-edit.
6. **One step, one focus.** Steps 3–5 (backend) and 6–7 (client) are each broken into sub-bullets in dependency order — don't reorder them (e.g. don't build Divisions before Setup/Auth works, don't build client Roles pages before the Roles API exists).

---

## Steps

- [x] **1. Scaffold the solution**
  `OneAccess.sln` + empty projects (`OneAccess.md` Section 3) + project references (Section 2, Dependency Rules) + NuGet packages (Section 16) + internal folder structure. No entities, no logic yet.

- [x] **2. Build the Domain layer**
  Entities, enums, exceptions — `OneAccess.md` Section 4. Skip anything still tagged `TODO` in Section 17.

- [x] **3. Application + Infrastructure for Setup and Auth**
  Everything else is gated behind First Run Setup, so this vertical slice goes first: interfaces, the Setup and Auth CQRS features (Section 6), `DbContext`, migrations, `PasswordHasher`, `JwtTokenService`/`RsaKeyProvider`, `SetupCodeService`.

- [x] **4. Wire the API layer and get login working**
  `Program.cs` composition, full `appsettings.json` sample (`agent.md` Section 2), `SetupGuardMiddleware`, `SetupEndpoints`, `AuthEndpoints` (including `GET /api/auth/me`, since `frontend.md` Section 4 depends on it entirely — build it now, not as an afterthought in Step 6). **Verify before checking this box:** run the app, get the setup code from console, `POST /api/setup/initialize`, then `POST /api/auth/login`, then `GET /api/auth/me` with the resulting cookie — all four must actually succeed.

- [ ] **5. Build out the remaining backend features** (in this order — each depends on the last)
  - [x] 5a. Users / Roles / RolePermissions (core RBAC)
  - [x] 5b. Divisions / Sections (`DivisionScopeBehavior` depends on these existing)
  - [ ] 5c. SubSystems (sub-system visibility, `GetMySubSystems`)
  - [ ] 5d. Permissions listing + Audit log endpoints (`GET /api/permissions`, `GET /api/audit`)
  Run the `agent.md` Section 7 checklist against each sub-step before moving to the next.

- [ ] **6. Scaffold `OneAccess.Client`**
  Blazor WebAssembly project hosted by `OneAccess.API` (`frontend.md` Section 2). `Program.cs`, `CookieAuthenticationStateProvider`, a bare `Login.razor`. **Verify before checking this box:** the browser round-trip actually works — login sets the cookie, `GET /api/auth/me` returns the logged-in user.

- [ ] **7. Build out client pages, mirroring backend order**
  - [ ] 7a. Users, Roles pages
  - [ ] 7b. Divisions, Sections pages
  - [ ] 7c. SubSystems pages
  - [ ] 7d. Audit log page
  Each page pairs with its typed API client interface (`frontend.md` Section 3) and goes through the `frontend.md` Section 11 checklist before being marked done.

- [ ] **8. Section 17 (PIN Code feature)** — only after everything above is done and stable
  Not started until explicitly requested — `OneAccess.md` Section 17 is still a locked-in design, not yet implemented. Re-read Section 17 in full before starting; it touches Domain, Application, Infrastructure, API, and `frontend.md` all at once.

---

## Progress Log

- 2026-09-18 — Step 5b completed: Built complete vertical slice for Divisions & Sections management. Implemented CQRS commands, queries, FluentValidation rules, and endpoints per OneAccess.md Section 14 (`CreateDivision`, `UpdateDivision`, `DeleteDivision`, `AssignUserDivision`, `RevokeUserDivision`, `GetDivisions`, `GetDivisionAdministrators`, `CreateSection`, `UpdateSection`, `DeleteSection`, `GetSectionsByDivision`). Enforced design guard keeping Division structural mutations System-Administrator-only (non-IDivisionScopedRequest) while delegating Section CRUD to division-scoped Administrators (`IDivisionScopedRequest` with `DivisionScopeBehavior`). Implemented deletion constraint guards (409 Conflict when Division referenced by Sections/Users/Assignments or Section referenced by Users) and Redis cache eviction on division assignments (`assigned_divisions:{userId}`). Added unit and live integration tests verifying all scoping, CRUD, conflict, and revocation scenarios (60 unit tests and 2 integration test suites passing) — nothing deferred or changed.
- 2026-09-18 — Step 5a completed: Built core RBAC vertical slice for Users, Roles, and RolePermissions. Implemented commands, queries, handlers, FluentValidation validators, and endpoints per OneAccess.md Section 14: `CreateUser`, `UpdateUser`, `DeleteUser` (soft delete with status change), `AssignRole`, `GetUsers` (division-scoped for non-sysadmins), `GetUserById`, `CreateRole`, `UpdateRole` (guarded against renaming IsSystemRole), `DeleteRole` (guarded against deleting IsSystemRole or in-use roles), `GetRoles`, `AssignPermission` / `RevokePermission` (guarded against modifying System Administrator permissions). Implemented same-handler Redis cache invalidation (`roles:{userId}`, `permissions:{userId}`, `assigned_divisions:{userId}`) and live token revocation. Validated all 7 negative/positive guard cases with live HTTP calls against running API in integration tests + verified full unit test suite (41 tests passing) — nothing deferred or changed.
- 2026-09-18 — Step 4 completed: Wired API layer composition root in `Program.cs` with exact Section 11 middleware pipeline (CorrelationId -> ExceptionHandling -> Serilog Request Logging -> HTTPS -> CORS -> RateLimiting -> Authentication -> Authorization -> SetupGuardMiddleware -> Endpoints); implemented standard ASP.NET Core Cookie Authentication (encrypted ticket session cookie, server-side-only RS256 JWT tokens); added `JwksEndpoints` (`GET /.well-known/jwks.json`), `SetupEndpoints` (`GET /api/setup/status`, `POST /api/setup/initialize`), `AuthEndpoints` (`POST /api/auth/login`, `POST /api/auth/refresh`, `POST /api/auth/logout`, `GET /api/auth/me`), HealthCheck endpoints, and `SetupGuardMiddleware`; executed live end-to-end verification against running API (Setup status -> Setup initialize with console code -> Login setting cookie -> Auth me returning sysadmin user profile -> JWKS returning public key -> Setup guard 403 lock) — nothing deferred or changed.
- 2026-09-18 — Step 3 completed: Built Application + Infrastructure vertical slice for Setup and Auth (Common interfaces, MediatR pipeline behaviors with strict order, Result models, Mapster configuration, Setup & Auth CQRS features, OneAccessDbContext with all configurations and interceptor, EF Core InitialCreate migration, PermissionSeeder, BCrypt PasswordHasher, RS256 JwtTokenService/RsaKeyProvider, SetupCodeService, RedisCacheService, RedisTokenRevocationService, SubSystemAccessService, background jobs, strongly-typed Options classes); comprehensive unit tests written and verified passing (15 tests total). — nothing deferred or changed.
- 2026-09-18 — Spec update sync: Synced `OneAccess.Application` folder structure with updated `OneAccess.md` spec (added `DeleteRole`, `DeleteUser`, `GetSubSystems`, `GetDivisionAdministrators` feature folders); verified Domain layer remains 100% consistent with updated spec.
- 2026-09-18 — Step 2 completed: Built complete Domain layer (`OneAccess.Domain`) with `BaseEntity`, `IAuditableEntity`, domain entities (`User`, `Role`, `Permission`, `UserRole`, `RolePermission`, `SubSystem`, `UserSubSystemAccess`, `RoleSubSystemAccess`, `Division`, `Section`, `UserDivisionAssignment`, `RefreshToken`, `SetupCode`, `AuditLog`), enums (`UserStatus`, `SystemRole`/`SystemRoles`), exceptions (`DomainException`, `NotFoundException`); zero external dependencies; skipped TODO Section 17 fields; unit tests created and verified. — nothing deferred or changed.
- 2026-09-18 — Step 1 completed: Scaffolded `OneAccess.sln`, empty projects (`OneAccess.Domain`, `OneAccess.Application`, `OneAccess.Infrastructure`, `OneAccess.API`, test projects), configured layer references per dependency rules, added NuGet packages per Section 16, created full internal folder structure (excluding deferred Step 6 client package and Step 8 PinCode folder); build and test runs verified cleanly. — nothing deferred or changed.
