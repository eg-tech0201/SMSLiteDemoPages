# SMS Lite

Blazor app and ASP.NET Core API host for SMS Lite.

## Repository Layout

- `SMSLiteUI/src/SMSLiteUI/` - Blazor UI host, pages, shared components, UI services, and `wwwroot`.
- `SMSLiteUI/documentation/` - UI documentation.
- `SMSLiteApi/src/SMSLiteCommandAPI/` - API endpoints, authentication, DTOs, repositories, services, data access, functions, query API, and integration clients.
- `SMSLiteDBO/` - database object scripts, organized by tables and stored procedures.
- `SMSLiteModels/src/SMSLiteModels/Entities/` - shared entity and integration model types.
- `SMSLite.slnx` - solution file for the UI, API, integration, and model projects.

## Local Run

```bash
dotnet run --project SMSLiteUI/src/SMSLiteUI/SMSLiteUI.csproj --urls http://localhost:5088
```

Open `http://localhost:5088`.

## Build

```bash
dotnet build SMSLite.slnx
```
