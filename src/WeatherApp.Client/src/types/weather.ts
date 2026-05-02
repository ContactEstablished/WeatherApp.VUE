export interface WeatherDashboard {
  current: CurrentWeather;
  hourly: HourlyForecast[];
  daily: DailyForecast[];
  previews: WeatherPreview[];
  metrics: WeatherMetric[];
  locations: LocationSuggestion[];
}

export interface CurrentWeather {
  location: string;
  observedAt: string;
  condition: string;
  summary: string;
  description: string;
  temperatureC: number;
  feelsLikeC: number;
  lowC: number;
  highC: number;
  sunrise: string;
  sunset: string;
  backgroundImageUrl: string;
}

export interface HourlyForecast {
  label: string;
  time: string;
  condition: string;
  temperatureC: number;
  windKph: number;
  precipitationChance: number;
}

export interface DailyForecast {
  day: string;
  date: string;
  condition: string;
  highC: number;
  lowC: number;
  precipitationChance: number;
}

export interface WeatherPreview {
  condition: string;
  highC: number;
  lowC: number;
  description: string;
}

export interface WeatherMetric {
  key: string;
  label: string;
  value: string;
  unit: string;
  hint: string;
  trend: number[];
}

export interface LocationSuggestion {
  name: string;
  region: string;
  country: string;
  latitude: number;
  longitude: number;
}
