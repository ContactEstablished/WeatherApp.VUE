<script setup lang="ts">
import {
  Bell,
  CalendarDays,
  ChevronDown,
  CloudLightning,
  Droplet as DropletIcon,
  Gem,
  Home,
  LocateFixed,
  Map,
  MapPin,
  Moon as MoonIcon,
  Navigation,
  Plus,
  Search,
  Settings,
  ShieldAlert,
  Star,
  Sun as SunIcon,
  Thermometer as ThermometerIcon,
  Trash2,
  Zap,
} from 'lucide-vue-next';
import { computed, onMounted, ref, watch } from 'vue';
import MetricCard from './components/MetricCard.vue';
import WeatherIcon from './components/WeatherIcon.vue';
import {
  deleteSavedLocation,
  getPreferences,
  getSavedLocations,
  getWeatherDashboard,
  saveLocation,
  searchLocations,
  setDefaultLocation,
  updatePreferences,
} from './services/weatherApi';
import type { LocationSuggestion, UnitSystem, WeatherDashboard } from './types/weather';

const dashboard = ref<WeatherDashboard | null>(null);
const loading = ref(true);
const error = ref('');
const search = ref('San Francisco, CA');
const unitSystem = ref<UnitSystem>('imperial');
const suggestions = ref<LocationSuggestion[]>([]);
const savedLocations = ref<LocationSuggestion[]>([]);
const searchFocused = ref(false);
const savingLocation = ref(false);
const updatingLocationId = ref<number | null>(null);
let searchTimer: number | undefined;

const navItems = [
  { label: 'Overview', icon: Home, active: true },
  { label: 'Forecast', icon: CalendarDays },
  { label: 'Maps', icon: Map },
  { label: 'Radar', icon: LocateFixed },
  { label: 'Locations', icon: MapPin },
  { label: 'Alerts', icon: Bell },
  { label: 'Settings', icon: Settings },
];

const formattedObservedAt = computed(() => {
  if (!dashboard.value) {
    return '';
  }

  return new Intl.DateTimeFormat('en-US', {
    weekday: 'long',
    month: 'long',
    day: 'numeric',
    hour: 'numeric',
    minute: '2-digit',
  }).format(new Date(dashboard.value.current.observedAt));
});

const showSuggestions = computed(() => searchFocused.value && suggestions.value.length > 0);

const activeLocation = computed(() => dashboard.value?.locations[0] ?? null);

const activeSavedLocation = computed(() => {
  const active = activeLocation.value;
  if (!active) {
    return null;
  }

  return savedLocations.value.find((location) => isSameLocation(location, active)) ?? null;
});

function formatTime(value: string): string {
  return new Intl.DateTimeFormat('en-US', {
    hour: 'numeric',
    minute: '2-digit',
  }).format(new Date(value));
}

function formatShortDate(value: string): string {
  return new Intl.DateTimeFormat('en-US', {
    month: 'short',
    day: 'numeric',
  }).format(new Date(`${value}T12:00:00`));
}

async function loadDashboard(): Promise<void> {
  loading.value = true;
  error.value = '';

  try {
    dashboard.value = await getWeatherDashboard(search.value, unitSystem.value);
    unitSystem.value = dashboard.value.unitSystem;
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'Unable to load weather.';
  } finally {
    loading.value = false;
  }
}

async function loadPreferences(): Promise<void> {
  try {
    const preferences = await getPreferences();
    unitSystem.value = preferences.unitSystem;
  } catch {
    unitSystem.value = 'imperial';
  }
}

async function loadSavedLocations(): Promise<void> {
  try {
    savedLocations.value = await getSavedLocations();
  } catch {
    savedLocations.value = [];
  }
}

async function changeUnits(nextUnitSystem: UnitSystem): Promise<void> {
  if (unitSystem.value === nextUnitSystem) {
    return;
  }

  unitSystem.value = nextUnitSystem;
  await updatePreferences(nextUnitSystem);
  await loadDashboard();
}

async function chooseLocation(location: LocationSuggestion): Promise<void> {
  search.value = locationLabel(location);
  searchFocused.value = false;
  suggestions.value = [];
  await loadDashboard();
}

async function saveActiveLocation(): Promise<void> {
  if (!activeLocation.value || activeSavedLocation.value || savingLocation.value) {
    return;
  }

  savingLocation.value = true;
  try {
    await saveLocation(activeLocation.value);
    await loadSavedLocations();
  } finally {
    savingLocation.value = false;
  }
}

async function removeSavedLocation(location: LocationSuggestion): Promise<void> {
  if (!location.id || updatingLocationId.value) {
    return;
  }

  updatingLocationId.value = location.id;
  try {
    await deleteSavedLocation(location.id);
    await loadSavedLocations();
  } finally {
    updatingLocationId.value = null;
  }
}

async function makeDefaultLocation(location: LocationSuggestion): Promise<void> {
  if (!location.id || location.isDefault || updatingLocationId.value) {
    return;
  }

  updatingLocationId.value = location.id;
  try {
    await setDefaultLocation(location.id);
    await loadSavedLocations();
  } finally {
    updatingLocationId.value = null;
  }
}

function locationLabel(location: LocationSuggestion): string {
  return [location.name, location.region].filter(Boolean).join(', ');
}

function isSameLocation(first: LocationSuggestion, second: LocationSuggestion): boolean {
  return first.name.toLowerCase() === second.name.toLowerCase() &&
    first.region.toLowerCase() === second.region.toLowerCase();
}

watch(search, (value) => {
  window.clearTimeout(searchTimer);

  if (value.trim().length < 2) {
    suggestions.value = [];
    return;
  }

  searchTimer = window.setTimeout(async () => {
    try {
      suggestions.value = await searchLocations(value);
    } catch {
      suggestions.value = [];
    }
  }, 250);
});

onMounted(async () => {
  await loadPreferences();
  await loadSavedLocations();

  const defaultLocation = savedLocations.value.find((location) => location.isDefault) ?? savedLocations.value[0];
  if (defaultLocation) {
    search.value = locationLabel(defaultLocation);
  }

  await loadDashboard();
});
</script>

<template>
  <main class="app-shell">
    <aside class="sidebar" aria-label="Primary">
      <a class="brand" href="#">
        <span class="brand__mark">
          <CloudLightning />
        </span>
        <span>
          <strong>Nimbus</strong>
          <em>Weather</em>
        </span>
      </a>

      <nav class="nav-list">
        <a
          v-for="item in navItems"
          :key="item.label"
          href="#"
          class="nav-list__item"
          :class="{ 'is-active': item.active }"
        >
          <component :is="item.icon" :stroke-width="1.8" />
          <span>{{ item.label }}</span>
        </a>
      </nav>

      <section v-if="savedLocations.length" class="saved-locations" aria-label="Saved locations">
        <header>
          <span>Saved</span>
        </header>
        <article
          v-for="location in savedLocations"
          :key="`${location.name}-${location.region}`"
          class="saved-location-row"
          :class="{ 'is-default': location.isDefault }"
        >
          <button type="button" class="saved-location-row__main" @click="chooseLocation(location)">
            <MapPin :stroke-width="1.8" />
            <span>
              <strong>{{ location.name }}</strong>
              <em>{{ location.region }}</em>
            </span>
          </button>
          <button
            type="button"
            class="saved-location-row__icon"
            :disabled="location.isDefault || updatingLocationId === location.id"
            :aria-label="`Make ${location.name} default`"
            @click="makeDefaultLocation(location)"
          >
            <Star :stroke-width="1.8" />
          </button>
          <button
            type="button"
            class="saved-location-row__icon"
            :disabled="updatingLocationId === location.id"
            :aria-label="`Remove ${location.name}`"
            @click="removeSavedLocation(location)"
          >
            <Trash2 :stroke-width="1.8" />
          </button>
        </article>
      </section>

      <section class="premium-card" aria-label="Premium upgrade">
        <div class="premium-card__gem">
          <Gem :stroke-width="1.7" />
        </div>
        <strong>Go Premium</strong>
        <p>Unlock advanced features and an ad-free experience.</p>
        <button type="button">
          <span>Upgrade Now</span>
          <Navigation :stroke-width="2" />
        </button>
      </section>

      <label class="theme-toggle">
        <span>
          <MoonIcon />
          Dark Mode
        </span>
        <input type="checkbox" checked />
        <i aria-hidden="true"></i>
      </label>
    </aside>

    <section class="workspace">
      <header class="topbar">
        <form class="search-box" @submit.prevent="loadDashboard">
          <Search :stroke-width="1.9" />
          <input
            v-model="search"
            placeholder="Search for a city, state, or ZIP..."
            @focus="searchFocused = true"
            @keydown.escape="searchFocused = false"
          />
          <kbd>Ctrl K</kbd>
          <div v-if="showSuggestions" class="search-suggestions">
            <button
              v-for="location in suggestions"
              :key="`${location.name}-${location.region}-${location.latitude}`"
              type="button"
              @mousedown.prevent="chooseLocation(location)"
            >
              <MapPin :stroke-width="1.7" />
              <span>
                <strong>{{ location.name }}</strong>
                <em>{{ location.region }} {{ location.country }}</em>
              </span>
            </button>
          </div>
        </form>

        <div class="unit-switch" aria-label="Temperature units">
          <button type="button" :class="{ 'is-active': unitSystem === 'imperial' }" @click="changeUnits('imperial')">
            &deg;F
          </button>
          <button type="button" :class="{ 'is-active': unitSystem === 'metric' }" @click="changeUnits('metric')">
            &deg;C
          </button>
        </div>

        <div class="profile-cluster">
          <button class="icon-button" type="button" aria-label="Notifications">
            <Bell :stroke-width="1.8" />
            <span></span>
          </button>
          <div class="profile">
            <img src="https://images.unsplash.com/photo-1500648767791-00dcc994a43e?auto=format&fit=crop&w=160&q=80" alt="Alex Morgan" />
            <span>
              <strong>Alex Morgan</strong>
              <em>Premium</em>
            </span>
            <ChevronDown :stroke-width="1.8" />
          </div>
        </div>
      </header>

      <section v-if="loading" class="loading-panel">
        <Zap :stroke-width="1.8" />
        <span>Loading live dashboard...</span>
      </section>

      <section v-else-if="error" class="loading-panel loading-panel--error">
        <ShieldAlert :stroke-width="1.8" />
        <span>{{ error }}</span>
        <button type="button" @click="loadDashboard">Retry</button>
      </section>

      <template v-else-if="dashboard">
        <section class="dashboard-grid">
          <article class="hero-weather" :style="{ '--hero-image': `url(${dashboard.current.backgroundImageUrl})` }">
            <div class="hero-weather__content">
              <div class="hero-weather__location">
                <span>
                  <Zap :stroke-width="1.8" />
                  {{ dashboard.current.location }}
                </span>
                <button
                  class="save-location-button"
                  type="button"
                  :class="{ 'is-saved': activeSavedLocation }"
                  :disabled="savingLocation || !!activeSavedLocation"
                  :aria-label="activeSavedLocation ? 'Location already saved' : 'Save location'"
                  @click="saveActiveLocation"
                >
                  <Star v-if="activeSavedLocation" :stroke-width="1.8" />
                  <Plus v-else :stroke-width="1.8" />
                </button>
              </div>
              <p>{{ formattedObservedAt }}</p>
              <div class="hero-weather__temp">
                <strong>{{ dashboard.current.temperature }}</strong>
                <span>&deg;{{ dashboard.temperatureUnit }}</span>
              </div>
              <div class="hero-weather__condition">
                <WeatherIcon :condition="dashboard.current.condition" size="sm" />
                <strong>{{ dashboard.current.condition }}</strong>
              </div>
              <p class="hero-weather__summary">
                {{ dashboard.current.summary }}<br />
                {{ dashboard.current.description }}
              </p>
              <div class="weather-facts">
                <span>
                  <ThermometerIcon />
                  <small>Feels like</small>
                  <strong>{{ dashboard.current.feelsLike }}&deg;</strong>
                </span>
                <span>
                  <SunIcon />
                  <small>Sunset</small>
                  <strong>{{ formatTime(dashboard.current.sunset) }}</strong>
                </span>
                <span>
                  <MoonIcon />
                  <small>Sunrise</small>
                  <strong>{{ formatTime(dashboard.current.sunrise) }}</strong>
                </span>
              </div>
            </div>
          </article>

          <section class="preview-row" aria-label="Condition previews">
            <article v-for="preview in dashboard.previews" :key="preview.condition" class="preview-card">
              <WeatherIcon :condition="preview.condition" size="xl" />
              <strong>{{ preview.condition }}</strong>
              <span>{{ preview.high }}&deg; / {{ preview.low }}&deg;</span>
              <p>{{ preview.description }}</p>
            </article>
            <div class="carousel-dots" aria-hidden="true">
              <span class="is-active"></span>
              <span></span>
              <span></span>
            </div>
          </section>

          <section class="panel hourly-panel">
            <header>
              <h2>Hourly Forecast</h2>
              <button type="button">View All</button>
            </header>
            <div class="hourly-strip">
              <article v-for="hour in dashboard.hourly" :key="hour.label" class="hour-card" :class="{ 'is-now': hour.label === 'Now' }">
                <span>{{ hour.label }}</span>
                <WeatherIcon :condition="hour.condition" size="md" />
                <strong>{{ hour.temperature }}&deg;</strong>
                <small>{{ hour.windSpeed }} {{ dashboard.windUnit }}</small>
              </article>
            </div>
          </section>

          <section class="panel forecast-panel">
            <header>
              <h2>5-Day Forecast</h2>
              <button type="button">View All</button>
            </header>
            <div class="daily-list">
              <article v-for="day in dashboard.daily" :key="day.date" class="daily-row">
                <span class="daily-row__date">
                  <strong>{{ day.day }}</strong>
                  <small>{{ formatShortDate(day.date) }}</small>
                </span>
                <WeatherIcon :condition="day.condition" size="sm" />
                <span class="daily-row__condition">{{ day.condition }}</span>
                <span class="daily-row__rain">
                  <DropletIcon />
                  {{ day.precipitationChance }}%
                </span>
                <span class="daily-row__range">
                  <strong>{{ day.high }}&deg;</strong>
                  <i>
                    <b :style="{ width: `${Math.max(22, Math.min(86, day.high * 1.1))}px` }"></b>
                  </i>
                  <em>{{ day.low }}&deg;</em>
                </span>
              </article>
            </div>
          </section>

          <section class="metric-stack" aria-label="Weather metrics">
            <MetricCard v-for="metric in dashboard.metrics" :key="metric.key" :metric="metric" />
          </section>
        </section>
      </template>
    </section>
  </main>
</template>
