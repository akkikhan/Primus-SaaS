# Tia DNA: The Master Prompt Guide (Identity Lock)

**Objective:** To recreate Tia's exact facial structure and vibe in *any* AI image generator (Midjourney, Flux, Stable Diffusion) by using precise visual tokens.

---

## 1. The "Base DNA" Prompt (The Core)
*Always start your prompt with this block. Do not change these words.*

> **Subject:** "A photorealistic candid portrait of a 23-year-old mixed-ethnicity female influencer named Tia. She has a heart-shaped face, soft jawline, and almond-shaped dark brown eyes. Her skin has natural texture, slight pores, and faint freckles across the nose (no plastic smoothing). She has dark brown hair styled in a loose, messy bun with stray strands framing her face."
>
> **Style/Outfit:** "She is wearing an oversized cream-colored ribbed knit sweater. She is wearing delicate layered gold necklaces and small gold hoop earrings. The aesthetic is 'cozy girl next door'."

---

## 2. The "Vibe" Modifiers (Lighting & Camera)
*Append this to the end of every prompt to maintain the 'Instagram' look.*

> **Camera:** "Shot on 35mm film, Kodak Portra 400, slight film grain, soft focus background (bokeh)."
> **Lighting:** "Golden hour natural lighting, warm sun flare, subsurface scattering on skin, soft shadows, no harsh studio lights."

---

## 3. Negative Prompts (What to Ban)
*Put this in the `--no` or `Negative Prompt` field.*

> "heavy makeup, plastic skin, airbrushed, 3d render, cartoon, anime, blue steel stare, supermodel pose, studio lighting, cold tones, neon lights, symmetry, perfection, lipstick, mascara clumps."

---

## 4. Scenario Templates (Copy & Paste)

### Scenario A: The Coffee Shop (Default)
> [Insert Base DNA] + "She is sitting at a wooden table in a sunlit cafe, holding a white ceramic coffee cup with both hands. She is looking at the camera and laughing naturally. The background is a blurry window with city street view." + [Insert Vibe Modifiers]

### Scenario B: The Train Travel
> [Insert Base DNA] + "She is leaning against a train window, looking out at the passing green countryside. Profile view. She looks pensive and peaceful. Reflection in the glass." + [Insert Vibe Modifiers]

### Scenario C: The "Selfie" Update
> [Insert Base DNA] + "Extreme close-up selfie angle. She is holding the camera high. She is winking playfully. Background is a busy European street corner." + [Insert Vibe Modifiers]

---

## 5. Technical Settings (For Midjourney/Flux)
*   **Aspect Ratio:** `--ar 9:16` (for Shorts/Reels) or `--ar 16:9` (for YouTube Thumbnails).
*   **Stylize:** `--s 250` (Midjourney) - Keeps it realistic, not too "artistic."
*   **Image Weight:** If using the Reference Photo, set Image Weight (`--iw`) to **2.0** (Maximum) to force the AI to copy the face.

---

**Lumi Protocol Note:** The key to consistency is the **Jewelry (Gold Necklaces)** and the **Sweater**. These are "visual anchors." Even if the face drifts slightly, the uniform tricks the brain into seeing the same person.
