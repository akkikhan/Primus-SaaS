# ↩️ Rollback & Recovery Procedures

**Objective:** Restore service within **5 minutes** of a failed deployment.

---

## 1. 🔄 Immediate Rollback (Kubernetes)

If the new deployment is unstable:

```bash
# 1. Check revision history
kubectl rollout history deployment/primus-api

# 2. Undo last rollout
kubectl rollout undo deployment/primus-api

# 3. Verify status
kubectl rollout status deployment/primus-api
```

---

## 2. 🔄 Immediate Rollback (Azure App Service)

If using Deployment Slots:

1.  Go to Azure Portal > App Service.
2.  Click **Deployment Slots**.
3.  Click **Swap** (Swap back Staging <-> Production).
4.  *Time to effect: ~30 seconds.*

---

## 3. 💽 Data Recovery (Point-in-Time Restore)

If a bad migration corrupted data:

1.  **Stop the App:** Prevent further corruption.
2.  **Restore DB:** Restore Azure SQL / AWS RDS to timestamp *before* deployment (e.g., `T-15 mins`).
3.  **Verify Data:** Check key tables.
4.  **Restart App:** Bring services back online.

---

## 4. 📢 Communication Template

**Subject:** [INCIDENT] Rollback of Release v1.2.0

> "We have detected elevated error rates following the release of v1.2.0.
> We are initiating an immediate rollback to v1.1.9.
> **Impact:** Service may be jittery for 2 minutes.
> **Next Steps:** Team will investigate logs and root cause."
