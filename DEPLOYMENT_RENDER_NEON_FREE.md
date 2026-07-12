# PratsPilot Free Deployment Guide: Render + Neon + Vercel

This is the current no-card deployment plan.

## Final Architecture

```text
React frontend        Vercel Hobby
ASP.NET Core API      Render Free Web Service
Database              Neon Free PostgreSQL
Context documents     Extracted text stored in Neon database
Python tools          Python inside the API Docker container
LLMs                  Gemini and Groq free-tier keys
```

## Why This Plan

Oracle and Koyeb both asked for card/payment onboarding. This path avoids that.

- Render supports free web services and Docker web services.
- Neon provides free PostgreSQL with no time limit and no credit card required.
- Vercel hosts the frontend for free.

Sources:

- Render free services: https://render.com/docs/free
- Render Docker web services: https://render.com/docs/web-services
- Neon pricing: https://neon.com/pricing
- Vercel pricing: https://vercel.com/pricing

## Limitations

Render Free is good for demos, portfolios, and MVP testing, but it is not an enterprise production tier.

Expected limitations:

- Backend can sleep after inactivity.
- First request after sleep can be slow.
- Free resources are limited.
- File originals are not persistent on the backend service.

For the free version, PratsPilot stores extracted document text in Neon. Later, add Cloudflare R2 for original file storage.

## Phase 1 - Code Preparation

Completed in this repo:

- Backend can use SQL Server locally.
- Backend can use PostgreSQL in production.
- PostgreSQL schema can be created on first boot for a fresh production database.
- Root Dockerfile added for Render.
- Docker image includes .NET 10 and Python 3.
- Render production env template added.
- Vercel SPA routing config added.

## Phase 2A - Neon Database

Already done if you created the `pratspilot` Neon project.

The connection string from Neon looks like this:

```text
postgresql://USER:PASSWORD@HOST/DATABASE?sslmode=require
```

Convert it to ASP.NET format:

```text
Host=HOST;Port=5432;Database=DATABASE;Username=USER;Password=PASSWORD;SSL Mode=Require;Trust Server Certificate=true
```

## Phase 2B - Render Backend

1. Go to `https://render.com`.
2. Click **Get Started** or **Sign Up**.
3. Sign up with GitHub.
4. In the dashboard, click **New +**.
5. Click **Web Service**.
6. Connect GitHub if asked.
7. Select:

```text
Prats222/Agentic_AI_Platform
```

8. Name:

```text
pratspilot-api
```

9. Region:
   - Choose the closest free region available.
   - If unsure, choose Oregon or Frankfurt.
10. Branch:

```text
master
```

11. Runtime:

```text
Docker
```

12. Dockerfile path:

```text
Dockerfile
```

13. Instance type:

```text
Free
```

14. Add environment variables from `deploy/render/.env.example`.

Required:

```text
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:10000
Database__Provider=PostgreSql
Database__EnsureCreated=true
ConnectionStrings__DefaultConnection=your-neon-aspnet-connection-string
JwtSettings__Issuer=PratsPilot
JwtSettings__Audience=PratsPilot.Web
JwtSettings__Secret=your-long-random-secret
Cors__AllowedOrigins__0=http://localhost:5173
RateLimiting__PermitLimit=60
RateLimiting__WindowSeconds=60
RateLimiting__QueueLimit=0
```

Use `http://localhost:5173` for CORS only until Vercel is deployed. After Vercel gives the real URL, replace it.

15. Click **Deploy Web Service**.

## Phase 2C - Test Backend

Render gives a URL like:

```text
https://pratspilot-api.onrender.com
```

Open:

```text
https://pratspilot-api.onrender.com/health
```

Expected:

```json
{
  "status": "Healthy"
}
```

## Phase 3 - Vercel Frontend

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

10. Environment variable:

```text
VITE_API_BASE_URL=https://pratspilot-api.onrender.com/api/v1
```

11. Deploy.

## Phase 4 - Fix Render CORS

After Vercel gives a URL like:

```text
https://pratspilot.vercel.app
```

Go back to Render:

1. Open `pratspilot-api`.
2. Go to **Environment**.
3. Change:

```text
Cors__AllowedOrigins__0=https://pratspilot.vercel.app
```

4. Save.
5. Render redeploys automatically.

## Phase 5 - First Test

1. Open Vercel frontend.
2. Register a normal user.
3. Login.
4. Go to AI Settings.
5. Add Gemini and Groq keys.
6. Test Chat.
7. Create Agent.
8. Create Python Tool.
9. Run Tool.
10. Create Workflow.
11. Run Execution.

