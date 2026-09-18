# agent.md

Instructions for any AI agent or developer working on **OneAccess**. Read this before writing a single line of code.

---

## 1. Read the source-of-truth docs first

`ROADMAP.md` tracks **build order and progress** — always read it first, before either doc below, to know what's already done and what step is current. `OneAccess.md` is the **single source of truth** for this project's backend architecture, layout, domain model, and endpoints. `frontend.md` is the same for `OneAccess.Client`, the Blazor WebAssembly SPA. Load whichever one covers the layer the current `ROADMAP.md` step touches — both, if the step crosses the BFF boundary.

### Rules

0. **Always read `ROADMAP.md` first.** It tells you the current step and what's already built. Don't skip ahead, don't redo a checked step, and update it (checkbox + Progress Log) when a step is actually finished — see `ROADMAP.md`'s own "How to use this file" for the exact procedure.
1. **Always load the relevant doc(s)** before generating, editing, or refactoring code — `OneAccess.md` for anything in `OneAccess.Domain`/`Application`/`Infrastructure`/`API`, `frontend.md` for anything in `OneAccess.Client`.
2. **Follow the folder structure there exactly.** Do not create a new top-level folder unless you also update the matching doc.
3. **Do not change layer boundaries.** If a new class does not fit any existing folder, stop and ask where it belongs — do not invent a location.
4. If there is a **conflict** between either doc and the user's request, surface the conflict before proceeding.
5. When you add a new entity, endpoint, permission code, configuration key, or (client-side) page/API-client/route, **update the matching doc in the same change.** Neither must ever fall out of date.
6. Both docs document the *current* state, not a wishlist. Do not put anything in either that is not implemented, unless it is clearly tagged `TODO`.

---

## 2. Configuration: EVERYTHING goes in `appsettings.json`

This is the strictest rule in the project.

> **No configuration value may live in `Program.cs` or in any C# file.** Everything comes from `appsettings.json`. The reason: when something changes, it can be swapped quickly without rebuilding or redeploying the application.

### Not allowed (hardcoded in `Program.cs`)

```csharp
// ❌ DO NOT
options.TokenValidationParameters = new TokenValidationParameters
{
    ValidIssuer = "OneAccess",
    ValidAudience = "OneAccessClient",
    IssuerSigningKey = new SymmetricSecurityKey(
        Encoding.UTF8.GetBytes("super-secret-key-12345"))
};

builder.Services.AddCors(o => o.AddPolicy("Default",
    p => p.WithOrigins("https://localhost:4200")));

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateLogger();

options.ExpiryMinutes = 15;
```

### Correct (bound from `appsettings.json`)

```csharp
// ✅ DO
builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection(JwtOptions.SectionName));

builder.Host.UseSerilog((ctx, cfg) =>
    cfg.ReadFrom.Configuration(ctx.Configuration));
```

### Configuration rules

1. Every logical group of settings gets its own **strongly-typed Options class** in `OneAccess.Infrastructure/Options/`.
2. Every Options class exposes `public const string SectionName` matching its key in `appsettings.json`.
3. Services consume `IOptions<T>` / `IOptionsSnapshot<T>` — **never** `IConfiguration["key"]` scattered through the code.
4. Add `.ValidateDataAnnotations().ValidateOnStart()` to every options registration so bad configuration fails fast at startup.
5. **No magic numbers or magic strings in code.** That includes timeouts, page sizes, retry counts, expiry values, paths, URLs, and connection strings.
6. Sensitive values (signing key, DB password) come from **User Secrets** in development and **environment variables or a secret manager** in production — but the *shape* of the key still appears in `appsettings.json` as an empty string or placeholder. `appsettings.Development.json` is **not** exempt from this — if a real secret is ever pasted in for local convenience, that file must already be in `.gitignore` before the paste happens, not after. Verify `.gitignore` covers `appsettings.Development.json`, `appsettings.*.local.json`, and `**/secrets.json` before the first commit of a new environment file.
7. `Program.cs` is for **composition only** — wiring and pipeline order. No values, no literals, no hardcoded policies.
8. When you add a new config key, document it in `OneAccess.md` section 13 and put its default in `appsettings.json`.

### Sample `appsettings.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": ""
  },
  "Jwt": {
    "Issuer": "OneAccess",
    "Audience": "OneAccess.Client",
    "ActiveKeyId": "",
    "SigningKeysDirectory": "",
    "AccessTokenExpiryMinutes": 15,
    "RefreshTokenExpiryDays": 7,
    "ClockSkewSeconds": 0
  },
  "Redis": {
    "ConnectionString": "",
    "InstanceName": "OneAccess:"
  },
  "Caching": {
    "PermissionCacheTtlMinutes": 5,
    "DivisionScopeCacheTtlMinutes": 5
  },
  "BackgroundJobs": {
    "RefreshTokenCleanup": {
      "IntervalHours": 24,
      "RetentionDays": 30
    },
    "PermissionCacheWarmup": {
      "IntervalMinutes": 10
    }
  },
  "Cookie": {
    "Name": "oneaccess.session",
    "HttpOnly": true,
    "Secure": true,
    "SameSite": "Strict",
    "ExpiryMinutes": 480
  },
  "Setup": {
    "CodeLength": 8,
    "CodeExpiryMinutes": 15,
    "MaxAttempts": 5,
    "ConsoleOutputEnabled": true
  },
  "PasswordPolicy": {
    "MinimumLength": 12,
    "RequireUppercase": true,
    "RequireLowercase": true,
    "RequireDigit": true,
    "RequireNonAlphanumeric": true,
    "MaxFailedAttempts": 5,
    "LockoutMinutes": 15
  },
  "Cors": {
    "AllowedOrigins": [],
    "AllowedMethods": [ "GET", "POST", "PUT", "DELETE" ],
    "AllowCredentials": true
  },
  "RateLimiting": {
    "Login": { "PermitLimit": 5, "WindowSeconds": 60 },
    "Setup": { "PermitLimit": 5, "WindowSeconds": 300 },
    "Refresh": { "PermitLimit": 10, "WindowSeconds": 60 }
  },
  "Pagination": {
    "DefaultPageSize": 20,
    "MaxPageSize": 100
  },
  "HealthChecks": {
    "DetailedPath": "/health",
    "LivenessPath": "/api/health",
    "Enabled": true
  },
  "SubSystems": [
    {
      "Code": "SYS1",
      "Name": "System 1",
      "BaseUrl": "",
      "Audience": "system1.api",
      "IsActive": true
    }
  ],
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft.AspNetCore": "Warning",
        "Microsoft.EntityFrameworkCore": "Warning"
      }
    },
    "WriteTo": [
      { "Name": "Console" },
      {
        "Name": "File",
        "Args": {
          "path": "logs/oneaccess-.log",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 30
        }
      }
    ],
    "Enrich": [ "FromLogContext", "WithMachineName", "WithEnvironmentName" ]
  }
}
```

`Jwt.SigningKeysDirectory` is a **path**, not a secret — OneAccess signs with RS256, so `RsaKeyProvider` loads private/public key pairs from that directory at startup, one file pair per `kid`. Multiple keys can be present at once during a rotation window (`OneAccess.md` Section 10, "JWT Signing Key Rotation"); `Jwt.ActiveKeyId` tells `JwtTokenService` which one to sign new tokens with. The directory path itself is not sensitive, but its contents are — the actual PEM files live outside the repository and outside `appsettings.json`, following the User Secrets / environment-variable rule above.

`Redis.ConnectionString` follows the same secrets rule as `ConnectionStrings.DefaultConnection` — placeholder here, real value from User Secrets or environment variables.

---

## 3. Architecture Rules

| # | Rule |
|---|---|
| 1 | **Domain** has no dependencies. No EF attributes, no MediatR, no JSON attributes. |
| 2 | **Application** contains **interfaces only** — no concrete service implementations. |
| 3 | All **implementations** (EF, JWT, hashing, logging, email) live in **Infrastructure**. |
| 4 | The **database** stays in Infrastructure. No `DbContext` crosses into Application. |
| 5 | The **client** talks only to the Application layer through the API/BFF — never directly to Infrastructure. |
| 6 | Each feature has its own folder containing Command/Query + Handler + Validator + Response. |
| 7 | Mapping is done with **Mapster** only. No manual `new Dto { ... }` inside handlers. |
| 8 | Every write goes through `IRepository<T>` and commits through `IUnitOfWork`. Repositories never call `SaveChanges`. |
| 9 | `Program.cs` is the composition root only: `AddApplication()`, `AddInfrastructure()`, `AddApi()`, and pipeline order. |

---

## 4. Coding Rules

1. **Async everywhere.** Every I/O method is `async Task<T>` and takes a `CancellationToken`.
2. **No `async void`** except in event handlers.
3. Use the **`Result<T>`** pattern in handlers for expected failures. Exceptions are for the unexpected only.
4. **No business logic in endpoints.** An endpoint builds the command/query, sends it through MediatR, and maps the result to an HTTP status.
5. **No `DateTime.Now`.** Use `IDateTimeProvider`.
6. **Validation through FluentValidation** in `ValidationBehavior`, not manual `if` checks in handlers.
7. Authorization is **permission-based** (`RequirePermission("user.create")`), never role-name checking.
8. Every public class and method that is part of a contract gets an XML doc comment.

---

## 5. Security Rules

1. The setup code is **never** returned in an HTTP response — CLI/console only. It also never reaches a non-console log sink — see the `IsSetupCode` filtering rule in `OneAccess.md` Section 12. Adding a new sink (Seq, Elasticsearch, whatever) without also excluding that property is a security regression, not a config oversight.
2. Setup codes, refresh tokens, and PIN codes are stored hashed, never in plain text. (PIN codes: **TODO**, not yet implemented — see `OneAccess.md` Section 17.)
3. Passwords are hashed with BCrypt (or the ASP.NET Core Identity hasher) — no custom crypto.
4. The browser's JavaScript never holds a token — httpOnly cookie at the BFF.
5. Logs contain no passwords, tokens, signing keys, or setup codes in structured properties.
6. Rate limiting is applied to `/api/auth/login`, `/api/setup/initialize`, and `/api/auth/refresh`.
7. On failed login, return a generic message — never reveal whether the username or the password was wrong.
8. A refresh token that is already revoked or replaced and gets presented again is treated as theft, not an expired-token error — it revokes the user's entire session set. See `OneAccess.md` Section 10 ("Refresh Token Reuse Detection").
9. A `Redis`-dependent authorization check (revocation, permission cache, sub-system access resolution) that cannot reach Redis **rejects the request**. It never falls back to "assume authorized" — see `OneAccess.md` Section 10.
10. State-changing endpoints (`POST`/`PUT`/`DELETE`) only accept `Content-Type: application/json`, which is the CSRF mitigation layered on top of `SameSite=Strict` — see `OneAccess.md` Section 10.
11. Any DTO mapped from `User`, `RefreshToken`, `SetupCode`, or `SubSystem` must have its sensitive field (`PasswordHash` / `TokenHash` / `CodeHash` / `PinCodeHash` / `ClientSecretHash` / `PreviousClientSecretHash`) explicitly `.Ignore()`-d in `MapsterConfiguration` — never rely on the DTO "just not declaring that property." (`PinCodeHash` / `ClientSecretHash` / `PreviousClientSecretHash`: **TODO**, not yet implemented — see `OneAccess.md` Section 17.)
12. Any endpoint that authenticates a sub-system rather than a user (e.g. `/api/auth/verify-pin`) must fail closed on a missing/invalid client credential, exactly like the Redis fail-closed rule in #9 — never fall back to treating an unauthenticated sub-system call as trusted. (**TODO**, not yet implemented — see `OneAccess.md` Section 17.)
13. A sub-system client credential authenticates *which sub-system* is calling — it is never treated as authorization to act on an arbitrary user. `/api/auth/verify-pin` additionally requires a `UserSubSystemAccess` grant for the specific `userId` in the request. (**TODO**, not yet implemented — see `OneAccess.md` Section 17.)
14. **Division-scoped authorization runs through `DivisionScopeBehavior`, never a per-handler `if` check.** Any command/query that targets a Division-associated resource (a `User`, `Division`, or `Section` row) implements `IDivisionScopedRequest` so the behavior can enforce it centrally — see `OneAccess.md` Section 5 ("Division-Scoped Authorization") for exactly which requests implement it and how each resolves its target Division. Adding a manual scope check inside a handler instead of implementing the interface is a layering violation, not a shortcut.
15. A manually-entered PIN is validated against a blacklist of trivially-guessable values before acceptance; an auto-generated PIN is exempt. (**TODO**, not yet implemented — see `OneAccess.md` Section 17.)
16. A sub-system's client secret is shown in plaintext exactly once — at registration (`POST /api/subsystems`) and again at rotation (`POST /api/subsystems/{id}/rotate-secret`) — and is never retrievable afterward, same rule as the setup code and the PIN. (**TODO**, not yet implemented — see `OneAccess.md` Section 17.)
17. `AssignRoleCommand` blocks assigning any role with `IsSystemRole = true` (e.g. `System Administrator`) unless the caller is already a System Administrator — regardless of what other permissions the caller holds. `user.update` is expected to be a common grant, so this check cannot be left to the permission system alone. See `OneAccess.md` Section 5 ("Role Assignment Guard").
18. `AssignRoleSubSystemAccess` rejects a request that targets the `System Administrator` role — validated in the handler, not just skipped when resolving effective access later. See `OneAccess.md` Section 5 ("Sub-System Visibility").

---

## 6. Logging Rules

1. **Structured logging only:** `Log.Information("User {UserId} logged in", userId)` — not string interpolation.
2. Every request carries a CorrelationId attached to all of that request's log entries.
3. Serilog configuration comes from `appsettings.json` via `ReadFrom.Configuration()`.
4. Must be logged: login success/failure, token refresh, token revoke, user create/update/delete, role change, permission assign/revoke, first-run setup completion, Division assignment grant/revoke, sub-system client secret rotation, PIN reset/verify attempts (the last two once Section 17 is implemented).

---

## 7. Before you commit

- [ ] I added no hardcoded value to `Program.cs` or any C# file
- [ ] Every new config key is in `appsettings.json` and has an Options class
- [ ] I followed the folder structure in `OneAccess.md`
- [ ] I updated `OneAccess.md` for any new entity/endpoint/permission/config
- [ ] No EF Core or DbContext leaked into the Application layer
- [ ] Every new service has an interface in Application and an implementation in Infrastructure
- [ ] Every command has a validator
- [ ] Every new endpoint has a permission requirement
- [ ] No secrets are committed to the repository (including `appsettings.Development.json`)
- [ ] Any new DTO derived from `User`/`RefreshToken`/`SetupCode`/`SubSystem` has sensitive fields explicitly `.Ignore()`-d in `MapsterConfiguration` (including `PinCodeHash` / `ClientSecretHash` once Section 17 is implemented)
- [ ] Any new Serilog sink added excludes the `IsSetupCode` property
- [ ] Any new Redis-backed authorization check fails closed (rejects) on Redis unavailability, not open
- [ ] Any new sub-system-to-OneAccess (server-to-server) endpoint fails closed on a missing/invalid client credential
- [ ] Any new PIN reset or PIN verification action is written to `AuditLog`
- [ ] `/api/auth/verify-pin` (once implemented) checks `UserSubSystemAccess` for the target user, not just the client credential
- [ ] Manually-entered PINs are checked against the weak-PIN blacklist
- [ ] Any new command/query touching a `User`, `Division`, or `Section` row implements `IDivisionScopedRequest` (unless it's one of the explicitly-exempt Division-management commands listed in `OneAccess.md` Section 5) so `DivisionScopeBehavior` enforces it — not a manual check inside the handler
- [ ] A sub-system client secret is only ever returned in the registration or rotation response body, never in any subsequent read
- [ ] Any command that assigns a Role checks `IsSystemRole` and blocks escalation unless the caller is already a System Administrator
- [ ] Deleting a Division checks `Section`, `User`, and `UserDivisionAssignment` references; deleting a Section checks `User` references
- [ ] Any command changing `RoleSubSystemAccess` or `UserSubSystemAccess` invalidates the affected sub-system-access cache entries in the same handler
- [ ] `RoleSubSystemAccess` cannot be created for the `System Administrator` role
- [ ] `CreateUserCommand`/`UpdateUserCommand` reject a `SectionId` that doesn't belong to the supplied `DivisionId`
- [ ] `UpdateUserCommand` checks the caller's scope against the *new* `DivisionId` when reassigning a user across Divisions, not just the current one
- [ ] If this change touches `OneAccess.Client`, I also went through `frontend.md` Section 11's checklist

---

## 8. When in doubt

Stop and ask. One question is cheaper than a refactor that crosses three layers.
