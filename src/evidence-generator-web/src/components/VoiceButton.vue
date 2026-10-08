<script setup lang="ts">
import { useNotification } from '../services/notifications';
import { onBeforeUnmount, ref } from "vue";
import Button from "primevue/button";
interface Recognition {
  lang: string;
  interimResults: boolean;
  continuous: boolean;
  onresult:
    | ((e: {
        results: { [key: number]: { [key: number]: { transcript: string } } };
      }) => void)
    | null;
  onerror: ((e: { error: string }) => void) | null;
  onend: (() => void) | null;
  start(): void;
  stop(): void;
  abort(): void;
}
type SpeechWindow = Window & {
  SpeechRecognition?: new () => Recognition;
  webkitSpeechRecognition?: new () => Recognition;
};
const emit = defineEmits<{ text: [value: string] }>();
const Speech =
  (window as SpeechWindow).SpeechRecognition ??
  (window as SpeechWindow).webkitSpeechRecognition;
const listening = ref(false);
const error = ref("");
let recognition: Recognition | undefined;
function dictate() {
  if (listening.value) {
    recognition?.stop();
    return;
  }
  if (!Speech) return;
  error.value = "";
  recognition = new Speech();
  recognition.lang = "es-CO";
  recognition.interimResults = false;
  recognition.continuous = false;
  recognition.onresult = (e) => {
    emit("text", e.results[0]![0]!.transcript);
  };
  recognition.onerror = (e) => {
    error.value = `Dictado no disponible (${e.error}). Puedes continuar escribiendo.`;
    listening.value = false;
  };
  recognition.onend = () => {
    listening.value = false;
  };
  try {
    recognition.start();
    listening.value = true;
  } catch {
    error.value = "No se pudo iniciar el micrófono.";
  }
}
onBeforeUnmount(() => recognition?.abort());
useNotification(error, 'error');
</script>
<template>
  <div>
    <Button
      :label="listening ? 'Detener' : 'Dictar'"
      :icon="listening ? 'pi pi-stop-circle' : 'pi pi-microphone'"
      size="small"
      severity="secondary"
      :disabled="!Speech"
      @click="dictate"
    /><small v-if="!Speech">Este navegador no admite dictado.</small
    >
  </div>
</template>
