# App Update & Notification Process Guide

This document outlines the end-to-end process for updating modules in the Primus SaaS platform, from publishing new versions to notifying clients and applying updates.

## 1. Overview

The update process is fully automated and event-driven:
1.  **Publisher** pushes a new version to the registry (npm or NuGet).
2.  **Registry** triggers a webhook to the Primus SaaS Portal.
3.  **Portal** validates the webhook, creates a new `ModuleVersion`, and identifies affected applications.
4.  **Notification Service** emails application owners based on their preferences.
5.  **Client** logs into the Portal, views the "Upgrade Manager", and applies the update.

## 2. Publishing Updates

### NPM Modules
When you publish a new version of a module to the npm registry:
```bash
npm publish
```
The npm registry triggers the configured webhook endpoint: `POST /api/webhooks/npm-registry`.

### NuGet Packages
When you push a new package to NuGet.org:
```bash
dotnet nuget push bin/Release/MyPackage.1.0.0.nupkg -k <ApiKey> -s https://api.nuget.org/v3/index.json
```
NuGet triggers the configured webhook endpoint: `POST /api/webhooks/nuget-registry`.

## 3. Webhook Processing

The `WebhooksController` handles the incoming events:

1.  **Validation**:
    *   Verifies the `X-Npm-Signature` or `X-NuGet-Signature` using the configured secrets (`Webhooks:NpmSecret`, `Webhooks:NuGetSecret`).
    *   Ensures the payload is valid JSON.

2.  **Mapping Check**:
    *   Looks up the package name in the `PackageRegistryMappings` table to find the corresponding internal `ModuleId`.
    *   If the package is not registered, the webhook returns `404 Not Found` (and logs a warning).

3.  **Version Creation**:
    *   Checks if the version already exists.
    *   Creates a new `ModuleVersion` record with:
        *   `Version`: From the payload.
        *   `ReleasedAt`: From the payload timestamp.
        *   `IsBreakingChange`: Automatically detected (true if Major version increments).
        *   `Changelog`: Link to the tarball or download URL.

## 4. Client Notification

Once the version is created, the `EmailService` is triggered:

1.  **Identify Targets**: Finds all `Applications` currently using the updated `Module`.
2.  **Check Preferences**: Checks `NotificationPreference` for each app owner:
    *   `EmailOnNewVersion`: For minor/patch updates.
    *   `EmailOnBreakingChange`: For major updates.
3.  **Send Email**: Sends an email to the owner (and any `AdditionalEmails`) with:
    *   Module Name & New Version.
    *   Release Notes & Changelog link.
    *   Upgrade commands (npm/NuGet).

## 5. Client Update Experience

Clients manage updates via the **Upgrade Manager** in the Portal:

1.  **Dashboard Notification**: The dashboard shows a summary of available upgrades.
2.  **Upgrade Manager Page**:
    *   Lists all applications and their modules.
    *   Shows "Update Available" status with a "Breaking" flag if applicable.
    *   **View Changelog**: Opens a modal with release notes for the current vs. latest version.
3.  **One-Click Upgrade**:
    *   Clicking "One-click Upgrade" calls `POST /api/upgrade/applications/{appId}/modules/{moduleId}/upgrade`.
    *   This updates the `ApplicationModule` record to point to the new `ModuleVersionId`.
    *   The client's application configuration is now updated (effective on next config fetch/deployment).

## 6. Configuration

Ensure the following are configured in `appsettings.json`:

```json
{
  "Webhooks": {
    "NpmSecret": "your-npm-webhook-secret",
    "NuGetSecret": "your-nuget-webhook-secret"
  },
  "Email": {
    "SmtpHost": "smtp.example.com",
    ...
  }
}
```
