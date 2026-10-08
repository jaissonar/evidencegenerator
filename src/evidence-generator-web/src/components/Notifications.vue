<script setup lang="ts">
import { notifications, dismissNotification, pauseNotification, resumeNotification } from '../services/notifications';
</script>
<template>
  <Teleport to="body">
    <div class="notifications" aria-label="Notificaciones">
      <article v-for="item in notifications" :key="item.id" :class="['notification', item.kind, 'message']"
        :role="item.kind === 'error' ? 'alert' : 'status'" aria-atomic="true"
        @mouseenter="pauseNotification(item.id)" @mouseleave="resumeNotification(item.id)"
        @focusin="pauseNotification(item.id, 'focus')" @focusout="resumeNotification(item.id, 'focus')">
        <i :class="item.kind === 'error' ? 'pi pi-exclamation-circle' : item.kind === 'success' ? 'pi pi-check-circle' : 'pi pi-info-circle'" aria-hidden="true"></i>
        <span>{{ item.message }}</span>
        <button type="button" aria-label="Cerrar notificación" @click="dismissNotification(item.id)"><i class="pi pi-times" aria-hidden="true"></i></button>
      </article>
    </div>
  </Teleport>
</template>
<style scoped>
.notifications { position:fixed; bottom:88px; right:16px; width:min(410px, calc(100vw - 32px)); z-index:10000; display:grid; gap:10px; max-height:60dvh; overflow:auto; pointer-events:none; }
.notification.message { --notice-bg:#eaf2ff; --notice-border:#b8cff2; --notice-accent:#245ea8; --notice-text:#183b65; pointer-events:auto; margin:0; padding:14px; background:var(--notice-bg); color:var(--notice-text); border:1px solid var(--notice-border); border-left:4px solid var(--notice-accent); border-radius:10px; box-shadow:0 4px 20px #11131818; display:flex; align-items:flex-start; gap:10px; font-size:12px; }
.notification.success { --notice-bg:#e6f4ec; --notice-border:#a9d5bb; --notice-accent:#237a48; --notice-text:#1c5133; }
.notification.error { --notice-bg:#fdecea; --notice-border:#efbbb5; --notice-accent:#b42318; --notice-text:#7a271f; }
.notification > i { color:var(--notice-accent); margin-top:3px; }
.notification span { flex:1; line-height:1.5; overflow-wrap:anywhere; }
.notification button { background:transparent; color:var(--notice-accent); border:0; padding:3px; border-radius:4px; flex-shrink:0; }
.notification button:hover { background:var(--notice-border); }
</style>
