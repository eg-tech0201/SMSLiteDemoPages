# External.ELMA.Client

Standalone microservice shell for SCT to submit ELMA transactions, retrieve status, and expose the approved FO Update Request link.

## Current Scope
- Swagger-documented REST API
- Configurable auth shell supporting API key or bearer token modes
- Configurable 5-second timeout
- In-memory circuit breaker shell
- Stub downstream client for development/demo use
- Shared DTOs via `Models/Integrations/ElmaClientModels.cs`

## Pending External Dependencies
- ELMA team API specification
- Final auth mechanism and credential issuance
- Real ELMA transaction schema
- Downstream error code mapping
- Decision on direct form prefill vs static link
