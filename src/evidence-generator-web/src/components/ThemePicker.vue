<script setup lang="ts">
import { themes, selectedTheme, applyTheme, themeStorageNotice } from "../services/theme";
</script>

<template>
  <section class="theme-picker" aria-labelledby="theme-heading">
    <h2 id="theme-heading"><i class="pi pi-palette" aria-hidden="true"></i> Temas de color</h2>
    <div class="theme-options" role="group" aria-label="Color de la aplicación">
      <button v-for="theme in themes" :key="theme.id" type="button"
        :aria-label="theme.label" :title="theme.label"
        :aria-pressed="selectedTheme === theme.id"
        :style="{ '--swatch': theme.color }" @click="applyTheme(theme.id)">
        <i v-if="selectedTheme === theme.id" class="pi pi-check" aria-hidden="true"></i>
      </button>
    </div>
    <p>{{ themes.find(theme => theme.id === selectedTheme)?.label }} · Preferencia guardada</p>
    <span class="theme-feedback" role="status">{{ themeStorageNotice }}</span>
  </section>
</template>

<style scoped>
.theme-picker { margin: 12px 10px 0; padding-top: 10px; border-top: 1px solid #292c34; }
.theme-picker h2 { font-size: 10px; font-weight: normal; letter-spacing: 0; margin: 0 0 8px; color: #d0d3dd; }
.theme-picker h2 i { margin-right: 6px; font-size: 11px; }
.theme-options { display: flex; gap: 6px; flex-wrap: wrap; }
.theme-options button { display: grid; place-items: center; width: 22px; height: 22px; padding: 0; border: 2px solid transparent; border-radius: 50%; background: var(--swatch); color: white; }
.theme-options button[aria-pressed="true"] { outline: 1px solid #fff; outline-offset: 2px; }
.theme-options button:focus-visible { outline: 2px dashed white; outline-offset: 4px; }
.theme-options i { font-size: 9px; }
.theme-picker p { font-size: 9px; color: #b8bdca; margin: 6px 0 0; }
.theme-feedback { position: absolute; width: 1px; height: 1px; padding: 0; overflow: hidden; clip-path: inset(50%); white-space: nowrap; }
</style>
