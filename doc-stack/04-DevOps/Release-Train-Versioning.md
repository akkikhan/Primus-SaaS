# 🚂 Release Train & Versioning Guidelines

**Standard:** Semantic Versioning (SemVer 2.0.0)  
**Format:** `MAJOR.MINOR.PATCH`

---

## 1. 🏷️ Versioning Rules

*   **MAJOR (1.0.0):** Breaking changes.
    *   *Example:* Renaming a public API method.
    *   *Action:* Requires migration guide.
*   **MINOR (1.1.0):** New features (backwards compatible).
    *   *Example:* Adding a new Notification Provider (Slack).
*   **PATCH (1.1.1):** Bug fixes.
    *   *Example:* Fixing a NullReferenceException.

---

## 2. 🚆 The Release Train

We ship on a **Weekly Cadence** (unless Hotfix).

| Day | Activity |
| :--- | :--- |
| **Mon** | Code Freeze for Release Branch. |
| **Tue** | QA & Staging Validation. |
| **Wed** | **Production Release Window (10:00 AM UTC).** |
| **Thu** | Post-Release Monitoring. |
| **Fri** | Planning for next sprint. |

---

## 3. 📦 Package Publishing

**NuGet (Public/Private Feed):**
1.  CI detects tag `v1.2.3`.
2.  Builds `Release` configuration.
3.  `dotnet pack`.
4.  `dotnet nuget push`.

**NPM (Public/Private Registry):**
1.  CI detects tag `v1.2.3`.
2.  `npm run build`.
3.  `npm publish`.

---

## 4. ⚠️ Deprecation Policy

1.  Mark method `[Obsolete]`.
2.  Log warning on usage.
3.  Support for **1 Major Version** (e.g., Deprecated in v2, Removed in v3).
