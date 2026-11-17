# Product Management API (C#)

A RESTful Product Management API built with **ASP.NET Core (.NET 10)** and **Entity Framework Core** using a Code-First approach. It provides full CRUD functionality, concurrency-safe stock management, partial-match search, interactive Swagger/OpenAPI documentation, automated integration tests, and multi-instance safe 6-digit product identifier generation.

---

## Table of Contents
- [Architecture & Design Decisions](#architecture--design-decisions)
- [Assumptions & Trade-offs](#assumptions--trade-offs)
- [API Endpoints](#api-endpoints)
- [Local Development Setup (LocalDB)](#local-development-setup-localdb)
- [API Documentation & Testing (Swagger / OpenAPI)](#api-documentation--testing-swagger--openapi)
- [Automated Integration Testing (xUnit)](#automated-integration-testing-xunit)
- [Upgrading to Production](#upgrading-to-production)

---

## Architecture & Design Decisions

### 1. Clean Vertical Slice Structure
The API is organized into focused, decoupled folders (`Entities`, `Data`, `DTOs`, and `Endpoints`) using ASP.NET Core **Minimal APIs** (`MapGroup("/api/products")`).
- **DTO Separation:** Dedicated `CreateProductDto`, `UpdateProductDto`, and `ProductDto` records decouple external API contracts from internal EF Core persistence entities and prevent over-posting vulnerabilities.
- **Validation & Standardized Errors:** Request payloads are validated using Data Annotations combined with built-in Minimal API validation (`AddValidation()`) and RFC 9457 `ProblemDetails` for consistent HTTP 400 error responses.

### 2. Multi-Instance Safe 6-Digit ID Generation
To guarantee unique 6-digit numeric product IDs (`100000`–`999999`) without collisions when running multiple API instances simultaneously, ID generation is delegated to an atomic **SQL Server Database Sequence** (`ProductIdSeq`):
- Defined via EF Core Fluent API (`StartsAt(100000)`, `IncrementsBy(1)`, `HasMin(100000)`, `HasMax(999999)`).
- Because the database engine manages sequence allocation atomically, horizontal scaling (multiple API replicas behind a load balancer) cannot produce duplicate identifiers or race conditions.

### 3. Concurrency-Safe Stock Adjustments
Stock modifications (`decrement-stock` and `add-to-stock`) bypass EF Core's Change Tracker and use **`ExecuteUpdateAsync`** to execute single, atomic SQL `UPDATE` statements:
- **Decrementing stock** runs conditionally (`WHERE Id = @id AND Stock >= @quantity`), preventing race conditions (lost updates) and ensuring stock never drops below zero under concurrent requests.
- A database-level `CHECK` constraint (`[Stock] >= 0`) provides a secondary safeguard against invalid inventory states.

---

## Assumptions & Trade-offs

| Area | Decision / Assumption | Trade-off |
| :--- | :--- | :--- |
| **Database Engine** | Uses **SQL Server Express LocalDB** for local development. | Zero-configuration setup on Windows with full T-SQL compatibility (including `SEQUENCE` and `CHECK` constraints), but strictly single-user and unsuitable for production workloads. |
| **ID Format & Capacity** | Stored as a 6-digit integer (`100000` to `999999`) using a non-cycling sequence. | Guarantees strict 6-digit numeric formatting and multi-instance uniqueness, but caps total lifetime product creations at 900,000 records before sequence exhaustion. |
| **Stock Adjustment Semantics** | `decrement-stock` and `add-to-stock` use `POST` and return `200 OK` (empty body) on success per specification. | While `PATCH` is more idiomatic for partial updates, `POST` adheres strictly to the required API contract. Returning `200 OK` minimizes payload overhead; clients retrieve updated state via `GET`. |
| **Search Implementation** | Uses SQL `LIKE '%term%'` with a non-clustered index on `Product.Name`. | Simple, zero-dependency partial matching suitable for small-to-medium catalogs. For large datasets, leading-wildcard `LIKE` queries cannot perform index seeks and should be upgraded to Full-Text Search. |
| **Testing Strategy** | Uses **Integration Tests** (`WebApplicationFactory<Program>`) against an isolated LocalDB test database rather than mocking EF Core in-memory. | Because SQL Server sequences (`ProductIdSeq`) and `ExecuteUpdateAsync` rely on real relational database engine behavior, testing against LocalDB guarantees true end-to-end verification without false positives from in-memory providers. |
| **Authentication** | Omitted in the current scope to focus on core domain functionality and evaluation ease. | Endpoints are unauthenticated locally; production environments must enforce JWT Bearer / OAuth2 authorization. |

---

## API Endpoints

| Method | Endpoint | Description |
| :--- | :--- | :--- |
| `GET` | `/api/products` | Retrieve all products (includes `id` and `stock`). |
| `POST` | `/api/products` | Create a new product with an auto-generated 6-digit ID. |
| `GET` | `/api/products/{id}` | Retrieve a specific product by its 6-digit ID. |
| `PUT` | `/api/products/{id}` | Update an existing product's details and stock. |
| `DELETE` | `/api/products/{id}` | Delete a product by ID. |
| `POST` | `/api/products/{id}/decrement-stock/{quantity}` | Atomically decrement stock by `quantity` (`200 OK` or `400 Bad Request` if insufficient). |
| `POST` | `/api/products/{id}/add-to-stock/{quantity}` | Atomically increment stock by `quantity` (`200 OK`). |
| `GET` | `/api/products/search?name={name}` | Case-insensitive partial name search. |
| `GET` | `/api/products/stock-level?min={min}&max={max}` | Filter products whose stock falls within `[min, max]` inclusive. |

---

## Local Development Setup (LocalDB)

This project runs on **SQL Server Express LocalDB** (`(localdb)\mssqllocaldb`) for zero-friction local development and evaluation on Windows.

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Visual Studio 2026 (or VS Code / CLI) with SQL Server LocalDB installed

### Running Locally
1. **Verify `appsettings.json`** contains the LocalDB connection string:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=ProductManagementDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
   }
