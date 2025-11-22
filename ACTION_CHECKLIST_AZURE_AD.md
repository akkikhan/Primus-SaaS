# ✅ Quick Action Checklist: Fixing Azure AD Integration

**Priority**: Critical gaps that break Azure AD authentication  
**Timeline**: Recommended order of implementation

---

## 🔴 Critical Fixes (Must Do)

### ☐ 1. Rename PrimusClientId → PrimusTrackingId
**Why**: Conceptual confusion - developers think it's Azure AD Client ID  
**Impact**: High - affects entire codebase  
**Files to change**:
- `portal/backend/Models/Application.cs`
- `portal/backend/Controllers/ApplicationsController.cs`
- `portal/backend/Controllers/DocumentationController.cs`
- All database migrations

**Database Migration**:
```sql
ALTER TABLE Applications RENAME COLUMN PrimusClientId TO PrimusTrackingId;
```

---

### ☐ 2. Add Azure AD Fields to Application Model
**Why**: Portal needs to store Azure AD credentials for documentation  
**Impact**: High - enables correct documentation generation  
**Files to change**:
- `portal/backend/Models/Application.cs`

**Add these fields**:
```csharp
public AuthenticationMode AuthMode { get; set; } = AuthenticationMode.Local;
public string? AzureAdTenantId { get; set; }
public string? AzureAdClientId { get; set; }
```

**Create migration**:
```bash
cd portal/backend
dotnet ef migrations add AddAzureAdFieldsToApplication
dotnet ef database update
```

---

### ☐ 3. Update Application Creation Endpoint
**Why**: Need to collect Azure AD credentials when creating app  
**Impact**: High - portal can't generate correct docs without this  
**File**: `portal/backend/Controllers/ApplicationsController.cs`

**Update CreateApplicationRequest**:
```csharp
public record CreateApplicationRequest(
    string Name, 
    string Stack, 
    string AuthMode,           // NEW: "Local", "AzureAd", "Hybrid"
    string? AzureAdTenantId,   // NEW: Required for AzureAd mode
    string? AzureAdClientId,   // NEW: Required for AzureAd mode
    string? Description = null
);
```

---

### ☐ 4. Fix Documentation Generation
**Why**: Currently generates wrong configuration  
**Impact**: Critical - developers copy-paste and it doesn't work  
**File**: `portal/backend/Controllers/DocumentationController.cs`

**Change**: Generate different snippets based on `AuthMode`:
- If `AzureAd`: Use `AzureAdTenantId` and `AzureAdClientId`
- If `Local`: Use `PrimusTrackingId` and generated JWT secret

---

### ☐ 5. Update Frontend Create Application Form
**Why**: Need UI to collect Azure AD credentials  
**Impact**: High - users can't input Azure AD info  
**File**: `portal/frontend/src/pages/Applications.tsx` (or CreateApplicationDialog)

**Add fields**:
- Authentication Mode dropdown (Local / AzureAd / Hybrid)
- Azure AD Tenant ID input (conditional, if AzureAd selected)
- Azure AD Client ID input (conditional, if AzureAd selected)

---

## 🟡 Important Fixes (Should Do)

### ☐ 6. Update SDK Type Definitions
**Why**: Clarify what `clientId` means in different modes  
**Impact**: Medium - improves developer experience  
**File**: `sdk/nodejs/primus-identity-validator/src/types.ts`

**Add better documentation**:
```typescript
/**
 * Client ID - meaning depends on mode:
 * - AzureAd mode: Azure AD Application (Client) ID
 * - Local mode: Primus Portal tracking ID
 */
clientId: string;
```

---

### ☐ 7. Remove Login Proxy from Acme Dashboard
**Why**: Implements wrong two-token flow  
**Impact**: Medium - simplifies architecture  
**File**: `test-apps/acme-dashboard/server.js`

**Remove**:
```javascript
// DELETE THIS ENTIRE ENDPOINT
app.post('/login-proxy', async (req, res) => { ... });
```

---

### ☐ 8. Update Acme Dashboard Configuration
**Why**: Currently uses wrong credentials  
**Impact**: Medium - example app doesn't work correctly  
**File**: `test-apps/acme-dashboard/server.js`

**Change from**:
```javascript
clientId: 'PSP-CLI-711224',  // Portal tracking ID
```

**To**:
```javascript
clientId: 'YOUR_AZURE_CLIENT_ID',  // Azure AD Client ID
```

---

## 🟢 Nice to Have (Can Do Later)

### ☐ 9. Add Azure AD Setup Guide
**Why**: Developers don't know how to register app in Azure  
**Impact**: Low - documentation improvement  
**Create**: `AZURE_AD_SETUP_GUIDE.md`

**Include**:
- How to register app in Azure Portal
- How to get Tenant ID and Client ID
- How to configure redirect URIs
- How to enable ID tokens

---

### ☐ 10. Create Separate SDK Interfaces Per Mode
**Why**: Type safety - ensure required fields per mode  
**Impact**: Low - developer experience improvement  
**File**: `sdk/nodejs/primus-identity-validator/src/types.ts`

**Create**:
```typescript
export type PrimusIdentityOptions = 
  | AzureAdModeOptions 
  | LocalModeOptions 
  | HybridModeOptions;
```

---

### ☐ 11. Add Validation in SDK
**Why**: Catch configuration errors early  
**Impact**: Low - better error messages  
**File**: `sdk/nodejs/primus-identity-validator/src/validator.ts`

**Add**:
```typescript
if (options.mode === 'AzureAd' && !options.tenantId) {
  throw new Error('tenantId is required for AzureAd mode');
}
```

---

## 📊 Implementation Priority Matrix

| Priority | Task | Effort | Impact | Order |
|----------|------|--------|--------|-------|
| 🔴 Critical | Add Azure AD fields to Application | Medium | High | 1 |
| 🔴 Critical | Update Application creation endpoint | Low | High | 2 |
| 🔴 Critical | Fix documentation generation | Medium | Critical | 3 |
| 🔴 Critical | Update frontend form | Medium | High | 4 |
| 🔴 Critical | Rename PrimusClientId | High | High | 5 |
| 🟡 Important | Update SDK documentation | Low | Medium | 6 |
| 🟡 Important | Remove login proxy | Low | Medium | 7 |
| 🟡 Important | Update Acme Dashboard config | Low | Medium | 8 |
| 🟢 Nice to Have | Add Azure AD setup guide | Medium | Low | 9 |
| 🟢 Nice to Have | Separate SDK interfaces | Medium | Low | 10 |
| 🟢 Nice to Have | Add SDK validation | Low | Low | 11 |

---

## 🚀 Recommended Implementation Sequence

### Sprint 1: Core Data Model (1-2 days)
1. ✅ Add Azure AD fields to Application model
2. ✅ Create and run database migration
3. ✅ Update Application creation endpoint
4. ✅ Test application creation with Azure AD credentials

### Sprint 2: Documentation Fix (1 day)
5. ✅ Fix documentation generation logic
6. ✅ Test documentation output for both modes
7. ✅ Verify generated code snippets are correct

### Sprint 3: Frontend Updates (1 day)
8. ✅ Add auth mode selection to UI
9. ✅ Add conditional Azure AD fields
10. ✅ Test end-to-end application creation flow

### Sprint 4: Cleanup & Polish (1 day)
11. ✅ Rename PrimusClientId → PrimusTrackingId
12. ✅ Update all references
13. ✅ Update SDK documentation
14. ✅ Update example apps

### Sprint 5: Documentation (0.5 days)
15. ✅ Create Azure AD setup guide
16. ✅ Update integration guide
17. ✅ Create video walkthrough

---

## 🧪 Testing Checklist

After each sprint, verify:

### Sprint 1 Testing
- [ ] Can create application with Local mode
- [ ] Can create application with AzureAd mode (requires Tenant ID & Client ID)
- [ ] Can create application with Hybrid mode
- [ ] Azure AD fields are stored correctly in database
- [ ] Validation rejects AzureAd mode without required fields

### Sprint 2 Testing
- [ ] Documentation for Local mode uses PrimusTrackingId
- [ ] Documentation for AzureAd mode uses Azure AD credentials
- [ ] Generated code snippets are copy-paste ready
- [ ] PDF download works correctly

### Sprint 3 Testing
- [ ] UI shows/hides Azure AD fields based on mode selection
- [ ] Form validation works correctly
- [ ] Can create app through UI with all modes
- [ ] Application details page shows correct credentials

### Sprint 4 Testing
- [ ] All references to PrimusClientId updated
- [ ] No broken links or references
- [ ] Example apps work with correct configuration
- [ ] SDK documentation is clear

### Sprint 5 Testing
- [ ] Azure AD setup guide is accurate
- [ ] Can follow guide and successfully integrate
- [ ] Video walkthrough is clear and complete

---

## 🎯 Success Criteria

You'll know the implementation is correct when:

✅ **Portal Application Creation**:
- User selects "Azure AD" mode
- User enters Azure AD Tenant ID and Client ID
- Portal stores these credentials

✅ **Documentation Generation**:
- Generated code uses Azure AD Tenant ID and Client ID
- No references to PSP-CLI-XXXXXX in Azure AD mode docs
- Copy-paste code works immediately

✅ **Client Integration**:
- Developer copies generated code
- Pastes into their app
- Authentication works without modification

✅ **Runtime Flow**:
- User logs in with Azure AD
- Frontend gets Azure AD token
- Frontend sends token directly to API
- API validates token using Primus SDK
- No Primus Portal involved in auth flow

---

## 📝 Notes

- **Breaking Changes**: Renaming `PrimusClientId` is a breaking change
  - Consider doing this in a major version bump
  - Or provide migration script for existing apps

- **Backward Compatibility**: Existing apps in Local mode should continue working
  - Default `AuthMode` to `Local` for existing apps
  - Only new apps need to specify mode

- **Documentation**: Update all guides after implementation
  - INTEGRATION_GUIDE.md
  - README.md
  - API documentation

---

**Start with Sprint 1 to fix the core data model!** 🚀
