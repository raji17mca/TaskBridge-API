# Tool Strategy — GitHub Copilot Usage Across This Case Study

## Feature Usage Log

| # | Where in the Case Study | Copilot Feature Used | Why This Feature (Not Another) | What Happened |
|---|---|---|---|---|
| 1 | Initial project scaffold — `.github/copilot-instructions.md`, `README.md`, `src/`, `tests/` layout | **Agent Mode** (multi-file autonomous generation) | Creating a multi-folder project structure requires actual file writes; Ask Mode is read-only and couldn't produce anything on disk. | Full layout (standards file, project skeleton, test project, docs) created in one coordinated pass and verified with `dotnet test`. |
| 2 | Setting up `.github/copilot-instructions.md` itself | **`.github/copilot-instructions.md`** (custom instructions) | This is the one mechanism that applies automatically to every later prompt/session without the rules needing to be re-pasted each time. | Every later task (remediation, Notification & Audit build) naturally followed the same tenant-isolation, DTO, and layering rules without those constraints being restated in each prompt. |
| 3 | Project Service — Review & Remediation (pasted assessment brief + "rewrite to production standards") | **Agent Mode** + **`#file`** (chat participant, attaching `ProjectService.cs`/`Project.cs`) | Needed both the exact current file content (`#file`) and coordinated multi-file rewriting (repository, DTOs, JWT auth, controller) in one pass — a single-file Edit Mode diff is too narrow for an architecture-wide change. | `REVIEW.md` produced and the Project Service fully remediated; 11/11 tests passing afterward. |
| 4 | "Validate `#sym:tenantId` and `#sym:teamId`, throw if empty" | **Edit Mode** (targeted change with diff preview, via inline chat + `#sym`) | A single, precise, localized change is faster and lower-risk reviewed as one direct diff than invoking full agent tooling for a one-line fix. | A scoped null/empty check was applied without touching unrelated code in the file. |
| 5 | "Explain the Audit controller" / "Is there implemented logging" | **Ask Mode** + **`#file`** | Pure explanation/review — no code should change; `#file` grounds the answer in the file's actual current content rather than a stale memory of an earlier version. | Got an accurate explanation that also surfaced a real gap (no controller-level access logging), with zero risk of an unwanted edit. |
| 6 | Notification & Audit Service — New Build (full spec pasted) | **Agent Mode** (after a brief detour through Ask Mode when file tools were temporarily unavailable) | ~20 new files needed to be created and verified together; Ask Mode literally could not write files at that point in the session. | The entire service was built; `dotnet test` then caught real bugs (enum-as-integer serialization, a stale test file) which were fixed in the same mode. |

*Features not used in this session (for completeness): inline ghost-text suggestions, literal slash commands (`/explain`/`/fix`/`/tests`/`/doc`), `@workspace`/`@terminal` as typed chat participants, Copilot-generated commit messages, and Quick Chat — this environment surfaces equivalent behavior through plain-language Agent/Ask mode requests instead of those specific UI affordances.

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
