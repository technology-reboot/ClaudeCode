# Brief: Online Catalog API — Full Implementation

## Product Owner

**Goal:** Build a production-grade REST API for an Online Catalog that allows authenticated clients to browse catalog items organized by categories, and for users to maintain a personal wishlist.

**Why it matters:** Provides a secure, scalable catalog backend that separates business logic from infrastructure concerns, enabling maintainability and testability as the catalog grows.

**Success definition:**
- All five API surfaces (Auth, User, Category, Catalog, Wishlist) are operational and return correct HTTP status codes and response shapes as specified.
- No unauthenticated request reaches any endpoint; all require a valid `X-Api-Key`.
- Data is persisted in SQL Server via EF Core with auto-applied migrations.
- The API is documented via Swagger in Development.
- Structured logging captures every request with `traceId`.

**Scope boundaries:**
- No front-end or admin UI.
- No email / notification services.
- No OAuth or social login — API key authentication only.
- Test projects are scaffolded with at least one representative test per layer; full 80% coverage is a post-MVP concern.

---

## Business Analyst

### User Stories

| # | As a… | I want to… | So that… |
|---|-------|-----------|---------|
| 1 | API consumer | Authenticate with an API key | My requests are authorized |
| 2 | API consumer | Create, read, update and delete users | User accounts are managed |
| 3 | API consumer | Browse and manage categories | Catalog items are organized |
| 4 | API consumer | Browse catalog items with pagination and filtering | I can display a product list efficiently |
| 5 | Authenticated user | Add catalog items to my wishlist and retrieve/remove them | I can track items of interest |

### Edge Cases & Business Rules

- **FR-AUTH-05**: Expired (`ExpiresAt` < now) or revoked (`RevokedAt` is not null) API keys are invalid → 401.
- **FR-USER-02**: Duplicate email → 409 Conflict on POST /users.
- **FR-USER-06**: User A cannot GET/PUT/DELETE User B → 403 Forbidden.
- **FR-CAT-07**: DELETE /categories/{id} when catalog items exist → 409 Conflict.
- **FR-CATALOG-10**: POST /catalog with non-existent `categoryId` → 422 Unprocessable Entity.
- **FR-WISH-05**: POST /wishlist with non-existent `catalogItemId` → 404 Not Found.
- **FR-WISH-06**: Duplicate wishlist entry (same userId + catalogItemId) → 409 Conflict.
- **FR-WISH-07**: User A cannot GET/DELETE User B's wishlist items → 403 Forbidden.
- **NFR-PERF-03**: Catalog list default page = 1, pageSize = 20, max = 100.
- **NFR-SEC-02**: API key stored as SHA-256 hash; raw key never returned after creation.
- **NFR-SEC-03**: Rate limit 100 req/min per API key; excess → 429.

### Acceptance Criteria

1. `POST /users` creates user, returns 201 with id/name/email/createdAt.
2. `GET /users/{id}` returns 200 with full profile or 404.
3. `PUT /users/{id}` returns 200 with updated profile.
4. `DELETE /users/{id}` returns 204.
5. `GET /categories` returns 200 array.
6. `POST /categories` returns 201; duplicate name → 409.
7. `DELETE /categories/{id}` with linked items → 409.
8. `GET /catalog?page=1&pageSize=20&categoryId=...&search=...` returns paginated envelope.
9. `POST /catalog` with missing category → 422.
10. `GET /wishlist` returns user-scoped list filterable by categoryId.
11. `POST /wishlist` duplicate → 409; missing item → 404.
12. Missing/invalid `X-Api-Key` → 401 on every endpoint.
13. All errors return `{ status, message, traceId }` envelope.

---

## Architect

### Solution Structure

```
d:\Shailesh\Training\Claude\dotnet-api\
├── OnlineCatalog.sln
└── src\
    ├── OnlineCatalog.Api\             (replaces ShaileshApi, or ShaileshApi refactored)
    ├── OnlineCatalog.Application\
    ├── OnlineCatalog.Domain\
    └── OnlineCatalog.Infrastructure\
```

> **Decision:** The existing `ShaileshApi` project becomes `OnlineCatalog.Api`. Three new class-library projects are created for Application, Domain, and Infrastructure. All are added to a single `.sln`.

### Layer Responsibilities

| Layer | Key Contents |
|-------|-------------|
| **Domain** | `User`, `ApiKey`, `Category`, `CatalogItem`, `WishlistItem` entities; repository interfaces `IUserRepository`, `ICategoryRepository`, `ICatalogItemRepository`, `IWishlistRepository`, `IApiKeyRepository`; domain exceptions (`NotFoundException`, `ConflictException`, `ForbiddenException`, `UnprocessableException`) |
| **Application** | MediatR commands & queries per feature; FluentValidation validators; AutoMapper profiles; DTOs (request/response models); `ICurrentUserService` interface |
| **Infrastructure** | `AppDbContext`; EF Core entity configurations; `IApiKeyRepository` / `ICategoryRepository` / etc. concrete implementations; `ApiKeyAuthenticationHandler`; `CurrentUserService`; `DependencyInjection` extension |
| **API** | Thin controllers calling `_mediator.Send()`; `GlobalExceptionMiddleware`; `ApiKeyAuthenticationScheme` registration; Serilog setup; `Program.cs` |

### NuGet Dependencies

| Project | Packages |
|---------|---------|
| Domain | _(none — pure C#)_ |
| Application | `MediatR`, `FluentValidation`, `AutoMapper` |
| Infrastructure | `Microsoft.EntityFrameworkCore.SqlServer`, `Microsoft.EntityFrameworkCore.Tools`, `AutoMapper.Extensions.Microsoft.DependencyInjection`, `MediatR.Extensions.Microsoft.DependencyInjection` (via MediatR 12+), `Microsoft.AspNetCore.Authentication` |
| API | `Serilog.AspNetCore`, `Serilog.Sinks.Console`, `Swashbuckle.AspNetCore`, `Asp.Versioning.Mvc`, `Microsoft.AspNetCore.RateLimiting` |

### Authentication Design

- Custom `ApiKeyAuthenticationHandler` (scheme name: `"ApiKey"`) reads `X-Api-Key` header.
- Hashes the key (SHA-256) → looks up `ApiKeys` table → validates `ExpiresAt` and `RevokedAt`.
- On success, sets `ClaimTypes.NameIdentifier` = `ApiKey.UserId` so all controllers can resolve the caller.
- All controllers decorated with `[Authorize(AuthenticationSchemes = "ApiKey")]`.
- `POST /users` is the only unauthenticated endpoint (user self-registration requires no prior key).

### Data Model (EF Core)

Entities map directly to spec §5. Key points:
- `WishlistItem` has composite unique index `(UserId, CatalogItemId)`.
- `CatalogItem.Price` stored as `decimal(18,2)`.
- All `CreatedAt` / `UpdatedAt` columns are `DateTimeOffset`.

### Caching

- `IMemoryCache` used in `GetCategoriesQueryHandler` and `GetCatalogItemsQueryHandler`.
- Cache invalidated on any write to the affected collection.
- Default TTL: 60 s (configurable via `appsettings.json`).

### Rate Limiting

- ASP.NET Core built-in `AddRateLimiter` with a fixed-window policy (100 req/60 s) keyed on `X-Api-Key` header value.

### Alternatives Considered

| Option | Ruled out because |
|--------|------------------|
| Redis distributed cache | Overkill for a single-node training API; IMemoryCache is sufficient |
| JWT Bearer instead of API key | Spec is explicit about X-Api-Key scheme |
| Minimal APIs (no controllers) | Clean Architecture spec calls for controllers; easier to decorate with `[Authorize]` |

### Risks

- SQL Server must be reachable at the connection string in `appsettings.json`; if unavailable, dev can use LocalDB or `(localdb)\mssqllocaldb`.
- EF Core auto-migration on startup risks production data loss if used carelessly; acceptable for this training scope.

---

## UX Designer

This is a pure REST API — no browser UI. UX concerns are API ergonomics:

- **Error envelope consistency**: every 4xx/5xx returns `{ status, message, traceId, errors? }`. The `errors` object (field-level) is only present on 400 and 422.
- **Empty states**: `GET /catalog` with no results returns `200` with `{ items: [], totalCount: 0, ... }` — never 404.
- **Pagination guards**: `pageSize` clamped to max 100 server-side to prevent oversized responses.
- **Wishlist filter**: `GET /wishlist?categoryId=...` returns `200` with filtered (potentially empty) items — never 404 for an empty result.

---

## Technical Lead

### Implementation Sequence

The sequence avoids blockers by building inward-to-outward:

1. **Solution scaffold** — create `.sln`, four projects, add project references, install NuGet packages.
2. **Domain layer** — entities, repository interfaces, domain exceptions.
3. **Application layer** — DTOs, AutoMapper profiles, MediatR commands/queries + handlers (no EF references), FluentValidation validators.
4. **Infrastructure layer** — `AppDbContext`, EF entity configs, repository implementations, `ApiKeyAuthenticationHandler`, `CurrentUserService`.
5. **API layer** — `Program.cs` wiring, controllers, global exception middleware, Serilog, Swagger.
6. **EF migrations** — `InitialCreate` migration.
7. **Test projects** — scaffold `UnitTests` with representative handler tests.

### Definition of Done

- [ ] All 5 controllers with correct routes and HTTP verbs.
- [ ] `X-Api-Key` authentication rejects missing/invalid keys with 401.
- [ ] Pagination works for `GET /catalog`.
- [ ] Category delete conflict returns 409.
- [ ] Catalog item create with bad categoryId returns 422.
- [ ] Wishlist duplicate returns 409.
- [ ] Global exception middleware returns 500 without stack trace.
- [ ] Swagger UI accessible at `/swagger` in Development.
- [ ] EF migration generated.
- [ ] At least one unit test per command handler.

### Parallelization

Not recommended — each layer depends on the previous. Single-agent sequential implementation.

---

## Trade-offs

| Trade-off | Decision |
|-----------|---------|
| Full test coverage (80%) vs. scaffolded tests | Scaffold representative tests only; coverage target is post-MVP |
| Redis vs. IMemoryCache | IMemoryCache — simpler, no external dependency for training |
| Separate `POST /users/register` vs. open `POST /users` | `POST /users` is unauthenticated (self-service registration) per spec |
| Scalar vs. Swashbuckle | Swashbuckle — more familiar, spec mentions both |

---

## Open Questions

| # | Question | Owner | Status |
|---|----------|-------|--------|
| 1 | Should `POST /users` also return an API key so the caller can immediately authenticate? | User | **Unresolved** — current brief does not include this; raise if needed |
| 2 | SQL Server connection string (LocalDB vs full instance)? | User | Defaulting to `(localdb)\mssqllocaldb` in `appsettings.Development.json` |

---

## Test Data

### Required seed states

To exercise every distinct flow, the following records must exist (or be creatable via the API):

| State | How to create |
|-------|--------------|
| Valid, non-expired API key linked to User A | Seed via `HasData` or manual SQL insert |
| Expired API key (ExpiresAt in the past) | Manual SQL insert with `ExpiresAt = GETUTCDATE()-1` |
| Revoked API key | Manual SQL insert with `RevokedAt = GETUTCDATE()` |
| At least one Category | POST /categories |
| At least one CatalogItem in that Category | POST /catalog |
| A second User B with their own API key | POST /users + manual key insert |
| WishlistItem for User A | POST /wishlist |

### Manual Test Checklist

- [ ] Request without `X-Api-Key` header → 401
- [ ] Request with revoked key → 401
- [ ] Request with expired key → 401
- [ ] POST /users with duplicate email → 409
- [ ] GET /users/{userId-of-user-B} while authenticated as User A → 403
- [ ] DELETE /categories/{id} with linked items → 409
- [ ] POST /catalog with non-existent categoryId → 422
- [ ] GET /catalog?page=2&pageSize=5 → correct pagination envelope
- [ ] POST /wishlist with same catalogItemId twice → 409 on second call
- [ ] GET /wishlist?categoryId={id} → only items in that category returned
- [ ] Exceed 100 requests/min → 429

### Minimum validation path (no SQL Server)

1. `dotnet build` — confirms all layers compile.
2. `dotnet test` — runs unit tests against in-memory repositories.
3. Code review of handlers for correct status-code exceptions.

---

## Tester

_(To be completed after implementation.)_

---

## Approval

**Status:** Pending user approval
**Date:** —
