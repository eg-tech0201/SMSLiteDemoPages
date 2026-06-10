# SMS Lite

Blazor app and ASP.NET Core API host for SMS Lite.

## Repository Layout

- `sms_lite/` - SMS Lite Blazor Server app, API endpoints, and `sms_lite.slnx`
- `elma/` - ELMA plain client library
- `survey_review/` - Survey Review plain client library

## Local Run

```bash
dotnet run --project sms_lite/sms_lite.csproj --urls http://localhost:5088
```

Open `http://localhost:5088`.

## Build

```bash
dotnet build sms_lite/sms_lite.slnx
```
