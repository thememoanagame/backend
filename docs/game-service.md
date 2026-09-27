# MemoAna Game Service

## Runtime model

The game service uses two transport layers:

- REST for room/theme discovery and room lifecycle operations.
- SignalRnet for authoritative realtime game events and player actions.

The broker runs inside the ASP.NET Core process through SignalRnet.AspNetCore. The default TCP endpoint is port `1883`, configurable through `SignalR:Endpoint` and `SignalR:Port`.

SQLite remains the relational store for room/player/game state. LiteDB remains the binary theme-image store.

## Room lifecycle

Rooms support three authoritative modes:

- PlayerVsTime — one human player, server-enforced countdown.
- PlayerVsAi — one human player plus an AI player controlled entirely by the backend.
- PlayerVsPlayer — two human players.

For PlayerVsPlayer:

1. Player A calls POST /api/v1/game/rooms.
2. The service creates a waiting room and the first player record.
3. A unique SignalR username/password and client identifier are generated for that player.
4. The password is returned once and only its PBKDF2 hash is stored in SQLite.
5. Player B polls GET /api/v1/game/rooms.
6. Player B calls POST /api/v1/game/rooms/{roomId}/join.
7. The service adds the second player, creates the authoritative board, chooses the starting player with a cryptographically secure random value and changes the room to InProgress.
8. The REST response contains the second player's SignalR credentials and board metadata.
9. The presentation layer publishes the retained board message through the SignalR hub.

Time and AI rooms start immediately during room creation. Their board is returned to the creator and the presentation layer publishes the retained board state.

The client never decides turns, pair composition, matching, score, game completion or time expiration. It only sends a card position and renders authoritative state received from the server.

## Theme upload

Themes require exactly 15 WebP card images plus one WebP logo.

The Blazor Server administration page uses `MaximumFileCount="15"` and `AppendMultipleFiles`. Browser files are staged sequentially to temporary files before the Application command is dispatched. This avoids opening multiple browser streams before they are consumed and gives the Application validator seekable streams for WebP signature validation.

Theme creation cleans up LiteDB image files if SQLite persistence fails after the images were uploaded.

## Image delivery and anti-cheat boundary

The public theme catalog no longer exposes the LiteDB card image identifiers. The theme DTO exposes only card filenames/names needed for catalog/UI purposes.

The game board also never contains the LiteDB image identifier. Hidden cards have no image token.

When a card becomes revealed, the authoritative SignalR state contains an opaque, room-scoped and time-limited image token for that position. The token is protected with ASP.NET Core Data Protection and contains the server-side image reference only inside the protected payload. It cannot be decoded or modified by the client without the server key.

The client downloads the revealed image through:

GET /api/v1/game/rooms/{roomId}/cards/{position}/image?token={imageToken}

The backend validates the token, room, position and current card visibility before opening the corresponding LiteDB stream. A hidden card therefore cannot be fetched merely by knowing its room and position. A previously revealed/matched card remains downloadable while its short-lived token is valid.

The former public theme-card image endpoints were removed from the game controller, so a player cannot enumerate the theme catalog and download all card images before playing.

The board remains authoritative for positions 0..29; the actual image IDs remain server-side in SQLite and are only resolved against LiteDB by the game service.

## Deployment

For the GCP e2-micro deployment, expose the SignalR TCP port in addition to the HTTP port used by the REST/Blazor application. Configure the endpoint advertised to MAUI clients with:

```json
{
  "SignalR": {
    "Endpoint": "your-public-hostname",
    "Port": 1883
  }
}
```

For production internet traffic, TLS should be added before treating SignalR credentials as sufficient transport security.


## SignalR transport

Realtime gameplay is exposed through the authenticated SignalR hub:

`/hubs/game`

The REST API remains responsible for room lifecycle operations:

- `GET /api/v1/game/rooms`
- `POST /api/v1/game/rooms`
- `POST /api/v1/game/rooms/{roomId}/join`

After obtaining the `PlayerId` from the REST room session, the client connects to SignalR and invokes `JoinGame(roomId, playerId)`. The server validates the player, associates the SignalR connection with the room, and broadcasts the authoritative board state.

Client gameplay uses only `SelectCard(position)`. The hub selects the mode-specific service through `IGameModeServiceResolver` and serializes operations per room through `IGameRoomExecutionCoordinator`.

Registered gameplay implementations are:

- `TimeAttackGameService` for `PlayerVsTime`.
- `AiGameService` for `PlayerVsAi`.
- `PvpGameService` for `PlayerVsPlayer`.

The hub publishes `GameState` and `GameUpdated` events through a strongly typed SignalR client contract. Card comparison, score calculation, turns, AI decisions, timer expiration, and game completion remain server-authoritative.
