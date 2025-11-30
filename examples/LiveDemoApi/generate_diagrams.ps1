$diagrams = @{
    "01_modules_overview" = @"
graph TB
    subgraph Platform["🚀 Primus SaaS Platform"]
        subgraph Identity["🛡️ Identity Validator"]
            I1["Multi-Provider Auth<br/>Auth0 • Azure AD • Custom OIDC"]
            I2["JWT Validation<br/>Automatic token verification"]
            I3["Built-in Diagnostics<br/>Configuration visibility"]
            I4["⏱️ Integration: 20 minutes"]
        end
        
        subgraph Logging["📊 Logging Module"]
            L1["Structured JSON Logging<br/>Easy parsing & monitoring"]
            L2["PII Redaction<br/>GDPR/HIPAA compliant"]
            L3["Multi-Target Output<br/>Console • File • Cloud"]
            L4["⏱️ Integration: 10 minutes"]
        end
        
        subgraph Notifications["🔔 Notifications Module"]
            N1["Multi-Channel Delivery<br/>Email • SMS • Push"]
            N2["Template Engine<br/>HTML/Text with variables"]
            N3["Auto-Retry Logic<br/>Exponential backoff"]
            N4["⏱️ Integration: 20 minutes"]
        end
    end
    
    Total["✅ Total Integration Time: 60 minutes<br/>💰 Cost Savings: $34,550 per project"]
    
    Identity --> Total
    Logging --> Total
    Notifications --> Total
    
    style Platform fill:#f5f5f5,stroke:#333,stroke-width:2px
    style Identity fill:#E3F2FD,stroke:#2196F3,stroke-width:3px
    style Logging fill:#F3E5F5,stroke:#9C27B0,stroke-width:3px
    style Notifications fill:#FFF3E0,stroke:#FF9800,stroke-width:3px
    style Total fill:#E8F5E9,stroke:#4CAF50,stroke-width:4px,color:#2E7D32
"@

    "02_roi_comparison" = @"
graph LR
    subgraph Manual["❌ Manual Implementation"]
        M1["Development Time<br/>⏱️ 2-3 weeks"]
        M2["Developer Cost<br/>💵 $18,000"]
        M3["Security Audit<br/>💵 $10,000"]
        M4["Testing & QA<br/>💵 $5,000"]
        M5["Documentation<br/>💵 $2,000"]
        M6["Total Cost<br/>💰 $35,000"]
        
        M1 --> M6
        M2 --> M6
        M3 --> M6
        M4 --> M6
        M5 --> M6
    end
    
    subgraph Primus["✅ Primus SaaS"]
        P1["Integration Time<br/>⏱️ 3 hours"]
        P2["Developer Cost<br/>💵 $450"]
        P3["Security Audit<br/>✅ Included"]
        P4["Testing & QA<br/>✅ Minimal"]
        P5["Documentation<br/>✅ Complete"]
        P6["Total Cost<br/>💰 $450"]
        
        P1 --> P6
        P2 --> P6
        P3 --> P6
        P4 --> P6
        P5 --> P6
    end
    
    M6 -.->|"97% Reduction"| Savings["💎 Savings Per Project<br/>$34,550<br/><br/>📈 10 Projects/Year<br/>$345,500 Annual Savings"]
    P6 -.->|"97% Reduction"| Savings
    
    style Manual fill:#FFEBEE,stroke:#f44336,stroke-width:3px
    style Primus fill:#E8F5E9,stroke:#4CAF50,stroke-width:3px
    style Savings fill:#FFF9C4,stroke:#F57F17,stroke-width:4px
    style M6 fill:#EF5350,color:#fff,stroke:#c62828,stroke-width:3px
    style P6 fill:#66BB6A,color:#fff,stroke:#388E3C,stroke-width:3px
"@

    "03_integration_flow" = @"
graph TD
    Start([🚀 New .NET Project]) --> Install
    
    Install["📦 Step 1: Install Packages<br/>dotnet add package PrimusSaaS.Identity.Validator<br/>dotnet add package PrimusSaaS.Logging<br/>dotnet add package PrimusSaaS.Notifications<br/>⏱️ 5 minutes"]
    
    Install --> Config["⚙️ Step 2: Configure appsettings.json<br/>Add PrimusIdentity section<br/>Add PrimusLogging section<br/>Add Notifications section<br/>⏱️ 20 minutes"]
    
    Config --> Parallel{Parallel Setup}
    
    Parallel --> Identity["🛡️ Identity Setup<br/>AddPrimusIdentity in Program.cs<br/>⏱️ 3 minutes"]
    Parallel --> Logging["📊 Logging Setup<br/>AddPrimus logging<br/>⏱️ 3 minutes"]
    Parallel --> Notifications["🔔 Notifications Setup<br/>AddPrimusNotifications<br/>⏱️ 5 minutes"]
    
    Identity --> Templates
    Logging --> Templates
    Notifications --> Templates
    
    Templates["📄 Step 4: Create Templates<br/>Welcome.html<br/>Welcome.txt<br/>⏱️ 5 minutes"]
    
    Templates --> Middleware["🔧 Step 5: Add Middleware<br/>UseAuthentication<br/>UseAuthorization<br/>⏱️ 2 minutes"]
    
    Middleware --> Test["✅ Step 6: Test Components<br/>Health checks<br/>Auth flow<br/>Send notifications<br/>⏱️ 15 minutes"]
    
    Test --> Success([🎉 Production Ready!<br/>Total Time: ~60 minutes])
    
    style Start fill:#4CAF50,stroke:#2E7D32,stroke-width:3px,color:#fff
    style Success fill:#2196F3,stroke:#1565C0,stroke-width:3px,color:#fff
    style Install fill:#E3F2FD,stroke:#2196F3,stroke-width:2px
    style Config fill:#F3E5F5,stroke:#9C27B0,stroke-width:2px
    style Identity fill:#E3F2FD,stroke:#2196F3,stroke-width:2px
    style Logging fill:#F3E5F5,stroke:#9C27B0,stroke-width:2px
    style Notifications fill:#FFF3E0,stroke:#FF9800,stroke-width:2px
    style Templates fill:#FFF9C4,stroke:#F57F17,stroke-width:2px
    style Middleware fill:#E0F2F1,stroke:#00897B,stroke-width:2px
    style Test fill:#E8F5E9,stroke:#4CAF50,stroke-width:2px
"@

    "05_before_manual" = @"
graph TD
    Start([Need Authentication]) --> Research["📚 Research JWT Libraries<br/>⏱️ 4-8 hours"]
    
    Research --> Install["📦 Install Microsoft.AspNetCore<br/>.Authentication.JwtBearer"]
    
    Install --> Auth0Config["⚙️ Configure Auth0 Manually<br/>TokenValidationParameters<br/>JwtBearerEvents<br/>Custom error handling<br/>⏱️ 8-12 hours"]
    
    Auth0Config --> AzureConfig["⚙️ Configure Azure AD Manually<br/>Different issuer format<br/>Different claim names<br/>More custom logic<br/>⏱️ 8-12 hours"]
    
    AzureConfig --> Policies["🔐 Create Authorization Policies<br/>RequireAuth0<br/>RequireAzureAD<br/>Custom claim requirements<br/>⏱️ 4-6 hours"]
    
    Policies --> Diagnostics["🔍 Build Custom Diagnostics<br/>Extract configuration<br/>Display issuer info<br/>Show loaded schemes<br/>⏱️ 6-8 hours"]
    
    Diagnostics --> Testing["✅ Extensive Testing<br/>Test each provider<br/>Test edge cases<br/>Security review<br/>⏱️ 16-24 hours"]
    
    Testing --> Bugs["🐛 Fix Issues<br/>Issuer mismatches<br/>Audience problems<br/>Claim mapping<br/>⏱️ 8-16 hours"]
    
    Bugs --> Docs["📄 Write Documentation<br/>Configuration guide<br/>Troubleshooting<br/>Examples<br/>⏱️ 8-12 hours"]
    
    Docs --> Done([😓 Done After 2-3 Weeks<br/>500+ lines of code<br/>High maintenance burden])
    
    style Start fill:#f44336,stroke:#c62828,stroke-width:2px,color:#fff
    style Done fill:#f44336,stroke:#c62828,stroke-width:3px,color:#fff
    style Research fill:#FFEBEE,stroke:#f44336,stroke-width:2px
    style Auth0Config fill:#FFCDD2,stroke:#f44336,stroke-width:2px
    style AzureConfig fill:#FFCDD2,stroke:#f44336,stroke-width:2px
    style Policies fill:#FFCDD2,stroke:#f44336,stroke-width:2px
    style Diagnostics fill:#FFCDD2,stroke:#f44336,stroke-width:2px
    style Testing fill:#FFCDD2,stroke:#f44336,stroke-width:2px
    style Bugs fill:#EF5350,stroke:#c62828,stroke-width:2px,color:#fff
    style Docs fill:#FFCDD2,stroke:#f44336,stroke-width:2px
"@

    "06_after_primus" = @"
graph TD
    Start([Need Authentication]) --> Install["📦 Install Package<br/>dotnet add package<br/>PrimusSaaS.Identity.Validator<br/>⏱️ 2 minutes"]
    
    Install --> Config["⚙️ Configure appsettings.json<br/>Add PrimusIdentity section<br/>List issuers declaratively<br/>⏱️ 10 minutes"]
    
    Config --> Code["💻 Add to Program.cs<br/>builder.Services.AddPrimusIdentity()<br/>app.MapPrimusIdentityDiagnostics()<br/>⏱️ 3 minutes"]
    
    Code --> Test["✅ Test & Verify<br/>Check diagnostics endpoint<br/>Get token and test<br/>⏱️ 5 minutes"]
    
    Test --> Done([🎉 Done in 20 Minutes!<br/>15 lines of code<br/>Zero maintenance<br/>Enterprise-grade security])
    
    style Start fill:#4CAF50,stroke:#2E7D32,stroke-width:2px,color:#fff
    style Done fill:#4CAF50,stroke:#2E7D32,stroke-width:3px,color:#fff
    style Install fill:#E8F5E9,stroke:#4CAF50,stroke-width:2px
    style Config fill:#C8E6C9,stroke:#4CAF50,stroke-width:2px
    style Code fill:#A5D6A7,stroke:#4CAF50,stroke-width:2px
    style Test fill:#81C784,stroke:#4CAF50,stroke-width:2px
"@
}

foreach ($name in $diagrams.Keys) {
    $content = $diagrams[$name]
    $path = "diagrams\$name.mmd"
    Set-Content -Path $path -Value $content
    Write-Host "Created $path"
    
    $outputPath = "diagrams\$name.png"
    Write-Host "Generating $outputPath..."
    
    # Run mermaid-cli
    # Using npx to run without global install
    # --backgroundColor transparent for better integration
    # -w 1920 -H 1080 for high res
    cmd /c "npx -y @mermaid-js/mermaid-cli -i $path -o $outputPath -w 1920 -H 1080 -b transparent"
}
