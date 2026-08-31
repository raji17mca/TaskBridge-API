# Project Service — Code Review & Remediation

Scope: the Project Service as it existed before this remediation pass — `src/projects/ProjectService.cs`, `src/projects/IProjectService.cs`, `src/projects/ProjectsController.cs`, `src/projects/Project.cs`, and their wiring in `Program.cs`. That version used `TaskBridgeDbContext` directly from the service, read the tenant id from an `X-Tenant-Id` HTTP header, and returned the `Project` EF entity straight out of the API.

## Review process

1. Read the generated code top to bottom against this repo's own [.github/copilot-instructions.md](.github/copilot-instructions.md) (layered architecture, DTOs, tenant isolation, structured logging, OAuth/JWT).
2. For each rule in that file, checked whether the generated code actually satisfied it or only *looked* like it did (e.g. a comment claiming something is a "placeholder" is not the same as it being safe).
3. Traced every request path end-to-end (controller → service → DbContext) asking "what does an attacker control here, and what do they get back?" — this is where the tenant-header issue was found; Copilot's own inline comment on that method was the tell.
4. Compared test coverage against the "Testing" section of the instructions file (explicitly requires tenant-access and permission-failure tests) — found none existed for the adversarial case, only happy-path and generic not-found.
5. Copilot was useful for quickly regenerating the corrected layers once the issues were identified (repository, DTOs, middleware boilerplate); it was **not** the thing that caught the issues in the first place — that required manually re-reading the code against the project's stated rules and thinking like an attacker.

## Issues found

### 1. Tenant identity trusted from a client-supplied HTTP header — Critical
- **Where:** `ProjectsController.GetTenantId()` (old version) parsed `Request.Headers["X-Tenant-Id"]` and used it, unvalidated, as the tenant scope for every read/write.
- **Impact:** Any caller can set that header to an arbitrary GUID and read, update, or delete another organization's projects. This is a complete bypass of multi-tenant isolation — in a B2B SaaS product this is a cross-tenant data breach, not a bug report. It maps directly to OWASP API Security's "Broken Object Level/Function Level Authorization."
- **How it was detected:** The code itself had a comment ("Placeholder until JWT-based tenant claims are wired up...") — Copilot flagged its own shortcut in a comment, but still produced fully wired, functioning code. That's a trap: the comment reads as "handled," when in fact it's a live vulnerability that compiles, passes tests, and looks finished.
- **Fix applied:** Removed the header entirely. Added `ICurrentTenantProvider` / `HttpContextTenantProvider` ([src/common/HttpContextTenantProvider.cs](src/common/HttpContextTenantProvider.cs)) that reads `tenant_id` from the authenticated user's **validated JWT claims**, plus JWT bearer authentication and `[Authorize]` on the controller ([Program.cs](Program.cs), [src/projects/ProjectsController.cs](src/projects/ProjectsController.cs)).

### 2. Domain entity returned directly as the API response — High
- **Where:** `ProjectService` and `ProjectsController` returned `Project` (the EF entity) straight to clients, including `TenantId`.
- **Impact:** Leaks internal-only fields (`TenantId`) in every response, coupling the public API contract to the database schema and handing out an identifier that's useful for tenant enumeration/probing. Violates the repo's own "Use DTOs for requests and responses to avoid leaking internal domain objects" rule.
- **How it was detected:** Only found by explicitly checking generated responses against the instructions file's DTO rule — Copilot has no innate notion of "this field is internal," it optimizes for "compiles and returns the data that was asked for."
- **Fix applied:** Added typed contracts — [CreateProjectRequest](src/projects/Dtos/CreateProjectRequest.cs), [UpdateProjectStatusRequest](src/projects/Dtos/UpdateProjectStatusRequest.cs), [ProjectResponse](src/projects/Dtos/ProjectResponse.cs) — `ProjectResponse` deliberately excludes `TenantId`.

### 3. No repository layer; service talked to `DbContext` directly — Medium
- **Where:** `ProjectService` constructor took `TaskBridgeDbContext` directly.
- **Impact:** Contradicts the mandated `model → repository → service → controller` layering. Not exploitable on its own, but it erodes the boundary meant to keep raw query/data-access logic out of business logic, making it easier for future changes (AI-assisted or not) to introduce ad hoc queries without the tenant filter discipline the repository now centralizes.
- **How it was detected:** Cross-checked against the "Architectural conventions" section of the instructions file; Copilot did not add this layer on its own even though it's an explicit, repo-documented rule.
- **Fix applied:** Added [IProjectRepository](src/projects/IProjectRepository.cs) / [ProjectRepository](src/projects/ProjectRepository.cs). `ProjectService` now depends only on the repository abstraction, and every repository query is tenant-scoped at the query level (`WHERE TenantId = ...`), not filtered afterward.

### 4. No structured logging — Medium
- **Where:** `ProjectService` had zero logging calls.
- **Impact:** No audit trail for who created, updated, or deleted a project — a compliance and incident-response gap for a B2B product where "who touched this record" is often a contractual requirement.
- **Fix applied:** Injected `ILogger<ProjectService>`; logs `Information` on successful create/update/delete and `Warning` on not-found/cross-tenant attempts — logging only IDs, never project `Name`/`Description`, per the "do not log sensitive payloads" rule.

### 5. No consistent error handling — Medium
- **Where:** Validation failures threw raw `ArgumentException` with nothing to catch them; an unhandled exception falls through to ASP.NET Core's default handler, which can return a stack trace in Development and an opaque 500 in Production either way — neither is a proper 400.
- **Impact:** Inconsistent, unpredictable client experience for bad input; risk of leaking internals if `UseDeveloperExceptionPage`-style output ever reaches a real environment misconfigured as Development.
- **Fix applied:** Added `[Required]`/`[MaxLength]` data annotations to the request DTOs (auto-validated by `[ApiController]`, returns 400 before the service is even called), plus [ExceptionHandlingMiddleware](src/common/ExceptionHandlingMiddleware.cs) that maps `ArgumentException` → 400, `TenantResolutionException` → 401, and anything else → a generic 500 `ProblemDetails` with no internal detail leaked.

### 6. No authentication/authorization configured at all — Critical
- **Where:** `Program.cs` called `app.UseAuthorization()` but no authentication scheme was ever registered — combined with issue #1, there was no real access control on the API.
- **Fix applied:** Added JWT bearer authentication (`Microsoft.AspNetCore.Authentication.JwtBearer`), `[Authorize]` on `ProjectsController`. **Still a known gap:** the signing key is currently a dev-only placeholder in `appsettings.Development.json` — production must source it from a secret manager/Key Vault and point `Jwt:Issuer`/`Jwt:Audience` at the real identity provider before this is deployable.

### 7. `Database.EnsureCreated()` instead of EF Core migrations — Low/Medium (unresolved)
- **Where:** `Program.cs` startup.
- **Impact:** No schema version history, no safe path to evolve the schema without data loss risk. Acceptable for local dev only.
- **Status:** Not fixed in this pass — the `dotnet-ef` CLI tool wasn't available in this environment to generate a migration. Flagging as required follow-up before production; do not let this quietly stay as-is.

## Architectural & Security Issues Copilot Introduced That Required Human Judgment

- **The client-supplied tenant header (#1).** Copilot even *documented* it as a placeholder in a code comment — but still shipped it as fully working, testable code. That's arguably worse than an unremarked shortcut: the comment gives false confidence that the risk is "known and tracked," when in fact it's live, exploitable code sitting one deploy away from production. Only a developer reading the comment *and* asking "so is this actually blocked from happening?" catches that it wasn't.
- **No adversarial tests.** Every AI-generated test covered the happy path and a generic "unknown id → 404." None asked "what if a different tenant requests *this exact, valid* project id?" That test only gets written if a human deliberately thinks about the abuse case — an LLM optimizes for making the given feature work, not for actively trying to break its own isolation guarantees.
- **Entity-as-response (#2) and missing repository layer (#3).** Both are cases where Copilot satisfied the literal request ("a service that uses a database") efficiently but didn't enforce this repo's own architectural rules unless explicitly checked against them. This matters more in a service other teams/services will depend on: once other services start integrating against a leaky contract (extra fields, no clear layering), correcting it later is a breaking change across the platform, not just a local refactor.
- **Why this is riskier for a shared/depended-upon service specifically:** a tenant-isolation bug in an internal-only tool is bad; the same bug in a Project Service that other TaskBridge services call is a platform-wide blast radius — every downstream consumer inherits the vulnerability, and by the time it's caught in an integration, the fix has to be coordinated across every caller instead of one PR.

## Remediated code

The Project Service has been rewritten in place following the fixes above:
- [Project.cs](src/projects/Project.cs) — entity (unchanged shape, still tenant/team-scoped)
- [IProjectRepository.cs](src/projects/IProjectRepository.cs) / [ProjectRepository.cs](src/projects/ProjectRepository.cs) — data access only, EF Core, no raw SQL
- [IProjectService.cs](src/projects/IProjectService.cs) / [ProjectService.cs](src/projects/ProjectService.cs) — validation, tenant enforcement, structured logging, XML doc comments on the public interface
- [Dtos/CreateProjectRequest.cs](src/projects/Dtos/CreateProjectRequest.cs), [Dtos/UpdateProjectStatusRequest.cs](src/projects/Dtos/UpdateProjectStatusRequest.cs), [Dtos/ProjectResponse.cs](src/projects/Dtos/ProjectResponse.cs) — typed request/response contracts
- [ProjectsController.cs](src/projects/ProjectsController.cs) — thin, `[Authorize]`, no header-based identity
- [common/ICurrentTenantProvider.cs](src/common/ICurrentTenantProvider.cs), [common/HttpContextTenantProvider.cs](src/common/HttpContextTenantProvider.cs), [common/TenantResolutionException.cs](src/common/TenantResolutionException.cs), [common/ExceptionHandlingMiddleware.cs](src/common/ExceptionHandlingMiddleware.cs) — cross-cutting security/error-handling concerns
- [Program.cs](Program.cs) — JWT bearer auth, DI wiring, exception middleware
- Tests updated/added in [tests/TaskBridge-API.Tests/ProjectsControllerTests.cs](tests/TaskBridge-API.Tests/ProjectsControllerTests.cs) (including two new cross-tenant isolation tests) and [tests/TaskBridge-API.Tests/ProjectServiceTests.cs](tests/TaskBridge-API.Tests/ProjectServiceTests.cs)

`dotnet test` — 11/11 passing after remediation.
