<script setup lang="ts">
import { useNotification } from '../services/notifications';
import Button from "primevue/button";
import Dialog from "primevue/dialog";
import { computed, nextTick, ref } from "vue";
import Select from 'primevue/select';

import Textarea from "primevue/textarea";
import type { EvidenceSection, EvidenceImage } from "../domain/document";
import { moveItem } from "../domain/document";
import ImageInput from "./ImageInput.vue";
import VoiceButton from "./VoiceButton.vue";
const section = defineModel<EvidenceSection>({ required: true });
const props = defineProps<{ engine: string; environmentUrls: string[]; connections: string[] }>();
const collapsed = ref(new Set<string>());
const deleteId = ref<string | null>(null);
const deleteDialog = ref(false);
const deletion = computed(() => section.value.items.find(item => item.id === deleteId.value));
const deletionNumber = computed(() => section.value.items.findIndex(item => item.id === deleteId.value) + 1);
function confirmDelete(id: string) { deleteId.value = id; deleteDialog.value = true; }
function removeEvidence() {
  const index = section.value.items.findIndex(item => item.id === deleteId.value);
  if (index >= 0) {
    collapsed.value.delete(section.value.items[index]!.id);
    section.value.items.splice(index, 1);
  }
  deleteDialog.value = false;
  deleteId.value = null;
}
function toggleEvidence(id: string) {
  if (collapsed.value.has(id)) collapsed.value.delete(id);
  else collapsed.value.add(id);
}
async function addEvidence() {
  if (section.value.items.length < 50) {
    const id = crypto.randomUUID();
    section.value.items.push({
      id,
      description: "",
      images: [],
    });
    await nextTick();
    document.getElementById(id)?.scrollIntoView({ behavior: 'smooth', block: 'center' });
    document.getElementById(id)?.focus({ preventScroll: true });
  }
}
const imageError = ref("");
function addImages(images: EvidenceImage[], target: EvidenceImage[]) {
  imageError.value = "";
  if (images.length + target.length > 1) {
    imageError.value =
      "Cada evidencia admite una sola imagen. Quita la actual para reemplazarla o crea otra evidencia.";
    return;
  }
  target.push(...images);
}
useNotification(imageError, 'error');
</script>
<template>
  <div class="section-heading">
    <div>
      <span class="eyebrow">ENTORNO DE PRUEBAS</span>
      <h2>Evidencias {{ engine }}</h2>
    </div>
    <Button
      class="add-evidence-fixed"
      label="Agregar evidencia"
      icon="pi pi-plus"
      :disabled="section.items.length >= 50"
      @click="addEvidence"
    />
  </div>

  <section class="panel fields two">
    <label>URL / Sitio<Select v-model="section.url" :options="props.environmentUrls" editable filter aria-label="URL / Sitio" placeholder="Selecciona o escribe una URL" /></label>
    <label>Conexión / Ambiente<Select v-model="section.connection" :options="props.connections" editable filter aria-label="Conexión / Ambiente" placeholder="Selecciona o escribe una conexión" /></label>
  </section>
  <section class="panel">
    <div class="section-heading compact">
      <h3>Inicio de sesión</h3>
      <span class="subtle">Captura opcional</span>
    </div>
    <ImageInput
      label="Cargar inicio de sesión"
      @images="section.loginImage = $event[0] ?? null"
    />
    <div v-if="section.loginImage" class="image-preview">
      <img
        :src="section.loginImage.dataUrl"
        alt="Captura del inicio de sesión"
      /><Button
        label="Quitar captura"
        severity="secondary"
        size="small"
        @click="section.loginImage = null"
      />
    </div>
  </section>
  <section v-if="!section.items.length" class="empty-state panel">
    <i class="pi pi-file-edit"></i>
    <h3>Comienza tu plan de pruebas</h3>
    <p>Agrega una descripción y las capturas que documentan cada validación.</p>
    <Button
      label="Crear primera evidencia"
      severity="secondary"
      @click="addEvidence"
    />
  </section>
  <section
    v-for="(evidence, index) in section.items"
    :key="evidence.id"
    class="panel evidence-card"
  >
    <div class="section-heading compact evidence-heading">
      <button type="button" class="evidence-toggle"
        :aria-expanded="!collapsed.has(evidence.id)"
        :aria-controls="`evidence-content-${evidence.id}`"
        :aria-label="`${collapsed.has(evidence.id) ? 'Expandir' : 'Contraer'} evidencia ${index + 1}`"
        @click="toggleEvidence(evidence.id)">
        <i :class="collapsed.has(evidence.id) ? 'pi pi-chevron-right' : 'pi pi-chevron-down'" aria-hidden="true"></i>
        <span class="number">{{ String(index + 1).padStart(2, "0") }}</span>
        <span class="evidence-summary"><strong>Evidencia</strong><small>{{ evidence.description || 'Sin descripción' }}</small></span>
        <span class="evidence-count">{{ evidence.images.length }} capturas</span>
      </button>
      <div class="actions">
        <Button
          icon="pi pi-arrow-up"
          aria-label="Subir evidencia"
          severity="secondary"
          size="small"
          :disabled="index === 0"
          @click="moveItem(section.items, index, -1)"
        /><Button
          icon="pi pi-arrow-down"
          aria-label="Bajar evidencia"
          severity="secondary"
          size="small"
          :disabled="index === section.items.length - 1"
          @click="moveItem(section.items, index, 1)"
        /><Button
          icon="pi pi-trash"
          aria-label="Eliminar evidencia"
          severity="danger"
          text
          size="small"
          @click="confirmDelete(evidence.id)"
        />
      </div>
    </div>
    <div v-show="!collapsed.has(evidence.id)" :id="`evidence-content-${evidence.id}`" class="evidence-content">
    <label :for="evidence.id">Descripción de la validación</label
    ><Textarea
      :id="evidence.id"
      v-model="evidence.description"
      rows="3"
      auto-resize
      maxlength="4000"
      placeholder="Describe la acción realizada y el resultado observado…"
      fluid
    />
    <div class="description-tools">
      <small>{{ evidence.description.length }} / 4000</small>
      <div class="actions">
        <Button
          label="Limpiar"
          icon="pi pi-eraser"
          severity="secondary"
          size="small"
          :disabled="!evidence.description"
          @click="evidence.description = ''"
        /><VoiceButton
          @text="
            evidence.description = `${evidence.description} ${$event}`
              .trim()
              .slice(0, 4000)
          "
        />
      </div>
    </div>
    <ImageInput
      v-if="evidence.images.length === 0"
      label="Agregar imagen"
      @images="addImages($event, evidence.images)"
    />
    <div class="capture-grid">
      <div
        v-for="(image, imageIndex) in evidence.images"
        :key="imageIndex"
        class="capture"
      >
        <img
          :src="image.dataUrl"
          :alt="`Evidencia ${index + 1}, captura ${imageIndex + 1}`"
        />
        <div class="actions">
          <span>Captura {{ imageIndex + 1 }}</span
          ><Button
            icon="pi pi-times"
            aria-label="Quitar imagen"
            size="small"
            text
            severity="danger"
            @click="evidence.images.splice(imageIndex, 1)"
          />
        </div>
      </div>
    </div>
    </div>
  </section>
  <Dialog v-model:visible="deleteDialog" header="Eliminar evidencia" modal :style="{width:'460px',maxWidth:'95vw'}">
    <p>¿Eliminar la evidencia <strong>{{ deletionNumber }}</strong> de <strong>{{ engine }}</strong>?</p>
    <p class="prewrap">{{ deletion?.description || 'Sin descripción' }}</p>
    <p>Se quitarán su descripción y sus {{ deletion?.images.length ?? 0 }} capturas del documento. Guarda el borrador para conservar esta eliminación. Los archivos descargados no se modifican.</p>
    <template #footer><Button label="Cancelar" severity="secondary" @click="deleteDialog=false" /><Button label="Eliminar evidencia" severity="danger" :disabled="!deletion" @click="removeEvidence" /></template>
  </Dialog>
  <p class="subtle">
    El dictado requiere permiso de micrófono. El navegador puede procesar la voz
    mediante su servicio en línea.
  </p>
</template>

<style scoped>
.evidence-heading { flex-wrap: wrap; }
.evidence-toggle { display: flex; align-items: center; gap: 10px; flex: 1; min-width: 0; background: transparent; border: 0; padding: 4px 0; text-align: left; color: inherit; }
.evidence-toggle > i { font-size: 11px; color: #667085; }
.evidence-toggle .number { margin: 0; }
.evidence-summary { display: grid; gap: 5px; min-width: 0; }
.evidence-summary small { white-space: nowrap; text-overflow: ellipsis; overflow: hidden; max-width: 480px; color: #667085; font-weight: normal; }
.evidence-count { margin-left: auto; white-space: nowrap; font-size: 11px; color: #667085; }
.evidence-content > label { margin-bottom: 10px; }
.evidence-card:has(.evidence-toggle[aria-expanded="false"]) .evidence-heading { margin-bottom: 0; }
@media (max-width: 760px) { .evidence-toggle { flex-basis: 100%; } .evidence-summary { flex: 1; } }
</style>
