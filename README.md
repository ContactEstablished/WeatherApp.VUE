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

The API is in `src/WeatherApp.Api`. It supports OpenWeather One Call 3.0 through backend-only configuration, and falls back to mock data when no API key is configured.

Endpoints:

- `GET /health`
- `GET /api/weather/dashboard?location=San%20Francisco%2C%20CA&unitSystem=imperial&userId=anonymous`
- `GET /api/weather/locations?query=Seattle`
- `GET /api/users/anonymous/preferences`
- `PUT /api/users/anonymous/preferences`
- `GET /api/users/anonymous/locations`
- `POST /api/users/anonymous/locations`

### Local Secrets

Create `src/WeatherApp.Api/appsettings.Local.json`. This file is ignored by Git.

```json
{
  "OpenWeather": {
    "ApiKey": "your-openweather-api-key"
  },
  "ConnectionStrings": {
    "WeatherApp": "your-sql-server-connection-string"
  }
}
```

The frontend never receives the OpenWeather key. The Vue app calls the .NET API, and the .NET API calls OpenWeather.

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

- Add migrations once the SQL schema settles.
- Add cache headers or server-side caching around provider calls.
- Add integration tests around OpenWeather mapping and SQL persistence.

## Next Frontend Work

- Add real search/autocomplete behavior against `/api/weather/locations`.
- Add saved locations and preferences.
- Add mobile interaction polish for the sidebar and forecast cards.
- Add provider loading/error states per panel instead of whole-page fallback only.
