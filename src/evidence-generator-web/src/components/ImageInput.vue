<script setup lang="ts">
import { ref } from "vue";
import Button from "primevue/button";
import { normalizeImage } from "../services/images";
import type { EvidenceImage } from "../domain/document";
const props = withDefaults(
  defineProps<{ label?: string; multiple?: boolean }>(),
  { label: "Agregar capturas", multiple: false },
);
const emit = defineEmits<{ images: [images: EvidenceImage[]] }>();
const input = ref<HTMLInputElement>();
const error = ref("");
const busy = ref(false);
async function add(files: File[]) {
  if (busy.value) return;
  error.value = "";
  busy.value = true;
  try {
    if (!props.multiple && files.length > 1)
      throw new Error("Solo puedes agregar una imagen por evidencia. Selecciona un único archivo.");
    const selected = files;
    if (selected.length > 10)
      throw new Error("Selecciona hasta 10 imágenes a la vez.");
    const images = await Promise.all(selected.map(normalizeImage));
    emit("images", images);
  } catch (e) {
    error.value = e instanceof Error ? e.message : "No se pudo leer la imagen.";
  } finally {
    busy.value = false;
    if (input.value) input.value.value = "";
  }
}
function paste(event: ClipboardEvent) {
  const files = Array.from(event.clipboardData?.files ?? []).filter((f) =>
    f.type.startsWith("image/"),
  );
  if (files.length) {
    event.preventDefault();
    void add(files);
  }
}
</script>
<template>
  <div
    class="image-input"
    tabindex="0"
    :aria-label="`${label}. También puedes pegar una imagen con Control V`"
    @paste="paste"
    @dragover.prevent
    @drop.prevent="add(Array.from($event.dataTransfer?.files ?? []))"
  >
    <i class="pi pi-images" aria-hidden="true"></i
    ><span
      >Pega una captura aquí con <b>Ctrl + V</b
      ><small>o arrastra una imagen PNG, JPG o WebP</small></span
    >
    <Button
      :label="label"
      icon="pi pi-plus"
      severity="secondary"
      :loading="busy"
      @click="input?.click()"
    />
    <input
      ref="input"
      type="file"
      accept="image/png,image/jpeg,image/webp"
      :multiple="multiple"
      hidden
      @change="add(Array.from(($event.target as HTMLInputElement).files ?? []))"
    />
  </div>
  <p v-if="error" role="alert" class="error-text">{{ error }}</p>
</template>
