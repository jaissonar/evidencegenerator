import { ref } from "vue";

export const themes = [
  { id: "purple", label: "Morado", color: "#5b3fa8", shades: ["#f4f0fb", "#e8def6", "#d8cff0", "#b8a0df", "#987dce", "#8367c7", "#6c4ab6", "#5b3fa8", "#483285", "#352563", "#241942"] },
  { id: "green", label: "Verde", color: "#15803d", shades: ["#f0fdf4", "#dcfce7", "#bbf7d0", "#86efac", "#4ade80", "#22c55e", "#16a34a", "#15803d", "#166534", "#14532d", "#052e16"] },
  { id: "blue", label: "Azul", color: "#1d4ed8", shades: ["#eff6ff", "#dbeafe", "#bfdbfe", "#93c5fd", "#60a5fa", "#3b82f6", "#2563eb", "#1d4ed8", "#1e40af", "#1e3a8a", "#172554"] },
  { id: "red", label: "Rojo", color: "#b91c1c", shades: ["#fef2f2", "#fee2e2", "#fecaca", "#fca5a5", "#f87171", "#ef4444", "#dc2626", "#b91c1c", "#991b1b", "#7f1d1d", "#450a0a"] },
  { id: "orange", label: "Naranja", color: "#c2410c", shades: ["#fff7ed", "#ffedd5", "#fed7aa", "#fdba74", "#fb923c", "#f97316", "#ea580c", "#c2410c", "#9a3412", "#7c2d12", "#431407"] },
] as const;
export type ThemeId = (typeof themes)[number]["id"];
const cookieName = "evidence-generator-theme";
const levels = [50, 100, 200, 300, 400, 500, 600, 700, 800, 900, 950];
export const primaryPalette = Object.fromEntries(levels.map(level => [level, `var(--accent-${level})`]));
export const selectedTheme = ref<ThemeId>("purple");
export const themeStorageNotice = ref("");

export function applyTheme(id: string, persist = true) {
  const theme = themes.find(item => item.id === id) ?? themes[0];
  selectedTheme.value = theme.id;
  const root = document.documentElement;
  root.dataset.theme = theme.id;
  levels.forEach((level, index) => root.style.setProperty(`--accent-${level}`, theme.shades[index]!));
  if (!persist) return;
  try {
    document.cookie = `${cookieName}=${theme.id}; Max-Age=31536000; Path=/; SameSite=Lax${location.protocol === "https:" ? "; Secure" : ""}`;
    themeStorageNotice.value = document.cookie.split("; ").includes(`${cookieName}=${theme.id}`)
      ? "Preferencia guardada en este navegador."
      : "Tema aplicado. Habilita las cookies para recordarlo.";
  } catch {
    themeStorageNotice.value = "Tema aplicado. Habilita las cookies para recordarlo.";
  }
}

export function initializeTheme() {
  let value = "purple";
  try {
    value = document.cookie.split(";").map(item => item.trim()).find(item => item.startsWith(`${cookieName}=`))?.split("=")[1] ?? value;
  } catch { /* The default theme remains available when cookies are blocked. */ }
  applyTheme(value, false);
}
