# Technical Specification – Online Catalog

## 1. Overview

A production-grade REST API built with **.NET 8** following **Clean Architecture** principles. Business logic is fully isolated in the Domain and Application layers. Infrastructure concerns (database, auth, external services) are wired up in the Infrastructure layer. The API layer is thin — controllers delegate to MediatR.

A set of APIs where authenticated requests can fetch all items from catalog, view a single item or fetch categories where each item from the catalog belongs to. APIs will also allow users to save the item as a wishlist and can be fetched as per the categories. Catalog data will be stored in a relational database.

### Core Features

- Secure APIs that cannot be invoked without API key
- User API
- Category API
- Catalog (list and item view). Each Catalog item will belong to a category)
- Wishlist 

### Tech

- .NET 8
- C#
- Entity Framework
- Serilog
- SQL Server

## 2. Architecture

### 2.1 High-Level Architecture

## Solution structure

```
MyApp.sln
├── src/
│   ├── MyApp.Api/               # Controllers, middleware, filters, Program.cs
│   ├── MyApp.Application/       # CQRS commands/queries, handlers, validators, DTOs
│   ├── MyApp.Domain/            # Entities, value objects, domain events, repo interfaces
│   └── MyApp.Infrastructure/    # EF Core, repositories, identity, external services
└── tests/
    ├── MyApp.UnitTests/
    ├── MyApp.IntegrationTests/
    └── MyApp.FunctionalTests/
```
### Layer responsibilities

| Layer | Project | Rule |
|---|---|---|
| API | `MyApp.Api` | Thin controllers only. Calls `_mediator.Send()`. No business logic. |
| Application | `MyApp.Application` | CQRS handlers, FluentValidation, AutoMapper. No EF Core references. |
| Domain | `MyApp.Domain` | Pure C#. Zero NuGet dependencies. Owns all business rules. |
| Infrastructure | `MyApp.Infrastructure` | EF Core, repositories, auth, 3rd-party services. |

**Dependency rule**: dependencies point inward only. Infrastructure → Application → Domain. Domain has no outward dependencies.

---

## Key libraries

| Purpose | Library |
|---|---|
| CQRS / Mediator | MediatR |
| Validation | FluentValidation |
| Object mapping | AutoMapper |
| ORM | Entity Framework Core 8 |
| Auth | ASP.NET Core Identity + JWT Bearer |
| Logging | Serilog (structured) |
| API docs | Swashbuckle / Scalar |
| Resilience | Microsoft.Extensions.Http.Resilience (Polly) |
| Caching | IDistributedCache (Redis) or IMemoryCache |
| API versioning | Asp.Versioning |
| Testing | xUnit + Moq + FluentAssertions + Testcontainers |

---


## 3. Functional Requirements

Here are the functional requirements for your spec file:

---

## Functional Requirements

### FR-AUTH: API Key Authentication

| ID | Requirement |
|----|-------------|
| FR-AUTH-01 | All API endpoints MUST reject requests that do not include a valid API key. |
| FR-AUTH-02 | The API key MUST be supplied via the `X-Api-Key` HTTP request header. |
| FR-AUTH-03 | Requests with a missing or invalid API key MUST return `401 Unauthorized`. |
| FR-AUTH-04 | API keys MUST be validated against a persistent store before granting access. |
| FR-AUTH-05 | Revoked or expired API keys MUST be treated as invalid. |

---

### FR-USER: User API

| ID | Requirement |
|----|-------------|
| FR-USER-01 | The system MUST allow creation of a new user with at minimum: name, email, and password. |
| FR-USER-02 | Email addresses MUST be unique across all users. |
| FR-USER-03 | The system MUST allow retrieval of a user's own profile via `GET /users/{id}`. |
| FR-USER-04 | The system MUST allow updating a user's profile fields (e.g., name, email). |
| FR-USER-05 | The system MUST allow deletion of a user account. |
| FR-USER-06 | A user MUST NOT be able to access or modify another user's profile. |

---

### FR-CAT: Category API

| ID | Requirement |
|----|-------------|
| FR-CAT-01 | The system MUST allow retrieval of all categories via `GET /categories`. |
| FR-CAT-02 | The system MUST allow retrieval of a single category by its ID via `GET /categories/{id}`. |
| FR-CAT-03 | Each category MUST have at minimum: a unique ID, a name, and an optional description. |
| FR-CAT-04 | The system MUST allow creation of a new category via `POST /categories`. |
| FR-CAT-05 | The system MUST allow updating an existing category via `PUT /categories/{id}`. |
| FR-CAT-06 | The system MUST allow deletion of a category via `DELETE /categories/{id}`. |
| FR-CAT-07 | Deleting a category that has associated catalog items MUST be prevented, returning `409 Conflict`. |

---

### FR-CATALOG: Catalog API

| ID | Requirement |
|----|-------------|
| FR-CATALOG-01 | The system MUST allow retrieval of all catalog items via `GET /catalog`. |
| FR-CATALOG-02 | The response for the catalog list MUST support pagination (page number and page size). |
| FR-CATALOG-03 | The catalog list MUST be filterable by `categoryId` as a query parameter. |
| FR-CATALOG-04 | The system MUST allow retrieval of a single catalog item by its ID via `GET /catalog/{id}`. |
| FR-CATALOG-05 | Each catalog item MUST have at minimum: a unique ID, name, description, price, and a reference to its category. |
| FR-CATALOG-06 | Each catalog item MUST belong to exactly one category. |
| FR-CATALOG-07 | The system MUST allow creation of a catalog item via `POST /catalog`. |
| FR-CATALOG-08 | The system MUST allow updating a catalog item via `PUT /catalog/{id}`. |
| FR-CATALOG-09 | The system MUST allow deletion of a catalog item via `DELETE /catalog/{id}`. |
| FR-CATALOG-10 | Creating a catalog item with a non-existent `categoryId` MUST return `422 Unprocessable Entity`. |

---

### FR-WISH: Wishlist API

| ID | Requirement |
|----|-------------|
| FR-WISH-01 | The system MUST allow an authenticated user to add a catalog item to their wishlist via `POST /wishlist`. |
| FR-WISH-02 | The system MUST allow an authenticated user to retrieve their full wishlist via `GET /wishlist`. |
| FR-WISH-03 | The wishlist MUST be filterable by `categoryId` as a query parameter, returning only wishlist items whose catalog item belongs to that category. |
| FR-WISH-04 | The system MUST allow an authenticated user to remove an item from their wishlist via `DELETE /wishlist/{itemId}`. |
| FR-WISH-05 | Adding a catalog item that does not exist MUST return `404 Not Found`. |
| FR-WISH-06 | Adding a duplicate catalog item to the same user's wishlist MUST return `409 Conflict`. |
| FR-WISH-07 | A user MUST NOT be able to view or modify another user's wishlist. |

---

### FR-ERR: Common Error Handling

| ID | Requirement |
|----|-------------|
| FR-ERR-01 | All error responses MUST return a consistent JSON body containing at minimum: `status`, `message`, and `traceId`. |
| FR-ERR-02 | Requests with malformed or invalid payloads MUST return `400 Bad Request` with field-level validation errors. |
| FR-ERR-03 | Access to a resource that does not exist MUST return `404 Not Found`. |
| FR-ERR-04 | Unhandled server errors MUST return `500 Internal Server Error` without exposing stack traces. |

---

These requirements are scoped to be directly actionable for Claude Code — each maps cleanly to a MediatR command/query, a controller endpoint, and a domain rule. Let me know if you'd like me to add non-functional requirements (NFRs), data model definitions, or request/response schema specs as well.

## 4. Non-Functional Requirements

Here are the additions to your spec file:

---

## Non-Functional Requirements

### NFR-PERF: Performance

| ID | Requirement |
|----|-------------|
| NFR-PERF-01 | All read endpoints (`GET`) MUST respond within **200ms** at p95 under normal load. |
| NFR-PERF-02 | All write endpoints (`POST`, `PUT`, `DELETE`) MUST respond within **500ms** at p95. |
| NFR-PERF-03 | The catalog list endpoint MUST support pagination to prevent unbounded result sets; default page size MUST be 20, maximum 100. |
| NFR-PERF-04 | Frequently read data (categories, catalog items) SHOULD be cached in-memory with a configurable TTL (default: 60 seconds). |

---

### NFR-SEC: Security

| ID | Requirement |
|----|-------------|
| NFR-SEC-01 | All traffic MUST be served over HTTPS; HTTP requests MUST be redirected or rejected. |
| NFR-SEC-02 | API keys MUST be stored hashed (e.g. SHA-256) and never returned in plaintext after creation. |
| NFR-SEC-03 | The API MUST enforce rate limiting per API key: maximum **100 requests/minute**; excess requests return `429 Too Many Requests`. |
| NFR-SEC-04 | All inputs MUST be validated and sanitised before reaching the domain layer to prevent injection attacks. |
| NFR-SEC-05 | Error responses MUST NOT leak internal implementation details, stack traces, or database schema information. |
| NFR-SEC-06 | Sensitive configuration values (connection strings, key secrets) MUST be supplied via environment variables or a secrets manager, never hardcoded. |

---

### NFR-REL: Reliability & Availability

| ID | Requirement |
|----|-------------|
| NFR-REL-01 | The API MUST expose a health check endpoint at `GET /health` returning service and database connectivity status. |
| NFR-REL-02 | Database migrations MUST be applied automatically on startup using a migration tool (e.g. EF Core Migrations). |
| NFR-REL-03 | All unhandled exceptions MUST be caught by global middleware, logged, and returned as a structured `500` response. |
| NFR-REL-04 | The API MUST be stateless; no session state stored in-process, to support horizontal scaling. |

---

### NFR-MAIN: Maintainability

| ID | Requirement |
|----|-------------|
| NFR-MAIN-01 | The codebase MUST follow Clean Architecture layer separation: Domain → Application → Infrastructure → API. |
| NFR-MAIN-02 | All inter-layer communication MUST use MediatR commands and queries; controllers MUST NOT contain business logic. |
| NFR-MAIN-03 | All public Application layer interfaces MUST have unit tests with a minimum of **80% code coverage**. |
| NFR-MAIN-04 | The API MUST expose an OpenAPI/Swagger document at `/swagger` in non-production environments. |
| NFR-MAIN-05 | Structured logging (e.g. Serilog) MUST be used throughout; each log entry MUST include `traceId`, `userId` (if available), and timestamp. |

---



## Request / Response Schema Specs

### User API

**`POST /users`** — Create User
```json
// Request
{
  "name": "Jane Doe",          // string, required, max 100
  "email": "jane@example.com", // string, required, valid email, max 255
  "password": "S3cur3P@ss!"    // string, required, min 8 chars
}

// Response 201 Created
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "Jane Doe",
  "email": "jane@example.com",
  "createdAt": "2025-01-01T10:00:00Z"
}
```

**`GET /users/{id}`** — Get User
```json
// Response 200 OK
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "Jane Doe",
  "email": "jane@example.com",
  "createdAt": "2025-01-01T10:00:00Z",
  "updatedAt": null
}
```

**`PUT /users/{id}`** — Update User
```json
// Request
{
  "name": "Jane Smith",           // string, optional, max 100
  "email": "jsmith@example.com"   // string, optional, valid email
}

// Response 200 OK — same shape as GET /users/{id}
```

**`DELETE /users/{id}`** — Delete User
```
// Response 204 No Content
```

---

### Category API

**`GET /categories`** — List Categories
```json
// Response 200 OK
[
  {
    "id": "a1b2c3d4-...",
    "name": "Electronics",
    "description": "Gadgets and devices",
    "createdAt": "2025-01-01T10:00:00Z"
  }
]
```

**`GET /categories/{id}`** — Get Category
```json
// Response 200 OK
{
  "id": "a1b2c3d4-...",
  "name": "Electronics",
  "description": "Gadgets and devices",
  "createdAt": "2025-01-01T10:00:00Z",
  "updatedAt": null
}
```

**`POST /categories`** — Create Category
```json
// Request
{
  "name": "Electronics",       // string, required, max 100, unique
  "description": "Gadgets..."  // string, optional, max 500
}

// Response 201 Created — same shape as GET /categories/{id}
```

**`PUT /categories/{id}`** — Update Category
```json
// Request
{
  "name": "Consumer Electronics",  // string, optional
  "description": "Updated desc"    // string, optional
}

// Response 200 OK — same shape as GET /categories/{id}
```

**`DELETE /categories/{id}`** — Delete Category
```
// Response 204 No Content
// Response 409 Conflict — if catalog items exist under this category
```

---

### Catalog API

**`GET /catalog`** — List Catalog Items
```
// Query params:
//   categoryId  GUID     optional — filter by category
//   page        integer  optional, default 1
//   pageSize    integer  optional, default 20, max 100
//   search      string   optional — filter by name (partial match)

// Response 200 OK
{
  "page": 1,
  "pageSize": 20,
  "totalCount": 84,
  "totalPages": 5,
  "items": [
    {
      "id": "b2c3d4e5-...",
      "name": "Wireless Headphones",
      "description": "Noise-cancelling over-ear headphones",
      "price": 149.99,
      "imageUrl": "https://cdn.example.com/items/wh-001.jpg",
      "isActive": true,
      "category": {
        "id": "a1b2c3d4-...",
        "name": "Electronics"
      },
      "createdAt": "2025-01-01T10:00:00Z"
    }
  ]
}
```

**`GET /catalog/{id}`** — Get Catalog Item
```json
// Response 200 OK
{
  "id": "b2c3d4e5-...",
  "name": "Wireless Headphones",
  "description": "Noise-cancelling over-ear headphones",
  "price": 149.99,
  "imageUrl": "https://cdn.example.com/items/wh-001.jpg",
  "isActive": true,
  "category": {
    "id": "a1b2c3d4-...",
    "name": "Electronics"
  },
  "createdAt": "2025-01-01T10:00:00Z",
  "updatedAt": null
}
```

**`POST /catalog`** — Create Catalog Item
```json
// Request
{
  "categoryId":   "a1b2c3d4-...", // GUID, required, must exist
  "name":         "Wireless Headphones", // string, required, max 200
  "description":  "Noise-cancelling...", // string, optional, max 1000
  "price":        149.99,                // decimal, required, >= 0
  "imageUrl":     "https://...",         // string, optional, valid URL
  "isActive":     true                   // bool, optional, default true
}

// Response 201 Created — same shape as GET /catalog/{id}
```

**`PUT /catalog/{id}`** — Update Catalog Item
```json
// Request — all fields optional; only supplied fields are updated
{
  "categoryId":  "a1b2c3d4-...",
  "name":        "Premium Headphones",
  "description": "Updated description",
  "price":       179.99,
  "imageUrl":    "https://cdn.example.com/items/wh-002.jpg",
  "isActive":    true
}

// Response 200 OK — same shape as GET /catalog/{id}
```

**`DELETE /catalog/{id}`** — Delete Catalog Item
```
// Response 204 No Content
```

---

### Wishlist API

**`GET /wishlist`** — Get User's Wishlist
```
// Query params:
//   categoryId  GUID  optional — filter items by their catalog item's category

// Response 200 OK
{
  "userId": "3fa85f64-...",
  "totalCount": 3,
  "items": [
    {
      "id": "c3d4e5f6-...",
      "addedAt": "2025-02-14T08:30:00Z",
      "catalogItem": {
        "id": "b2c3d4e5-...",
        "name": "Wireless Headphones",
        "price": 149.99,
        "imageUrl": "https://cdn.example.com/items/wh-001.jpg",
        "category": {
          "id": "a1b2c3d4-...",
          "name": "Electronics"
        }
      }
    }
  ]
}
```

**`POST /wishlist`** — Add Item to Wishlist
```json
// Request
{
  "catalogItemId": "b2c3d4e5-..."  // GUID, required, must exist
}

// Response 201 Created
{
  "id": "c3d4e5f6-...",
  "addedAt": "2025-02-14T08:30:00Z",
  "catalogItem": {
    "id": "b2c3d4e5-...",
    "name": "Wireless Headphones",
    "price": 149.99,
    "imageUrl": "https://cdn.example.com/items/wh-001.jpg",
    "category": {
      "id": "a1b2c3d4-...",
      "name": "Electronics"
    }
  }
}
```

**`DELETE /wishlist/{itemId}`** — Remove Item from Wishlist
```
// Response 204 No Content
```

---

### Common Error Response

All `4xx` and `5xx` responses share this envelope:

```json
{
  "status": 422,
  "message": "Validation failed.",
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01",
  "errors": {
    "categoryId": ["Category with the specified ID does not exist."],
    "price": ["Price must be greater than or equal to 0."]
  }
}
```

> `errors` is only present on `400` and `422` responses. All other error types omit it.

---



## 5. Data Model & Database Schema (SQLite)

### 5.1 Tables

## Data Model Definitions

### Entity: `User`

| Column | Type | Constraints |
|--------|------|-------------|
| `Id` | `GUID` | PK, non-null |
| `Name` | `NVARCHAR(100)` | non-null |
| `Email` | `NVARCHAR(255)` | non-null, unique |
| `PasswordHash` | `NVARCHAR(512)` | non-null |
| `CreatedAt` | `DATETIMEOFFSET` | non-null, default `GETUTCDATE()` |
| `UpdatedAt` | `DATETIMEOFFSET` | nullable |

---

### Entity: `ApiKey`

| Column | Type | Constraints |
|--------|------|-------------|
| `Id` | `GUID` | PK, non-null |
| `UserId` | `GUID` | FK → `User.Id`, non-null |
| `KeyHash` | `NVARCHAR(512)` | non-null, unique |
| `Label` | `NVARCHAR(100)` | nullable |
| `ExpiresAt` | `DATETIMEOFFSET` | nullable |
| `RevokedAt` | `DATETIMEOFFSET` | nullable |
| `CreatedAt` | `DATETIMEOFFSET` | non-null, default `GETUTCDATE()` |

---

### Entity: `Category`

| Column | Type | Constraints |
|--------|------|-------------|
| `Id` | `GUID` | PK, non-null |
| `Name` | `NVARCHAR(100)` | non-null, unique |
| `Description` | `NVARCHAR(500)` | nullable |
| `CreatedAt` | `DATETIMEOFFSET` | non-null, default `GETUTCDATE()` |
| `UpdatedAt` | `DATETIMEOFFSET` | nullable |

---

### Entity: `CatalogItem`

| Column | Type | Constraints |
|--------|------|-------------|
| `Id` | `GUID` | PK, non-null |
| `CategoryId` | `GUID` | FK → `Category.Id`, non-null |
| `Name` | `NVARCHAR(200)` | non-null |
| `Description` | `NVARCHAR(1000)` | nullable |
| `Price` | `DECIMAL(18,2)` | non-null, ≥ 0 |
| `ImageUrl` | `NVARCHAR(2048)` | nullable |
| `IsActive` | `BIT` | non-null, default `1` |
| `CreatedAt` | `DATETIMEOFFSET` | non-null, default `GETUTCDATE()` |
| `UpdatedAt` | `DATETIMEOFFSET` | nullable |

---

### Entity: `WishlistItem`

| Column | Type | Constraints |
|--------|------|-------------|
| `Id` | `GUID` | PK, non-null |
| `UserId` | `GUID` | FK → `User.Id`, non-null |
| `CatalogItemId` | `GUID` | FK → `CatalogItem.Id`, non-null |
| `AddedAt` | `DATETIMEOFFSET` | non-null, default `GETUTCDATE()` |

> **Unique constraint:** `(UserId, CatalogItemId)` — prevents duplicate wishlist entries.

---

### Entity Relationship Summary

```
User ──< ApiKey
User ──< WishlistItem >── CatalogItem >── Category
```

---

## 6. Backend: DB & API Layer

### 6.1 Database Access Module

**File:** `lib/db.ts`

- Initialize Bun SQLite client with DB file (`app.db`)
- Export helper functions such as:
  - `getDb()` – returns singleton DB connection
  - Utility wrappers for:
    - `query<T>(sql, params?): T[]`
    - `get<T>(sql, params?): T | undefined`
    - `run(sql, params?)`

### 6.2 Note Repository Functions

**File:** `lib/notes.ts`

**TypeScript types:**

```typescript
export type Note = {
  id: string;
  userId: string;
  title: string;
  contentJson: string; // stringified TipTap doc
  isPublic: boolean;
  publicSlug: string | null;
  createdAt: string;
  updatedAt: string;
};
```

**Repository functions:**

- `createNote(userId: string, data: { title?: string; contentJson?: string }): Promise<Note>`
- `getNoteById(userId: string, noteId: string): Promise<Note | null>`
- `getNotesByUser(userId: string): Promise<Note[]>`
- `updateNote(userId: string, noteId: string, data: Partial<{ title: string; contentJson: string }>): Promise<Note | null>`
- `deleteNote(userId: string, noteId: string): Promise<void>`
- `setNotePublic(userId: string, noteId: string, isPublic: boolean): Promise<Note | null>`
- `getNoteByPublicSlug(slug: string): Promise<Note | null>`

Each function enforces `user_id = ?` in SQL where applicable, to avoid cross-user access.

## 7. API Design (Next.js Route Handlers)

Base path under `/api/notes`.

### 7.1 Authentication Access

Implement a server helper from better-auth like `getCurrentUser()` or `getSession()`.

All `/api/notes` handlers (except public read) must:
- Check auth
- Return 401 if not authenticated

### 7.2 Endpoints

#### GET /api/notes

**Description:** List notes for current user.

**Response 200:**
```json
[
  {
    "id": "note-id",
    "title": "My Note",
    "isPublic": true,
    "updatedAt": "2025-01-01T12:00:00Z"
  }
]
```

Optionally omit `contentJson` for list for performance.

#### POST /api/notes

**Description:** Create a new note.

**Request body (JSON):**
```json
{
  "title": "Optional title",
  "contentJson": { "type": "doc", ... }
}
```

**Behavior:**
- Default title = "Untitled note" if missing
- Default `contentJson` = empty TipTap document if missing

**Response 201:** created Note (or minimal subset)

#### GET /api/notes/:id

**Description:** Get single note owned by current user.

**Response:**
- 200 with full note including `contentJson`
- 404 if not found or not owned by user

#### PUT /api/notes/:id

**Description:** Update note title/content.

**Request body:**
```json
{
  "title": "New title",
  "contentJson": { ... }
}
```

**Response:**
- 200 with updated note
- 404 if not found

#### DELETE /api/notes/:id

**Description:** Delete note.

**Response:**
- 204 on success
- 404 if not found

#### POST /api/notes/:id/share

**Description:** Toggle public sharing.

**Request body:**
```json
{
  "isPublic": true
}
```

**Behavior:**
- If `isPublic = true` and note has no `public_slug`, generate new slug (`nanoid()`)
- If `isPublic = false`, set `is_public = 0` and `public_slug = NULL`

**Response 200:**
```json
{
  "id": "note-id",
  "isPublic": true,
  "publicSlug": "abcdef1234"
}
```

### 7.3 Public Note Endpoint

#### GET /api/public-notes/:slug

**Description:** Read-only access to public notes.

**Response 200:**
```json
{
  "title": "Public note",
  "contentJson": { ... }
}
```

- 404 if slug not found or `is_public = 0`

(Or skip this API and just resolve directly in the `/p/[slug]` route using server components.)

