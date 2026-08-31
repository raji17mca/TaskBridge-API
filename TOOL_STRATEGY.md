# Tool Strategy — GitHub Copilot Usage Across This Case Study

## Feature Usage Log

| # | What I Used | Why This Feature (Not Another) | What Happened |
|---|---|---|---|
| 1 | **Agent mode** (Chat + file/terminal tools) | Needed to create/edit many files across the workspace and immediately verify with `dotnet test` — Ask mode can't touch files, and Edit mode's single-file inline diff is too narrow for multi-file architectural work (e.g. repository + DTOs + controller + DI wiring in one pass). | Used for the initial project scaffold, the Project Service remediation, and the full Notification & Audit Service build. Every pass ended with a real `dotnet test` run, not just generated code assumed to work. |
| 2 | **Ask mode** (Chat, no file/terminal tools) | Appropriate when the goal was explanation or review, not modification — e.g. "Explain the Audit controller," "Is there implemented logging," "How is model validation implemented." Keeping it read-only avoids the model silently "fixing" things while I'm still trying to understand them. | Got direct, accurate explanations grounded in the actual current file (via `#file`). One side effect: mid-session, tools were unavailable in this mode when I needed to build the Notification & Audit Service — see Limitation #3 below. |
| 3 | **Edit mode (inline chat)** + **`#sym`** symbol reference | For a single, narrow, well-defined change ("validate `#sym:tenantId` and `#sym:teamId`, throw if empty"), inline chat scoped to exact symbols is faster and lower-risk than invoking full agent tooling for a one-line fix. | Applied a scoped null/empty check without touching unrelated code in the file. |
| 4 | **`#file` attachments** | Used repeatedly (`ProjectService.cs`, `Project.cs`, `AuditController.cs`) to ground reviews/explanations in the actual current file content instead of relying on memory of an earlier version — important since files were edited many times across the session. | Caught real, current issues (missing enum validation, missing controller-level access logging) rather than commenting on stale assumptions. |
| 5 | **Clarifying-questions tool** (ask-before-acting) | Used before consequential, hard-to-reverse decisions: feature-folders vs. separate class-library projects for the module layout, where the new layout should live, what the "dependency file" should be, and — critically — before rewriting already-pushed git history. | Got explicit direction each time instead of guessing, avoiding wasted rework on a wrong structural assumption (and avoiding an unapproved destructive git operation). |
| 6 | **Terminal tool** (`dotnet build` / `dotnet test`) | LLM-generated code can look syntactically plausible and still fail to compile or misbehave; only an actual build/test run proves it works. Ran after essentially every code-generation pass. | Caught several real, concrete bugs this way: a nested test project being silently compiled twice by the main project's file glob, an audit snapshot serializing enums as raw integers instead of readable strings, and a stale test file that no longer matched a rewritten controller's constructor signature. |
| 7 | **Codebase search/read tools** (`grep_search`, `file_search`, `read_file`) | Used to verify actual current file/repo state before editing or before any git operation, rather than trusting assumptions. | Discovered the git repository root was nested one folder deeper than expected, and that a stray PDF file had been accidentally committed alongside source code — neither would have surfaced without checking directly. |
| 8 | **Batched/parallel file creation** (multiple `create_file`/multi-replace calls in one turn) | When building the Notification & Audit Service, ~20 new files were independent of each other (models, DTOs, repositories, services, controller) — batching them was faster and clearer than one file per turn with no added risk, since none of them depended on reading each other's just-edited state first. | The entire new service layer was created in two batched tool-call rounds instead of twenty sequential ones. |

## Scenario Responses

**1. Understanding a complex 600-line legacy service in an unfamiliar codebase before wiring a new service to it.**
I'd use **Copilot Chat in Ask mode with `@workspace`/codebase-wide semantic search** (not Edit or Agent — no code should change yet). Asking targeted questions ("what are this service's public entry points and their callers?", "what does it assume about the caller's identity?") lets Copilot trace cross-file references and DI wiring that a manual top-to-bottom read might miss, and staying in a read-only mode guarantees exploration can't accidentally mutate the code I'm still trying to understand.

**2. Generating consistent, standards-compliant request-validation middleware across 10 existing route handlers.**
I'd use **Agent mode** with a single **constraint + few-shot prompt**: point at one already-correct handler as the pattern to match, then apply it across all 10 in one coordinated batch rather than handler-by-handler. Doing it as one pass with a shared instruction and shared context prevents the kind of drift you get from repeating a similar-but-not-identical prompt ten separate times.

**3. Quickly verifying whether a JWT verification implementation correctly handles token expiry and signature tampering.**
I'd use the **`/tests` (test-generation) feature** to generate unit tests that construct actually-expired and actually-tampered tokens and assert rejection — not just ask Copilot to "read the code and confirm it's correct." A generated test either passes or fails against real `TokenValidationParameters` behavior, which is concrete verification instead of a plausible-sounding but unverified textual opinion.

**4. Enforcing that all commits to main pass linting and test coverage thresholds automatically, with no human intervention.**
This isn't a Chat-time behavior at all — it's a CI/CD policy. I'd use **Agent mode to scaffold the GitHub Actions workflow YAML** (running `dotnet build`/`dotnet test`/coverage, wired to branch protection rules), then rely on GitHub's branch protection settings (not Copilot itself) to actually block merges. Copilot's role here is generating the automation, not being the enforcement mechanism.

**5. Reviewing a contractor's AI-generated service module for security vulnerabilities before it reaches staging.**
I'd use **Ask mode with `#file`**, deliberately *not* Agent mode, framed as a **role-based, constraint-driven prompt** ("review this as a security-focused reviewer against our tenant-isolation and OWASP API Top 10 rules — list issues, don't fix them yet"). Keeping it read-only for the first pass means the review surfaces every issue for a human decision before anything gets silently "corrected" — exactly what I did with the Project Service review in this session.

**6. Ensuring Copilot follows multi-tenant data isolation rules consistently across all developers and sessions.**
I'd rely on **`.github/copilot-instructions.md`** (persistent custom repo instructions), not a per-session reminder. It's the one mechanism that's automatically applied to every developer's session without anyone needing to remember to paste the rules each time — which is precisely why this repo has one with an explicit "Enforce tenant isolation in all reads and writes" rule.

## Limitations Encountered

**1. A security vulnerability that "looked handled" because of its own comment.**
- **Prompted:** "Generate a Project model and a Project service with create, update status, get by team, and delete functions. Use a database." (run deliberately as-is, unconstrained).
- **What went wrong:** Copilot trusted a client-supplied `X-Tenant-Id` HTTP header as the caller's tenant identity — a complete multi-tenant isolation bypass — and added a comment describing it as a "placeholder until JWT-based tenant claims are wired up." The comment made the code *look* like a tracked, intentional gap rather than a live, shippable vulnerability.
- **How I detected it:** Manual review against the repo's own `copilot-instructions.md` security rules, not anything Copilot flagged unprompted.
- **How I fixed it:** A follow-up remediation prompt replacing the header with `ICurrentTenantProvider` reading a validated JWT claim.
- **What I'd do differently:** Never accept an AI-generated identity/auth mechanism without a deliberate adversarial pass ("how would an attacker abuse this specific line?"), especially when a comment makes it look already accounted for.

**2. Missing input validation that wasn't caught until explicitly asked to look.**
- **Prompted:** Same low-effort generation as above.
- **What went wrong:** `UpdateStatusAsync` accepted any `int` as a `ProjectStatus` with no check that it was a defined enum value — an out-of-range status could be silently persisted.
- **How I detected it:** Only surfaced via a later, explicit "fix any issues if you found in this file" review prompt — Copilot did not proactively flag it during the original generation or during unrelated follow-up work.
- **How I fixed it:** Added an `Enum.IsDefined` check, mapped to a 400 response by the exception middleware.
- **What I'd do differently:** Treat "does this endpoint validate every field of its request body, including enums and enum-like values?" as a standing checklist item for any new endpoint, rather than something to be caught only if I happen to ask for a general review pass.

**3. Generated code existed only as chat text because file-write tools were unavailable in that mode.**
- **Prompted:** "Complete the below requirement for Notification & Audit Service — New Build" (the full spec pasted in).
- **What went wrong:** This wasn't incorrect code — it was a tooling/environment limitation. The session was in a mode without file-editing tools, so a complete, correct implementation was produced as chat text but never actually written to the repository, creating a risk of mistaking "Copilot described the solution" for "the solution exists in the codebase."
- **How I detected it:** The repository tree didn't reflect any of the described changes; the user had to explicitly say "I've switched to agent mode, can you write the file now."
- **How I fixed it:** Re-applied the exact same plan once file-write tools were available, then ran `dotnet test` to confirm it actually compiled and passed (it hadn't been verified at all while it only existed as text).
- **What I'd do differently:** Explicitly confirm whether the current mode can write files *before* promising a large multi-file deliverable, rather than generating the full solution first and discovering the gap afterward.
