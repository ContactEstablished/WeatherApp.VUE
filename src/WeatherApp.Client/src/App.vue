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
  Search,
  Settings,
  ShieldAlert,
  Sun as SunIcon,
  Thermometer as ThermometerIcon,
  Zap,
} from 'lucide-vue-next';
import { computed, onMounted, ref } from 'vue';
import MetricCard from './components/MetricCard.vue';
import WeatherIcon from './components/WeatherIcon.vue';
import { getWeatherDashboard, updatePreferences } from './services/weatherApi';
import type { UnitSystem, WeatherDashboard } from './types/weather';

const dashboard = ref<WeatherDashboard | null>(null);
const loading = ref(true);
const error = ref('');
const search = ref('San Francisco, CA');
const unitSystem = ref<UnitSystem>('imperial');

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

async function changeUnits(nextUnitSystem: UnitSystem): Promise<void> {
  if (unitSystem.value === nextUnitSystem) {
    return;
  }

  unitSystem.value = nextUnitSystem;
  await updatePreferences(nextUnitSystem);
  await loadDashboard();
}

onMounted(loadDashboard);
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
          <input v-model="search" list="locations" placeholder="Search for a city or place..." />
          <datalist id="locations">
            <option v-for="location in dashboard?.locations" :key="location.name" :value="`${location.name}, ${location.region}`" />
          </datalist>
          <kbd>Ctrl K</kbd>
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
                <Navigation :stroke-width="1.8" />
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
