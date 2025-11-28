# Tia - Voice & Audio Engineering Guide (ElevenLabs)

**Objective:** Create a voice that sounds "imperfectly human," not like a robotic narrator.

## 1. Voice Selection (The "Instrument")
*   **Platform:** ElevenLabs (Standard or Creator Plan).
*   **Recommended Voice Models:**
    *   **"Rachel"** (American, Young, Calm) - *Good base.*
    *   **"Mimi"** (Australian, Childish) - *Too young.*
    *   **"Nicole"** (American, Whispery) - *Great for "cozy" vibes.*
    *   **PRO TIP:** Clone a real human voice (with permission) or use the **Voice Design** tool to generate a unique voice.
    *   **Target Attributes:** Female, Age 20-25, American Accent, Soft/Raspy Tone.

## 2. The "Human settings" (The Knobs)
*   **Stability:** **35% - 40%**
    *   *Why:* Lower stability allows the voice to have more "cracks," pitch shifts, and emotional variance. 100% is a robot; 35% is a human.
*   **Similarity Boost:** **75%**
    *   *Why:* Keeps the voice recognizable but allows for range.
*   **Style Exaggeration:** **15%**
    *   *Why:* Adds a bit of "influencer energy" without sounding cartoonish.

## 3. Scripting for Realism (SSML & Phonetics)
*Don't just paste text. You must "direct" the AI.*

### A. The "Umm" and "Like"
Type these phonetically to get the right sound:
*   "Umm..." -> `...um...` (use ellipses)
*   "Like" -> `like,` (comma adds a micro-pause)
*   "So..." -> `Sooo...` (elongates the word)

### B. Pauses (The Breath)
Use the `<break>` tag or dashes for timing.
*   **Short Pause (Thinking):** `I don't know... - maybe?`
*   **Long Pause (Dramatic):** `And then I saw it. <break time="0.8s" /> The Eiffel Tower.`

### C. Laughs & Sighs
*   **Laugh:** Write `[laughs]` or `(haha)` - *Note: ElevenLabs Speech-to-Speech is better for this.*
*   **Sigh:** `[sighs]`

## 4. The "Speech-to-Speech" Hack (Advanced)
If text-to-speech feels too stiff:
1.  Record **YOURSELF** (or a friend) reading the script on your phone. It doesn't matter if your voice sounds bad.
2.  Upload that audio to ElevenLabs **Speech-to-Speech**.
3.  Select "Tia's Voice."
4.  **Result:** The AI will copy your *exact* intonation, timing, and laughter, but use Tia's vocal cords. This is the secret to 100% realism.
