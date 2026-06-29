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

## Phase 9 AI Foundation

Phase 9 adds global and per-agent AI settings plus provider abstractions for Gemini, Ollama, and OpenRouter.

Migration:

```powershell
dotnet tool run dotnet-ef database update --project src/AgenticPlatform.Infrastructure --startup-project src/AgenticPlatform.API --context ApplicationDbContext
```

AI settings endpoints:

```text
GET  /api/v1/ai-settings
PUT  /api/v1/ai-settings
GET  /api/v1/ai-settings/agents/{agentId}/resolved
POST /api/v1/ai-settings/test
```

Default global settings use local Ollama:

```text
Provider: Ollama
Model: llama3.1
BaseUrl: http://localhost:11434
```

Provider runtime requirements:

- Ollama needs a running local Ollama server and installed model.
- Gemini needs an API key.
- OpenRouter needs an API key.

API keys can be saved through update requests but are not returned by read endpoints; responses expose only `hasApiKey`.

## Phase 10 Tool Execution Framework

Phase 10 adds real built-in tool execution while keeping the existing `Tool` entity unchanged.

Manual execution endpoint:

```text
POST /api/v1/tools/{toolId}/execute
```

Built-in categories:

```text
Calculator
Http
REST API
WebSearch
FileReader
```

Example calculator input:

```json
{
  "inputJson": "{\"expression\":\"(2 + 3) * 4\"}"
}
```

Example HTTP input:

```json
{
  "inputJson": "{\"method\":\"GET\",\"url\":\"https://example.com\"}"
}
```

Example web search input:

```json
{
  "inputJson": "{\"query\":\"Gemini API\"}"
}
```

Example file reader input:

```json
{
  "inputJson": "{\"path\":\"appsettings.json\"}"
}
```

FileReader is limited to files under the API content root.

## Phase 11 Real Agent Runtime

Phase 11 replaces simulated execution with runtime services:

- Agent executions resolve AI settings and call the configured LLM provider.
- Workflow tool steps execute through the Phase 10 tool execution framework.
- Workflow agent steps call the configured agent's LLM provider.
- Each workflow step receives the previous step output as its input.
- Final execution output is persisted in `Executions.OutputJson`.
- Execution logs record provider calls, tool execution, step start/completion, and failures.

Agent execution input can use any of these fields:

```json
{
  "prompt": "Summarize what agentic AI means in one paragraph."
}
```

```json
{
  "question": "What is 2 + 2?"
}
```

Workflow output now includes:

```text
input
finalOutput
steps[]
completedAt
```

## Phase 12 Workflow Orchestration

Phase 12 adds explicit step input mapping. By default, each step still receives the previous step output.

Use `inputMappingJson` to choose and shape a step's input:

```json
{}
```

```text
Default. Use previous step output.
```

```json
{
  "source": "original"
}
```

```text
Use the workflow's original execution input.
```

```json
{
  "source": "step",
  "stepOrder": 1
}
```

```text
Use a previous step output by order.
```

```json
{
  "template": "Summarize this result: {{previous.result}}",
  "wrapAs": "prompt"
}
```

```text
Render a template and send it as `{ "prompt": "..." }`.
```

Supported template placeholders:

```text
{{original}}
{{previous}}
{{original.fieldName}}
{{previous.fieldName}}
{{step1}}
{{step1.fieldName}}
```

Example two-step calculator workflow:

```json
{
  "template": "{{previous.result}} + 12",
  "wrapAs": "expression"
}
```

If step 1 returns `{ "result": 30 }`, step 2 receives:

```json
{
  "expression": "30 + 12"
}
```

## Phase 13 Demo Polish

Phase 13 seeds ready-to-run demo data and adds a demo catalog endpoint.

Demo catalog:

```text
GET /api/v1/demo/catalog
```

Seeded tools:

```text
Demo Calculator
Demo Web Search
Demo File Reader
```

Seeded agents:

```text
Demo Research Agent
Demo Summary Agent
```

Seeded workflows:

```text
Demo Calculator Chain
Demo Research And Summary
```

Fastest no-LLM demo:

```text
POST /api/v1/executions
```

```json
{
  "targetType": "Workflow",
  "targetId": "cccccccc-0000-0000-0000-000000000001",
  "inputJson": "{\"expression\":\"(8 + 2) * 3\"}"
}
```

Then poll:

```text
GET /api/v1/executions/{executionId}
```

Expected final output:

```text
42
```

Fastest LLM demo:

```json
{
  "targetType": "Workflow",
  "targetId": "cccccccc-0000-0000-0000-000000000002",
  "inputJson": "{\"prompt\":\"Explain how AI agents use tools in software platforms.\"}"
}
```

The LLM demo requires valid global AI settings.

## Phase 14 React Frontend

Phase 14 adds the React + MUI frontend:

```text
src/PratsPilot.Web
```

Frontend name:

```text
PratsPilot
```

Run the backend:

```powershell
dotnet run --project src/AgenticPlatform.API/AgenticPlatform.API.csproj
```

Run the frontend:

```powershell
cd src/PratsPilot.Web
npm install
npm run dev
```

Open:

```text
http://localhost:5173
```

Default login:

```text
Email: admin@agenticplatform.local
Password: Admin@12345
```

The frontend uses:

```text
Vite
React
TypeScript
MUI
React Query
Axios
```

Set a different backend URL with:

```text
VITE_API_BASE_URL=http://localhost:5167/api/v1
```

Phase 14 product expansion adds:

- Public signup at `POST /api/v1/auth/signup`.
- Agent builder metadata: project name, role, goal, expected output, and tags.
- Provider-aware model catalogs in the frontend for Gemini, OpenRouter, and Ollama.
- Direct LLM playground at `POST /api/v1/ai-settings/chat`.
- Frontend agent creation form with per-agent model override controls.
- Frontend workflow builder with searchable agent/tool cards and drag-to-order step creation.
- Chat page for asking selected LLMs questions outside a workflow.
