<script setup lang="ts">
import { computed, ref } from "vue";
import Button from "primevue/button";
import MailBodyEditor from "./MailBodyEditor.vue";
import type { EvidenceDocument } from "../domain/document";
import { mailHtml } from "../services/mail";
import { prepareMailClipboard } from "../services/mailClipboard";
import { download } from "../services/api";
import ImageInput from "./ImageInput.vue";
import { defaultSignature } from "../domain/mailDefaults";
const doc = defineModel<EvidenceDocument>({ required: true });
const html = computed(() => mailHtml(doc.value));
const clipboard = computed(() => prepareMailClipboard(html.value));
const subject = computed({
  get: () =>
    doc.value.mail.subject || `Pruebas Unitarias - ${doc.value.requirement}`,
  set: (value: string) => {
    doc.value.mail.subject = value;
  },
});
const status = ref("");
function downloadHtml() {
  download(
    new Blob(
      [
        clipboard.value.html,
      ],
      { type: "text/html" },
    ),
    "correo.html",
  );
}
async function copy(rich: boolean) {
  try {
    if (rich) {
      await navigator.clipboard.write([
        new ClipboardItem({
          "text/html": new Blob([clipboard.value.html], { type: "text/html" }),
          "text/plain": new Blob([clipboard.value.text], { type: "text/plain" }),
        }),
      ]);
    } else await navigator.clipboard.writeText(clipboard.value.html);
    status.value = rich
      ? "Correo copiado con formato. En Outlook usa Ctrl+V y Mantener formato de origen; el mensaje debe estar en formato HTML. Adjunta el Excel descargado."
      : "Código HTML copiado.";
  } catch {
    status.value =
      "El navegador bloqueó el portapapeles. Puedes descargar el HTML.";
  }
}
</script>
<template>
  <div class="section-heading">
    <div>
      <span class="eyebrow">COMUNICACIÓN</span>
      <h2>Preparar correo</h2>
    </div>
    <span class="pill">Tahoma {{ doc.mail.fontSize }} pt</span>
  </div>
  <details class="panel recipients-panel"><summary>Destinatarios y asunto <span>{{ doc.mail.to || 'Sin destinatarios' }}</span></summary><div class="fields two recipients-fields">
    <label
      >Para<input
        v-model="doc.mail.to"
        placeholder="Destinatarios; separados por punto y coma" /></label
    ><label
      >CC<input
        v-model="doc.mail.cc"
        placeholder="Copias; separadas por punto y coma" /></label
    ><label
      >Nombre para el saludo<input
        v-model="doc.mail.recipientName"
        placeholder="Nombre del destinatario" /></label
    ><label
      >Tamaño de fuente<select v-model.number="doc.mail.fontSize">
        <option :value="11">Tahoma 11 pt</option>
        <option :value="12">Tahoma 12 pt</option>
      </select></label
    ><label class="full"
      >Asunto<input
        v-model="subject"
        :placeholder="`Pruebas Unitarias - ${doc.requirement}`"
    /></label>
  </div></details>
  <MailBodyEditor v-model="doc" />
  <details class="panel signature-panel">
    <summary>Firma <span>Configurar imagen y tamaño</span></summary>
    <div class="signature-panel-content">
    <div class="fields two signature-settings">
      <label
        >Ancho de la firma<select v-model.number="doc.mail.signatureWidth">
          <option :value="280">280 px · Pequeña</option>
          <option :value="360">360 px · Compacta</option>
          <option :value="420">420 px · Estándar</option>
          <option :value="480">480 px · Amplia</option>
          <option :value="600">600 px · Grande</option>
        </select></label
      >
      <div class="actions">
        <Button
          label="Usar firma predeterminada"
          severity="secondary"
          @click="doc.mail.signature = defaultSignature()"
        />
      </div>
    </div>
    <ImageInput
      label="Cargar firma"
      @images="doc.mail.signature = $event[0] ?? null"
    />
    <div v-if="doc.mail.signature" class="signature">
      <img :src="doc.mail.signature.dataUrl" alt="Firma de correo" /><Button
        label="Quitar firma"
        size="small"
        text
        @click="doc.mail.signature = null"
      />
    </div>

    <p class="subtle">
      Avisos en Tahoma 8 pt: ambiental en verde y confidencialidad en negro,
      después de la firma.
    </p>
    </div>
  </details>
  <section class="panel">
    <div class="section-heading compact">
      <h3>Vista previa del cuerpo</h3>
      <Button label="Copiar correo" icon="pi pi-copy" @click="copy(true)" />
    </div>
    <div class="email-preview" v-html="clipboard.fragment"></div>
    <div class="actions">
      <Button
        label="Copiar HTML"
        severity="secondary"
        @click="copy(false)"
      /><Button
        label="Descargar HTML"
        severity="secondary"
        @click="downloadHtml"
      />
    </div>
    <p v-if="status" role="status">{{ status }}</p>
  </section>
  <p class="subtle">
    Para, CC y asunto se guardan con el borrador y se trasladan manualmente a
    Outlook. La compatibilidad de la firma al pegar depende del cliente de
    correo. El envío y los adjuntos automáticos se integrarán con Microsoft
    Graph en una fase posterior.
  </p>
</template>

<style scoped>
.signature-panel summary { cursor: pointer; font-size: 15px; font-weight: bold; }
.signature-panel summary span { font-size: 11px; font-weight: normal; color: #667085; margin-left: 12px; }
.signature-panel-content { padding-top: 22px; }
@media (max-width: 760px) { .signature-panel summary span { display: block; margin: 8px 0 0 18px; } }
</style>



