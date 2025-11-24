# Angular Integration Guide

This guide demonstrates how to integrate your Angular frontend with a .NET backend secured by PrimusSaaS.Identity.Validator.

## Overview

The integration involves:
1.  **Acquiring Tokens**: Using MSAL (Microsoft Authentication Library) for Azure AD.
2.  **Attaching Tokens**: Using an Angular HttpInterceptor to automatically add the Authorization header to API requests.
3.  **Handling Errors**: Gracefully handling 401/403 responses.

## 1. Install Dependencies

You will need the MSAL library for Angular:

```bash
npm install @azure/msal-browser @azure/msal-angular
```

## 2. Configure MSAL (Auth Config)

Create a auth-config.ts file to manage your Azure AD configuration.

```typescript
// src/app/auth-config.ts
import { LogLevel, Configuration, BrowserCacheLocation } from '@azure/msal-browser';

export const msalConfig: Configuration = {
  auth: {
    clientId: 'YOUR_AZURE_AD_CLIENT_ID', // The ID of your Angular App Registration
    authority: 'https://login.microsoftonline.com/YOUR_TENANT_ID',
    redirectUri: '/', // Must match your Azure AD registration
  },
  cache: {
    cacheLocation: BrowserCacheLocation.LocalStorage,
    storeAuthStateInCookie: false,
  },
  system: {
    loggerOptions: {
      loggerCallback: (level, message, containsPii) => {
        if (containsPii) { return; }
        switch (level) {
          case LogLevel.Error:
            console.error(message);
            return;
          case LogLevel.Info:
            console.info(message);
            return;
          case LogLevel.Verbose:
            console.debug(message);
            return;
          case LogLevel.Warning:
            console.warn(message);
            return;
        }
      }
    }
  }
};

// Add the scopes you defined in your .NET Backend App Registration
export const protectedResources = {
  primusApi: {
    endpoint: 'https://localhost:7000/api', // Your .NET API URL
    scopes: ['api://YOUR_API_CLIENT_ID/access_as_user'],
  },
};
```

## 3. Create the HTTP Interceptor

This interceptor will automatically attach the Access Token to outgoing requests to your API.

```typescript
// src/app/auth.interceptor.ts
import { Injectable } from '@angular/core';
import {
  HttpRequest,
  HttpHandler,
  HttpEvent,
  HttpInterceptor
} from '@angular/common/http';
import { Observable, from, switchMap } from 'rxjs';
import { MsalService } from '@azure/msal-angular';
import { protectedResources } from './auth-config';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {

  constructor(private authService: MsalService) {}

  intercept(request: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    // Check if the request is for our protected API
    if (request.url.startsWith(protectedResources.primusApi.endpoint)) {
      
      // Get the active account
      const account = this.authService.instance.getActiveAccount();
      
      if (!account) {
        // No user logged in, proceed without token (or handle redirect)
        return next.handle(request);
      }

      // Acquire token silently
      return from(
        this.authService.instance.acquireTokenSilent({
          account: account,
          scopes: protectedResources.primusApi.scopes
        })
      ).pipe(
        switchMap((result) => {
          // Clone the request and add the Authorization header
          const authReq = request.clone({
            headers: request.headers.set('Authorization', `Bearer ${result.accessToken}`)
          });
          return next.handle(authReq);
        })
      );
    }

    return next.handle(request);
  }
}
```

## 4. Register in App Module

Register the interceptor and MSAL modules in your app.module.ts.

```typescript
// src/app/app.module.ts
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { MsalModule, MsalService, MsalGuard, MsalInterceptor, MsalBroadcastService, MsalRedirectComponent } from '@azure/msal-angular';
import { PublicClientApplication, InteractionType } from '@azure/msal-browser';
import { msalConfig, protectedResources } from './auth-config';
import { AuthInterceptor } from './auth.interceptor';

@NgModule({
  declarations: [AppComponent],
  imports: [
    BrowserModule,
    HttpClientModule,
    MsalModule.forRoot(
      new PublicClientApplication(msalConfig),
      {
        interactionType: InteractionType.Redirect,
        authRequest: {
          scopes: protectedResources.primusApi.scopes
        }
      },
      {
        interactionType: InteractionType.Redirect,
        protectedResourceMap: new Map([
          [protectedResources.primusApi.endpoint, protectedResources.primusApi.scopes]
        ])
      }
    )
  ],
  providers: [
    {
      provide: HTTP_INTERCEPTORS,
      useClass: AuthInterceptor, // Or use MsalInterceptor directly
      multi: true
    },
    MsalService,
    MsalGuard,
    MsalBroadcastService
  ],
  bootstrap: [AppComponent, MsalRedirectComponent]
})
export class AppModule { }
```

## 5. Making API Calls

Now, standard HTTP calls in your components will be automatically secured.

```typescript
// src/app/dashboard.component.ts
import { Component, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-dashboard',
  template: `
    <div *ngIf="data">
      <h1>{{ data.message }}</h1>
      <p>User: {{ data.user.name }}</p>
    </div>
  `
})
export class DashboardComponent implements OnInit {
  data: any;

  constructor(private http: HttpClient) {}

  ngOnInit() {
    // The Interceptor will add the Bearer token automatically
    this.http.get('https://localhost:7000/api/secure')
      .subscribe({
        next: (response) => this.data = response,
        error: (err) => console.error('API Error', err)
      });
  }
}
```

## Troubleshooting

### CORS Errors
If you see CORS errors in the browser console:
1. Ensure your .NET backend has CORS enabled.
2. Ensure the WithOrigins in .NET matches your Angular URL (e.g., http://localhost:4200).

### 401 Unauthorized
1. Check the Network tab to ensure the Authorization header is present.
2. Verify the token in jwt.ms to ensure it has the correct aud (audience) and iss (issuer) claims.
3. Ensure the scopes in auth-config.ts match what the backend expects.
