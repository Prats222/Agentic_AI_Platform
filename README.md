# Agentic AI Platform Backend

Phase 1 creates a clean ASP.NET Core Web API skeleton targeting .NET 10 with three projects:

- `AgenticPlatform.API`: controllers, middleware, configuration, startup.
- `AgenticPlatform.Core`: shared contracts, DTOs, mapping markers, validation markers.
- `AgenticPlatform.Infrastructure`: EF Core, Identity, database integration, external services.

## How To Start

1. Open a terminal in this folder.
2. Restore NuGet packages:

   ```powershell
   dotnet restore
   ```

3. Build the solution:

   ```powershell
   dotnet build
   ```

4. Run the API:

   ```powershell
   dotnet run --project src/AgenticPlatform.API/AgenticPlatform.API.csproj
   ```

5. Open Swagger:

   ```text
   https://localhost:7167/swagger
   ```

## Phase 1 Notes

- Business models and real database tables come in Phase 2.
- Repositories and Unit of Work come in Phase 3.
- Authentication endpoints and refresh tokens come in Phase 4.
- The current `ApplicationDbContext` only contains ASP.NET Core Identity tables so the scaffold compiles.
- This project targets `net10.0`, matching the .NET SDK/runtime installed on this machine.

## Phase 2 Migration Command

If this is a fresh clone, restore the local EF tool first:

```powershell
dotnet tool restore
```

The Phase 2 migration was scaffolded with:

```powershell
dotnet tool run dotnet-ef migrations add InitialCreate --project src/AgenticPlatform.Infrastructure --startup-project src/AgenticPlatform.API --output-dir Data/Migrations
```

Then create/update the local database with:

```powershell
dotnet tool run dotnet-ef database update --project src/AgenticPlatform.Infrastructure --startup-project src/AgenticPlatform.API
```

Seeded admin user:

```text
Email: admin@agenticplatform.local
Password: Admin@12345
```

## Phase 3 Notes

Phase 3 adds Repository Pattern and Unit of Work:

- Generic repository: `IRepository<T>` and `Repository<T>`.
- Specific repositories: agents, workflows, tools, executions.
- Unit of Work: `IUnitOfWork` and `UnitOfWork`.
- All repository services are registered in dependency injection.

## Phase 4 Auth Endpoints

Phase 4 adds Identity + JWT authentication with refresh tokens.

Available endpoints:

```text
POST /api/v1/auth/login
POST /api/v1/auth/refresh-token
POST /api/v1/auth/logout
POST /api/v1/auth/register
```

Login with the seeded admin user:

```json
{
  "email": "admin@agenticplatform.local",
  "password": "Admin@12345"
}
```

Register requires an Admin bearer token. Valid roles are `Admin`, `Developer`, and `Viewer`.

## Phase 5 Agents API

Phase 5 adds versioned Agents CRUD.

Endpoints:

```text
GET    /api/v1/agents
GET    /api/v1/agents/{id}
POST   /api/v1/agents
PUT    /api/v1/agents/{id}
DELETE /api/v1/agents/{id}
```

List query parameters:

```text
name, status, sortBy=name|createdAt, sortDirection=asc|desc, pageNumber, pageSize
```

Viewers can read agents. Developers and admins can create/update. Only admins can delete.

## Phase 6 Workflows And Tools APIs

Phase 6 adds:

```text
GET    /api/v1/tools
GET    /api/v1/tools/{id}
POST   /api/v1/tools
PUT    /api/v1/tools/{id}
DELETE /api/v1/tools/{id}

GET    /api/v1/workflows
GET    /api/v1/workflows/{id}
POST   /api/v1/workflows
PUT    /api/v1/workflows/{id}
DELETE /api/v1/workflows/{id}

POST   /api/v1/workflows/{workflowId}/steps
GET    /api/v1/workflows/{workflowId}/steps/{stepId}
PUT    /api/v1/workflows/{workflowId}/steps/{stepId}
DELETE /api/v1/workflows/{workflowId}/steps/{stepId}
```

Tool registration stores `inputSchemaJson` as JSON. Workflow steps can target either a tool or an agent.

## Phase 7 Execution Engine

Phase 7 adds execution endpoints:

```text
POST /api/v1/executions
GET  /api/v1/executions/{id}
GET  /api/v1/executions
```

Executions are queued into a background worker. The API returns `202 Accepted`, then the worker marks the execution `Running`, writes logs, simulates agent/workflow steps, and finishes as `Completed` or `Failed`.

## Phase 8 Cross-Cutting Concerns

Phase 8 adds:

- Global exception handling with `ProblemDetails` JSON.
- Request/response logging middleware using Serilog.
- Response cache headers for GET endpoints and no-store headers for writes/auth.
- Fixed-window rate limiting with JSON `429` responses.
- Health checks at `/health` for SQL Server and the execution engine.
