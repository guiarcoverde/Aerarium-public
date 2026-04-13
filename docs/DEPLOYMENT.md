# Aerarium — AWS Deployment Guide

This guide walks through deploying Aerarium to AWS from scratch and explains how to maintain or change the setup later. It matches the architecture in `.github/workflows/deploy.yml`, `docker-compose.prod.yml`, `Caddyfile`, and `src/Api/Dockerfile`.

## Architecture at a glance

```
                  ┌────────────────────┐
  users  ───────▶ │  CloudFront (CDN)  │ ─▶ S3  (Angular static files)
                  └────────────────────┘
                              │
                              │ XHR → https://api.<domain>
                              ▼
                  ┌────────────────────┐
                  │  Lightsail VM      │
                  │  ┌──────────────┐  │
                  │  │ Caddy :443   │  │ ← Let's Encrypt (auto)
                  │  │   │          │  │
                  │  │   ▼          │  │
                  │  │ API :8080    │  │ ─▶ SSM Parameter Store (/aerarium/prod/*)
                  │  │   │          │  │
                  │  │   ▼          │  │
                  │  │ Postgres 17  │  │ ─▶ nightly pg_dump → S3 backups
                  │  └──────────────┘  │
                  └────────────────────┘
```

| Component       | Service                              | Approx. cost          |
|-----------------|---------------------------------------|-----------------------|
| Frontend        | S3 + CloudFront + ACM                 | ~$1/mo                |
| API + DB        | Lightsail `small_3_0` (2 GB, $10/mo)  | $10/mo                |
| Domain          | Route 53 registered domain + zone     | ~$12/yr + $0.50/mo    |
| Secrets         | SSM Parameter Store (Standard)        | free                  |
| Backups         | S3 + Glacier lifecycle                | <$1/mo                |
| CI              | GitHub Actions + GHCR                 | free (public repo)    |

Expected total: **~$12/mo + $12/yr domain**.

---

## Prerequisites

- AWS account with a payment method
- GitHub repo admin access (to add secrets)
- `aws` CLI installed locally and configured with an admin profile (used only for the one-time bootstrap)
- An SSH key pair you'll use for Lightsail access

Pick an AWS region close to your users and **use it consistently** for every resource below except the ACM cert for CloudFront, which MUST be in `us-east-1`. Examples in this guide use `sa-east-1` (São Paulo). Replace as needed.

---

## Step 1 — Register the domain (Route 53)

1. Route 53 → Registered domains → Register domain.
2. Pick something like `aerarium.app`. Route 53 creates a public hosted zone automatically.
3. Note the hosted zone ID; you'll reference it when creating records below.

You'll end up with two subdomains:
- `app.<domain>` → CloudFront (frontend)
- `api.<domain>` → Lightsail static IP (backend)

---

## Step 2 — Frontend hosting (S3 + CloudFront + ACM)

### 2.1 S3 bucket
```bash
aws s3api create-bucket \
  --bucket aerarium-frontend-prod \
  --region sa-east-1 \
  --create-bucket-configuration LocationConstraint=sa-east-1

aws s3api put-public-access-block \
  --bucket aerarium-frontend-prod \
  --public-access-block-configuration \
  "BlockPublicAcls=true,IgnorePublicAcls=true,BlockPublicPolicy=true,RestrictPublicBuckets=true"

aws s3api put-bucket-versioning \
  --bucket aerarium-frontend-prod \
  --versioning-configuration Status=Enabled
```

The bucket stays private. CloudFront will access it via an Origin Access Control (OAC).

### 2.2 ACM certificate (must be in us-east-1 for CloudFront)

AWS Console → Certificate Manager → **us-east-1** → Request public cert for `app.<domain>`. Use DNS validation → "Create records in Route 53". Wait for status = Issued.

### 2.3 CloudFront distribution

Console → CloudFront → Create distribution:
- **Origin**: S3 bucket `aerarium-frontend-prod`. Click "Origin access" → "Origin access control settings" → create a new OAC → "Sign requests".
- **Viewer protocol policy**: Redirect HTTP to HTTPS.
- **Default root object**: `index.html`.
- **Alternate domain names (CNAMEs)**: `app.<domain>`.
- **Custom SSL certificate**: select the ACM cert from step 2.2.
- **Custom error responses** (required so Angular client-side routing works):
  - 403 → `/index.html`, response code 200
  - 404 → `/index.html`, response code 200
- Create distribution. Copy the displayed S3 bucket policy snippet and apply it to the bucket (Console will offer a one-click "Copy policy" button).

### 2.4 DNS record
Route 53 → hosted zone → Create record:
- Name: `app`
- Type: A (alias)
- Target: the CloudFront distribution
- Also create an AAAA alias to the same distribution

### 2.5 Save the distribution ID
Copy the CloudFront **Distribution ID** (looks like `E2ABCD1234`). You'll add it to GitHub secrets as `CLOUDFRONT_DIST_ID`.

---

## Step 3 — Backend host (Lightsail + static IP)

### 3.1 Create the instance
Console → Lightsail → Create instance:
- Location: same region as everything else (e.g. `sa-east-1a`).
- Blueprint: **OS Only → Ubuntu 22.04 LTS**.
- Plan: **$10/mo (2 GB RAM, 2 vCPU, 60 GB SSD)** — `small_3_0`. The $5 plan has 512 MB RAM and will OOM under .NET + Postgres.
- Upload your SSH public key or use the Lightsail default key (download the `.pem`).
- Name it `aerarium-prod`.

### 3.2 Attach a static IP
Lightsail → Networking → Create static IP → attach to `aerarium-prod`. Note the IP.

### 3.3 Open firewall ports
Lightsail → Instance → Networking tab → add rules:
- TCP 22 (SSH — restrict to your IP if you can)
- TCP 80 (HTTP, needed for Let's Encrypt HTTP-01)
- TCP 443 (HTTPS)

### 3.4 DNS record
Route 53 → create an A record `api.<domain>` → the static IP.

### 3.5 Install Docker on the VM
SSH in and run:
```bash
sudo apt-get update
sudo apt-get install -y ca-certificates curl
sudo install -m 0755 -d /etc/apt/keyrings
sudo curl -fsSL https://download.docker.com/linux/ubuntu/gpg -o /etc/apt/keyrings/docker.asc
sudo chmod a+r /etc/apt/keyrings/docker.asc
echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.asc] https://download.docker.com/linux/ubuntu $(. /etc/os-release && echo "$VERSION_CODENAME") stable" | \
  sudo tee /etc/apt/sources.list.d/docker.list > /dev/null
sudo apt-get update
sudo apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
sudo usermod -aG docker ubuntu
```
Log out and back in so the group change takes effect.

---

## Step 4 — Secrets (SSM Parameter Store)

Create secrets under the prefix `/aerarium/prod/`. The API loads them automatically at startup when `ASPNETCORE_ENVIRONMENT=Production` (see `src/Api/Program.cs`).

Generate a strong JWT key:
```bash
openssl rand -base64 64
```

Create the parameters (replace placeholders):
```bash
REGION=sa-east-1

aws ssm put-parameter --region $REGION --type SecureString \
  --name /aerarium/prod/ConnectionStrings/Default \
  --value "Host=postgres;Port=5432;Database=aerarium;Username=aerarium;Password=<STRONG_DB_PASSWORD>"

aws ssm put-parameter --region $REGION --type SecureString \
  --name /aerarium/prod/Jwt/Key \
  --value "<OUTPUT_OF_OPENSSL_RAND>"

aws ssm put-parameter --region $REGION --type String \
  --name /aerarium/prod/Jwt/Issuer --value "Aerarium"

aws ssm put-parameter --region $REGION --type String \
  --name /aerarium/prod/Jwt/Audience --value "Aerarium"

aws ssm put-parameter --region $REGION --type String \
  --name /aerarium/prod/Cors/AllowedOrigins/0 \
  --value "https://app.<your-domain>"
```

> **Note**: the `Host=postgres` in the connection string matches the service name in `docker-compose.prod.yml`. Docker's internal DNS resolves it to the Postgres container.

### 4.1 IAM user for the API to read SSM

Create an IAM user `aerarium-api` with **programmatic access only**. Attach this inline policy (replace `ACCOUNT_ID` and `REGION`):

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": ["ssm:GetParametersByPath", "ssm:GetParameters", "ssm:GetParameter"],
      "Resource": "arn:aws:ssm:REGION:ACCOUNT_ID:parameter/aerarium/prod/*"
    },
    {
      "Effect": "Allow",
      "Action": ["kms:Decrypt"],
      "Resource": "*",
      "Condition": {
        "StringEquals": {
          "kms:ViaService": "ssm.REGION.amazonaws.com"
        }
      }
    }
  ]
}
```

Save the access key + secret — you'll drop them into the server `.env` in step 5.

---

## Step 5 — Bootstrap the Lightsail box

SSH in, then:
```bash
sudo mkdir -p /opt/aerarium
sudo chown ubuntu:ubuntu /opt/aerarium
cd /opt/aerarium
```

Copy `docker-compose.prod.yml` and `Caddyfile` from the repo onto the box (scp from your laptop, or paste with `nano`). These two files are the only runtime config the server needs — the API image comes from GHCR.

Create `/opt/aerarium/.env`:
```bash
# Image published by GitHub Actions
API_IMAGE=ghcr.io/<github-user>/aerarium-api:latest

# Public API hostname — Caddy reads this to issue the cert
API_DOMAIN=api.<your-domain>

# Postgres (must match the connection string stored in SSM)
POSTGRES_DB=aerarium
POSTGRES_USER=aerarium
POSTGRES_PASSWORD=<STRONG_DB_PASSWORD>

# IAM user from step 4.1
AWS_ACCESS_KEY_ID=AKIA...
AWS_SECRET_ACCESS_KEY=...
AWS_REGION=sa-east-1
```
`chmod 600 .env`.

Log in to GHCR so the box can pull the image (create a GitHub PAT with `read:packages`):
```bash
echo <GHCR_PAT> | docker login ghcr.io -u <github-user> --password-stdin
```

First run:
```bash
docker compose -f docker-compose.prod.yml pull
docker compose -f docker-compose.prod.yml up -d
docker compose -f docker-compose.prod.yml logs -f api
```

You should see EF Core migrations applied, then Kestrel listening on 8080. Caddy will request the Let's Encrypt cert on first HTTPS hit.

Sanity check from your laptop:
```bash
curl -I https://api.<your-domain>/scalar/v1
```

---

## Step 6 — Nightly database backup (optional but recommended)

Create the backup bucket:
```bash
aws s3api create-bucket --bucket aerarium-db-backups \
  --region sa-east-1 \
  --create-bucket-configuration LocationConstraint=sa-east-1

aws s3api put-bucket-lifecycle-configuration \
  --bucket aerarium-db-backups \
  --lifecycle-configuration '{
    "Rules": [{
      "ID": "archive",
      "Status": "Enabled",
      "Filter": {"Prefix": ""},
      "Transitions": [{"Days": 30, "StorageClass": "GLACIER_IR"}],
      "Expiration": {"Days": 180}
    }]
  }'
```

On the Lightsail box, create `/opt/aerarium/backup.sh`:
```bash
#!/usr/bin/env bash
set -euo pipefail
cd /opt/aerarium
export $(grep -v '^#' .env | xargs)
STAMP=$(date +%F)
docker compose -f docker-compose.prod.yml exec -T postgres \
  pg_dump -U "$POSTGRES_USER" "$POSTGRES_DB" | gzip | \
  aws s3 cp - "s3://aerarium-db-backups/${STAMP}.sql.gz"
```
`chmod +x backup.sh`, then add a cron entry:
```bash
crontab -e
# paste:
0 3 * * * /opt/aerarium/backup.sh >> /var/log/aerarium-backup.log 2>&1
```

Also enable **Lightsail automatic snapshots** in the console (Instance → Snapshots → Enable).

---

## Step 7 — GitHub Actions CI/CD

### 7.1 IAM user for CI
Create `aerarium-ci` with this inline policy (replace placeholders):
```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": ["s3:PutObject", "s3:DeleteObject", "s3:ListBucket"],
      "Resource": [
        "arn:aws:s3:::aerarium-frontend-prod",
        "arn:aws:s3:::aerarium-frontend-prod/*"
      ]
    },
    {
      "Effect": "Allow",
      "Action": ["cloudfront:CreateInvalidation"],
      "Resource": "arn:aws:cloudfront::ACCOUNT_ID:distribution/CLOUDFRONT_DIST_ID"
    }
  ]
}
```

### 7.2 GitHub repo secrets
Repo → Settings → Secrets and variables → Actions → add:

| Secret                  | Value                                              |
|-------------------------|----------------------------------------------------|
| `AWS_ACCESS_KEY_ID`     | `aerarium-ci` access key                           |
| `AWS_SECRET_ACCESS_KEY` | `aerarium-ci` secret                               |
| `AWS_REGION`            | e.g. `sa-east-1`                                   |
| `FRONTEND_BUCKET`       | `aerarium-frontend-prod`                           |
| `CLOUDFRONT_DIST_ID`    | CloudFront distribution ID                         |
| `LIGHTSAIL_HOST`        | Static IP of the Lightsail box                     |
| `LIGHTSAIL_USER`        | `ubuntu`                                           |
| `LIGHTSAIL_SSH_KEY`     | **Private** key (full PEM) with access to the VM   |

The workflow (`.github/workflows/deploy.yml`) runs on every push to `master`:
- **frontend** job: builds Angular, syncs to S3, invalidates CloudFront.
- **api** job: builds the Docker image, pushes to GHCR, SSHes to Lightsail and runs `docker compose pull && up -d`.

### 7.3 First deploy
Before the first push, make sure:
- `src/Frontend/src/environments/environment.production.ts` has the correct `apiUrl: 'https://api.<your-domain>'`.
- The Lightsail box has been bootstrapped (step 5) — the SSH step will fail otherwise.

Then push to `master` and watch the Actions tab.

---

## Verification checklist

After the first successful deploy:
1. `dig app.<domain>` resolves to CloudFront, `dig api.<domain>` to the Lightsail IP.
2. `curl -I https://api.<domain>/scalar/v1` → `200 OK`, valid Let's Encrypt cert.
3. `https://app.<domain>` loads the Angular app with no CORS or mixed-content errors in the browser console.
4. Register → log in → create a transaction → dashboard updates.
5. On the VM: `docker compose -f /opt/aerarium/docker-compose.prod.yml logs api | grep -i migrat` shows migrations applied.
6. `aws ssm get-parameters-by-path --path /aerarium/prod --recursive --with-decryption --region <region>` returns all the parameters.
7. Trigger a dummy push to `master`; the workflow runs both jobs green.
8. Run `/opt/aerarium/backup.sh` manually once; confirm the dump appears in `s3://aerarium-db-backups/`.

---

## Day-2 operations

### Deploying a change
Push to `master`. That's it. Frontend goes to S3, API image gets built and the VM pulls it.

### Manually redeploying the API
```bash
ssh ubuntu@<lightsail-ip>
cd /opt/aerarium
docker compose -f docker-compose.prod.yml pull
docker compose -f docker-compose.prod.yml up -d
```

### Viewing logs
```bash
docker compose -f docker-compose.prod.yml logs -f api
docker compose -f docker-compose.prod.yml logs -f caddy
```

### Rotating a secret
```bash
aws ssm put-parameter --overwrite --type SecureString \
  --name /aerarium/prod/Jwt/Key --value "<new-value>"
# Then restart the API so it re-reads config:
ssh ubuntu@<lightsail-ip> 'cd /opt/aerarium && docker compose -f docker-compose.prod.yml restart api'
```

### Restoring from a backup
```bash
# On your laptop:
aws s3 cp s3://aerarium-db-backups/2026-04-10.sql.gz .
gunzip 2026-04-10.sql.gz
scp 2026-04-10.sql ubuntu@<lightsail-ip>:/tmp/
# On the VM:
docker compose -f /opt/aerarium/docker-compose.prod.yml exec -T postgres \
  psql -U aerarium -d aerarium < /tmp/2026-04-10.sql
```

### Applying new EF migrations
Migrations run automatically on startup because `RUN_MIGRATIONS_ON_STARTUP=true` is set in `docker-compose.prod.yml`. Just deploy.

### Changing the domain
1. Update `app.<new-domain>` and `api.<new-domain>` DNS records.
2. Issue a new ACM cert in us-east-1, attach it to CloudFront.
3. Update `apiUrl` in `src/Frontend/src/environments/environment.production.ts`.
4. Update `API_DOMAIN` in `/opt/aerarium/.env` on the VM, then `docker compose up -d caddy` so Caddy re-issues the Let's Encrypt cert.
5. Update the `Cors/AllowedOrigins/0` SSM parameter and restart the API.

### Scaling up
- Bigger Lightsail plan: stop instance → snapshot → create new instance from snapshot with larger plan → re-attach static IP.
- Move Postgres off-box: switch to Lightsail Managed Database or RDS and update the SSM connection string.

---

## Troubleshooting

| Symptom                                             | Cause / Fix                                                                                 |
|-----------------------------------------------------|---------------------------------------------------------------------------------------------|
| Caddy logs `tls: no certificate`                    | DNS for `api.<domain>` not pointing at the VM yet, or ports 80/443 blocked in Lightsail.    |
| API logs `Unable to get IAM security credentials`   | `.env` on the VM is missing `AWS_*` vars or the IAM user lacks SSM permissions.             |
| API logs `password authentication failed`           | `.env` `POSTGRES_PASSWORD` doesn't match the password in the SSM connection string.         |
| `CORS error` in browser                             | `Cors/AllowedOrigins/0` SSM parameter doesn't match the frontend origin exactly (scheme!).  |
| `404` on deep link like `/transactions/123`         | CloudFront custom error responses not configured — see step 2.3.                            |
| CI `api` job fails on `ssh-action`                  | `LIGHTSAIL_SSH_KEY` secret must be the full PEM including `-----BEGIN/END-----` lines.      |
| `docker compose pull` fails with `unauthorized`     | GHCR PAT expired — regenerate and `docker login ghcr.io` again on the VM.                   |
| Migrations not running                              | `RUN_MIGRATIONS_ON_STARTUP` env var missing or not `"true"` exactly (string).               |

---

## File reference

| File                                               | Purpose                                                      |
|----------------------------------------------------|--------------------------------------------------------------|
| `src/Api/Dockerfile`                               | Multi-stage build for the API image                          |
| `.dockerignore`                                    | Keeps build context small                                    |
| `docker-compose.prod.yml`                          | Runs api + postgres + caddy on the VM                        |
| `Caddyfile`                                        | Reverse proxy + automatic TLS                                |
| `src/Api/appsettings.Production.json`              | Non-secret prod defaults (secrets come from SSM)             |
| `src/Api/Program.cs`                               | SSM provider registration, CORS from config, startup migrate |
| `src/Frontend/src/environments/environment.production.ts` | Frontend API base URL                                 |
| `.github/workflows/deploy.yml`                     | CI/CD pipeline                                               |
