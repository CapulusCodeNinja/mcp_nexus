### 🚦 IP Rate Limiting Configuration

Defines rate limiting settings in configuration.

Note: in the current codebase, these settings are loaded into configuration models, but the HTTP server does not reference them directly. If rate limiting is required, it must be wired into the HTTP pipeline.

#### ⚙️ Settings

```json
{
  "IpRateLimiting": {
    "EnableEndpointRateLimiting": true,
    "StackBlockedRequests": false,
    "RealIpHeader": "X-Real-IP",
    "ClientIdHeader": "X-ClientId",
    "GeneralRules": [
      {
        "Endpoint": "*:/",
        "Period": "1m",
        "Limit": 100
      }
    ]
  }
}
```

- **`EnableEndpointRateLimiting`**: Enables/disables endpoint rate limiting.
- **`StackBlockedRequests`**: Whether to stack/accumulate blocked requests (implementation-dependent).
- **`RealIpHeader`**: Header used to identify the real client IP when behind a proxy.
- **`ClientIdHeader`**: Header used to identify a client ID.
- **`GeneralRules`**: Array of rate limit rules.
  - **`Endpoint`**: Endpoint pattern (example: `*:/`).
  - **`Period`**: Time window (example: `1m`).
  - **`Limit`**: Maximum requests allowed in the period.
