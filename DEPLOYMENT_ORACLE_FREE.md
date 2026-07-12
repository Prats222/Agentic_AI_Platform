# PratsPilot Free Deployment Guide

This guide deploys PratsPilot using free-tier-friendly services.

## Final Architecture

```text
React frontend        Vercel Hobby
ASP.NET Core API      Oracle Cloud Always Free VM
Database              PostgreSQL Docker container on Oracle VM
Context uploads       Docker volume on Oracle VM
Python tools          Python inside the API Docker container
LLMs                  Gemini and Groq free tiers
```

## Why Python Tools Work Online

Users do not need Python installed on their laptops. The deployed API Docker image installs Python 3 inside the backend container. When a Python tool runs, the backend executes it inside that server container.

## Phase 1 - Code Preparation

Completed in this repo:

- Backend can use SQL Server locally.
- Backend can use PostgreSQL in production.
- PostgreSQL schema can be created on first boot for a fresh production database.
- Backend Dockerfile includes .NET 10 and Python 3.
- Oracle Docker Compose file runs API + PostgreSQL.
- Vercel SPA routing config added.
- Frontend API URL env example added.

## Phase 2 - Create Oracle Cloud Account

1. Go to `https://www.oracle.com/cloud/free/`.
2. Click **Start for free**.
3. Sign in or create an Oracle account.
4. Add phone verification.
5. Add card verification if Oracle asks.
6. Choose a home region carefully.
   - Pick the region closest to your expected users.
   - For India users, try Mumbai or Hyderabad if available.
7. Finish account creation.

Important:

- Stay inside Always Free resources.
- Do not create paid databases or paid compute shapes.
- If Oracle offers a trial credit, ignore it for production planning.

## Phase 3 - Create Always Free VM

1. Open Oracle Cloud Console.
2. Search **Compute**.
3. Click **Instances**.
4. Click **Create instance**.
5. Name it:

```text
pratspilot-api
```

6. Image:

```text
Canonical Ubuntu 24.04
```

7. Shape:

```text
Ampere Arm A1 Always Free
```

Recommended free shape:

```text
1 OCPU
6 GB RAM
```

If Oracle capacity is unavailable, try:

```text
1 OCPU
4 GB RAM
```

8. Networking:
   - Keep default VCN if you are new.
   - Make sure **Assign public IPv4 address** is enabled.
9. SSH key:
   - Choose **Generate a key pair for me** if you do not already have one.
   - Download the private key.
   - Store it safely.
10. Click **Create**.

## Phase 4 - Open Server Ports

In Oracle:

1. Go to your VM instance.
2. Click the virtual cloud network link.
3. Click **Security Lists**.
4. Open the default security list.
5. Click **Add Ingress Rules**.
6. Add these rules:

```text
Source CIDR: 0.0.0.0/0
IP Protocol: TCP
Destination Port Range: 22
Description: SSH
```

```text
Source CIDR: 0.0.0.0/0
IP Protocol: TCP
Destination Port Range: 80
Description: HTTP
```

```text
Source CIDR: 0.0.0.0/0
IP Protocol: TCP
Destination Port Range: 443
Description: HTTPS
```

For temporary testing only, we may also open:

```text
Destination Port Range: 8080
Description: API temporary test
```

After HTTPS is configured, close port `8080`.

## Phase 5 - Connect to VM

From Windows:

1. Open PowerShell.
2. Go to the folder where your Oracle private key was downloaded.
3. Run:

```powershell
ssh -i .\your-oracle-key.key ubuntu@YOUR_PUBLIC_IP
```

Replace:

```text
YOUR_PUBLIC_IP
```

with the public IP shown on the Oracle VM page.

## Phase 6 - Install Docker on VM

Inside the SSH terminal:

```bash
sudo apt update
sudo apt install -y ca-certificates curl git
curl -fsSL https://get.docker.com | sudo sh
sudo usermod -aG docker ubuntu
newgrp docker
docker --version
docker compose version
```

## Phase 7 - Pull Code on VM

Inside the SSH terminal:

```bash
git clone https://github.com/Prats222/Agentic_AI_Platform.git
cd Agentic_AI_Platform
```

If the repo is private:

1. Go to GitHub.
2. Open **Settings**.
3. Open **Developer settings**.
4. Open **Personal access tokens**.
5. Create a token with repository read access.
6. Use that token as the password when Git asks.

## Phase 8 - Create Production Env File

Inside the SSH terminal:

```bash
cd deploy/oracle
cp .env.example .env
nano .env
```

Fill values like this:

```text
POSTGRES_DB=pratspilot
POSTGRES_USER=pratspilot
POSTGRES_PASSWORD=make-this-long-and-random
JWT_SECRET=make-this-even-longer-at-least-64-characters
FRONTEND_ORIGIN=https://your-vercel-app.vercel.app
RATE_LIMIT_PER_MINUTE=60
```

Save in nano:

1. Press `Ctrl + O`.
2. Press `Enter`.
3. Press `Ctrl + X`.

## Phase 9 - Start Backend

Inside:

```bash
deploy/oracle
```

run:

```bash
docker compose up -d --build
docker compose ps
docker compose logs -f api
```

Health check:

```bash
curl http://localhost:8080/health
```

Temporary browser test:

```text
http://YOUR_PUBLIC_IP:8080/health
```

## Phase 10 - Deploy Frontend to Vercel

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
VITE_API_BASE_URL=http://YOUR_PUBLIC_IP:8080/api/v1
```

This temporary URL is only for first testing. Later, replace it with HTTPS:

```text
VITE_API_BASE_URL=https://api.your-domain.com/api/v1
```

11. Click **Deploy**.

## Phase 11 - HTTPS

For final public use, the API needs HTTPS. The clean path is:

```text
Domain or free DNS name -> Oracle VM -> Caddy/Nginx -> API container
```

We will configure this after the backend works on port `8080`.

## Phase 12 - LLM Keys

After login:

1. Open PratsPilot.
2. Go to **AI Settings**.
3. Add Gemini key.
4. Add Groq key.
5. Prefer Groq for fast/free coding-style demos.
6. Prefer Gemini as backup.

Do not put LLM keys in frontend env variables.

## Free-Tier Safety Checklist

- Use Vercel Hobby only.
- Use Oracle Always Free VM shape only.
- Use PostgreSQL inside the VM, not paid managed DB.
- Keep uploads small.
- Keep API rate limiting enabled.
- Do not expose admin credentials publicly.
- Do not commit `.env` files.
- Monitor Oracle resources once a week.

