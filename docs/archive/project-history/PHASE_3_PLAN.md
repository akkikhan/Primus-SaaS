# Phase 3: Critical Features Implementation Plan

Based on the latest feedback, we are initiating Phase 3 to implement the "Top 3 Must-Have Features" (plus Health Checks).

## 1. Testable TenantResolver (Identity) 🔥 CRITICAL
**Goal:** Allow developers to implement `ITenantResolver` and register it via DI, enabling easy mocking and testing.

- [ ] Create `ITenantResolver` interface.
- [ ] Create `TenantResolverAdapter` to support existing lambda-based configuration.
- [ ] Update `AddPrimusIdentity` to register `ITenantResolver` in the service container.
- [ ] Update `PrimusIdentityOptions` to allow type-based registration.

## 2. Tenant Isolation Middleware (Identity) 🔥 CRITICAL
**Goal:** Prevent data leaks by enforcing tenant context presence at the middleware level.

- [ ] Create `TenantIsolationMiddleware.cs`.
- [ ] Implement logic to check `HttpContext.GetTenantContext()`.
- [ ] Return 403 Forbidden if tenant is missing (configurable).
- [ ] Add `app.UsePrimusTenantIsolation()` extension method.

## 3. Structured Exception Logging (Logging) 🔥 CRITICAL
**Goal:** Make debugging 10x easier by capturing full exception details as structured data.

- [ ] Update `Logger.cs` to add overloads for `Exception`.
- [ ] Implement exception deconstruction (Message, StackTrace, Source, InnerException).
- [ ] Ensure PII masking applies to exception messages.

## 4. Built-in Health Checks
**Goal:** Verify configuration and connectivity to IDP.

- [ ] Create `PrimusIdentityHealthCheck` implementing `IHealthCheck`.
- [ ] Verify OIDC discovery endpoint reachability.
- [ ] Verify JWKS endpoint reachability.

---

**Status:** Starting Implementation
**Date:** 2025-11-24
