# 🔧 Quick Fix for Login Issue

## ✅ Problem Identified

The **backend is running** on: `http://localhost:5267`  
But the **frontend is trying to connect to**: `http://localhost:5000` (wrong port!)

This is why login fails - the frontend can't reach the backend API.

---

## 🚀 **Solution (2 Steps)**

### Step 1: Create `.env` File

In the terminal, run:

```powershell
cd c:\Users\Akki\Primus SaaS\portal\frontend
copy .env.example .env
```

### Step 2: Edit `.env` File

Open `portal/frontend/.env` and make sure the **first line** is:

```bash
VITE_API_BASE_URL=http://localhost:5267/api
```

**Important:** Change the port from `5000` to `5267` to match your running backend!

---

## 🔄 **Then Restart Frontend**

1. Stop the current dev server (Press `Ctrl+C` in the terminal)
2. Start it again:

```powershell
npm run dev
```

3. Refresh your browser at http://localhost:5173
4. Try logging in again with:
   - **Email:** `admin@primussaas.com`
   - **Password:** `Admin123!`

---

## ✅ **Status Check**

### Backend ✅
- **Running on:** http://localhost:5267
- **Status:** Active and ready
- **Database:** Migrated successfully

### Frontend ❌
- **Running on:** http://localhost:5173
- **Status:** Running but **wrong API URL**
- **Fix:** Update `.env` file (see above)

---

## 📝 **Complete .env File Template**

Your `.env` file should look like this:

```bash
# API Base URL - MUST match backend port!
VITE_API_BASE_URL=http://localhost:5267/api

# Azure AD (optional)
VITE_AZURE_AD_CLIENT_ID=
VITE_AZURE_AD_TENANT_ID=
VITE_AZURE_AD_REDIRECT_URI=http://localhost:5173/login

# Docs
VITE_DOCS_BASE_URL=https://akkikhan.github.io/Primus-SaaS

# Voice Assistant (optional)
VITE_OPENAI_API_KEY=
VITE_ELEVENLABS_API_KEY=
VITE_ELEVENLABS_VOICE_ID=EXAVITQu4vr4xnSDxMaL
```

---

## 🎯 **After the Fix**

Once you update the `.env` file and restart the frontend, you'll be able to:

1. ✅ Login successfully
2. ✅ See the voice assistant (purple mic button)
3. ✅ Test all voice commands
4. ✅ Access all portal features

---

## 🆘 **Still Having Issues?**

Check:
1. Backend is running: http://localhost:5267/api (should show Swagger UI)
2. Frontend `.env` has correct port: `5267` not `5000`
3. Frontend was restarted after changing `.env`
4. Browser cache is cleared (Ctrl+Shift+R)

---

**Quick Commands:**

```powershell
# Check if backend is responding
curl http://localhost:5267/api

# Create .env file
cd portal/frontend
copy .env.example .env
notepad .env

# Restart frontend
npm run dev
```

---

**🎉 Once fixed, you can test the voice assistant!**
