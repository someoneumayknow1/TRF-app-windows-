# TRF-app-windows-

Native C# implementation (non-Electron) of the TRF client.

## What is included

- C#/.NET native client (`TRF.NativeClient`)
- Role-based side-tab navigation

| Tab               | Required role(s)        |
|-------------------|-------------------------|
| Discord Auth      | *(always visible)*      |
| Exit              | *(always visible)*      |
| Dashboard         | `user` or `admin`       |
| Configuration     | `user` or `admin`       |
| Message Creator   | `user` or `admin`       |
| Analytics         | `user` or `admin`       |
| Account           | `user` or `admin`       |
| Automation        | `user` or `admin`       |
| Nation            | `member` or `admin`     |
| Alliance          | `member` or `admin`     |
| Bot Panel         | `admin` only            |
| Endpoint Coverage | `admin` only            |

Users may hold more than one role (e.g. `["user", "member"]` grants both sets of tabs).
Roles are checked case-insensitively against the `roles` array in `GET /auth/session`.
The `isAdmin: true` field from the session response is treated as an implicit `admin` role.

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
- `POST /api/bot/config`
- `GET /api/bot/servers`
- `GET /api/bot/commands/usage`
- `POST /api/bot/send`
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
- Login uses browser launch to `/auth/discord` with `returnTo=http://localhost:9876/callback`.
- The native client starts a temporary local HTTP listener on `http://localhost:9876/` and captures
  the returned session cookie automatically.
- If your backend expects cookie auth, optionally pass an existing cookie header via:
  - `BAR3_DISCORD_COOKIE`

## Bot Panel

The **Bot Panel** tab is visible only to users where `isAdmin: true` in `GET /auth/session`.
When opened it:

1. Lists every Discord server the bot is in — `GET /api/bot/servers`
2. Shows the most-used slash commands — `GET /api/bot/commands/usage`
3. Optionally sends a message through the bot — `POST /api/bot/send`

### Required server-side changes (bar3-server)

Add the three routes below to your bar3-server Express app.  All three must be protected
so that only sessions where `isAdmin === true` (from your existing Discord session check)
can call them.  Return `401` if the session cookie is absent and `403` if the user is
authenticated but not an admin.

#### `GET /api/bot/servers`

Returns the Discord guilds the bot is currently a member of.

```json
[
  {
    "id": "123456789012345678",
    "name": "My Alliance Server",
    "icon": "a_abcdef1234567890abcdef1234567890",
    "memberCount": 412
  }
]
```

Implementation notes:
- Use the cached guild list from the Discord.js gateway client (avoid extra API calls).
- Fall back to `GET https://discord.com/api/v10/users/@me/guilds` with the bot token if no
  gateway cache is available.
- Return an empty array `[]` when the bot is not in any guilds.

#### `GET /api/bot/commands/usage`

Returns slash-command usage counts, sorted by `usageCount` descending.

```json
[
  { "name": "ping",   "usageCount": 1042, "description": "Check the bot's latency" },
  { "name": "nation", "usageCount":  876, "description": "Look up a P&W nation" }
]
```

Implementation notes:
- Track a per-command counter in your database; increment it inside the bot's
  `interactionCreate` handler on every successful invocation.
- Return the top 20 results ordered by `usageCount DESC`.
- Return an empty array `[]` if no data exists yet.

#### `POST /api/bot/send`

Sends a plain-text message through the bot to a Discord channel.

Request body:

```json
{ "message": "Hello from Bar 3!" }
```

Responses:

| Status | Meaning |
|--------|---------|
| `204 No Content` | Message sent successfully |
| `400 Bad Request` | `message` is missing, empty, or longer than 2000 chars |
| `403 Forbidden`  | Caller is not an admin |
| `502 Bad Gateway` | Discord API call failed |

Implementation notes:
- The native client sends `{ "message": "..." }` (not `{ "content": "..." }`); map
  `message` to Discord's `content` field when calling the Discord API.
- Validate that `message` is non-empty and at most 2000 characters before forwarding.
- Log the sender's Discord user ID and target channel for auditing.
- Apply a rate limit (e.g. 10 requests per minute per user).

### Optional – appData sent-messages fix (`src/services/v2AutomationRunner.ts`)

The automation runner currently discards the result of `sendMessageWithConfig`, so the
*Messages Sent* card on the Dashboard is always empty for v2 users.

```diff
-    await messagesService.sendMessageWithConfig(configLike, nation).catch(() => undefined);
+    const msg = await messagesService.sendMessageWithConfig(configLike, nation).catch(() => undefined);
+    if (msg) {
+      if (!state.userKeys[pwKey]) {
+        state.userKeys[pwKey] = { sentMessages: [], config: new Config(), applicationOn: false, apiDetails: { used: 0, max: 0 } };
+      }
+      state.userKeys[pwKey].sentMessages.push(msg);
+    }
     seen.add(nation.nation_id);
```

## Run

Run from the repository root:

```bash
dotnet run --project TRF.NativeClient/TRF.NativeClient.csproj
```

Optional environment variables:

- `BAR3_SERVER_URL` (default: `https://your-actual-server.com`)
- `BAR3_API_KEY` (sent as `x-api-key`)
- `BAR3_DISCORD_COOKIE` (optional cookie header for authenticated session calls)

## Distribution

Install as a global .NET tool:

```bash
dotnet pack TRF.NativeClient/TRF.NativeClient.csproj -c Release
dotnet tool install -g trf-client --add-source ./TRF.NativeClient/bin/Release
```

Build a self-contained single-file Windows executable:

```bash
dotnet publish TRF.NativeClient/TRF.NativeClient.csproj -c Release -r win-x64 -p:PublishSingleFile=true --self-contained true
```
