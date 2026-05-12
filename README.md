# TRF-app-windows-

Native C# implementation (non-Electron) of the TRF client.

## What is included

- C#/.NET native client (`TRF.NativeClient`)
- Side-tab style navigation with:
  - Dashboard
  - Configuration
  - Message Creator
  - Analytics
  - Nation
  - Alliance
  - Discord Auth
  - Endpoint Coverage

## Endpoint alignment

Implemented in the native client API layer:

- `GET /api/appData`
- `GET /api/config`
- `POST /api/setConfig`
- `POST /api/sendMessage`
- `POST /api/setApplicationState`
- `GET /analytics/campaigns`
- `POST /analytics/campaigns`
- Nation fetch fallback: `GET /api/nations`, `/api/nation`, `/nations`, `/nation`
- Alliance fetch fallback: `GET /api/alliances`, `/api/alliance`, `/alliances`, `/alliance`
- `GET /auth/session`
- Browser auth URLs: `/auth/discord`, `/auth/logout`
- `GET /account`
- `GET /api/bot/status`
- `POST /api/bot/config`
- `POST /api/v2/auth/login`
- `GET /api/v2/automation/state`
- `POST /api/v2/automation/state`
- `POST /api/v2/templates`
- `GET /api/v2/analytics/me`
- `POST /api/v2/automation/send-active-unallied`
- `POST /api/v2/automation/send-active-unallied-discord`
- `POST /api/v2/automation/send-by-nation-ids`
- GitHub latest release check endpoint:
  - `GET https://api.github.com/repos/TheonlyGlaernisch/bar3-server/releases/latest`

## Discord auth

- Session check uses `GET /auth/session`.
- Login uses browser launch to `/auth/discord`.
- If your backend expects cookie auth, optionally pass an existing cookie header via:
  - `BAR3_DISCORD_COOKIE`

## Run

```bash
dotnet run --project /home/runner/work/TRF-app-windows-/TRF-app-windows-/TRF.NativeClient/TRF.NativeClient.csproj
```

Optional environment variables:

- `BAR3_SERVER_URL` (default: `http://localhost:8055`)
- `BAR3_API_KEY` (sent as `x-api-key`)
- `BAR3_DISCORD_COOKIE` (optional cookie header for authenticated session calls)
