# Copilot Instructions for TaskBridge API

## Project overview
TaskBridge is a multi-service B2B SaaS platform for distributed engineering teams. This repository contains shared service boundaries and supporting documentation for the TaskBridge domain. All generated code must respect clear service ownership, secure multi-tenant isolation, and production-readiness expectations.

## Technology stack
- Primary language: C# / ASP.NET Core
- Runtime: .NET 8
- Architecture: layered service design with clear boundaries
- Persistence: Entity Framework Core / ORM-based data access
- Validation: model validation and typed DTOs
- Testing: xUnit or NUnit with real behavior tests
- Logging: structured logging with correlation context
- Security: OAuth/JWT authentication, tenant-aware authorization, least privilege

## Architectural conventions
- Keep service boundaries explicit: Project Service and Notification & Audit Service must remain separate.
- Prefer layered architecture: model -> repository -> service -> controller/endpoint.
- No raw database access in application code; use ORM abstractions.
- Use DTOs for requests and responses to avoid leaking internal domain objects.
- Keep controllers thin; business logic belongs in services.
- Keep data access, validation, and security rules explicit and reusable.
- Document public interfaces and key decisions.

## Security and compliance requirements
- Enforce tenant isolation in all reads and writes.
- Validate all user input and reject invalid or malicious payloads early.
- Never expose organization data across tenant boundaries.
- Do not log secrets, tokens, or raw sensitive payloads.
- Restrict access to internal-only endpoints and privileged operations.
- Treat audit logs as sensitive records; ensure they are immutable and access-controlled.
- Consider privacy impacts for IP addresses and other personal data before retention or logging.
- Prefer explicit authorization checks over implicit trust.

## Coding standards
- Prefer clear naming, small classes, and strong separation of concerns.
- Use async patterns for I/O-bound work where appropriate.
- Use strongly typed models and enums instead of loose stringly-typed values when possible.
- Handle errors consistently with domain-specific exceptions or result models.
- Preserve immutability where required, especially for audit entries.
- Avoid hidden side effects and avoid broad catch-all exceptions without meaningful handling.
- Write code that is easy to review and easier to test.

## Testing
- Tests live in `tests/TaskBridge-API.Tests` using xUnit.
- Run with `dotnet test` from the project root.
- Write tests for business rules, authorization, validation, and edge cases.
- Cover security-sensitive behavior explicitly: tenant access, audit immutability, and permission failures.
- Prefer real behavior tests over mock-heavy tests.
- Ensure each service has tests for happy path and failure path.
- Keep tests deterministic and independent.

## Output expectations for Copilot
- Generate production-quality code, not rushed prototypes.
- If a generated result introduces security, architectural, or validation issues, flag them and propose remediation.
- When asked to build a feature, produce realistic architecture, not a single-file shortcut.
- Include documentation for public methods, business logic, and integration assumptions.
- Maintain consistency with the project’s multi-service model.

## Review mindset
Before accepting AI-generated code, ask:
- Does this respect the tenant boundary?
- Does this expose unauthorized data?
- Is validation present?
- Is there a clear ownership boundary?
- Are the audit and security implications reviewed?
- Is the code readable and maintainable in a real sprint environment?

