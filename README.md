# Nimbus Weather

Nimbus Weather is a sample responsive weather dashboard built with Vue 3 on the frontend and .NET 10 on the backend. The first implementation focuses on the polished dark dashboard shown in the design reference: sidebar navigation, search, current conditions, hourly forecast, five-day forecast, preview cards, and metric trend cards.

## Project Structure

```text
WeatherApp.VUE/
  WeatherApp.slnx
  NuGet.Config
  src/
    WeatherApp.Api/       .NET 10 minimal API
    WeatherApp.Client/    Vue 3 + Vite frontend
```

## Backend

The API is in `src/WeatherApp.Api` and currently serves mock weather data through typed contracts.

Endpoints:

- `GET /health`
- `GET /api/weather/dashboard?location=San%20Francisco%2C%20CA`
- `GET /api/weather/locations`

Run locally:

```powershell
$env:DOTNET_CLI_HOME="$PWD\.dotnet-home"
dotnet run --project src\WeatherApp.Api\WeatherApp.Api.csproj --urls http://localhost:5078
```

Build:

```powershell
$env:DOTNET_CLI_HOME="$PWD\.dotnet-home"
dotnet build WeatherApp.slnx
```

## Frontend

The client is in `src/WeatherApp.Client`. Vite proxies `/api` and `/health` to `http://localhost:5078` during development.

Install dependencies:

```powershell
cd src\WeatherApp.Client
npm.cmd install
```

Run locally:

```powershell
npm.cmd run dev -- --host 127.0.0.1
```

Build:

```powershell
npm.cmd run build
```

Open the app at:

```text
http://127.0.0.1:5173
```

## Next Backend Work

- Replace `MockWeatherForecastService` with a provider-backed implementation.
- Add SQL Server persistence for saved locations, preferences, and user settings.
- Add configuration sections for provider API keys and connection strings.
- Add integration tests once the real provider and persistence boundaries are chosen.

## Next Frontend Work

- Add real search/autocomplete behavior against `/api/weather/locations`.
- Add saved locations and preferences.
- Add mobile interaction polish for the sidebar and forecast cards.
- Add provider loading/error states per panel instead of whole-page fallback only.
