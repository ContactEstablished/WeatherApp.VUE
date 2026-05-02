<script setup lang="ts">
const props = defineProps<{
  values: number[];
  variant?: 'line' | 'bars';
}>();

const width = 132;
const height = 46;

function linePath(values: number[]): string {
  if (values.length === 0) {
    return '';
  }

  const min = Math.min(...values);
  const max = Math.max(...values);
  const span = Math.max(max - min, 1);
  const step = width / Math.max(values.length - 1, 1);

  return values
    .map((value, index) => {
      const x = index * step;
      const y = height - ((value - min) / span) * (height - 10) - 5;
      return `${index === 0 ? 'M' : 'L'} ${x.toFixed(2)} ${y.toFixed(2)}`;
    })
    .join(' ');
}
</script>

<template>
  <svg class="sparkline" :viewBox="`0 0 ${width} ${height}`" role="img" aria-label="metric trend">
    <g v-if="variant === 'bars'" class="sparkline-bars">
      <rect
        v-for="(value, index) in values"
        :key="`${value}-${index}`"
        :x="index * 14"
        :y="height - value * 2.2 - 2"
        width="6"
        :height="Math.max(value * 2.2, 4)"
        rx="2"
      />
    </g>
    <path v-else :d="linePath(values)" />
  </svg>
</template>
