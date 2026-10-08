import { afterEach, expect, test, vi } from 'vitest';
import { notifications, notify, dismissNotification, pauseNotification, resumeNotification } from './notifications';
afterEach(() => { for (const n of [...notifications.value]) dismissNotification(n.id); vi.useRealTimers(); });
test('cierra a los cinco segundos y conserva el tiempo restante al pausar', () => {
  vi.useFakeTimers();
  const id = notify('Guardado')!;
  vi.advanceTimersByTime(2000); pauseNotification(id);
  vi.advanceTimersByTime(9000); expect(notifications.value).toHaveLength(1);
  resumeNotification(id); vi.advanceTimersByTime(2999); expect(notifications.value).toHaveLength(1);
  vi.advanceTimersByTime(1); expect(notifications.value).toHaveLength(0);
});
test('errores persistentes y pausa combinada de cursor y teclado', () => {
  vi.useFakeTimers();
  const error = notify('Error', 'error')!;
  const success = notify('Listo')!;
  pauseNotification(success); pauseNotification(success, 'focus');
  resumeNotification(success); vi.advanceTimersByTime(6000); expect(notifications.value).toHaveLength(2);
  resumeNotification(success, 'focus'); vi.advanceTimersByTime(2000);
  expect(notifications.value.map(n => n.id)).toEqual([error]);
  dismissNotification(error); expect(notifications.value).toHaveLength(0);
});
