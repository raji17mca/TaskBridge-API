# Prompt Engineering Documentation — Notification & Audit Service

> **Note:** This session built the Notification & Audit Service (models, repositories, services, controllers, tests) on top of the remediated Project Service. A dedicated `SPEC.md` for the Notification & Audit Service was **not** produced in this session — flagging that as outstanding rather than fabricating a prompt history for it.

## Prompt chain

| # | Prompt Text (summarised) | Copilot Feature Used | Prompting Technique | Why This Approach? |
|---|---|---|---|---|
| 1 | "Generate a Project model and a Project service with create, update status, get by team, and delete functions. Use a database." | Agent mode (Chat) | **Naive / unconstrained** (deliberate) | Run exactly as required, with no architectural constraints, to produce genuinely unreviewed, flawed output — this became the "contractor code" that the Notification & Audit Service was later built on top of, after remediation. |
| 2 | *(pasted)* "Project Service — Review & Remediation" brief + "Rewrite the Project Service to production standards… layered architecture, ORM-only, DTOs, tenant isolation, docs" | Agent mode + **#file** (attached `ProjectService.cs`, `Project.cs`) | **Constraint-based** | Multiple non-negotiable requirements (layering, no raw SQL, DTOs, tenant scoping, docs) needed to be satisfied together; listing them explicitly stops the model from silently dropping one — this produced the `IProjectService`/`ProjectService`/`ProjectsController` shape the new service integrates with. |
| 3 | *(pasted)* full "C. Notification & Audit Service — New Build" spec — Audit Log Model, Notification Model, Core Service Logic, 4 API endpoints | Ask mode *(chat, tools temporarily unavailable)* + **@workspace**-style full-repo context (existing Project Service used as implicit template) | **Decomposition** + **Few-shot** | The large multi-part spec was decomposed into model → repository → service → controller layers; the already-remediated Project Service was used as an implicit "few-shot" example to mirror (same tenant-scoping pattern, same DTO/logging/repository conventions), rather than inventing a new style. |
| 4 | "I have switched to agent mode, can you write the file now" | Agent mode (Chat + file tools) | **Iterative Refinement** | Same plan as #3, re-executed once file-write access was available — applied every proposed file (models, DTOs, repositories, services, controller, DI wiring) and ran `dotnet test` to confirm it actually worked, not just compiled in theory. |
| 5 | "Validate the `#sym:tenantId` and `#sym:teamId`, if null or empty throw error" | Edit mode (inline chat) + **#sym** (symbol reference) | **Specificity** | A narrow, surgical validation fix scoped to exact symbols, applied to the pattern later reused across the Audit/Notification services' own tenant/user checks. |
| 6 | "Fix any issues if you found in this file" *(ProjectService.cs)* | Ask/Agent mode + **#file** (active file attachment) | **Iterative Refinement** | Open-ended review pass that caught the missing `Enum.IsDefined` status validation — a defensive pattern also applied when building the Audit event type validation. |
| 7 | "Explain the Audit controller" / "Is there implemented logging" | Ask mode + **#file** (active file attachment) | **Role-based** (reviewer stance) | Framed as code-review questions rather than change requests, prompting critique (e.g. flagging the controller's missing access-logging) instead of silent acceptance of already-generated code. |

**Coverage check:**
- ✅ 2+ Copilot features: Agent mode chat, Ask mode chat, Edit mode (inline chat), `#file` attachments, `#sym` symbol references, `@workspace`-style context
- ✅ 3+ prompting techniques: Naive/unconstrained, Constraint-based, Decomposition, Few-shot, Iterative Refinement, Specificity, Role-based (7 distinct)

## Post-Generation Corrections

1. **Nested test project broke the main build.** Copilot produced `tests/TaskBridge-API.Tests/` nested inside the main project folder; the main project's default file glob silently compiled the test `.cs` files too, causing `CS0246: 'Xunit' could not be found`. Fixed with a manual edit adding `<Compile Remove="tests/**/*.cs" />` to `TaskBridge-API.csproj`.

2. **Client-supplied tenant header (critical security issue).** The low-effort Project Service trusted an `X-Tenant-Id` HTTP header as the tenant scope for every read/write — a complete multi-tenant isolation bypass. Fixed via a follow-up remediation prompt (Agent mode): replaced it with `ICurrentTenantProvider` reading a `tenant_id` claim from a validated JWT.

3. **Domain entity leaked directly as API response.** The service returned the `Project` EF entity (including internal `TenantId`) straight to clients. Fixed in the same remediation pass by introducing `CreateProjectRequest`/`UpdateProjectStatusRequest`/`ProjectResponse` DTOs.

4. **Missing enum validation on status update.** `UpdateStatusAsync` assigned `request.Status` with no check, so an out-of-range integer could be persisted as an invalid status. Caught via the "fix any issues" review prompt; fixed with a manual edit adding `Enum.IsDefined(typeof(ProjectStatus), request.Status)`.

5. **Documentation only at class level, not per-member.** Several classes had a single class-level `/// <inheritdoc cref="IInterface"/>` with individual methods left undocumented, so IDE tooltips showed nothing per method. Fixed with manual edits adding member-level `/// <inheritdoc/>` or explicit `<summary>` across `IProjectRepository`, `ProjectRepository`, `ProjectService`, `ProjectResponse`, `HttpContextTenantProvider`, and `ExceptionHandlingMiddleware`.

6. **Audit snapshot serialized enums as raw numbers.** `ProjectService.Snapshot()` used default `JsonSerializer` options, so `ProjectStatus` serialized as `0`/`1` instead of `"NotStarted"`/`"InProgress"` — a test asserting the snapshot text failed and caught it. Fixed by adding a `JsonSerializerOptions` with `JsonStringEnumConverter`.

7. **Stale test file broke compilation after controller rewrite.** `NotificationsControllerTests.cs` still targeted the old in-memory `NotificationsController()` (parameterless constructor, sync `Create`/`GetById`/`MarkAsRead(int)`). After the controller was rewritten to use DI and async DTO-based methods, `dotnet test` failed with `CS7036`/`CS1061`. Fixed by rewriting the entire test file to construct the new controller with fakes (`FakeTenantProvider`, `FakeUserProvider`) and call the new async signatures.

8. **Missing `using` directive in a new test file.** `ProjectEventNotifierTests.cs` referenced `AuditService`, `NotificationService`, `TeamMembership`, etc. (all in `TaskBridge_API.Notifications`) without importing that namespace. Fixed with a manual one-line edit adding `using TaskBridge_API.Notifications;`.

9. **Workflow gap, not a code defect.** The full Notification & Audit Service was initially generated as chat text only, because file-editing tools were disabled (Ask mode) at that point. No code was actually written to disk. Fixed once the user switched to Agent mode and asked to "write the file now" — every proposed file was then applied via real edit/create tool calls, and `dotnet test` confirmed 20/20 passing.
