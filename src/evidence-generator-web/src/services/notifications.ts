import { ref, watch, type Ref } from 'vue';
export type NotificationKind = 'success' | 'info' | 'error';
export interface Notification { id: number; message: string; kind: NotificationKind }
export const notifications = ref<Notification[]>([]);
let sequence = 0;
const clocks = new Map<number, { remaining: number; started: number; timer?: ReturnType<typeof setTimeout>; pauses: Set<string> }>();
export function dismissNotification(id: number) {
  clearTimeout(clocks.get(id)?.timer);
  clocks.delete(id);
  notifications.value = notifications.value.filter(n => n.id !== id);
}
export function resumeNotification(id: number, reason = 'hover') {
  const clock = clocks.get(id);
  if (!clock) return;
  clock.pauses.delete(reason);
  if (clock.pauses.size || clock.timer !== undefined) return;
  clock.started = Date.now();
  clock.timer = setTimeout(() => dismissNotification(id), clock.remaining);
}
export function pauseNotification(id: number, reason = 'hover') {
  const clock = clocks.get(id);
  if (!clock) return;
  clock.pauses.add(reason);
  if (clock.timer !== undefined) {
    clearTimeout(clock.timer); clock.timer = undefined;
    clock.remaining = Math.max(0, clock.remaining - (Date.now() - clock.started));
  }
}
export function notify(message: string, kind: NotificationKind = 'success') {
  if (!message) return;
  const id = ++sequence;
  notifications.value.push({ id, message, kind });
  if (kind !== 'error') {
    clocks.set(id, { remaining: 5000, started: 0, pauses: new Set() });
    resumeNotification(id);
  }
  return id;
}
// Synchronous watching also catches repeated messages reset within one operation.
export function useNotification(message: Ref<string>, kind: NotificationKind) {
  watch(message, value => { if (value) notify(value, kind); }, { flush: 'sync', immediate: true });
}
