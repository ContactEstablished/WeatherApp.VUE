<script setup lang="ts">
import { Cloud, CloudRain, CloudSun, Moon, Sun } from 'lucide-vue-next';
import { computed } from 'vue';

const props = withDefaults(defineProps<{
  condition: string;
  size?: 'sm' | 'md' | 'lg' | 'xl';
}>(), {
  size: 'md',
});

const icon = computed(() => {
  const condition = props.condition.toLowerCase();

  if (condition.includes('rain')) {
    return CloudRain;
  }

  if (condition.includes('cloud') && condition.includes('partly')) {
    return CloudSun;
  }

  if (condition.includes('cloud')) {
    return Cloud;
  }

  if (condition.includes('night') || condition.includes('clear')) {
    return Moon;
  }

  return Sun;
});
</script>

<template>
  <span class="weather-icon" :class="[`weather-icon--${size}`, `weather-icon--${condition.toLowerCase().replaceAll(' ', '-')}`]">
    <component :is="icon" :stroke-width="1.8" />
  </span>
</template>
