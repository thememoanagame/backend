# MemoAna Backend Architecture

## Runtime

MemoAna uses ASP.NET Core/.NET 10 for the backend and a MAUI Blazor Hybrid client. The backend solution is represented by `MemoAna.slnx` and contains the five DDD-oriented projects plus backend tests.

## Layers

### Presentation — `MemoAna`

Hosts the ASP.NET Core application, Web API controllers and Interactive Server Blazor components. It composes the application through the Composition project and consumes Application contracts. Authentication/authorization middleware is part of the HTTP pipeline, while identity business operations remain in Application/Infrastructure.

### Application — `MemoAna.Application`

Defines use cases and application-facing abstractions. The existing Identity feature follows a command/query/handler structure, with requests, responses, validators and abstractions. Handlers orchestrate use cases and must not depend directly on provider SDKs.

### Composition — `MemoAna.Composition`

Owns the composition root: service registration, options binding, authentication/authorization setup, mediator/pipeline registration, persistence configuration and application pipeline setup. Provider-specific authentication registration belongs here when it is part of ASP.NET authentication configuration.

### Domain — `MemoAna.Domain`

Contains domain concepts and rules. It must remain independent of ASP.NET Core, EF Core, Google SDKs and other infrastructure concerns.

### Infrastructure — `MemoAna.Infrastructure`

Contains concrete implementations for persistence and external services. The existing Identity implementation lives here, including Identity models, options and services. Google provider SDK integration belongs here when it requires calls to Google APIs; expose an Application abstraction instead of leaking Google SDK types across the layer boundary.

## Dependency graph

```mermaid
%%{init: {"flowchart": {"curve": "basis", "nodeSpacing": 100, "rankSpacing": 100}} }%%
flowchart TD

    subgraph Flow [" "]
        direction TD
        Presentation[🗄️MemoAna<br/>Presentation]
        Composition[📦MemoAna.Composition<br/>Composition]
        Infrastructure[📦MemoAna.Infrastructure<br/>Infrastructure]
        Application[📦MemoAna.Application<br/>Application]
        Domain[📦MemoAna.Domain<br/>Domain]
    
        Presentation --> Application
        Presentation --> Composition
        Composition --> Application
        Composition --> Infrastructure
        Infrastructure --> Application
        Application --> Domain
    end
    
    classDef Box fcolor:#FFF,ill:#F70000,stroke:#CCC,stroke-width:4px
    cstylefFlow color:#FFF,ill:#FFFF7F,stroke:#333,stroke-width:4px
    
    class Presentation,Composition,Infrastructure,Application,Domain Box
 ```

## Identity architecture

The current Identity implementation uses ASP.NET Core Identity with `User`, `Role`, EF Core persistence, `IIdentityService`, JWT token issuance, refresh/revocation support, validators, mediator handlers and authorization policies. The Composition root registers Identity and JWT authentication. The HTTP pipeline invokes authentication before authorization.

The Google Play Games authentication capability must be additive. It is an alternative credential acquisition path, not a replacement for local password-based Identity. A successful Google authentication must resolve to the same application user model and ultimately to the same MemoAna JWT/session model used by the existing client, unless the existing contract proves otherwise.

Provider SDK types must not become Application contracts. Validate the provider credential server-side, map the provider identity to a local Identity user, provision/link the local user when appropriate, and issue the application's own tokens. Never trust a user-supplied email/name as proof of Google identity.

The exact Google Play Games authentication flow must be verified against the versions already installed in `MemoAna.Infrastructure.csproj` and current Google API semantics before implementation. Do not invent SDK methods or assume that a package provides an OAuth flow it does not provide.

## Error handling

Expected invalid credentials, duplicate identities, missing users and provider validation failures should be represented by explicit application/domain exceptions or result contracts consistent with the existing feature. Unexpected exceptions should reach the application's global exception handling pipeline. Never expose provider stack traces, credentials or sensitive token material to clients.

## Async and resources

All I/O is asynchronous. Cancellation must flow from the HTTP/application boundary into EF Core and provider calls. Dispose resources owned by application code with `using`/`await using`; do not dispose DI-owned services.


## Game service transport

The Game feature follows the same Application/Infrastructure separation while adding MQTT as a realtime transport. REST controllers dispatch room commands and queries through Mediator; the Infrastructure game service owns authoritative room state through the existing repository/unit-of-work abstractions. MQTTnet.AspNetCore hosts the broker in the ASP.NET Core process, while GameMqttHub adapts MQTT connection, subscription and publish events to the Application game service.

The MQTT hub is configured on the built application before host startup so authentication and topic authorization are active before the broker accepts clients. MQTT credentials are generated per player and only password hashes are persisted.


### Authoritative game modes and image security

The game engine supports PlayerVsTime, PlayerVsAi and PlayerVsPlayer. Room mode, timer, turn, board composition, pair matching, score/streak/statistics, AI decisions and completion are server-owned state. The MAUI client sends only a card position through the MQTT player topic.

LiteDB image identifiers are never part of a public theme response or board payload. Hidden board positions contain no image reference. When a card is revealed, the server emits a short-lived protected image token. The room-scoped image endpoint validates that token against the current room/card state before opening the LiteDB stream. This prevents enumerating the theme catalog to pre-download all gameplay cards by their persistent LiteDB identifiers.
