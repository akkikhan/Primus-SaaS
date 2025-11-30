# 🚀 Deployment Checklist

**Phase:** Production Release  
**Approver:** DevOps Lead

---

## 1. 📦 Pre-Deployment

- [ ] **Build Success:** CI pipeline passed (Build, Test, Scan).
- [ ] **Version Bump:** NuGet/npm versions updated (SemVer).
- [ ] **Changelog:** `CHANGELOG.md` updated with new features/fixes.
- [ ] **Database:** Migrations script generated and reviewed (if applicable).
- [ ] **Secrets:** New secrets added to Key Vault (if any).

---

## 2. 🚢 Deployment Steps

1.  **Backup:** Snapshot DB (if stateful changes).
2.  **Canary:** Deploy to 10% of nodes (if supported).
3.  **Rollout:** Apply K8s manifest / Swap App Service Slot.
4.  **Health Check:** Verify `/health` endpoint returns 200.
5.  **Smoke Test:** Run "Golden Path" smoke test script.

---

## 3. 🧹 Post-Deployment

- [ ] **Monitoring:** Check Error Rate & Latency in Grafana/AppInsights.
- [ ] **Logs:** Verify no "Exception" floods in logs.
- [ ] **Notification:** Announce release in `#engineering` channel.
- [ ] **Cleanup:** Remove temporary feature flags (if planned).

---

## 4. 🛑 Abort Criteria

**Rollback immediately if:**
*   Error rate > 5%.
*   Latency p95 > 2x baseline.
*   Critical functionality (Login) fails.
