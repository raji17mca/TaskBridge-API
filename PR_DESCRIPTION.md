# PR Description — Project Service Remediation + Notification & Audit Service

## Summary

This PR does two things. First, it remediates the AI-generated Project Service that was committed without review: it replaces a client-spoofable `X-Tenant-Id` header with JWT-claim-based tenant resolution, introduces a proper repository layer, typed request/response DTOs, structured logging, and centralized exception handling (full findings in [REVIEW.md](REVIEW.md)). Second, it builds a new Notification & Audit Service on top of that remediated foundation, which writes an immutable audit entry and dispatches notifications to team members on every project create/status-update/delete. Together these give TaskBridge a compliance-grade audit trail and a real notification mechanism, replacing what was previously unreviewed, tenant-unsafe, single-file logic with a layered, tenant-isolated, tested design. See [ARCHITECTURE.md](ARCHITECTURE.md) for the full data-flow and design-decision writeup.

## AI Tool Disclosure

**Copilot features used:** Agent mode (multi-file, tool-using chat — file creation/edits + running `dotnet build`/`dotnet test`), Ask mode (read-only chat for explanation/review), Edit mode / inline chat with `#sym` symbol references for narrow single-line fixes, `#file` attachments to ground reviews in actual current file content, the clarifying-questions mechanism before consequential/hard-to-reverse decisions (layout choices, the git-history rewrite question), and `.github/copilot-instructions.md` as persistent custom instructions applied automatically across the whole session. This environment doesn't expose literal VS Code slash-commands (`/explain`, `/fix`, `/tests`, `/doc`) as separate invocable commands — equivalent behavior (explain code, fix an issue, generate tests, add docs) was achieved via plain natural-language requests instead.

**Mode used most:** Agent mode, for essentially all code generation and modification — scaffolding, the Project Service remediation, and the Notification & Audit Service build all required real file writes plus an immediate `dotnet test` verification loop, which Ask mode can't do. Ask mode was used specifically for explanation/review-only turns (e.g. "Explain the Audit controller," "Is there implemented logging").

**Accepted as-is vs. overridden:**
- Accepted largely as generated: DTO shapes, repository interfaces, EF Core entity configuration, and most of `AuditService`/`NotificationService`'s business logic.
- Overrode/modified: the client-supplied `X-Tenant-Id` header (replaced entirely with JWT claims — see REVIEW.md issue #1), missing `ProjectStatus` enum validation (added `Enum.IsDefined`), the audit snapshot's default enum-as-integer JSON serialization (added `JsonStringEnumConverter`), a stale test file left targeting an old controller constructor after a rewrite, a missing `using` directive, and the introduction of `IProjectEventNotifier` as an explicit seam between the two services (a design choice, not a bug fix).

**AI-generated vs. hand-written estimate:** ~90% AI-generated as first-pass content, ~10% direct manual correction (the specific fixes listed above) — though effectively 100% of the final code was reviewed, prompted, or corrected by a human before being accepted; very little was "generated and merged with zero scrutiny."

**Did `.github/copilot-instructions.md` improve quality/consistency?** Yes, concretely. Without it, the original Project Service generation had no tenant-isolation awareness, no logging convention, and no DTO convention. Once the instructions file existed, the remediation and the entire new Notification & Audit Service naturally followed the same "no raw `DbContext` outside a repository," "always filter by `tenantId` in the query itself," and "typed DTOs only over HTTP" patterns without those constraints having to be repeated in every prompt.

## Service Integration

`IProjectEventNotifier.NotifyAsync(tenantId, actorUserId, eventType, project, previousState, newState)` is the contract between the two services. `ProjectService` calls it after every successful create/status-update/delete. Today it's an in-process call (single deployable); if the Notification & Audit Service is ever split out, this is the one call site that becomes an HTTP call to `POST /api/audit` instead — see [ARCHITECTURE.md](ARCHITECTURE.md).

## Testing

**20 tests** across `ProjectServiceTests`, `ProjectsControllerTests`, `NotificationsControllerTests`, and `ProjectEventNotifierTests`. Coverage includes: happy-path CRUD, cross-tenant isolation (adversarial 404s, never a revealing 403), input validation failures (empty tenant/team ids, blank name, invalid status enum), audit immutability (asserted structurally — `IAuditRepository` has no Update/Remove method), notification fan-out to all team members, audit history filtering by date range and event type, and notification mark-as-read ownership checks.

**Known gaps:**
- No integration/end-to-end tests against the real SQLite database or the actual HTTP pipeline — all tests use EF Core InMemory and instantiate controllers directly, which bypasses `[Authorize]`/middleware entirely.
- No test exercises the `InternalService` authorization policy actually rejecting a non-internal caller (direct controller instantiation skips the auth pipeline).
- No test for JWT validation itself (expiry, signature tampering) since no real identity provider is wired up yet.
- No migration/schema-evolution test — `Database.EnsureCreated()` is used instead of EF Core migrations (tracked in REVIEW.md).

## Risks & Trade-offs

The audit trail's immutability is enforced structurally (no delete/update path exists), but this creates a genuine, unresolved conflict with data-subject erasure rights once IP addresses or other PII are added to audit entries (see IMPACT_ANALYSIS.md) — an immutable-by-design compliance record and "right to be forgotten" pull in opposite directions, and this PR doesn't resolve that tension, only documents it. Separately, `ProjectEventNotifier` calls `AuditService` and `NotificationService` sequentially with no failure isolation: if the audit write succeeds but notification dispatch throws, the exception currently propagates back to the original `POST /api/projects` caller as a 500, even though their project was actually created successfully — this may or may not be the desired behavior and should be confirmed with the team before shipping.

## Self-Review Checklist

- [x] No hardcoded secrets or PII in code — the JWT signing key is a documented dev-only placeholder in `appsettings.Development.json`, explicitly flagged as needing a real secret manager in non-dev environments.
- [x] All inputs validated — DataAnnotations on DTOs plus service-layer defense-in-depth checks (tenant id, team id, name, status enum) that don't rely solely on HTTP-layer validation.
- [x] Error handling uses specific exceptions — `ArgumentException`, `TenantResolutionException`, and `UserResolutionException` are each mapped to distinct HTTP status codes by `ExceptionHandlingMiddleware`, not a single catch-all.
- [x] Code follows `.github/copilot-instructions.md` standards — layered architecture, DTOs, tenant isolation, and structured logging are all present; REVIEW.md documents specifically where the original AI-generated version didn't and how each was fixed.
- [x] All Copilot suggestions reviewed before accepting — see REVIEW.md and PROMPTS.md's "Post-Generation Corrections" for the specific issues caught and fixed rather than merged as-is.
- [x] Tests cover happy path, edge cases, and error scenarios — see Testing section above.
- [x] Used explanation requests on any code block not fully understood — done throughout (e.g. reviewing `AuditController`, `ProjectEventNotifier`, and the DbContext configuration before accepting them) — no unfamiliar inherited code remained unexplained before merge.

## Peer Review Simulation

| # | Code Location | What Should Change | Why (benefit or risk) |
|---|---|---|---|
| 1 | `src/notifications/AuditController.cs` — `RecordEvent` and `GetHistory` actions | Add `_logger.LogInformation` calls logging the caller's tenant and which project's history was accessed (never the payload contents) | For a controller whose entire purpose is auditing, "who looked at whose audit trail" is itself audit-relevant information — right now it's invisible, which undermines the compliance value of the feature we just built. |
| 2 | `src/notifications/ProjectEventNotifier.cs` — `NotifyAsync` | Decide and implement explicit failure handling between the `_auditService.RecordEventAsync` call and the `_notificationService.DispatchAsync` call (e.g. catch/log notification failures instead of letting them propagate) | Currently, if the audit write succeeds but notification dispatch throws, the exception propagates back to the original `POST /api/projects` caller as a 500 — even though their project was actually created successfully. Callers get a false failure signal for something that worked. |
| 3 | `src/notifications/TeamMembershipRepository.cs` — `GetTeamMemberUserIdsAsync` (**AI blind spot**) | Add a tracking ticket/comment noting there is no endpoint or service anywhere in this PR that populates `TeamMemberships` in a real environment — our tests only get non-empty results because they insert rows directly via `dbContext.TeamMemberships.Add(...)` | As shipped, notification fan-out will silently return zero recipients in every real deployment until a Team Service/endpoint exists to write to this table. The code we wrote is correct and fully tested, but it has no real-world path to non-empty data — an easy gap to miss precisely because nothing here is actually broken. |

## 6A. Why the AI Blind Spot Comment Gets Missed

Comment #3 is a **cross-boundary data-lifecycle gap**, not a defect in any single function — `GetTeamMemberUserIdsAsync` correctly queries `TeamMemberships`, and its test correctly proves that query works, so every artifact an AI tool evaluates in isolation (the method, its test, the diff) looks complete and passes. The problem only exists when you ask a question no individual file answers: "in a real deployment, what actually writes to this table?" That requires reasoning about the *system* the code lives in, including services and tickets that don't exist yet in this repository, not just the code that does. AI code review tools are fundamentally comparing a diff against patterns and correctness within its own scope; a human reviewer who knows "we never built a Team Service" catches this because they're holding the product roadmap in their head, not just the file tree.
