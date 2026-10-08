import { createApp } from "vue";
import "./style.css";
import App from "./App.vue";
import PrimeVue from "primevue/config";
import Aura from "@primeuix/themes/aura";
import { definePreset } from "@primeuix/themes";
import "primeicons/primeicons.css";

import { initializeTheme, primaryPalette } from "./services/theme";

initializeTheme();
const theme = definePreset(Aura, {
  semantic: {
    primary: primaryPalette,
    colorScheme: {
      light: {
        primary: {
          color: "{primary.700}",
          contrastColor: "#ffffff",
          hoverColor: "{primary.800}",
          activeColor: "{primary.900}",
        },
      },
    },
  },
});
createApp(App)
  .use(PrimeVue, {
    theme: { preset: theme, options: { darkModeSelector: ".dark-theme" } },
  })
  .mount("#app");

