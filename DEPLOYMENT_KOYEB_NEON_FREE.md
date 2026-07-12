# PratsPilot Free Deployment Guide: Koyeb + Neon + Vercel

This is the current no-card deployment plan.

## Final Architecture

```text
React frontend        Vercel Hobby
ASP.NET Core API      Koyeb Free Web Service
Database              Neon Free PostgreSQL
Context documents     Extracted text stored in Neon database
Python tools          Python inside the API Docker container
LLMs                  Gemini and Groq free-tier keys
```

## Why We Switched Away From Oracle

Oracle signup requires card verification and can place temporary authorization holds. Since that flow was unreliable, PratsPilot is now planned around services that can be started without card-based VM provisioning.

## Free-Tier Reality Check

- Koyeb Free gives one small web service. It is good for demos and MVP traffic, but it can cold-start.
- Neon Free gives PostgreSQL with no time limit and no credit card required, but the free database has storage and compute limits.
- Vercel Hobby is good for the React frontend.
- If the product becomes popular, the first upgrade should be backend compute.

Sources:

- Koyeb free instances: https://www.koyeb.com/docs/reference/instances
- Neon pricing: https://neon.com/pricing
- Vercel pricing: https://vercel.com/pricing

## Phase 1 - Code Preparation

Completed in this repo:

- Backend can use SQL Server locally.
- Backend can use PostgreSQL in production.
- PostgreSQL schema can be created on first boot for a fresh production database.
- Backend Dockerfile includes .NET 10 and Python 3.
- Koyeb production env template added.
- Vercel SPA routing config added.
- Frontend API URL env example added.

## Phase 2A - Create Neon Free Database

1. Go to `https://neon.tech`.
2. Click **Sign up**.
3. Sign up with GitHub or email.
4. After login, click **New Project**.
5. Project name:

```text
pratspilot
```

6. Region:
   - Pick the closest free region available.
   - If unsure, choose a region near your users.
7. PostgreSQL version:
   - Keep the default.
8. Click **Create project**.
9. Neon will show a connection string.
10. Copy the connection string.

It will look similar to:

```text
postgresql://neondb_owner:password@host.neon.tech/neondb?sslmode=require
```

Keep this private. Do not paste it in GitHub.

## Phase 2B - Convert Neon URL For ASP.NET

ASP.NET can use the URL format in many cases, but the safest format is:

```text
Host=HOST;Port=5432;Database=DATABASE;Username=USERNAME;Password=PASSWORD;SSL Mode=Require;Trust Server Certificate=true
```

Example:

```text
Host=ep-example-123456.us-east-2.aws.neon.tech;Port=5432;Database=neondb;Username=neondb_owner;Password=abc123;SSL Mode=Require;Trust Server Certificate=true
```

I can help convert it when you reach this step.

## Phase 2C - Create Koyeb Backend Service

1. Go to `https://www.koyeb.com`.
2. Click **Sign up**.
3. Sign up with GitHub if possible.
4. In the Koyeb dashboard, click **Create Web Service**.
5. Choose **GitHub**.
6. Connect GitHub if Koyeb asks.
7. Select repository:

```text
Prats222/Agentic_AI_Platform
```

8. Branch:

```text
master
```

9. Builder:

```text
Dockerfile
```

10. Dockerfile path:

```text
src/AgenticPlatform.API/Dockerfile
```

11. Instance type:

```text
Free
```

12. Region:
   - Pick the closest available free region.
   - If unsure, use Frankfurt.
13. Exposed port:

```text
8080
```

14. Add environment variables from `deploy/koyeb/.env.example`.

Required variables:

```text
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:8080
Database__Provider=PostgreSql
Database__EnsureCreated=true
ConnectionStrings__DefaultConnection=your-neon-connection-string
JwtSettings__Issuer=PratsPilot
JwtSettings__Audience=PratsPilot.Web
JwtSettings__Secret=your-long-secret
Cors__AllowedOrigins__0=https://your-vercel-app.vercel.app
RateLimiting__PermitLimit=60
RateLimiting__WindowSeconds=60
RateLimiting__QueueLimit=0
```

For the first deploy, if you do not have Vercel yet, temporarily set:

```text
Cors__AllowedOrigins__0=http://localhost:5173
```

We will replace it after Vercel gives us the frontend URL.

15. Click **Deploy**.

## Phase 2D - Test Backend

After Koyeb finishes deploying, it gives a URL like:

```text
https://your-service-name.koyeb.app
```

Open:

```text
https://your-service-name.koyeb.app/health
```

Expected result:

```json
{
  "status": "Healthy"
}
```

If it is unhealthy, open Koyeb logs and check:

- database connection string
- PostgreSQL SSL settings
- JWT secret length
- exposed port is `8080`

## Phase 3 - Deploy Frontend To Vercel

1. Go to `https://vercel.com`.
2. Sign in with GitHub.
3. Click **Add New**.
4. Click **Project**.
5. Import:

```text
Prats222/Agentic_AI_Platform
```

6. Root directory:

```text
src/PratsPilot.Web
```

7. Framework preset:

```text
Vite
```

8. Build command:

```text
npm run build
```

9. Output directory:

```text
dist
```

10. Add environment variable:

```text
VITE_API_BASE_URL=https://your-service-name.koyeb.app/api/v1
```

11. Click **Deploy**.

## Phase 4 - Fix CORS After Vercel Deploy

After Vercel gives a URL like:

```text
https://pratspilot.vercel.app
```

Go back to Koyeb:

1. Open your backend service.
2. Go to **Settings**.
3. Open **Environment variables**.
4. Update:

```text
Cors__AllowedOrigins__0=https://pratspilot.vercel.app
```

5. Save.
6. Redeploy.

## Phase 5 - First App Test

In the Vercel frontend:

1. Register a new user.
2. Login.
3. Go to **AI Settings**.
4. Add Gemini key.
5. Add Groq key.
6. Create one agent.
7. Run direct chat.
8. Create one Python tool.
9. Run the Python tool.
10. Create one workflow.
11. Run one execution.

## Python Tool Notes

Koyeb users do not install Python. Python is installed in the backend Docker container.

Because Koyeb free instances are small, keep Python tools lightweight:

- no huge libraries
- no long-running loops
- small inputs
- small outputs

## Context Document Notes

For the free Koyeb version, uploaded file originals are not guaranteed permanent because free services do not provide persistent volumes by default. The important extracted text is stored in PostgreSQL and remains available to agents.

Later, when the app grows, add Cloudflare R2 for original file storage.

## Free-Tier Safety Checklist

- Use Koyeb Free service only.
- Use Neon Free project only.
- Use Vercel Hobby only.
- Do not add card unless you intentionally upgrade.
- Keep upload files small.
- Keep API rate limiting enabled.
- Prefer Groq for fast coding demos.
- Use Gemini as backup.
- Do not commit `.env` files or API keys.
