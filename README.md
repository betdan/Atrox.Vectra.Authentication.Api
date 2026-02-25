# Atrox.Vectra.Authentication.Api

Atrox.Vectra.Authentication.Api is a .NET 8 multi-transport authentication service that validates API keys (SHA-256), issues RS256 JWTs for Runtime access, and exposes JWKS for public-key discovery.

## Features

- Multi-transport support:
  - REST
  - AMQP (RabbitMQ / MassTransit)
  - gRPC
  - WebSocket
- SQL Server and PostgreSQL support (single active engine by configuration).
- Centralized auth use case through `IExecutionService`.
- RS256 token signing with a single global RSA private key.
- JWKS endpoint for Runtime local signature validation.
- Required header validation middleware:
  - `x-TransactionId`
  - `x-SessionId`
  - `x-ChannelId`
  - `x-I18n`

## Solution Structure

```text
Atrox.Vectra.Authentication.Api/
├─ Atrox.Vectra.Authentication.Api.sln
├─ Atrox.Vectra.Authentication.Api/                    # Host (Program, transports, DI wiring)
├─ Atrox.Vectra.Authentication.Api.Application/        # Use cases/services
├─ Atrox.Vectra.Authentication.Api.Application.Contracts
├─ Atrox.Vectra.Authentication.Api.Business            # Domain models
├─ Atrox.Vectra.Authentication.Api.CrossCutting        # Middleware and shared helpers
├─ Atrox.Vectra.Authentication.Api.DataAccess          # SQL Server/PostgreSQL repositories
├─ Atrox.Vectra.Authentication.Api.DataAccess.Contracts
└─ Atrox.Vectra.Authentication.Api.Tests
```

## Core Flow

1. Receive API key (header or body, configurable).
2. Hash API key using SHA-256.
3. Query `ATROX.atrox_security_client`.
4. Validate:
   - exists
   - active
   - not expired
5. Generate JWT with claims:
   - `iss`
   - `aud`
   - `sub` (`client_id`)
   - `company_id`
   - `iat`
   - `exp`
   - `jti`
6. Return token and expiration.

## Endpoints

## REST

- `POST /api/v1/auth`
- `GET /.well-known/jwks.json`

## WebSocket

- Configurable path, default: `/ws/auth`

## gRPC

- Proto: `Atrox.Vectra.Authentication.Api/Transports/Grpc/Protos/authentication_execution.proto`
- Service: `authentication.AuthenticationExecution`

## AMQP

- Consumer: `AtroxVectraAuthenticationApiConsumer`
- Queue from configuration:
  - `RabbitMqQueueName:Atrox.Vectra.Authentication.Api`

## Configuration

Main file:

- `Atrox.Vectra.Authentication.Api/appsettings.json`

Key sections:

```json
{
  "Database": {
    "Engine": "SqlServer"
  },
  "Authentication": {
    "Issuer": "Atrox.Vectra.Authentication",
    "Audience": "Atrox.Vectra.Runtime",
    "TokenExpirationMinutes": 30,
    "ApiKey": {
      "Source": "Header",
      "HeaderName": "x-api-key"
    },
    "Rsa": {
      "KeyId": "atrox-auth-rs256-k1",
      "PrivateKey": "-----BEGIN RSA PRIVATE KEY-----..."
    }
  },
  "Transports": {
    "Grpc": { "Enabled": true, "Port": 5005 },
    "WebSocket": { "Enabled": true, "Path": "/ws/auth", "KeepAliveSeconds": 120 }
  }
}
```

## Local Run

From solution root:

```powershell
dotnet run --project .\Atrox.Vectra.Authentication.Api\Atrox.Vectra.Authentication.Api.csproj
```

## Build and Test

```powershell
dotnet build .\Atrox.Vectra.Authentication.Api.sln
dotnet test .\Atrox.Vectra.Authentication.Api.Tests\Atrox.Vectra.Authentication.Api.Tests.csproj
```

## Runtime Integration

Runtime should validate JWTs locally using JWKS from:

- `GET /.well-known/jwks.json`

Recommended Runtime checks:

- signature valid against JWKS
- `iss == Atrox.Vectra.Authentication`
- `aud == Atrox.Vectra.Runtime`
- token not expired

## Security Notes

- Do not store API keys in plaintext; store only SHA-256 hash.
- Move RSA private key to secret storage for production.
- Use HTTPS in all environments.
- Keep token expiration short (default 30 minutes).
