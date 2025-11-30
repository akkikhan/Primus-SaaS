# Text-to-PDF Module – Copilot Pack Usage

This pack contains everything needed to let an AI assistant (GitHub Copilot Chat, Cursor, etc.) implement the
**Text-to-PDF / Document Renderer** module inside the Primus SaaS repo, with intensive self-tests and validation.

## Files

- `TextToPdfModule_Requirements.md`  
  Full functional and technical requirements, including self-test and multi-level validation design.

- `TextToPdfModule_Copilot_Task.md`  
  Step-by-step task plan for Copilot (phases, expectations, and a master prompt at the bottom).

## How To Use With AI Copilot

1. Copy these files into your repo (recommended):

   ```text
   /docs/
     TextToPdfModule_Requirements.md
     TextToPdfModule_Copilot_Task.md
   ```

2. Open the Primus SaaS repo in VS Code (or Cursor).

3. Open these files in the editor so Copilot sees them in context.

4. Open Copilot Chat and paste the **Master Prompt** from the bottom of `TextToPdfModule_Copilot_Task.md`.

5. Let Copilot work phase by phase. When it pauses or asks for confirmation, keep it aligned with:
   - DI + configuration patterns from existing modules.
   - No logging of document bodies.
   - Proper LiveDemoApi + LiveDemoFrontend integration.

6. When Copilot finishes, manually run:

   ```bash
   dotnet test
   # and
   dotnet run  # for LiveDemoApi and the frontend equivalent
   ```

   Then manually verify the Golden Path:
   - Render a sample markdown document and download the PDF.
   - Run a **full self-test** from the Live Demo UI and inspect results.

This pack turns the Text-to-PDF module into a **self-validating, AI-friendly feature** that fits perfectly into the
Primus SaaS platform and Lumi-style governance.
