# Business Stakeholder Live Demo Script

**Goal**: Demonstrate the strategic value of `PrimusSaaS.Identity.Validator`: Speed-to-Market, Risk Reduction, and Cost Savings.

**Audience**: Product Owners, Business Analysts, Executives.
**Time Estimate**: 5 Minutes

---

## 1. The Problem: "The Identity Tax" (1 Minute)
"Every time we build a new application, we pay a 'tax'. We spend weeks building login screens, connecting to Azure AD, auditing security, and fixing bugs.

This slows down our time-to-market and introduces risk. If one app is misconfigured, we are vulnerable."

---

## 2. The Solution: "Security as a Building Block" (1 Minute)
"We have solved this with the **Primus Identity Module**. 

Think of it as a pre-approved, secure building block. Instead of building a lock from scratch every time, we just install this certified lock."

**Key Benefits:**
1.  **Speed**: What used to take weeks now takes minutes.
2.  **Compliance**: It comes pre-configured with our security standards.
3.  **Flexibility**: We are not tied to one vendor. We can switch from Azure to Auth0 without rewriting code.

---

## 3. The Demonstration (2 Minutes)
*Open the `LiveDemoApi` project. Don't focus on the code syntax, focus on the "Config".*

**A. The "Empty" App**
"Here is a new application. Right now, it's open to the world. Anyone can access it. This is a risk."

**B. The "Installation"**
*Show `Program.cs` briefly.*
"I am going to install our Primus Identity block. I don't need to write complex security logic. I just plug it in."

**C. The "Configuration" (The Business Layer)**
*Show `appsettings.json`.*
"This is the most important part. This file controls our security policy. 
- Want to use **Azure AD** for employees? We just add it here.
- Want to use **Auth0** for external partners? We add it right next to it.
- Want to switch vendors next year? We just update this file. No code changes required."

---

## 4. The Result (1 Minute)
*Run the application and show the 401 Unauthorized error.*

"I've hit 'Run'. 
If I try to access the app now... **Access Denied**.

The system is secure. It is enforcing our corporate policies immediately.
We didn't spend a month building this. We did it in this meeting."

---

## 5. Strategic Takeaways (1 Minute)
"By adopting this standard, we achieve:
1.  **Faster Releases**: Developers focus on business features, not login pages.
2.  **Reduced Risk**: Security is handled centrally by experts, not reinvented by every team.
3.  **Future Proofing**: We control our identity providers, they don't control us."

"Ready to answer any questions on how this impacts our roadmap."
