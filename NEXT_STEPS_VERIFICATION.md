# Next Steps for Verification

## ✅ 1. Docs Site URL Configuration
**Status: COMPLETE**
- `DocsBaseUrl` is set to `http://localhost:3001` in `portal/backend/appsettings.json`
- This URL will be used in email notifications for documentation links

## 📋 2. Install .NET SDK and Build Backend

### Install .NET SDK
1. Download .NET 7.0 SDK from: https://dotnet.microsoft.com/download/dotnet/7.0
2. Run the installer
3. Verify installation:
   ```powershell
   dotnet --version
   ```

### Build the Backend
```powershell
cd "c:\Users\aakib\Primus SaaS"
dotnet build portal/backend/PrimusSaaS.Portal.Api.csproj
```

Expected output: Build succeeded with 0 errors

## 🧪 3. Sanity Test - Email Notification Flow

### Prerequisites
- Backend API running: `http://localhost:5000`
- Frontend running: `http://localhost:3000`
- Docs site running: `http://localhost:3001`
- Database seeded with test data

### Test Steps

#### Step 1: Create Test Application
1. Navigate to Applications page
2. Click "Add Application"
3. Fill in:
   - **Name**: Test Email App
   - **Owner Email**: your-email@example.com
   - **Description**: Testing email notifications
4. Click "Create"
5. Note the Application ID

#### Step 2: Assign Module to Application
1. Go to the application details page
2. Click "Assign Module"
3. Select a module (e.g., `@primus/identity-validator`)
4. Specify version constraint (e.g., `^1.0.0`)
5. Click "Assign"

#### Step 3: Publish Major Version
1. Navigate to Modules page
2. Select the module you assigned
3. Click "Publish Version"
4. Fill in:
   - **Version**: `2.0.0` (major version bump)
   - **Release Notes**: "Breaking changes - testing email notifications"
5. Click "Publish"

#### Step 4: Verify Email
Check the inbox for `your-email@example.com`. You should receive:

**Email Subject**: `Breaking Change Alert: @primus/identity-validator v2.0.0`

**Email Should Contain**:
- Module name and new version
- Release notes
- Link to documentation: `http://localhost:3001/docs/integrations/overview`
- Application name that uses this module
- Call to action to review changes

**Email Should Be Sent To**:
- ✅ Only application owners whose apps use this module
- ❌ NOT sent to owners of apps that don't use this module

### Verification Checklist
- [ ] .NET SDK installed and verified
- [ ] Backend builds successfully
- [ ] Created test application
- [ ] Assigned module to application
- [ ] Published major version (2.0.0)
- [ ] Email received at correct address
- [ ] Email contains correct module information
- [ ] Email links to docs site (`http://localhost:3001/...`)
- [ ] Email only sent to affected app owners

## 🐛 Troubleshooting

### Email Not Received
1. Check SMTP settings in `appsettings.json`
2. Verify `DocsBaseUrl` is correct
3. Check backend logs for email sending errors
4. Verify the app owner email is correct in the database

### Build Errors
1. Ensure .NET 7.0 SDK is installed (not just runtime)
2. Clean and rebuild:
   ```powershell
   dotnet clean
   dotnet build
   ```
3. Check for missing NuGet packages:
   ```powershell
   dotnet restore
   ```

### Links Not Working
1. Ensure docs site is running on port 3001
2. Verify `DocsBaseUrl` in appsettings matches the actual docs site URL
3. Check that the documentation pages exist at the expected paths

## 📊 Current Configuration

| Setting | Value |
|---------|-------|
| Backend API | http://localhost:5000 |
| Frontend | http://localhost:3000 |
| Docs Site | http://localhost:3001 |
| SMTP Host | smtp.gmail.com |
| From Email | khanakkijpr@gmail.com |

## 🚀 Production Deployment Notes

When deploying to production:
1. Update `DocsBaseUrl` to production docs URL (e.g., `https://docs.primussaas.com`)
2. Update SMTP credentials if using different email service
3. Ensure all three components (backend, frontend, docs) are accessible
4. Test the email flow in production environment
