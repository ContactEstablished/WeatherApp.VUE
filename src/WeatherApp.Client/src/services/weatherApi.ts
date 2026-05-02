import type { LocationSuggestion, UnitSystem, UserPreferences, WeatherDashboard } from '../types/weather';

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

export async function getPreferences(): Promise<UserPreferences> {
  const response = await fetch(`${apiBaseUrl}/api/users/${userId}/preferences`);

  if (!response.ok) {
    throw new Error(`Preference request failed with ${response.status}`);
  }

  return response.json() as Promise<UserPreferences>;
}

export async function searchLocations(query: string): Promise<LocationSuggestion[]> {
  const params = new URLSearchParams({ query });
  const response = await fetch(`${apiBaseUrl}/api/weather/locations?${params}`);

  if (!response.ok) {
    throw new Error(`Location request failed with ${response.status}`);
  }

  return response.json() as Promise<LocationSuggestion[]>;
}

export async function getSavedLocations(): Promise<LocationSuggestion[]> {
  const response = await fetch(`${apiBaseUrl}/api/users/${userId}/locations`);

  if (!response.ok) {
    throw new Error(`Saved locations request failed with ${response.status}`);
  }

  return response.json() as Promise<LocationSuggestion[]>;
}

export async function saveLocation(location: LocationSuggestion): Promise<void> {
  const response = await fetch(`${apiBaseUrl}/api/users/${userId}/locations`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({ ...location, isDefault: false }),
  });

  if (!response.ok) {
    throw new Error(`Save location request failed with ${response.status}`);
  }
}

export async function deleteSavedLocation(locationId: number): Promise<void> {
  const response = await fetch(`${apiBaseUrl}/api/users/${userId}/locations/${locationId}`, {
    method: 'DELETE',
  });

  if (!response.ok) {
    throw new Error(`Delete location request failed with ${response.status}`);
  }
}

export async function setDefaultLocation(locationId: number): Promise<void> {
  const response = await fetch(`${apiBaseUrl}/api/users/${userId}/locations/${locationId}/default`, {
    method: 'PUT',
  });

  if (!response.ok) {
    throw new Error(`Default location request failed with ${response.status}`);
  }
}
