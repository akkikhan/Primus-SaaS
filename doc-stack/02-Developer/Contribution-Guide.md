# 🤝 Contribution Guide + Coding Standards

**Welcome!** We love contributions. Please follow these rules to keep the platform elite.

---

## 1. 📐 Coding Standards

### General
*   **Clean Code:** Follow SOLID principles.
*   **Async All the Way:** No `.Result` or `.Wait()`. Use `await`.
*   **No Magic Strings:** Use constants or configuration.

### C# / .NET
*   **Namespace:** `PrimusSaaS.<Module>.<SubNamespace>`
*   **Style:** Use standard C# conventions (PascalCase for methods, camelCase for locals).
*   **Nullable:** Enable `<Nullable>enable</Nullable>` in all projects.

### JavaScript / Node
*   **Style:** Use ESLint + Prettier.
*   **Types:** Use TypeScript (or JSDoc) for all public APIs.

---

## 2. 🧪 Testing Requirements

Before submitting a PR, you must:

1.  **Unit Tests:** Cover >80% of business logic.
2.  **Integration Tests:** Verify DI and Configuration binding.
3.  **Golden Path:** Verify the "Golden Path" example still works.
4.  **Live Demo:** If you added a feature, add a demo page for it.

---

## 3. 📝 Documentation

*   **Self-Documenting:** Public methods must have XML comments (`/// <summary>`).
*   **Update Docs:** If you change behavior, update the `docs-site` markdown.

---

## 4. 🚫 Anti-Patterns (Immediate Rejection)

*   Hardcoded secrets.
*   Tight coupling between modules (e.g., Identity referencing Notifications directly).
*   Business logic in Liquid templates.
*   "Works on my machine" (must work in Docker/CI).
