import type { WeatherDashboard } from '../types/weather';

const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? '';

export async function getWeatherDashboard(location: string): Promise<WeatherDashboard> {
  const params = new URLSearchParams({ location });
  const response = await fetch(`${apiBaseUrl}/api/weather/dashboard?${params}`);

  if (!response.ok) {
    throw new Error(`Weather request failed with ${response.status}`);
  }

  return response.json() as Promise<WeatherDashboard>;
}
