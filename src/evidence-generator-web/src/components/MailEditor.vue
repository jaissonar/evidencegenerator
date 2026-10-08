<script setup lang="ts">
import { computed, ref } from "vue";
import Button from "primevue/button";
import { notify } from '../services/notifications';
import MultiSelect from 'primevue/multiselect';
import type { Contact, WorkspaceSettings } from '../domain/settings';
import MailBodyEditor from "./MailBodyEditor.vue";
import type { EvidenceDocument } from "../domain/document";
import { mailHtml } from "../services/mail";
import { prepareMailClipboard } from "../services/mailClipboard";
import { download } from "../services/api";
import ImageInput from "./ImageInput.vue";
import { defaultSignature } from "../domain/mailDefaults";
const doc = defineModel<EvidenceDocument>({ required: true });
const props = defineProps<{ contacts: Contact[]; profile: WorkspaceSettings | null }>();
const previewOnly = ref(false);
const addresses = (value: string) => value.split(/[;,]/).map(s => s.trim()).filter(Boolean);
const contactOptions = computed(() => {
  const options = props.contacts.map(c => ({ email: c.email, label: `${c.name} · ${c.email}` }));
  for (const email of [...addresses(doc.value.mail.to), ...addresses(doc.value.mail.cc)])
    if (!options.some(c => c.email.toLowerCase() === email.toLowerCase())) options.push({ email, label: email });
  return options;
});
function recipientModel(field: 'to' | 'cc') {
  return computed({ get: () => addresses(doc.value.mail[field]), set: (emails: string[]) => {
    doc.value.mail[field] = emails.join('; ');
    if (field === 'to') doc.value.mail.recipientName = emails.map(email => props.contacts.find(c => c.email === email)?.name || email).join(', ');
  } });
}
const to = recipientModel('to');
const cc = recipientModel('cc');
function useProfileSignature() {
  if (!props.profile) return;
  doc.value.mail.signature = props.profile.signature ? { ...props.profile.signature } : null;
  doc.value.mail.signatureWidth = props.profile.signatureWidth;
}
const html = computed(() => mailHtml(doc.value));
const clipboard = computed(() => prepareMailClipboard(html.value));
const subject = computed({
  get: () =>
    doc.value.mail.subject || `Pruebas Unitarias - ${doc.value.requirement}`,
  set: (value: string) => {
    doc.value.mail.subject = value;
  },
});
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
    notify(rich
      ? "Correo copiado con formato. En Outlook usa Ctrl+V y Mantener formato de origen; el mensaje debe estar en formato HTML. Adjunta el Excel descargado."
      : "Código HTML copiado.");
  } catch {
    notify("El navegador bloqueó el portapapeles. Puedes descargar el HTML.", 'error');
  }
}
</script>
<template>
  <div class="section-heading">
    <div>
      <span class="eyebrow">COMUNICACIÓN</span>
      <h2>Preparar correo</h2>
    </div>
    <div class="actions"><span class="pill">Tahoma {{ doc.mail.fontSize }} pt</span><Button :label="previewOnly ? 'Volver al editor' : 'Vista previa del correo'" :icon="previewOnly ? 'pi pi-pencil' : 'pi pi-eye'" severity="secondary" @click="previewOnly = !previewOnly" /></div>
  </div>
  <div v-show="!previewOnly">
  <details class="panel recipients-panel"><summary>Destinatarios y asunto <span>{{ doc.mail.to || 'Sin destinatarios' }}</span></summary><div class="fields two recipients-fields">
    <label
      >Para<MultiSelect v-model="to" :options="contactOptions" option-label="label" option-value="email" filter display="chip" :show-toggle-all="false" placeholder="Buscar nombre o correo" /></label
    ><label
      >CC<MultiSelect v-model="cc" :options="contactOptions" option-label="label" option-value="email" filter display="chip" :show-toggle-all="false" placeholder="Seleccionar copias" /></label
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
    <p class="subtle full">Administra los contactos en Perfil y configuración. Puedes seleccionar varios; los destinatarios anteriores también se conservan.</p>
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
        <Button label="Usar firma de mi perfil" severity="secondary" :disabled="!profile?.revision" @click="useProfileSignature" />
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
  </div>
  <section v-show="previewOnly" class="panel mail-preview-panel">
    <div class="section-heading compact">
      <h3>Vista previa del cuerpo</h3>
      <Button label="Copiar correo" icon="pi pi-copy" @click="copy(true)" />
    </div>
    <p class="subtle">Hoja de solo lectura. Muestra la estructura del correo; su apariencia final puede variar ligeramente en Outlook.</p>
    <div class="mail-paper-stage"><article class="mail-paper" aria-label="Vista previa del correo, solo lectura">
      <div class="mail-envelope"><p><strong>Para:</strong> {{ doc.mail.to || 'Sin destinatarios' }}</p><p v-if="doc.mail.cc"><strong>CC:</strong> {{ doc.mail.cc }}</p><p><strong>Asunto:</strong> {{ subject }}</p></div>
      <div class="email-preview" v-html="clipboard.fragment"></div>
    </article></div>
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



