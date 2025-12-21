# 🎬 The "Cooking Show" Demo Script

**Strategy:** "Pre-baked ingredients, live assembly."
**Why:** Senior management gets bored watching `npm install`. They want to see the *result* and the *ease of use*.

---

## 🛠️ PRE-MEETING PREP (Do this 10 mins before)

1.  **Open VS Code** to `test-apps/executive-demo/server.js`.
2.  **Open Terminal** in that folder and run:
    ```bash
    npm install
    ```
    *(Do not do this live! It takes too long.)*
3.  **Open Browser** to your Primus Portal (`http://localhost:5173`).
4.  **Login** so you are on the Dashboard.

---

## 🎥 LIVE DEMO START

### Part 1: The "Manager" View (Browser)
*Narrative: "Let's see how quickly we can onboard a new application."*

1.  **Create App**:
    *   Click **"Create Application"**.
    *   Name: "Finance Dashboard 2.0".
    *   Stack: "NodeJS".
    *   Click **Submit**.
2.  **Assign Module**:
    *   Go to **Modules** tab.
    *   Assign `IdentityValidator`.
    *   *Say: "We just attached enterprise security policies to this app."*
3.  **Get Keys**:
    *   Go to **Overview** tab.
    *   *Say: "Here are the secure credentials generated for the developer."*
    *   **COPY** the Client ID and Client Secret.

---

### Part 2: The "Developer" View (VS Code)
*Narrative: "Now I put on my developer hat. I have a basic API ready, but it needs security."*

1.  **Show `server.js`**:
    *   Show the file I created for you (`test-apps/executive-demo/server.js`).
    *   *Say: "I have the SDK installed. I just need to connect it."*

2.  **Paste Credentials**:
    *   Paste the ID and Secret into the config block (lines 15-16).
    *   *Say: "I simply paste the credentials we just generated."*

3.  **Enable Security**:
    *   **Uncomment** the code block at the bottom (lines 35-40).
    *   *Say: "And with this one block, I apply the security policy. No complex code, just configuration."*

4.  **Run It**:
    *   Type: `node server.js`
    *   *Say: "The server is live."*

---

### Part 3: The "Proof" (Terminal/Browser)
*Narrative: "Does it actually work?"*

1.  **Fail First**:
    *   Open a browser tab to `http://localhost:3000/secure`.
    *   **Result:** `Unauthorized` (or error).
    *   *Say: "Access Denied. The system is protecting the data."*

2.  **Succeed**:
    *   (Optional) If you have a way to generate a token quickly, do it.
    *   OR just say: "Once the frontend sends the valid token we generated earlier, this unlocks."

---

## 🏁 Closing
"We went from zero to a secured, policy-compliant API in under 3 minutes. That is the power of Primus SaaS."
