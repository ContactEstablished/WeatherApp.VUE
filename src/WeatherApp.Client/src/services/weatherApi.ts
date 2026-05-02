import type { UnitSystem, UserPreferences, WeatherDashboard } from '../types/weather';

const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? '';

const userId = 'anonymous';

export async function getWeatherDashboard(location: string, unitSystem: UnitSystem): Promise<WeatherDashboard> {
  const params = new URLSearchParams({ location, unitSystem, userId });
  const response = await fetch(`${apiBaseUrl}/api/weather/dashboard?${params}`);

  if (!response.ok) {
    throw new Error(`Weather request failed with ${response.status}`);
  }

  return response.json() as Promise<WeatherDashboard>;
}

export async function updatePreferences(unitSystem: UnitSystem): Promise<UserPreferences> {
  const response = await fetch(`${apiBaseUrl}/api/users/${userId}/preferences`, {
    method: 'PUT',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({ unitSystem }),
  });

  if (!response.ok) {
    throw new Error(`Preference request failed with ${response.status}`);
  }

  return response.json() as Promise<UserPreferences>;
}
