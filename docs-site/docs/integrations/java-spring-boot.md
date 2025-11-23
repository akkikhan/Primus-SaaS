---
id: java-spring-boot
title: Java (Spring Boot Resource Server)
---

Secure Spring Boot APIs with Azure AD and LocalAuth using the built-in OAuth2 resource server plus an `AuthenticationManagerResolver` to route tokens by issuer.

## Install

```bash
mvn install:install-file
# or add to pom.xml dependencies:
# <dependency>
#   <groupId>org.springframework.boot</groupId>
#   <artifactId>spring-boot-starter-oauth2-resource-server</artifactId>
# </dependency>
```

## Configuration (`application.yml`)

```yaml
spring:
  security:
    oauth2:
      resourceserver:
        jwt:
          issuer-uri: https://login.microsoftonline.com/cbd15a9b-cd52-4ccc-916a-00e2edb13043/v2.0
primus:
  audiences: acc675f1-e32f-40b9-a0c6-716066cc6890
  local:
    issuer: http://localhost:4000
    secret: local-dev-secret-123
```

## Multi-issuer security config

```java
// SecurityConfig.java
package com.example.security;

import java.util.Map;
import java.util.function.Function;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.core.convert.converter.Converter;
import org.springframework.security.authentication.AuthenticationManagerResolver;
import org.springframework.security.config.annotation.web.builders.HttpSecurity;
import org.springframework.security.oauth2.jwt.*;
import org.springframework.security.web.SecurityFilterChain;
import org.springframework.security.web.authentication.preauth.PreAuthenticatedAuthenticationToken;
import org.springframework.security.oauth2.server.resource.authentication.JwtAuthenticationConverter;

import jakarta.servlet.http.HttpServletRequest;

@Configuration
public class SecurityConfig {
    private static final String AZURE_ISSUER = "https://login.microsoftonline.com/cbd15a9b-cd52-4ccc-916a-00e2edb13043/v2.0";
    private static final String LOCAL_ISSUER = "http://localhost:4000";
    private static final String AUDIENCE = "acc675f1-e32f-40b9-a0c6-716066cc6890";

    @Bean
    SecurityFilterChain security(HttpSecurity http) throws Exception {
        http
            .authorizeHttpRequests(auth -> auth
                .requestMatchers("/api/public").permitAll()
                .requestMatchers("/api/admin").hasRole("Admin")
                .anyRequest().authenticated()
            )
            .oauth2ResourceServer(oauth -> oauth
                .authenticationManagerResolver(authManagerResolver())
            );
        return http.build();
    }

    @Bean
    AuthenticationManagerResolver<HttpServletRequest> authManagerResolver() {
        var azureDecoder = JwtDecoders.fromIssuerLocation(AZURE_ISSUER);
        var localDecoder = NimbusJwtDecoder.withSecretKey(new org.springframework.security.crypto.keygen.KeyGenerators.StringKeyGenerator() {
            @Override public String generateKey() { return "local-dev-secret-123"; }
        }).build();

        Converter<Jwt, ?> authConverter = jwtAuthenticationConverter();

        Map<String, JwtDecoder> decoders = Map.of(
            AZURE_ISSUER, azureDecoder,
            LOCAL_ISSUER, localDecoder
        );

        return request -> token -> {
            if (token instanceof PreAuthenticatedAuthenticationToken preAuth && preAuth.getCredentials() instanceof Jwt jwt) {
                var decoder = decoders.get(jwt.getIssuer().toString());
                if (decoder == null) {
                    throw new JwtException("Untrusted issuer: " + jwt.getIssuer());
                }
                Jwt validated = decoder.decode(jwt.getTokenValue());
                if (validated.getAudience() == null || !validated.getAudience().contains(AUDIENCE)) {
                    throw new JwtException("Invalid audience");
                }
                return (org.springframework.security.core.Authentication) authConverter.convert(validated);
            }
            throw new JwtException("Unsupported token");
        };
    }

    private JwtAuthenticationConverter jwtAuthenticationConverter() {
        var converter = new JwtAuthenticationConverter();
        converter.setJwtGrantedAuthoritiesConverter(new org.springframework.security.oauth2.server.resource.authentication.JwtGrantedAuthoritiesConverter());
        return converter;
    }
}
```

## Routes

```java
// DemoController.java
package com.example.api;

import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RestController;

@RestController
public class DemoController {
    @GetMapping("/api/public")
    public Object publicApi() {
        return Map.of("message", "No auth required");
    }

    @GetMapping("/api/protected")
    public Object protectedApi(org.springframework.security.oauth2.server.resource.authentication.JwtAuthenticationToken token) {
        return Map.of(
            "message", "Authenticated",
            "user", Map.of(
                "id", token.getToken().getSubject(),
                "issuer", token.getToken().getIssuer(),
                "roles", token.getAuthorities()
            )
        );
    }

    @GetMapping("/api/admin")
    public Object adminApi() {
        return Map.of("message", "Admin only");
    }
}
```

## Notes

- Ensure `issuer-uri` matches Azure AD `iss` exactly; the custom resolver routes LocalAuth tokens by `iss=http://localhost:4000`.
- Replace the hardcoded local secret with an env/`application.yml` property in real deployments.
- Keep `clockSkew` small by default; Nimbus respects JWT `exp`/`nbf` automatically.

## Update steps

1. Update your Spring Boot starter version to the latest compatible release (e.g., `spring-boot-starter-oauth2-resource-server`).
2. If issuer endpoints or audiences change, update `AZURE_ISSUER`, `LOCAL_ISSUER`, and `AUDIENCE` constants (or their config equivalents).
3. Re-test `/api/protected` and `/api/admin` with both Azure AD and LocalAuth tokens to ensure routing and roles still work.
