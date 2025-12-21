# Copilot Task – Implement Text-to-PDF / Document Renderer Module (Primus SaaS)

You are operating inside the **Primus SaaS** repository. Your job is to implement the new **Text-to-PDF / Document Renderer**
module end-to-end, similar in spirit to the Identity, Logging, and Notifications modules.

Use these files as primary references:

- `architecture-handbook.md`
- `copilot-instructions.md`
- `copilot-rules.txt`
- `docs/TextToPdfModule_Requirements.md`

You must follow all architectural rules, avoid anti-patterns, and ensure the module is **self-testable** with multiple
levels of validation (basic, validation, complexity, full).

---

## High-Level Goals

1. Create a reusable .NET SDK module for text-to-PDF rendering.
2. Integrate it into `examples/LiveDemoApi` with proper endpoints.
3. Integrate it into `examples/LiveDemoFrontend` with a “Text to PDF” screen.
4. Implement a **self-test subsystem** with different modes of validation.
5. Add unit + integration + Golden Path tests.
6. Confirm everything runs locally without breaking existing modules.

---

## Phase 0 – Read & Plan

Before writing code:

1. Read:
   - `architecture-handbook.md`
   - `copilot-instructions.md`
   - `copilot-rules.txt`
   - `docs/TextToPdfModule_Requirements.md`
2. Summarize (for yourself) the key rules:
   - DI + configuration patterns.
   - Golden Path + Live UI Demo expectations.
   - Logging & security constraints.
   - Prohibition on tight coupling and logging content bodies.

Do **not** skip this step; your implementation must match these rules.

---

## Phase 1 – SDK (.NET) Implementation

1. Under `sdk/dotnet/`, create the new module (e.g., `Primus.Documents`):
   - Options class (`DocumentRendererOptions`).
   - Content type enum and `RenderDocumentRequest` model.
   - `IDocumentRenderer` interface.
   - `DefaultDocumentRenderer` implementation using a PDF library.
2. Add DI extension `AddPrimusDocumentRenderer` as defined in the requirements.
3. Add the self-test service interface and implementation:
   - `IDocumentRendererSelfTest`
   - `DocumentRendererSelfTest`
4. Ensure configuration binding is done via `PrimusDocuments` section, environment-friendly.
5. Add unit tests for:
   - `DefaultDocumentRenderer` behavior.
   - `DocumentRendererSelfTest` in `basic`/`validation`/`complexity` modes.

Validation criteria for this phase:

- `dotnet build` succeeds for SDK and tests.
- Unit tests pass.

---

## Phase 2 – LiveDemoApi Integration

1. Wire `AddPrimusDocumentRenderer(builder.Configuration);` in `examples/LiveDemoApi/Program.cs`.
2. Add `DocumentRendererController` with endpoints:
   - `POST /api/documents/render`
   - `POST /api/documents/render/link` (if link mode is implemented)
   - `GET /api/documents/download/{token}` (optional for link mode)
   - `POST /api/documents/self-test`
3. Enforce:
   - Input validation (title, content length, supported content type).
   - HTTP error codes for invalid input.
   - Respect for `SelfTestEnabled` flag (disable self-tests in production-like configs).
4. Integrate logging via the Logging module (metadata only).
5. Add integration tests using the existing testing stack to verify:
   - Rendering success and PDF content type.
   - Proper error handling for invalid inputs.
   - Self-test endpoint returns structured results and honors `SelfTestEnabled`.

Validation criteria for this phase:

- `dotnet test` passes for LiveDemoApi tests.
- `dotnet run` for LiveDemoApi starts cleanly (no unhandled exceptions).

---

## Phase 3 – LiveDemoFrontend Integration

1. Add a new “Text to PDF” screen in `examples/LiveDemoFrontend`:
   - Inputs: title, subtitle, content, content type selector.
   - Buttons for:
     - `Generate PDF` (direct render).
     - `Generate Link` (if supported).
     - `Run Self-Test` with selectable modes (`basic`, `validation`, `complexity`, `full`).
2. Call the LiveDemoApi endpoints created in Phase 2.
3. Display self-test results in a structured table (name, category, status, duration, content length, error).
4. Integrate navigation: ensure users can reach this screen from the main Live Demo menu.

Validation criteria for this phase:

- LiveDemoFrontend builds and runs without errors.
- Manual test: user can generate and download a PDF.
- Manual test: user can run self-tests and see meaningful results.

---

## Phase 4 – Configuration & Golden Path Documentation

1. Update `.env.example` and `appsettings.*` for `PrimusDocuments` configuration keys.
2. Add a short **Golden Path** section to the appropriate documentation (e.g., README in `examples/` or docs-site):
   - How to start LiveDemoApi + LiveDemoFrontend.
   - How to navigate to “Text to PDF”.
   - How to perform a basic render and run a full self-test.
3. Ensure configuration patterns match existing modules and respect environment overrides.

Validation criteria for this phase:

- Documentation builds (if docs-site uses a static generator).
- Following the documented steps reproduces the Golden Path.

---

## Phase 5 – Self-Validation & Rule Compliance

Before you consider the task complete:

1. Re-read `copilot-rules.txt` and check your work against it:
   - No secrets or document bodies in logs.
   - No tight coupling between modules.
   - Proper use of DI and configuration binding.
2. Run all tests:

```bash
dotnet test
```

3. Run LiveDemoApi and LiveDemoFrontend together and perform a manual Golden Path:
   - Render a sample markdown document.
   - Download the resulting PDF.
   - Run a **full** self-test and inspect the results.

4. Summarize:
   - Files created/modified.
   - How to run the Golden Path (commands + URLs).
   - Any limitations or TODOs clearly marked in documentation.

---

## Master Prompt (for Chat)

You can give the following single prompt to an AI assistant (e.g., GitHub Copilot Chat or Cursor) inside the Primus repo:

> Using `architecture-handbook.md`, `copilot-instructions.md`, `copilot-rules.txt`, and `docs/TextToPdfModule_Requirements.md` as the source of truth, implement the new Text-to-PDF / Document Renderer module end-to-end.  
> Follow the phases in `docs/TextToPdfModule_Copilot_Task.md`.  
> Ensure the module includes a self-test subsystem with multiple modes (`basic`, `validation`, `complexity`, `full`) exposed via `/api/documents/self-test`, plus a “Text to PDF” screen in LiveDemoFrontend that can run these self-tests.  
> After implementation, run all tests, perform a manual Golden Path (render + download), run a full self-test, and then summarize the changes and how to reproduce the Golden Path.
