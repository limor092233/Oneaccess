# frontend.md

Single source of truth for `OneAccess.Client`, the Blazor WebAssembly SPA. `OneAccess.md` remains the source of truth for the domain model, endpoints, and security rules — this document only covers what's specific to the client. `agent.md` Section 1's rules (load before coding, keep in sync, don't invent locations, TODO tagging) apply here exactly as they do to `OneAccess.md`.

---

## 1. Overview

`OneAccess.Client` is a **Blazor WebAssembly** SPA, hosted by `OneAccess.API` — not a separate deployable, no separate domain, no CORS in production. It talks only to the BFF endpoints documented in `OneAccess.md` Section 14, and never holds a token itself: the same `httpOnly` cookie rule from `agent.md` Section 5.4 applies to it exactly as it would to any other browser client.

---

## 2. Architecture

```
┌────────────────────────────────────────────────┐
│            OneAccess.API (BFF Host)             │
│  ┌────────────────────────────────────────┐     │
│  │ wwwroot/  (published OneAccess.Client)  │     │  ← static files: WASM, dll, css, js
│  │ Serves index.html + framework files     │     │
│  └────────────────────────────────────────┘     │
│  Endpoints (OneAccess.md Section 14)             │
│  httpOnly cookie auth                            │
└────────────────────────────────────────────────┘
        ▲
        │ same-origin fetch/HttpClient, cookie attached automatically
        │
┌───────┴─────────┐
│ Browser          │
│ OneAccess.Client │  ← runs entirely client-side (Blazor WebAssembly)
└──────────────────┘
```

Because the client is same-origin (hosted by the same API project), **no CORS configuration is needed in production.** `Cors.AllowedOrigins` (`agent.md` sample `appsettings.json`) stays reserved for local development only — see Section 11.

### Hosting

- `OneAccess.Client` is referenced by `OneAccess.API` (standard ASP.NET Core hosted Blazor WASM model, `Microsoft.AspNetCore.Components.WebAssembly.Server`), published into `OneAccess.API`'s `wwwroot/`.
- `Program.cs` (API) wires `app.UseBlazorFrameworkFiles(); app.UseStaticFiles(); app.MapFallbackToFile("index.html");` — composition only, per `agent.md` Section 3.9 (no logic, just pipeline wiring).
- One deployable, one process.

---

## 3. Project Layout

```
src/OneAccess.Client/
├── OneAccess.Client.csproj
├── Program.cs
├── App.razor
├── wwwroot/
│   ├── index.html
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   └── css/
├── Layout/
│   ├── MainLayout.razor
│   ├── NavMenu.razor                     ← filters items by permission, not role (Section 5)
│   └── ForbiddenLayout.razor
├── Pages/
│   ├── Setup/
│   │   └── InitializeSystemAdmin.razor
│   ├── Auth/
│   │   └── Login.razor
│   ├── Users/
│   │   ├── UserList.razor
│   │   ├── UserForm.razor
│   │   ├── UserRoles.razor
│   │   └── UserPinCode.razor              ← TODO, see OneAccess.md Section 17 — reset/regenerate
│   │                                         a user's PIN via POST /api/users/{id}/pincode/reset
│   ├── Roles/
│   │   ├── RoleList.razor
│   │   ├── RoleForm.razor
│   │   └── RolePermissions.razor
│   ├── SubSystems/
│   │   ├── SubSystemList.razor
│   │   ├── SubSystemForm.razor           ← register/edit (BaseUrl, Audience, IsActive)
│   │   └── SubSystemAccess.razor         ← Role/User sub-system allow-list mgmt
│   ├── Divisions/
│   │   ├── DivisionList.razor
│   │   ├── DivisionForm.razor
│   │   └── DivisionUsers.razor           ← manages UserDivisionAssignment (which Administrators
│   │                                        are granted this Division), NOT a filtered list of
│   │                                        users whose home DivisionId is here — that's just a
│   │                                        filter on UserList.razor
│   ├── Sections/
│   │   ├── SectionList.razor
│   │   └── SectionForm.razor
│   ├── Audit/
│   │   └── AuditLogList.razor
│   └── Shared/
│       ├── ForbiddenPage.razor           ← 403
│       └── NotFoundPage.razor            ← 404
├── Components/                            ← reusable, feature-agnostic
│   ├── PermissionView.razor               ← <PermissionView Requires="user.create">…</PermissionView>
│   ├── ConfirmDialog.razor
│   ├── DataTable.razor                    ← standard loading/empty/error states (Section 9)
│   └── Pagination.razor
├── Services/
│   ├── Api/
│   │   ├── IUserApi.cs / UserApi.cs
│   │   ├── IRoleApi.cs / RoleApi.cs
│   │   ├── ISubSystemApi.cs / SubSystemApi.cs
│   │   ├── IDivisionApi.cs / DivisionApi.cs
│   │   ├── ISectionApi.cs / SectionApi.cs
│   │   ├── IAuditApi.cs / AuditApi.cs
│   │   ├── IPermissionApi.cs / PermissionApi.cs
│   │   └── IAuthApi.cs / AuthApi.cs
│   ├── Auth/
│   │   └── CookieAuthenticationStateProvider.cs
│   └── ApiResponseHandler.cs              ← central 401/403/429/ProblemDetails handling
├── State/
│   └── CurrentUserState.cs                ← scoped: user, role, permissions, effective sub-systems
└── Models/                                ← DTOs mirroring OneAccess.Application Responses
```

Each `Services/Api/*Api.cs` mirrors an `OneAccess.Application/Features/*` folder one-for-one — same rationale as the backend's CQRS-per-feature layout (`OneAccess.md` Section 7): nobody has to guess where the API calls for a feature live. If a new backend feature folder is added, its matching client API interface is added in the same change — same discipline as `agent.md` Section 1's "update `OneAccess.md` in the same change" rule, applied here to `frontend.md`.

---

## 4. Authentication (cookie, not token)

This is the part most different from a typical Blazor WASM tutorial, which usually assumes a Bearer token cached in `localStorage`. **Do not do this** — it would contradict `agent.md` Section 5.4 ("the browser's JavaScript never holds a token").

- On `POST /api/auth/login`, the BFF sets the `httpOnly` cookie. `OneAccess.Client` never sees or touches it — the browser attaches it automatically because every API call is same-origin.
- The client cannot read the cookie's contents (by design) to know who's logged in, and the JWT itself carries `role` only, not permissions (`OneAccess.md` Section 10, "JWT Claims"). So on app start, and again right after login, the client calls `GET /api/auth/me` (`OneAccess.md` Section 14) to hydrate identity and the permission set.
- `CookieAuthenticationStateProvider : AuthenticationStateProvider` calls `IAuthApi.GetCurrentUserAsync()` in `GetAuthenticationStateAsync()`:
  - `401` → returns an anonymous `ClaimsPrincipal` (not logged in).
  - `200` → builds a `ClaimsPrincipal` with a `role` claim and one custom `"permission"` claim per permission code returned, so Blazor's native `AuthorizeView`/`[Authorize]`/policy checks work without a bespoke gating mechanism.
- After a successful `Login.razor` submit, or after logout, the provider re-fetches and calls `NotifyAuthenticationStateChanged()` so the UI updates immediately — no full page reload needed.
- **Any** API response with status `401` mid-session means the cookie session is over (expired or revoked) — the client redirects to `/login`. It does not attempt its own refresh/retry logic: `POST /api/auth/refresh` and the JWT reuse-detection rules (`OneAccess.md` Section 10) are BFF-internal concerns, opaque to the client.

---

## 5. Permission-based UI

The client-side counterpart to `OneAccess.md` Section 5.

- Permission codes (`OneAccess.md` Section 5, "Permission Codes (seeded)") are the only thing the client gates UI on — never `role` directly, so a new permission-to-role mapping never requires a client code change.
- `<PermissionView Requires="user.create">…</PermissionView>` wraps any button, menu item, or section that should only render when the current user holds that code. Built on Blazor's `AuthorizeView` against the `"permission"` claim populated in Section 4.
- **Hiding a control is UX only, never security** — same distinction `OneAccess.md` Section 5 draws for the portal's own icon list: *"Hiding an icon in the portal is a UX nicety; the token-issuance check is what actually keeps a restricted user out."* Every mutating call is still independently rejected server-side by `RequirePermission` regardless of what the client renders.
- `NavMenu.razor` builds its link list the same way, plus checks `IsSystemAdministrator` for System-Administrator-only sections (Role Permission assignment, Division management). An Administrator's Division scope is **not** re-filtered client-side beyond what the server already returns (Section 7) — the server is the only source of truth for *which* Divisions/Users an Administrator can see.
- Client-side route "guards" (`[Authorize]` on a page) exist only to avoid a confusing blank page full of failed API calls — never treat them as the actual security boundary. `Pages/Shared/ForbiddenPage.razor` renders whenever any API call returns `403`.

---

## 6. State management

Kept intentionally boring — no Redux/Fluxor-style library. A small set of **scoped DI services** is enough for this app's size:

- `CurrentUserState` (Scoped) — current user, role, permission set, effective sub-system list. Populated once by `CookieAuthenticationStateProvider`, read everywhere via DI.
- Each `Pages/*List.razor` owns its own local state (loaded in `OnInitializedAsync`, refreshed after its own mutations) — there's no global "all users" cache, since Division-scoped Administrators only ever receive a filtered slice from the server anyway (`OneAccess.md` Section 5).
- If a future feature needs cross-page reactive state (e.g. a live audit feed), reach for a small pub/sub service first, before reaching for a state-management package — same "no unneeded complexity" spirit as `agent.md`.

---

## 7. API client layer

- One typed interface per feature (`IUserApi`, `IRoleApi`, …) — see Section 3 for the folder mapping.
- All API clients share one named `HttpClient` (`"OneAccessApi"`), `BaseAddress` set to the app's own origin — trivial since the client is same-origin/hosted (Section 2). Registered once in `Program.cs`.
- Every mutating call uses `PostAsJsonAsync`/`PutAsJsonAsync`/etc., which default to `Content-Type: application/json` — this is exactly what satisfies the CSRF mitigation in `OneAccess.md` Section 10 ("state-changing endpoints only accept `Content-Type: application/json`"). Nothing extra is needed client-side, but **never** switch a form to `multipart/form-data` or a plain HTML `<form>` post without re-reading that section first — doing so would silently reopen the CSRF hole the whole mechanism exists to close.
- `ApiResponseHandler` (a `DelegatingHandler` in the `HttpClient` pipeline) centralizes, in one place instead of per-page:
  - `401` → redirect to `/login` (Section 4)
  - `403` → redirect to `/forbidden`
  - `429` → surface a "too many attempts" toast, reading `Retry-After` when present (`OneAccess.md` Section 13, `RateLimiting`)
  - other 4xx/5xx → parse the ASP.NET Core `ProblemDetails`/`ValidationProblemDetails` body (`ExceptionHandlingMiddleware`, `OneAccess.md` Section 11) and surface `title`/`detail`, or map `errors[fieldName]` onto the matching `EditForm` field

---

## 8. Division-scoped UI

Mirrors `OneAccess.md` Section 5, "Division-Scoped Authorization." The client is never the source of truth for what an Administrator can touch — but hiding out-of-scope options still matters for UX:

- `GetUsers`/`GetDivisions`/etc. already return only the caller's allowed rows (server-side filtering) — the client renders exactly what it receives, no client-side re-filtering.
- `UserForm.razor`'s Division/Section pickers, for a caller who is an Administrator (not System Administrator), only offer the Administrator's own assigned Divisions (from `CurrentUserState`, Section 6) — this doesn't replace `DivisionScopeBehavior`'s own re-check on submit (`OneAccess.md` Section 5), it just avoids showing an Administrator an option that would come back `403`.

---

## 9. Error, empty, and loading states

- `DataTable.razor` standardizes loading (skeleton rows), empty ("No records"), and error (retry button) states across every `*List.razor` page — written once, reused everywhere, instead of every list page reinventing its own spinner and error handling.
- Form validation: `FluentValidation` failures from the BFF (`agent.md` Section 4.6) come back as a standard `ValidationProblemDetails` shape. `ApiResponseHandler` (Section 7) maps `errors[fieldName]` onto the matching `EditForm` field so server-side messages appear next to the right input with no per-form boilerplate.

---

## 10. Local development

Running `OneAccess.Client` standalone (`dotnet run` from the client project on its own port) instead of through the hosted `OneAccess.API` breaks the same-origin assumption in Section 2 and needs:

- `Cors.AllowedOrigins` in `appsettings.Development.json` (never the production sample) to temporarily include the client's dev port.
- **Do not** relax `Cookie.SameSite` off `Strict` to make this work — that's a real CSRF-defense downgrade (`OneAccess.md` Section 10), not a dev-only convenience. Prefer running the client through the hosted API project (`dotnet watch` on `OneAccess.API`, standard hosted-Blazor-WASM dev workflow) so `SameSite=Strict` is exercised the same way it runs in production. Only fall back to the standalone cross-origin dev server for pure UI work against a mocked API.

---

## 11. Before you commit (addendum to `agent.md` Section 7)

- [x] No API base URL, secret, or environment-specific value hardcoded in a `.razor`/`.cs` file — comes from `wwwroot/appsettings.json` / `wwwroot/appsettings.{Environment}.json`, consistent with `agent.md` Section 2's "no magic strings" rule.
- [x] Every control mapping to a permission-gated backend action is wrapped in `<PermissionView>` (or an equivalent check) — remembering this is UX only; the API call itself must still be verified against a `403`.
- [x] Every new API client method has a matching typed DTO in `Models/`, not an anonymous/dynamic payload.
- [x] Every mutating form handles the `ValidationProblemDetails` shape, not just a generic error toast.
- [x] No `localStorage`/`sessionStorage` holds a token, user identity, or permission list — `CurrentUserState` is rehydrated from `GET /api/auth/me` on every app load, never persisted client-side.
- [x] Any new backend feature folder (`OneAccess.Application/Features/X`) gets a matching `Services/Api/IXApi.cs` in the same change, and this file's Section 3 layout is updated — same "never let it fall out of date" rule `agent.md` Section 1 applies to `OneAccess.md`.

---

## 12. When in doubt

Same rule as `agent.md` Section 8: stop and ask. A UI-only workaround for a missing backend endpoint is still a workaround — check `OneAccess.md` Section 14 first, and if the endpoint doesn't exist yet, that's a backend gap to raise, not a reason to call a sub-system directly from the client.
