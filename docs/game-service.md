# MemoAna Game Service

## Runtime model

The game service uses two transport layers:

- REST for room/theme discovery and room lifecycle operations.
- MQTTnet for authoritative realtime game events and player actions.

The broker runs inside the ASP.NET Core process through MQTTnet.AspNetCore. The default TCP endpoint is port `1883`, configurable through `Mqtt:Endpoint` and `Mqtt:Port`.

SQLite remains the relational store for room/player/game state. LiteDB remains the binary theme-image store.

## Room lifecycle

1. Player A calls `POST /api/v1/game/rooms`.
2. The service creates a waiting room and the first player record.
3. A unique MQTT username/password and client identifier are generated for that player.
4. The password is returned once and only its PBKDF2 hash is stored in SQLite.
5. Player B polls `GET /api/v1/game/rooms`.
6. Player B calls `POST /api/v1/game/rooms/{roomId}/join`.
7. The service adds the second player, creates the authoritative board, chooses the starting player with a cryptographically secure random value and changes the room to `InProgress`.
8. The REST response contains the second player's MQTT credentials and board metadata.
9. The presentation layer publishes the retained board message through the MQTT hub.

## MQTT topics

Every room has exactly two application topics:

- `/rooms/{roomId}/board` — retained board definition. The card order is position `0..29`, left-to-right and top-to-bottom.
- `/rooms/{roomId}/player` — player actions and authoritative game state.

Clients authenticate with the credentials returned by REST. The broker validates the MQTT username/password against the player record and binds the connection to its room and player identifier.

A client can only subscribe to topics under its own room. Client-originated publishes are accepted only on that room's `/player` topic and only for the `card.select` action.

The raw client action is not forwarded to other subscribers. The hub processes it through the authoritative game service and publishes the resulting state instead.

## Player action

A card selection is represented as JSON:

```json
{
  "action": "card.select",
  "position": 7
}
```

The server verifies the MQTT session, player turn, card position/state and whether another pair is currently awaiting resolution. The server owns pair comparison, score changes, turn changes and completion state.

A mismatched pair is published while visible, held for approximately 800 ms, then hidden and the turn remains with the next player. A matched pair increments the selecting player's score. Completing all pairs finishes the room.

## Theme upload

Themes require exactly 15 WebP card images plus one WebP logo.

The Blazor Server administration page uses `MaximumFileCount="15"` and `AppendMultipleFiles`. Browser files are staged sequentially to temporary files before the Application command is dispatched. This avoids opening multiple browser streams before they are consumed and gives the Application validator seekable streams for WebP signature validation.

Theme creation cleans up LiteDB image files if SQLite persistence fails after the images were uploaded.

## Image caching and anti-cheat boundary

The board contains the image identifiers used by the existing theme-image endpoints. The client can download each image once while entering the loading screen and cache it for the lifetime of the match.

The MQTT board is authoritative for card order and game state, but the current theme API still exposes theme card identifiers. Therefore this design prevents the client from changing game state or turn results, but it is not a complete secrecy mechanism against a client that deliberately downloads all theme images. A future hardening step can replace public card identifiers with room-scoped image tokens and only resolve a token to an image when the server permits it.

## Deployment

For the GCP e2-micro deployment, expose the MQTT TCP port in addition to the HTTP port used by the REST/Blazor application. Configure the endpoint advertised to MAUI clients with:

```json
{
  "Mqtt": {
    "Endpoint": "your-public-hostname",
    "Port": 1883
  }
}
```

For production internet traffic, TLS should be added before treating MQTT credentials as sufficient transport security.
