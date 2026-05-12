# TRF-app-windows-

Native C# implementation (non-Electron) of the TRF client.

## What is included

- C#/.NET native client (`TRF.NativeClient`)
- Side-tab style navigation with:
  - Dashboard
  - Configuration
  - Message Creator
  - Analytics
  - **Nation**
  - **Alliance**
- Backend integration aligned with bar3-style endpoints:
  - `GET /api/appData`
  - `GET /api/config`
  - `GET /analytics/campaigns`
  - Nation fetch fallback: `GET /api/nations`, `/api/nation`, `/nations`, `/nation`
  - Alliance fetch fallback: `GET /api/alliances`, `/api/alliance`, `/alliances`, `/alliance`

## Run

```bash
dotnet run --project /home/runner/work/TRF-app-windows-/TRF-app-windows-/TRF.NativeClient/TRF.NativeClient.csproj
```

Optional environment variables:

- `BAR3_SERVER_URL` (default: `http://localhost:8055`)
- `BAR3_API_KEY` (sent as `x-api-key`)
