# Postgres Verification Guide (Deployments System)

This document explains how to **verify deployments, logs, and steps** stored in PostgreSQL using the `psql` CLI.

It applies to these tables:

- `deployments`
- `deployment_logs`
- `deployment_steps`

---

## 1. Connect to PostgreSQL

If Postgres is running in Docker:

```bash
docker exec -it blazor-postgres psql -U postgres -d deployments
```

If local Postgres:

```bash
psql -U postgres -d deployments
```

---

## 2. List All Tables

Check that all required tables exist:

```sql
\dt
```

Expected output should include:

```
deployments
deployment_logs
deployment_steps
```

---

## 3. Inspect Table Schemas

### 3.1 Deployments Table

```sql
\d deployments
```

Key columns:

- `id` (UUID, primary key)
- `application`
- `status`
- `started_at`
- `finished_at`

### 3.2 Deployment Logs Table

```sql
\d deployment_logs
```

Key columns:

- `id`
- `deployment_id` (FK → deployments.id)
- `timestamp`
- `level`
- `message`

### 3.3 Deployment Steps Table

```sql
\d deployment_steps
```

Key columns:

- `deployment_id` (FK → deployments.id)
- `step` (int)
- `status`
- `started_at`
- `finished_at`

Required constraint:

```sql
UNIQUE (deployment_id, step)
```

---

## 4. View All Deployments

```sql
SELECT
    id,
    application,
    status,
    started_at,
    finished_at
FROM deployments
ORDER BY started_at DESC;
```

Use this to:

- see deployment history
- confirm Success / Failed states
- find deployment IDs

---

## 5. View Logs for a Deployment

Replace `<DEPLOYMENT_ID>` with a real UUID.

```sql
SELECT
    timestamp,
    level,
    message
FROM deployment_logs
WHERE deployment_id = '<DEPLOYMENT_ID>'
ORDER BY timestamp;
```

This shows:

- Podman output
- error messages
- execution trace

---

## 6. View Steps for a Deployment

```sql
SELECT
    deployment_id,
    step,
    status,
    started_at,
    finished_at
FROM deployment_steps
WHERE deployment_id = '<DEPLOYMENT_ID>'
ORDER BY step;
```

Expected steps:

- 1 → Login
- 2 → Pull
- 3 → Tag
- 4 → Push

---

## 7. Deployment + Steps Combined View

This shows deployment state and each step together:

```sql
SELECT
    d.id,
    d.status AS deployment_status,
    s.step,
    s.status AS step_status,
    s.started_at,
    s.finished_at
FROM deployments d
JOIN deployment_steps s
  ON s.deployment_id = d.id
ORDER BY d.started_at DESC, s.step;
```

Use this to:

- confirm stepper correctness
- debug partial failures

---

## 8. Find Deployments Without Steps (Old Data)

```sql
SELECT
    d.id,
    d.status
FROM deployments d
LEFT JOIN deployment_steps s
  ON s.deployment_id = d.id
WHERE s.deployment_id IS NULL;
```

These are deployments created before step tracking existed. Safe to ignore or clean up.

---

## 9. Find Failed Steps

```sql
SELECT
    deployment_id,
    step,
    status
FROM deployment_steps
WHERE status = 'Failed';
```

Quick way to identify where deployments break.

---

## 10. Cleanup (Optional)

### 10.1 Mark Stuck Deployments as Failed

```sql
UPDATE deployments
SET status = 'Failed',
    finished_at = NOW()
WHERE status = 'Running';
```

### 10.2 Delete Old Test Deployments (optional)

```sql
DELETE FROM deployments
WHERE started_at < NOW() - INTERVAL '7 days';
```

---

## 11. Exit psql

```bash
\q
```

---

## Summary Checklist

- ✔ Tables exist
- ✔ Deployments are recorded
- ✔ Logs are stored
- ✔ Steps are tracked
- ✔ Status is consistent
- ✔ UI reflects DB truth

If all queries above work, the system is healthy.

---

## Final Note

This README is exactly what production systems **should** have and usually don't.

Put it in your repo root, commit it, and future-you will silently thank present-you.

When you're ready, next step is **#8 (registry browsing)** or **hardening (#9 cleanups + retention)**.