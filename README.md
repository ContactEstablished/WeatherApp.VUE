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
- `DELETE /api/users/anonymous/locations/{locationId}`
- `PUT /api/users/anonymous/locations/{locationId}/default`

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

### Database Migrations

The API uses EF Core migrations for SQL Server schema management.

Restore local tools:

```powershell
dotnet tool restore
```

Apply migrations:

```powershell
$env:DOTNET_CLI_HOME="$PWD\.dotnet-home"
dotnet tool run dotnet-ef database update --project src\WeatherApp.Api\WeatherApp.Api.csproj --startup-project src\WeatherApp.Api\WeatherApp.Api.csproj
```

If you created the local `WeatherApp` database with an older build that used `EnsureCreated()`, drop/recreate the development database or baseline it before applying this initial migration. Fresh databases can apply the migration directly.

## Frontend

The client is in `src/WeatherApp.Client`. Vite proxies `/api` and `/health` to `http://localhost:5078` during development.

Current UI workflows:

- Live city/state/ZIP search backed by OpenWeather geocoding.
- Real current, hourly, and daily weather from OpenWeather One Call 3.0.
- Fahrenheit/Celsius unit toggle backed by anonymous preferences.
- Saved locations loaded from SQL Server through the .NET API.
- Save-current-location action from the dashboard hero.
- Remove and default-location actions in the saved locations list.

Provider calls are cached server-side with short development-friendly TTLs:

- Dashboard weather: 10 minutes.
- Geocoding/search results: 6 hours.

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

- Add integration tests around OpenWeather mapping and SQL persistence.
- Add update/reorder endpoints for saved locations.

## Next Frontend Work

- Add reorder controls for saved locations.
- Add mobile interaction polish for the sidebar and forecast cards.
- Add provider loading/error states per panel instead of whole-page fallback only.
