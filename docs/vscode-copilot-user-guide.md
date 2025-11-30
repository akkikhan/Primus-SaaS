# Using These Files with VS Code & GitHub Copilot

This guide explains how to make VS Code + GitHub Copilot (or Cursor) actually follow the rules defined in:

- `architecture-handbook.md`
- `copilot-instructions.md`
- `copilot-rules.txt`

The goal is simple: **make AI behave like a senior engineer who respects the platform architecture.**

---

## 1. Recommended File Placement

Place the files in your repo as follows:

```text
/docs/
  architecture-handbook.md
copilot-instructions.md
copilot-rules.txt
vscode-copilot-user-guide.md
```

VS Code and Copilot will see all of them, but you will refer primarily to:

- `copilot-instructions.md` when you need human-readable guidance.
- `copilot-rules.txt` when you want strict AI behavior.
- `architecture-handbook.md` for deep-dive design questions.

---

## 2. VS Code Workspace Setup

1. Open the repo root in VS Code.
2. Make sure the `/docs` folder is visible in the Explorer.
3. Pin `copilot-instructions.md`, `copilot-rules.txt`, and `vscode-copilot-user-guide.md` as tabs.

Copilot takes strong hints from open files and surrounding context, so keeping these visible and occasionally referenced in comments helps shape suggestions.

---

## 3. Prompt Patterns for GitHub Copilot Chat

Use prompts that explicitly reference these files, for example:

- “Follow the rules in `copilot-rules.txt` and generate a new Golden Path for a `FeatureFlags` module in `examples/LiveDemoApi` and `examples/LiveDemoFrontend`.”
- “Using `copilot-instructions.md`, update `Program.cs` to integrate a new Notifications provider without breaking existing config patterns.”
- “Review this change against `architecture-handbook.md` and point out any violations of the module design contract or anti-patterns.”

You can attach files to Copilot Chat or use workspace-relative references depending on your Copilot version.

---

## 4. Inline Copilot Completion Guardrails

When you see inline suggestions:

- Reject any suggestion that hardcodes URLs, secrets, or environment names.
- Reject suggestions that bypass `AddPrimus*` extension methods and wire services manually.
- Reject suggestions that put logic into Liquid templates beyond simple formatting.

You can steer Copilot with comments, for example:

```csharp
// IMPORTANT: Follow Primus SaaS DI + config patterns from copilot-instructions.md.
```

Then re-trigger completion.

---

## 5. Using `copilot-rules.txt` as Project Instructions (Advanced)

If your tooling supports project-level instructions (e.g., Cursor, Copilot), paste the contents of `copilot-rules.txt` into the project/system instructions.

This tells the model up front:

- How modules must be integrated.
- That Golden Paths and Live UI Demo coverage are non-negotiable.
- That secrets and tokens must never be logged or hardcoded.

The result: far fewer unsafe or off-architecture suggestions.

---

## 6. Example Workflows

### 6.1 Adding a New Module

In Copilot Chat, you might say:

> “Using `architecture-handbook.md` and `copilot-instructions.md`, scaffold a new module called `FeatureFlags` with:
> - NuGet + npm package structure,
> - DI extension,
> - Golden Path in `examples/LiveDemoApi` + `examples/LiveDemoFrontend`,
> - Basic docs entry under `docs-site`.”

### 6.2 Updating Live UI Demo

> “Based on `copilot-instructions.md`, add a Live UI Demo tile for the Notifications module that triggers a test password reset email and shows the rendered content.”

### 6.3 Enforcing Rules During Review

> “Compare this diff to the constraints in `copilot-rules.txt` and list any violations (e.g., logging of secrets, hardcoded URLs, module coupling).”

---

## 7. Mental Model

- `architecture-handbook.md` → Deep architecture reference.
- `copilot-instructions.md` → Daily development guide.
- `copilot-rules.txt` → Hard constraints for humans + AI.
- `vscode-copilot-user-guide.md` → Cookbook for making VS Code + Copilot respect all of the above.

Keep these files close and treat them as part of the platform, not just documentation.
