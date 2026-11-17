Apply Code-First Migrations to create the database, `Products` table, constraints, and `ProductIdSeq` sequence:

```bash
dotnet ef database update
```

Start the API:

```bash
dotnet run
```

## API Documentation & Testing (Swagger / OpenAPI)

In Development mode, the API exposes both the raw OpenAPI 3 specification and an interactive Swagger UI powered by `Microsoft.AspNetCore.OpenApi` and `Swashbuckle.AspNetCore.SwaggerUI`:

- **Interactive Swagger UI:** `https://localhost:<port>/swagger` (launches automatically when starting the project with F5 in Visual Studio).
- **OpenAPI JSON Specification:** `https://localhost:<port>/openapi/v1.json`

Every endpoint is enriched with `.WithSummary()`, `.WithDescription()`, and `.Produces(...)` metadata so reviewers can inspect DTO schemas, expected HTTP status codes, and execute live requests directly from the browser.

## Automated Integration Testing (xUnit)

The solution includes a dedicated `ProductManagementAPI.Tests` project built with xUnit and `Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory<Program>`).

### How It Works

- **`CustomWebApplicationFactory`:** Overrides the application's database registration to target an isolated LocalDB test database (`ProductManagementTestDb`), automatically resetting (`EnsureDeleted()`) and migrating (`Migrate()`) the schema before test execution.
- **`ProductEndpointsTests`:** Exercises the real HTTP pipeline and SQL Server engine across critical scenarios:
  - **Concurrent 6-Digit ID Generation:** Fires parallel `POST /api/products` requests to verify all generated IDs fall within `100000`–`999999` with zero duplicates.
  - **Payload Validation:** Confirms invalid inputs (empty names, non-positive prices, negative stock) return `400 Bad Request` with `ProblemDetails`.
  - **Stock Boundaries & Concurrency Guards:** Verifies add-to-stock and decrement-stock updates and ensures attempting to decrement more stock than available returns `400 Bad Request` without corrupting inventory.
  - **Search & Stock-Level Filtering:** Validates case-insensitive partial name searches and inclusive `[min, max]` range queries.

### Running the Tests

- **Visual Studio:** Open **Test > Test Explorer** (`Ctrl + E, T`) and click **Run All Tests**.
- **CLI:** Run the following command from the solution root:

```bash
dotnet test
```

## Upgrading to Production

> **Important:** LocalDB is an in-process, single-user development database and cannot be used in production or multi-instance cloud environments. Because this API uses standard T-SQL features (`SEQUENCE`, `CHECK` constraints, and atomic `UPDATE` statements), promoting it to a full SQL Server engine requires no changes to your entities, migrations, or endpoint logic.

### Step 1: Provision a Production Database

Create a dedicated, multi-user SQL Server instance:

- **Azure:** Azure SQL Database or Azure SQL Managed Instance
- **AWS:** Amazon RDS for SQL Server
- **On-Premises / Containers:** SQL Server Standard or Enterprise Edition (Windows or Linux container)

### Step 2: Replace the Connection String via Environment Variables

Do not store production credentials in `appsettings.json`. Override `ConnectionStrings:DefaultConnection` using environment variables (`ConnectionStrings__DefaultConnection`), Azure Key Vault, or AWS Secrets Manager, and enforce strict TLS encryption (`Encrypt=True;TrustServerCertificate=False;`):

```bash
# Example environment variable configuration
ConnectionStrings__DefaultConnection="Server=tcp:your-prod-server.database.windows.net,1433;Database=ProductManagementDb;User ID=api_user;Password=YOUR_STRONG_PASSWORD;Encrypt=True;TrustServerCertificate=False;"
```

### Step 3: Apply Database Migrations via CI/CD

Generate an idempotent SQL script from your existing Code-First migrations and run it as a deployment step before starting the production API instances (avoiding startup race conditions across multiple replicas):

```bash
dotnet ef migrations script --idempotent --output migrate.sql
```

Execute `migrate.sql` against your production database using `sqlcmd` or your CI/CD release pipeline.

### Step 4: Enable Connection Resiliency & Production Hardening

Before exposing the API publicly, apply the following production configurations in `Program.cs`:

- **Database Transient Fault Retries:** Update `UseSqlServer` to handle temporary cloud network drops:

  ```csharp
  builder.Services.AddDbContext<AppDbContext>(options =>
      options.UseSqlServer(
          builder.Configuration.GetConnectionString("DefaultConnection"),
          sqlOptions => sqlOptions.EnableRetryOnFailure()));
  ```

- **CORS Policy:** Update `"AngularDev"` in `Program.cs` to replace `http://localhost:4200` with your deployed production Angular frontend URL.
- **Authentication & Authorization:** Register JWT Bearer authentication (`builder.Services.AddAuthentication().AddJwtBearer()`) and apply `.RequireAuthorization()` to `/api/products` endpoints.
- **Health Checks & Monitoring:** Add `builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>()` and map `app.MapHealthChecks("/health")` for load balancer and container orchestrator probes.
