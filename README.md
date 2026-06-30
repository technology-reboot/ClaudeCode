# ClaudeCode — A Claude Code Capability Showcase

This repository exists to demonstrate **Claude Code's spec-driven, multi-persona development workflow** in practice — not just to ship an API. The generated code (`src/OnlineCatalog.*`) is the *output*; the real subject of this repo is the **process artifacts** that produced it: a reusable organizational governance file, a fully fleshed-out product brief, and a complete technical specification, all authored as part of a single Claude Code engagement.

If you're here to see what disciplined, governed agentic coding looks like end-to-end — request → governance → brief → approval → spec → implementation → tests — start with the three markdown files below.

## The Three Documents That Drove This Build

### 1. `OnlineCatalogApi/CLAUDE-ORG-GUIDANCE.md` — the governance layer

A portable, project-agnostic template (designed to be copied into any new project and referenced from its `CLAUDE.md`) that defines **how Claude Code is allowed to work**, independent of what it's building. Highlights:

- **Seven personas, always in order** — Product Owner → Business Analyst → Architect → UX Designer → Technical Lead → Developer → Tester. No persona is ever skipped, regardless of task size, including one-line fixes and config edits.
- **The brief is a hard gate, not a formality.** Before any file is touched, three conditions must all be true: the brief exists on disk at a defined path, the user has explicitly approved it (silence or a related question is not approval), and a properly named branch has been checked out.
- **A fixed brief structure** every feature must follow — Product Owner, Business Analyst, Architect, UX Designer, Technical Lead, Trade-offs, Open Questions, Test Data, Tester, Approval — so every persona's reasoning is captured in one place and traceable later.
- **A Tester persona with two distinct jobs**: pre-implementation, it audits whether the brief's states are actually testable and what seed data is missing (a brief that can't be tested is incomplete); post-implementation, it validates the build against the Business Analyst's acceptance criteria and routes failures back to the right persona — a logic bug to the Developer, a design flaw to the Architect, a missing requirement to the BA.
- **A Gitflow branching model** (`main` / `develop` / `feature/` / `fix/` / `hotfix/` / `docs/` / `chore/`) with an explicit rule: never commit directly to `main` or `develop`.
- **Opt-in multi-agent parallelization** — Developer and Tester work can fan out across isolated git worktrees for independent units of work, but only when proposed in the brief and explicitly approved by the user; the planning personas (Product Owner through Technical Lead) always run as a single coherent voice and are never fragmented across agents.

This is the file that turns Claude Code from "writes code on request" into something closer to a governed engineering process with checks, approvals, and accountability built in.

### 2. `OnlineCatalogApi/docs/briefs/online-catalog-api.md` — the brief in action

A complete, filled-out implementation brief for the Online Catalog API, following the exact structure mandated by the governance file. It captures the full reasoning chain for a real feature:

- **Product Owner** — goal, why it matters, an explicit success definition (five API surfaces operational, no unauthenticated request reaches any endpoint, EF Core migrations applied automatically, Swagger documented, structured logging with `traceId`), and clear scope boundaries (no UI, no OAuth, no email service).
- **Business Analyst** — five user stories, eleven edge-case/business-rule entries cross-referenced to spec requirement IDs (e.g. `FR-USER-02`, `NFR-SEC-03`), and thirteen concrete, testable acceptance criteria.
- **Architect** — the chosen solution structure (`OnlineCatalogApi` → `OnlineCatalog.Api` plus three new class libraries), a full layer-responsibility table, NuGet dependencies per layer, the API-key authentication design, the EF Core data model decisions, the caching and rate-limiting approach, **and a documented "Alternatives Considered" table** (Redis vs. in-memory cache, JWT vs. API key, minimal APIs vs. controllers — each with a stated reason it was ruled out), plus explicit risks.
- **UX Designer** — even for a pure REST API, this persona reasons about API ergonomics: a consistent error envelope, empty-list states returning `200` rather than `404`, and server-side pagination guards.
- **Technical Lead** — an inward-to-outward implementation sequence (Domain → Application → Infrastructure → API → migrations → tests), a ten-item Definition of Done checklist, and an explicit call that parallelization is *not* recommended here because each layer depends on the previous one.
- **Trade-offs** — four explicit, named trade-offs (e.g. scaffolded tests now vs. 80% coverage later) with the reasoning for each decision kept visible rather than buried.
- **Open Questions** — unresolved items tracked with an owner and status rather than silently assumed away.
- **Test Data** — the seed states required to exercise every code path (valid/expired/revoked API keys, a second user for cross-user authorization tests, etc.), an 11-item manual test checklist, and a minimum validation path for when the full stack isn't available.
- **Approval** — a status field, left as "Pending user approval" in this snapshot, showing the gate the governance file requires before implementation can begin.

### 3. `OnlineCatalogApi/SPEC.MD` — the technical specification

The detailed technical spec the brief and implementation were built against: full functional requirements (`FR-AUTH`, `FR-USER`, `FR-CAT`, `FR-CATALOG`, `FR-WISH`, `FR-ERR`) and non-functional requirements (`NFR-PERF`, `NFR-SEC`, `NFR-REL`, `NFR-MAIN`) as ID'd, testable tables; complete request/response JSON schemas for every endpoint; the common error envelope contract; and full data model definitions (columns, types, constraints, and the entity-relationship summary) for every table. Every requirement in this document maps directly to a MediatR command/query, a controller endpoint, or a domain rule — by design, so Claude Code (or any engineer) can trace from spec line to implementation file.

## What Got Built From This Process

Following the brief and spec, Claude Code produced a working **.NET 8 Clean Architecture** REST API under `src/`:

```
OnlineCatalog.sln
├── src/
│   ├── OnlineCatalog.Api/             # Thin controllers, middleware, Program.cs
│   ├── OnlineCatalog.Application/     # MediatR commands/queries, FluentValidation, AutoMapper, DTOs
│   ├── OnlineCatalog.Domain/          # Entities, repository interfaces, domain exceptions — pure C#
│   └── OnlineCatalog.Infrastructure/  # EF Core, repositories, API key auth handler
└── tests/
    └── OnlineCatalog.UnitTests/       # xUnit + Moq + FluentAssertions
```

**Implemented surfaces:** Users (CRUD), Categories (CRUD with delete-conflict protection), Catalog items (CRUD with pagination, category filtering, and search), and a per-user Wishlist (add / list / remove) — all secured behind a custom `X-Api-Key` authentication handler, rate-limited at 100 requests/minute per key, versioned via `Asp.Versioning`, documented through Swagger, and logged through Serilog.

This is exactly the traceability the spec promised: every controller route, every `409`/`422`/`401` business rule, and every entity in the database maps back to a numbered requirement in `SPEC.MD` and an acceptance criterion in the brief.

> Note: `OnlineCatalogApi/Program.cs` and `OnlineCatalogApi/appsettings.json` are preserved in their original, unmodified state — the default ASP.NET Core Web API scaffold (complete with the template `WeatherForecastController`) that this project started from, before the Clean Architecture solution under `src/` was generated. They're left in place intentionally, as the "before" half of the before/after story this repo tells.

## Build & Run the Generated API

```bash
git clone https://github.com/technology-reboot/ClaudeCode.git
cd ClaudeCode

dotnet restore OnlineCatalog.slnx
dotnet build OnlineCatalog.slnx
dotnet run --project src/OnlineCatalog.Api
```

Requires .NET 8 SDK and a reachable SQL Server instance (LocalDB works by default — see `appsettings.json`). Migrations are applied automatically on startup; Swagger UI is available at `/swagger` in Development, with the `X-Api-Key` scheme pre-wired for testing.

```bash
dotnet test OnlineCatalog.slnx
```

## Why This Is Useful as a Reference

Most "AI wrote this code" demos show only the output. This repo is structured to show the **governance and reasoning that preceded the output** — the kind of artifact trail (brief → approval gate → spec → traceable implementation → tests) that makes agentic coding viable in a real engineering organization rather than just a fast way to generate code. It's used in the Technology Reboot Agentic AI / Claude Code training specifically to walk through that process document-by-document.

## About

Part of the **Technology Reboot** Agentic AI training series.
