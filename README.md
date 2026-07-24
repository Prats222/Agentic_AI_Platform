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

An administrator account is seeded for local development. Use your configured admin credentials to sign in.

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

Login with your configured administrator account to manage protected resources.

Valid roles are `Admin`, `Developer`, and `Viewer`. Public signup creates a normal user account.

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

Use the login screen with your configured admin account, or register a normal user from the Register tab.

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

## Current Implemented Feature Set

The project has now grown into a working full-stack agentic AI platform named **PratsPilot**.

### Backend Foundation

- Clean Architecture style solution split into API, Core, and Infrastructure projects.
- ASP.NET Core Web API targeting `.NET 10`.
- Entity Framework Core with SQL Server / LocalDB.
- ASP.NET Core Identity.
- JWT authentication.
- Refresh token support.
- Role-based authorization for Admin, Developer, and Viewer roles.
- Seeded admin account.
- Repository Pattern and Unit of Work.
- AutoMapper mapping profiles.
- FluentValidation validators.
- Serilog request logging.
- Global exception handling middleware.
- Request/response logging middleware.
- CORS configuration for the React frontend.
- Response caching and cache headers.
- Rate limiting.
- Health checks for SQL Server and the execution engine.
- Swagger/OpenAPI documentation.

### Core Domain APIs

- Authentication:
  - Login.
  - Signup for normal users.
  - Refresh token.
  - Logout.
- Agents:
  - Create agents.
  - Edit agents.
  - Delete agents with dependent cleanup.
  - Agent metadata: project name, role, goal, expected output, tags.
  - Global or per-agent AI settings.
  - Optional tool assignment per agent.
- Workflows:
  - Create workflows.
  - Edit workflows.
  - Delete workflows with dependent cleanup.
  - Add, update, and delete workflow steps.
  - Agent and tool workflow steps.
  - Step input mapping between workflow steps.
- Tools:
  - Create tools from the frontend.
  - Execute tools directly.
  - Built-in tool categories.
  - Python script tools.
- Executions:
  - Start agent executions.
  - Start workflow executions.
  - Background queued execution.
  - Execution status tracking.
  - Execution logs.
  - Persisted input, output, errors, timestamps.
  - Retry failed executions.

### AI Provider Support

- Global AI settings.
- Agent-level AI setting overrides.
- Provider abstraction through `ILLMProvider`.
- Gemini provider.
- Ollama provider.
- OpenRouter provider.
- Direct chat/playground endpoint.
- Live OpenRouter model lookup.
- Free OpenRouter model filtering.
- Provider error handling with useful API responses.
- API key storage in backend settings for local development.

### Execution Runtime

- Background execution queue.
- Hosted execution worker.
- Real workflow execution service.
- Agent execution through configured LLM provider.
- Workflow orchestration across multiple steps.
- Previous step output passed into the next step.
- Tool step execution.
- Agent step execution.
- Persisted final workflow output.
- Persisted step-by-step runtime logs.
- Assigned agent tools are included in the agent system prompt as available capabilities.
- Human approval gate steps can pause a workflow for reviewer action.
- Approved human gates resume the queued execution path.
- Rejected human gates stop the execution and persist the rejection.
- Execution observability captures provider, model, duration, estimated input tokens, estimated output tokens, and estimated cost fields.

### Tool Engine

- Calculator tool executor.
- HTTP / REST API tool executor.
- Web search-style tool executor.
- File reader tool executor.
- Python script tool executor.
- Python tools receive input JSON through `stdin`.
- Python tools return JSON through `stdout`.
- Tool execution result includes success state, output JSON, error message, timestamps, and duration.

### Seeded Demo Data

- Seeded demo tools:
  - Demo Calculator.
  - Demo Web Search.
  - Demo File Reader.
- Seeded demo agents:
  - Demo Research Agent.
  - Demo Summary Agent.
- Seeded demo workflows:
  - Demo Calculator Chain.
  - Demo Research And Summary.
- Demo catalog endpoint for frontend samples.

### React Frontend: PratsPilot

- Vite + React + TypeScript frontend.
- MUI-based UI.
- React Query for server state.
- Axios API client.
- Login page with Admin login, User login, and Register modes.
- Normal user registration page.
- Optional email verification for newly registered users without blocking sign-in.
- Branded Brevo confirmation and welcome emails.
- Resend-confirmation flow with anti-enumeration responses and a dedicated email rate limit.
- Protected routes.
- JWT token handling.
- Refresh token handling on expired access tokens.
- Logout.
- Realm selector:
  - User Realm is shared by all users and admins.
  - Admin Realm is visible only to users with the Admin role.
  - Existing artifacts are stored in User Realm.
- Admin panel:
  - View all users.
  - Inspect user roles and realm access.
  - Grant or remove Admin access.
  - Inspect whether each email is confirmed.
  - Send or resend the PratsPilot welcome guide to one confirmed existing user at a time.
- Arena page:
  - Create creator-vs-creator agent challenges.
  - Submit agents into a battle.
  - Run the same task through competing agents.
  - Use the configured LLM as an impartial judge.
  - Persist winner, scores, judge summary, feedback, outputs, latency, provider, and model.
- Dashboard with runtime metrics and execution launcher.
- Agents page:
  - Create agents.
  - Edit agents.
  - Delete agents.
  - Project filter.
  - Per-agent AI override controls.
  - Dynamic input schema generation from `{{fieldName}}` placeholders in agent instructions.
  - Searchable multi-select tool picker.
  - Built-in tools grouped at the top of the picker.
  - Custom tools searchable below built-ins.
  - Attach contextualization documents to agents.
- Context page:
  - Upload contextualization documents.
  - Supports `.txt`, `.json`, `.md`, `.csv`, `.docx`, `.xlsx`, and `.pdf` uploads.
  - Extracts text from text-like files, Word documents, and Excel workbooks for agent context.
  - Delete context documents and detach them from agents.
- Autopilot page:
  - Describe a desired outcome.
  - Draft a workflow from matching existing agents.
  - Optionally insert human approval gates between drafted agent steps.
- Approvals page:
  - Review workflow payloads waiting at human approval gates.
  - Approve and resume executions.
  - Reject and stop executions with reviewer comments.
- Workflows page:
  - Search agents and tools.
  - Drag steps into a workflow.
  - Drag human approval gates into specific points of a workflow.
  - Create workflows.
  - Edit workflows.
  - Delete workflows.
- Tools page:
  - View tools.
  - Create tools.
  - Create Python script tools.
  - Execute tools.
  - Auto-generate sample input from tool input schema.
  - Show tool execution output or validation errors.
- Executions page:
  - Latest execution history.
  - History size selector: latest 10, 25, or 100.
  - Retry failed executions.
  - Cost and latency observability strip for selected executions.
  - Structured final output viewer.
  - Step-by-step workflow output boxes.
  - Expandable raw JSON.
  - Expandable runtime logs.
  - Top-position toast messages.
- Chat page:
  - Select provider.
  - Select model.
  - Ask direct LLM questions.
  - Display provider errors.
- AI Settings page:
  - Configure global provider.
  - Configure model, temperature, max tokens, top-p, system prompt, base URL, and API key.
  - Live/free OpenRouter model loading.
- Dark mode and light mode.
- Theme mode persists in local storage.
 
### Current Practical Status

- Backend builds successfully.
- Frontend builds successfully.
- The platform supports creating agents, assigning tools and context documents, building workflows with human approval gates, running executions, inspecting outputs, retrying failures, chatting with LLMs, creating custom Python tools, and reviewing cost/latency telemetry.

### Free Deployment Plan

The planned zero-cost deployment path is documented in [DEPLOYMENT_RENDER_NEON_FREE.md](DEPLOYMENT_RENDER_NEON_FREE.md).

- Frontend: Vercel Hobby.
- Backend: Render Free Web Service.
- Database: Neon Free PostgreSQL.
- Context uploads: extracted text stored in PostgreSQL for the free deployment.
- Python tools: Python 3 installed inside the backend Docker container.
- LLMs: Gemini and Groq free-tier keys configured from the app.

### Transactional Email

PratsPilot sends account confirmation and welcome-guide emails through Brevo's HTTP API. Secrets stay on the backend and are never returned to the React client.

Configure these Render environment variables:

```text
Email__ApiKey=<Brevo API key>
Email__SenderEmail=<verified Brevo sender>
Email__SenderName=PratsPilot
Email__FrontendBaseUrl=https://pratspilot.vercel.app
Email__LinkedInPostUrl=https://www.linkedin.com/posts/prateek-mishra-686945243_agenticai-aiagents-generativeai-activity-7483723888552517632-8HIb
```

Existing accounts remain confirmed. Newly registered accounts can sign in immediately and may follow the verification link to mark their email as confirmed. The Admin user panel provides an explicit per-user action for delivering the welcome guide to existing confirmed users.
