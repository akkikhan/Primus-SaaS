# Security Module Architecture

## Vulnerability Data Source Strategy

### Current Approach: Local Database (Privacy-First)
*   **Mechanism**: We scrape NVD data into a local SQLite database (`cve-database.db`) and ship it (or download it) to the developer's machine.
*   **Pros**:
    *   **Privacy**: The list of dependencies *never* leaves the local environment. No "phone home" to a cloud API.
    *   **Performance**: Local SQL queries are instant.
    *   **Reliability**: Works offline or in air-gapped environments.
*   **Cons**:
    *   **Freshness**: Data is only as new as the last scrape/download.
    *   **Size**: Initial download is large (~100MB+).

### Alternative Approach: Cloud API (Freshness-First)
*   **Mechanism**: The scanner sends the list of packages (name + version) to a Primus SaaS Cloud API, which queries the live NVD/GitHub database.
*   **Pros**:
    *   **Freshness**: Real-time vulnerability data.
    *   **Size**: No local DB required.
*   **Cons**:
    *   **Privacy**: Requires sending dependency manifest to the cloud.
    *   **Latency**: Network round-trips.

## Recommendation
We recommend a **Hybrid Approach**:
1.  **Default to Local**: For fast, private inner-loop development.
2.  **Option for Cloud**: For CI/CD pipelines where freshness is critical and network access is available.

## Implementation Status
*   [x] Local Database Scanner (Implemented)
*   [ ] Cloud API Client (Roadmap)
