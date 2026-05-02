<script setup lang="ts">
import { Droplet, Eye, Umbrella, Wind } from 'lucide-vue-next';
import { computed } from 'vue';
import type { WeatherMetric } from '../types/weather';
import Sparkline from './Sparkline.vue';

const props = defineProps<{
  metric: WeatherMetric;
}>();

const icon = computed(() => {
  switch (props.metric.key) {
    case 'humidity':
      return Droplet;
    case 'wind':
      return Wind;
    case 'precipitation':
      return Umbrella;
    default:
      return Eye;
  }
});
</script>

<template>
  <article class="metric-card">
    <div class="metric-card__icon">
      <component :is="icon" :stroke-width="1.8" />
    </div>
    <div class="metric-card__body">
      <span>{{ metric.label }}</span>
      <strong>{{ metric.value }}<small>{{ metric.unit }}</small></strong>
      <em>{{ metric.hint }}</em>
    </div>
    <Sparkline :values="metric.trend" :variant="metric.key === 'precipitation' ? 'bars' : 'line'" />
  </article>
</template>
