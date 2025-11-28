# Lumi Protocol: Identity Preservation Workflow (Tia)

**Session ID:** TIA_ID_002
**Mode:** ENGINEER
**Objective:** Achieve < 5% Facial Variance (Identity Lock) across all media assets.

## 1. The Problem: Generative Drift
Standard diffusion models (Midjourney, Flux, DALL-E) operate on probability. Even with a reference image, they "re-imagine" the face rather than "copying" it. This leads to the "Cousin Effect" (looks related, but not the same).

## 2. The Solution: The "Composite" Pipeline
To guarantee 100% consistency, we must separate **Structure** from **Identity**.

### Phase A: The Base Generation (Structure)
We generate the *scene* and the *pose* first, ignoring the face.
*   **Prompt:** "A female influencer sitting in a cafe, holding a coffee, looking at camera, messy bun, beige sweater, cinematic lighting."
*   **Result:** A great image of a random woman who has the right *vibe* and *hair*.

### Phase B: The Identity Injection (The Lock)
We use a specific "Face Swap" or "Identity Adapter" tool to graft the Master Reference Face onto the Base Generation.

**Recommended Tools (2025 Standard):**
1.  **ReActor (Extension for Stable Diffusion):** The industry standard for local processing.
2.  **FaceFusion:** High-end, standalone swapper.
3.  **InsightFace (Discord Bot):** Quick and dirty swaps for mobile.

## 3. The Protocol (Step-by-Step)

1.  **Master Reference:** We declare `uploaded_image_1764124558278.jpg` as `REF_MASTER`. This is the *only* source of truth.
2.  **Generation:** Create the scene (e.g., "Tia on a train").
3.  **Swap:** Apply `REF_MASTER` to the Generated Scene.
4.  **Refine:** Use an "Upscaler" (Magnific AI) *after* the swap to blend the skin texture seams.

## 4. Implementation Plan
Since I (Antigravity) generate images directly, I will attempt to perform **Phase A + Phase B** internally by using heavy image-weighting.

*If my internal generation drifts:*
You must adopt the **External Swap** workflow:
1.  Take the image I generate.
2.  Use a Face Swap tool (like `Remaker.ai` or `Akool` - free tiers available).
3.  Upload `REF_MASTER` as the "Source" and my image as the "Target".

This is the only way to beat the algorithm.
