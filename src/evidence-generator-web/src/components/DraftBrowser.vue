<script setup lang="ts">
import { useNotification } from '../services/notifications';
import { ref, onMounted } from "vue";
import Button from "primevue/button";
import Dialog from "primevue/dialog";
import { listDocuments, deleteDocument } from "../services/api";
import type { DocumentSummary } from "../domain/document";
defineProps<{ disabled?: boolean; activeId?: string; activeDirty?: boolean }>();
const emit = defineEmits<{ open: [id: string]; deleted: [id: string] }>();
const deletionTarget = ref<DocumentSummary | null>(null);
const confirmingDelete = ref(false);
const deleting = ref(false);
const deleteError = ref("");
const deletionNotice = ref("");
function confirmDelete(item: DocumentSummary) {
  deletionTarget.value = item; deleteError.value = ""; confirmingDelete.value = true;
}
async function remove() {
  if (!deletionTarget.value || deleting.value) return;
  deleting.value = true; deleteError.value = "";
  try {
    const item = deletionTarget.value;
    await deleteDocument(item.id, item.revision);
    rows.value = rows.value.filter(row => row.id !== item.id);
    offset.value = Math.max(0, offset.value - 1);
    confirmingDelete.value = false;
    deletionNotice.value = `Documento ${item.requirement} eliminado.`;
    emit('deleted', item.id);
  } catch (e) { deleteError.value = e instanceof Error ? e.message : "No se pudo eliminar el documento."; }
  finally { deleting.value = false; }
}
const search = ref(""); const from = ref(""); const to = ref("");
const rows = ref<DocumentSummary[]>([]); const offset = ref(0);
const busy = ref(false); const error = ref(""); const hasMore = ref(false);
async function load(append = false) {
  if (busy.value) return;
  if (from.value && to.value && from.value > to.value) { error.value = "La fecha inicial no puede superar la final."; return; }
  busy.value = true; error.value = "";
  try {
    const nextOffset = append ? offset.value : 0;
    const filters: Record<string, string> = { search: search.value, offset: String(nextOffset) };
    if (from.value) filters.from = from.value;
    if (to.value) filters.to = to.value;
    const result = await listDocuments(filters);
    rows.value = append ? [...rows.value, ...result] : result;
    offset.value = nextOffset + result.length; hasMore.value = result.length === 100;
  } catch (e) { error.value = e instanceof Error ? e.message : "No se pudieron consultar los borradores."; }
  finally { busy.value = false; }
}
function clear() { search.value = from.value = to.value = ""; void load(); }
onMounted(() => load());
useNotification(error, 'error');
useNotification(deleteError, 'error');
useNotification(deletionNotice, 'success');
</script>
<template>
  <form class="fields two" @submit.prevent="load()">
    <label class="full">Buscar borradores<input v-model="search" :disabled="busy" maxlength="4000" placeholder="Requerimiento, cliente o descripción" /></label>
    <label>Fecha del documento desde<input v-model="from" :disabled="busy" type="date" /></label>
    <label>Fecha del documento hasta<input v-model="to" :disabled="busy" type="date" /></label>
    <div class="actions full"><Button type="submit" label="Buscar" icon="pi pi-search" :loading="busy" /><Button label="Limpiar filtros" severity="secondary" :disabled="busy" @click="clear" /></div>
  </form>


  <p class="subtle" role="status">{{ busy ? 'Buscando…' : `${rows.length} borradores mostrados` }}</p>
  <p v-if="!busy && !error && !rows.length">No hay borradores que coincidan con estos filtros.</p>
  <div v-for="item in rows" :key="item.id" class="history-row">
  <button class="history-item" :disabled="disabled || busy || deleting" @click="emit('open',item.id)">
    <div><strong>{{ item.requirement }}</strong><p>{{ item.client || 'Sin cliente' }} · {{ item.date }}</p><small>{{ item.description }}</small><p>v{{ item.revision }} · Actualizado {{ new Date(item.updatedAt).toLocaleString('es-CO') }}</p></div><i class="pi pi-arrow-right" aria-hidden="true"></i>
  </button>
  <Button icon="pi pi-trash" :aria-label="`Eliminar documento ${item.requirement}`" title="Eliminar documento" severity="danger" text :disabled="disabled || busy || deleting" @click="confirmDelete(item)" />
  </div>
  <Button v-if="hasMore" label="Cargar más" :loading="busy" @click="load(true)" />
  <Dialog v-model:visible="confirmingDelete" header="Eliminar documento" modal :closable="!deleting" :close-on-escape="!deleting" :style="{width:'460px',maxWidth:'95vw'}">
    <p>¿Eliminar <strong>{{ deletionTarget?.requirement }}</strong> de {{ deletionTarget?.client || 'Sin cliente' }}?</p>
    <p>Se eliminará el borrador con sus evidencias, imágenes y correo guardado. Esta acción no se puede deshacer. Los archivos Excel y HTML descargados se conservan.</p>
    <p v-if="deletionTarget?.id === activeId">Es el documento abierto actualmente; también se cerrará{{ activeDirty ? ' y se descartarán sus cambios sin guardar' : '' }}.</p>

    <template #footer><Button label="Cancelar" severity="secondary" :disabled="deleting" @click="confirmingDelete=false" /><Button label="Eliminar definitivamente" severity="danger" :loading="deleting" :disabled="disabled" @click="remove" /></template>
  </Dialog>
</template>
<style scoped>
form { margin-bottom: 20px; }
.history-row { display: flex; align-items: center; gap: 8px; border-bottom: 1px solid #eee; }
.history-row .history-item { flex: 1; min-width: 0; border-bottom: 0; }
.history-item { justify-content: space-between; }
.history-item div { min-width: 0; }
.history-item p { margin: 6px 0; font-size: 11px; color: #667085; }
.history-item small { display: -webkit-box; -webkit-line-clamp: 2; -webkit-box-orient: vertical; overflow: hidden; overflow-wrap: anywhere; }
</style>
