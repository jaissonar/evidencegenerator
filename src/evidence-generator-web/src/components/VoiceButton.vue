<script setup lang="ts">
import { computed, onBeforeUnmount, ref, watch } from 'vue';
import Button from 'primevue/button';
import { notify } from '../services/notifications';
interface SpeechResult { isFinal: boolean; [index: number]: { transcript: string } }
interface Recognition {
  lang: string; interimResults: boolean; continuous: boolean;
  onresult: ((event: { resultIndex: number; results: ArrayLike<SpeechResult> }) => void) | null;
  onerror: ((event: { error: string }) => void) | null;
  onstart: (() => void) | null; onend: (() => void) | null;
  onaudiostart: (() => void) | null; onaudioend: (() => void) | null;
  onspeechstart: (() => void) | null; onspeechend: (() => void) | null;
  start(): void; stop(): void; abort(): void;
}
type SpeechWindow = Window & { SpeechRecognition?: new () => Recognition; webkitSpeechRecognition?: new () => Recognition };
const props = withDefaults(defineProps<{ remaining?: number }>(), { remaining: 4000 });
const emit = defineEmits<{ text: [value: string] }>();
const Speech = (window as SpeechWindow).SpeechRecognition ?? (window as SpeechWindow).webkitSpeechRecognition;
const active = ref(false);
const microphone = ref(false);
const state = ref('');
const partial = ref('');
const unconfirmed = ref('');
const elapsed = ref(0);
const timerLabel = computed(() => `${Math.floor(elapsed.value / 60)}:${String(elapsed.value % 60).padStart(2, '0')}`);
const identity = Symbol('dictation');
let recognition: Recognition | undefined;
let wanted = false;
let disposed = false;
let emptySessions = 0;
let retry: ReturnType<typeof setTimeout> | undefined;
let stopTimeout: ReturnType<typeof setTimeout> | undefined;
let timer: ReturnType<typeof setInterval> | undefined;
function finish() {
  active.value = false; microphone.value = false;
  clearInterval(timer); clearTimeout(retry); clearTimeout(stopTimeout);
}
function retainPartial() {
  if (partial.value.trim()) unconfirmed.value = [unconfirmed.value, partial.value.trim()].filter(Boolean).join(' ');
  partial.value = '';
}
function append(text: string) {
  const available = Math.max(0, props.remaining);
  if (text.length <= available) emit('text', text);
  else {
    if (available) emit('text', text.slice(0, available));
    unconfirmed.value = [unconfirmed.value, text.slice(available)].filter(Boolean).join(' ');
    notify('Se alcanzó el límite de 4.000 caracteres. El texto sobrante queda visible para copiarlo o revisarlo.', 'info');
    stop();
  }
}
function stop() {
  wanted = false; clearTimeout(retry);
  if (!recognition) { finish(); return; }
  state.value = 'Finalizando la última frase…';
  const current = recognition;
  // Give the service time to deliver its final result before aborting.
  stopTimeout = setTimeout(() => {
    if (recognition !== current) return;
    retainPartial(); recognition = undefined; current.abort(); finish();
    state.value = 'Dictado detenido';
  }, 2500);
  try { current.stop(); } catch { retainPartial(); finish(); }
}
function startSession() {
  if (!Speech || !wanted || disposed) return;
  const current = new Speech(); recognition = current;
  const emitted = new Set<number>();
  let receivedFinal = false;
  current.lang = 'es-CO'; current.interimResults = true; current.continuous = true;
  state.value = 'Activando micrófono…';
  const valid = () => !disposed && recognition === current;
  current.onstart = () => { if (valid()) state.value = 'Esperando audio del micrófono…'; };
  current.onaudiostart = () => { if (valid()) { microphone.value = true; state.value = 'Micrófono activo · Habla con naturalidad'; } };
  current.onaudioend = () => { if (valid()) { microphone.value = false; state.value = 'Procesando la última frase…'; } };
  current.onspeechstart = () => { if (valid()) state.value = 'Voz detectada · Transcribiendo…'; };
  current.onspeechend = () => { if (valid()) state.value = 'Pausa detectada · Esperando más voz…'; };
  current.onresult = event => {
    if (!valid()) return;
    const finals: string[] = [];
    const interim: string[] = [];
    for (let i = 0; i < event.results.length; i++) {
      const result = event.results[i]!;
      const text = result[0]?.transcript?.trim() ?? '';
      if (result.isFinal) {
        if (i >= event.resultIndex && !emitted.has(i)) { emitted.add(i); if (text) finals.push(text); }
      } else if (text) interim.push(text);
    }
    partial.value = interim.join(' ');
    if (finals.length) { receivedFinal = true; emptySessions = 0; append(finals.join(' ')); }
  };
  current.onerror = event => {
    if (!valid()) return;
    if (event.error === 'no-speech') {
      state.value = 'No se detectó voz. Revisa el micrófono seleccionado y habla cerca de él.';
      return;
    }
    if (event.error === 'aborted' && !wanted) return;
    wanted = false;
    const messages: Record<string, string> = {
      'not-allowed': 'Permite el micrófono en el navegador y en Privacidad de Windows para usar el dictado.',
      'service-not-allowed': 'El navegador o una política bloqueó el servicio de dictado.',
      'audio-capture': 'No se pudo capturar audio. Revisa que el micrófono esté conectado, seleccionado y sin silenciar.',
      'network': 'El servicio de dictado no respondió. Revisa la conexión y vuelve a intentarlo.',
    };
    state.value = 'Dictado interrumpido';
    retainPartial(); finish();
    notify(messages[event.error] ?? `El dictado se interrumpió (${event.error}). Puedes continuar escribiendo.`, 'error');
    current.abort();
  };
  current.onend = () => {
    if (!valid()) return;
    retainPartial(); recognition = undefined; microphone.value = false;
    clearTimeout(stopTimeout);
    if (wanted && props.remaining > 0) {
      emptySessions = receivedFinal ? 0 : emptySessions + 1;
      if (emptySessions < 3) {
        state.value = 'Reconectando el dictado…';
        retry = setTimeout(startSession, 700);
        return;
      }
      wanted = false;
      notify('No se detectó voz en varios intentos. Revisa el micrófono predeterminado, su volumen y los permisos; luego pulsa Dictar.', 'info');
    }
    wanted = false; finish(); state.value = 'Dictado detenido';
  };
  try { current.start(); }
  catch { wanted = false; recognition = undefined; finish(); notify('No se pudo iniciar el dictado. Revisa los permisos del micrófono.', 'error'); }
}
function dictate() {
  if (active.value) { stop(); return; }
  if (!Speech || props.remaining <= 0) return;
  window.dispatchEvent(new CustomEvent('evidence-dictation-start', { detail: identity }));
  wanted = true; active.value = true; emptySessions = 0; elapsed.value = 0;
  timer = setInterval(() => { elapsed.value++; }, 1000);
  startSession();
}
function otherDictation(event: Event) {
  if ((event as CustomEvent).detail === identity || !active.value) return;
  wanted = false; retainPartial();
  const previous = recognition; recognition = undefined;
  previous?.abort(); finish(); state.value = 'Dictado detenido al activar otro campo';
}
watch(() => props.remaining, value => { if (value <= 0 && wanted) stop(); });
function useUnconfirmed() { const text = unconfirmed.value; unconfirmed.value = ''; append(text); }
window.addEventListener('evidence-dictation-start', otherDictation);
onBeforeUnmount(() => {
  disposed = true; wanted = false; finish();
  recognition?.abort();
  window.removeEventListener('evidence-dictation-start', otherDictation);
});
</script>
<template>
  <div class="voice-control">
    <div class="voice-actions">
      <Button :label="active ? 'Detener' : 'Dictar'" :icon="active ? 'pi pi-stop-circle' : 'pi pi-microphone'"
        size="small" :severity="active ? 'danger' : 'secondary'" :disabled="!Speech || (!active && remaining <= 0)" @click="dictate" />
      <span v-if="active || state" class="voice-status" role="status"><span v-if="microphone" class="microphone-dot" aria-hidden="true"></span>{{ state }} <span v-if="active">{{ timerLabel }}</span></span>
    </div>
    <small v-if="!Speech">Este navegador no admite dictado.</small>
    <small v-else-if="remaining <= 0">Límite de 4.000 caracteres alcanzado.</small>
    <div v-if="partial" class="voice-partial"><strong>Reconociendo:</strong> {{ partial }}</div>
    <div v-if="unconfirmed" class="voice-partial"><label>Texto pendiente de revisar<textarea v-model="unconfirmed" rows="3" /></label>
      <small>El servicio no confirmó este fragmento o no cabe en el campo. Puedes corregirlo y copiarlo.</small>
      <div class="voice-actions"><Button label="Usar texto pendiente" text size="small" :disabled="active || remaining <= 0" @click="useUnconfirmed" /><Button label="Descartar texto pendiente" text size="small" :disabled="active" @click="unconfirmed=''" /></div>
    </div>
    <small v-if="active">No guardamos audio. Revisa el texto reconocido; el campo admite hasta 4.000 caracteres.</small>
  </div>
</template>
<style scoped>
.voice-control { display:grid; gap:8px; max-width:540px; min-width:0; text-align:left; }
.voice-actions { display:flex; flex-wrap:wrap; gap:10px; align-items:center; }
.voice-status { color:#185b63; font-size:11px; display:flex; flex-wrap:wrap; align-items:center; gap:6px; }
.microphone-dot { width:8px; height:8px; background:#237a48; border-radius:50%; animation:voice-pulse 1.4s ease-in-out infinite; }
.voice-partial { background:#eaf2ff; border:1px solid #b8cff2; border-radius:8px; padding:10px; font-size:12px; overflow-wrap:anywhere; }
.voice-partial textarea { width:100%; }
@keyframes voice-pulse { 50% { opacity:.4; } }
@media(prefers-reduced-motion:reduce) { .microphone-dot { animation:none; } }
</style>
