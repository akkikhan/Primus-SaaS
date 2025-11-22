# Development Guide for `trunked-npm-backend`

## 📚 Overview
The **trunked‑npm‑backend** example demonstrates how to integrate the **Primus Identity Validator** SDK in a plain Express backend.  It works out‑of‑the‑box when the SDK is installed from npm (`npm i primus-identity-validator@1.1.0`).

When developing locally you may want to use the **local SDK source** (via `npm link`).  This can cause a **TypeScript type conflict** with `@types/express` because the linked SDK pulls in its own version of `@types/express` (v5) while the example uses v4.  The compiler then reports errors such as:
```
Property 'param' is missing in type 'Request<...>'
```
These errors are **non‑blocking** – they only affect the local development workflow and will disappear once the SDK is installed from the npm registry.

## 🚧 Known Issue
```
src/server.ts:70:21 - error TS2345: Argument of type 'import(".../node_modules/@types/express-serve-static-core").Request<...>'
  is not assignable to parameter of type 'import(".../sdk/nodejs/primus-identity-validator/node_modules/@types/express").Request<...>'.
  Property 'param' is missing in type 'Request<...>'.
```
The root cause is a **duplicate `@types/express`** dependency with mismatched major versions.

## ✅ Recommended Work‑arounds
### 1️⃣ Use the published package (recommended)
```bash
# From the example directory
npm uninstall primus-identity-validator   # remove the linked version
npm install primus-identity-validator@1.1.0
npm run build
```
The example will compile cleanly because the SDK and the example now share the same `@types/express` version.

### 2️⃣ Use `npm pack` instead of `npm link`
```bash
# In the SDK directory
cd ../../sdk/nodejs/primus-identity-validator
npm pack   # creates primus-identity-validator-1.1.0.tgz

# In the example directory
cd ../../examples/trunked-npm-backend
npm install ../sdk/nodejs/primus-identity-validator/primus-identity-validator-1.1.0.tgz
npm run build
```
`npm pack` bundles the compiled package and its dependencies, avoiding the symlink conflict.

### 3️⃣ Align TypeScript dependencies manually
If you really need to keep the symlink, make the `@types/express` versions match:
```bash
# In the example directory
npm install @types/express@5   # or downgrade the SDK's @types/express to 4
npm run build
```
Be aware that the SDK itself targets Express v5, so downgrading may affect other SDK internals.

## 📦 Quick Checklist
- [ ] Prefer **npm install** of the published SDK for day‑to‑day development.
- [ ] If you must use the local source, use **npm pack** to avoid type conflicts.
- [ ] Only resort to manual version alignment if you understand the implications.

---

## 🎉 Happy Coding!
If you run into any other issues, feel free to open an issue on the repository or check the **GitHub Discussions** page.
