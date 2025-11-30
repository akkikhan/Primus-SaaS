# 🏗️ Infrastructure & Deployment Guide

**Target:** Azure Kubernetes Service (AKS) / AWS EKS / Docker  
**Strategy:** Containerized, Stateless, Environment-Driven

---

## 1. 🐳 Container Strategy

Primus applications are designed to be Docker-native.

### Standard Dockerfile Pattern
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["MySaaSApp/MySaaSApp.csproj", "MySaaSApp/"]
RUN dotnet restore "MySaaSApp/MySaaSApp.csproj"
COPY . .
WORKDIR "/src/MySaaSApp"
RUN dotnet build "MySaaSApp.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MySaaSApp.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
# COPY NotificationTemplates if needed
COPY --from=build /src/MySaaSApp/NotificationTemplates ./NotificationTemplates
ENTRYPOINT ["dotnet", "MySaaSApp.dll"]
```

---

## 2. ☸️ Kubernetes (AKS) Deployment

### Deployment YAML
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: primus-saas-api
spec:
  replicas: 3
  selector:
    matchLabels:
      app: primus-api
  template:
    metadata:
      labels:
        app: primus-api
    spec:
      containers:
      - name: api
        image: myregistry.azurecr.io/primus-api:v1.0
        env:
        # 🔑 Inject Secrets via K8s Secrets
        - name: PrimusIdentity__Issuers__0__Authority
          valueFrom:
            secretKeyRef:
              name: auth-secrets
              key: authority
        - name: PrimusNotifications__Smtp__Password
          valueFrom:
            secretKeyRef:
              name: email-secrets
              key: smtp-password
        ports:
        - containerPort: 80
```

---

## 3. 🔄 CI/CD Pipeline (GitHub Actions)

### Build & Test
1.  **Checkout Code**
2.  **Setup .NET 8**
3.  **Restore Dependencies** (NuGet/npm)
4.  **Run Unit Tests** (`dotnet test`)
5.  **Run Integration Tests**
6.  **Build Docker Image**

### Deploy (CD)
1.  **Login to Container Registry**
2.  **Push Image**
3.  **Update K8s Manifest** (Helm/Kustomize)
4.  **Verify Health Check** (`/health`)

---

## 4. 🌍 Environment Management

| Environment | Purpose | Configuration Source |
| :--- | :--- | :--- |
| **Local** | Developer testing | `.env`, `appsettings.Development.json` |
| **Dev** | Integration testing | CI/CD Variables, K8s ConfigMap |
| **Staging** | Pre-production mirror | K8s ConfigMap (Sanitized Prod Data) |
| **Prod** | Live traffic | Key Vault / Secret Manager |

**Rule:** Never commit environment-specific config files (like `appsettings.Production.json`) if they contain secrets. Use Environment Variables injection.
