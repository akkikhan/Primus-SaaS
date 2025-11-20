import { PublicClientApplication } from '@azure/msal-browser';

const tenantId = import.meta.env.VITE_AZURE_AD_TENANT_ID;
const clientId = import.meta.env.VITE_AZURE_AD_CLIENT_ID;
const redirectUri = import.meta.env.VITE_AZURE_AD_REDIRECT_URI ?? `${window.location.origin}/login`;

const authority = tenantId
  ? `https://login.microsoftonline.com/${tenantId}`
  : 'https://login.microsoftonline.com/common';

export const msalInstance = new PublicClientApplication({
  auth: {
    clientId: clientId ?? '00000000-0000-0000-0000-000000000000',
    authority,
    redirectUri,
  },
  cache: {
    cacheLocation: 'localStorage',
    storeAuthStateInCookie: false,
  },
});

export const isAzureAdConfigured = Boolean(tenantId && clientId);
