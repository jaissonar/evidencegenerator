<script setup lang="ts">
import { useNotification } from './services/notifications';
import { computed, onBeforeUnmount, onMounted, ref, watch } from "vue";
import Button from "primevue/button";
import Textarea from "primevue/textarea";
import Dialog from "primevue/dialog";
import EvidenceEditor from "./components/EvidenceEditor.vue";
import MailEditor from "./components/MailEditor.vue";
import VoiceButton from "./components/VoiceButton.vue";
import ThemePicker from "./components/ThemePicker.vue";
import DraftBrowser from "./components/DraftBrowser.vue";
import Notifications from "./components/Notifications.vue";
import AboutGenerator from "./components/AboutGenerator.vue";
import WorkspaceSettingsPanel from './components/WorkspaceSettings.vue';
import type { WorkspaceSettings, ExportLocation } from './domain/settings';
import { applyProfile } from './domain/settings';
import { newDocument, splitLegacyEvidenceImages } from "./domain/document";
import { upgradeMailDefaults } from "./domain/mailDefaults";
import {
  getSettings, getExcelLocation, saveExcel,
  getDocument,
  saveDocument,
  getStorageInfo,
} from "./services/api";
const doc = ref(newDocument());
const settings = ref<WorkspaceSettings | null>(null);
const settingsDirty = ref(false);
const excelLocation = ref<ExportLocation | null>(null);
const saveExcelDialog = ref(false);
const excelDirectory = ref('');
const excelMode = ref<'ask' | 'configured'>('ask');
let locationRequest = 0;
watch(() => doc.value.requirement, async requirement => {
  const request = ++locationRequest;
  excelLocation.value = null;
  if (!requirement.trim()) return;
  try { const location = await getExcelLocation(requirement); if (request === locationRequest) excelLocation.value = location; }
  catch { /* Export remains available when the location lookup fails. */ }
});
async function loadSettings() {
  try {
    settings.value = await getSettings();
    excelMode.value = settings.value.saveMode;
    if (!doc.value.author && !doc.value.revision) {
      applyProfile(doc.value, settings.value);
      if (!dirty.value) savedSnapshot = JSON.stringify(doc.value);
    }
  } catch (e) { error.value = e instanceof Error ? e.message : 'No se pudo cargar la configuración.'; }
}
function settingsSaved(value: WorkspaceSettings) {
  settings.value = value;
  excelMode.value = value.saveMode;
  settingsDirty.value = false;
  if (!doc.value.author) doc.value.author = value.name;
}
async function changeStep(next: string) {
  if (next === 'settings' && step.value !== 'settings') await loadSettings();
  if (settingsDirty.value && next !== 'settings') {
    pendingNavigation = () => { settingsDirty.value = false; step.value = next; };
    discardDialog.value = true;
  } else step.value = next;
}
async function prepareExcel() {
  if (!settings.value) { await loadSettings(); if (!settings.value) return; }
  excelDirectory.value = excelMode.value === 'ask' && excelLocation.value
    ? excelLocation.value.path.replace(/[\\/][^\\/]+$/, '') : settings.value.excelDirectory;
  if (excelMode.value === 'ask') saveExcelDialog.value = true;
  else await writeExcel();
}
async function writeExcel() {
  await run(async () => {
    const location = await saveExcel(doc.value, excelDirectory.value);
    ++locationRequest;
    excelLocation.value = location;
    saveExcelDialog.value = false;
    notice.value = `Excel guardado o actualizado en: ${location.path}`;
  });
}
const step = ref("general");
const storageDetails = ref<HTMLDetailsElement>();
watch(step, () => {
  window.scrollTo({ top: 0, behavior: "instant" });
  if (storageDetails.value) storageDetails.value.open = false;
}, {
  flush: "post",
});
const busy = ref(false);
const dirty = ref(false);
const error = ref("");
const notice = ref("");
const showHistory = ref(false);

const storagePath = ref("Consultando ubicación…");
async function loadStoragePath() {
  try {
    storagePath.value = (await getStorageInfo()).databasePath;
  } catch {
    storagePath.value =
      "Ubicación no disponible. Comprueba la conexión con la API.";
  }
}
const discardDialog = ref(false);
let pendingNavigation: (() => void | Promise<void>) | undefined;
let savedSnapshot = JSON.stringify(doc.value);
watch(
  doc,
  (value) => {
    dirty.value = JSON.stringify(value) !== savedSnapshot;
  },
  { deep: true },
);
const steps = [
  { id: "general", label: "Información general", icon: "pi pi-file-edit" },
  { id: "sql", label: "Evidencias SQL", icon: "pi pi-database" },
  { id: "oracle", label: "Evidencias Oracle", icon: "pi pi-database" },
  { id: "preview", label: "Vista previa", icon: "pi pi-eye" },
  { id: "mail", label: "Preparar correo", icon: "pi pi-envelope" },
  { id: "about", label: "Acerca del generador", icon: "pi pi-info-circle" },
];
const evidenceCount = computed(
  () => doc.value.sql.items.length + doc.value.oracle.items.length,
);
const imageCount = computed(() =>
  [doc.value.sql, doc.value.oracle].reduce(
    (total, section) =>
      total +
      (section.loginImage ? 1 : 0) +
      section.items.reduce((sum, item) => sum + item.images.length, 0),
    0,
  ),
);
const sections = computed(() => [
  { name: "SQL", section: doc.value.sql },
  { name: "Oracle", section: doc.value.oracle },
]);
async function run(action: () => Promise<void>) {
  busy.value = true;
  error.value = "";
  notice.value = "";
  try {
    await action();
  } catch (e) {
    error.value =
      e instanceof Error ? e.message : "No se pudo completar la operación.";
  } finally {
    busy.value = false;
  }
}
async function save() {
  await run(async () => {
    // Keep edits made while the request was running; only advance the persisted revision.
    const snapshot = JSON.parse(JSON.stringify(doc.value));
    const saved = await saveDocument(snapshot);
    doc.value.revision = saved.revision;
    savedSnapshot = JSON.stringify(saved);
    dirty.value = JSON.stringify(doc.value) !== savedSnapshot;
    notice.value = dirty.value
      ? "Versión guardada. Hay cambios posteriores pendientes."
      : "Borrador guardado en este equipo.";
  });
}
function navigate(action: () => void | Promise<void>) {
  if (busy.value) return;
  if (dirty.value || settingsDirty.value) {
    pendingNavigation = () => { settingsDirty.value = false; return action(); };
    discardDialog.value = true;
  } else void action();
}
function create() {
  navigate(() => {
    doc.value = newDocument();
    applyProfile(doc.value, settings.value);
    savedSnapshot = JSON.stringify(doc.value);
    dirty.value = false;
    step.value = "general";
    notice.value = "";
    error.value = "";
  });
}
function open(id: string) {
  navigate(async () => {
    await run(async () => {
      doc.value = await getDocument(id);
      savedSnapshot = JSON.stringify(doc.value);
      dirty.value = upgradeMailDefaults(doc.value);
      if (dirty.value)
        notice.value =
          "Se aplicaron firma y avisos a los campos vacíos de este borrador. Guarda para conservarlos.";
      if (splitLegacyEvidenceImages(doc.value)) {
        dirty.value = true;
        notice.value =
          "Las capturas del borrador anterior se separaron en evidencias individuales, sin eliminar imágenes. Guarda para conservar la nueva organización.";
        if (
          [doc.value.sql, doc.value.oracle].some(
            (section) => section.items.length > 50,
          )
        )
          error.value =
            "La conversión supera 50 evidencias en un motor. Todas las imágenes se conservaron; distribuye el contenido en varios documentos antes de guardar.";
      }
      showHistory.value = false;
      step.value = "general";
    });
  });
}
function documentDeleted(id: string) {
  if (doc.value.id !== id) return;
  doc.value = newDocument();
  applyProfile(doc.value, settings.value);
  savedSnapshot = JSON.stringify(doc.value);
  dirty.value = false;
  step.value = "general";
  notice.value = "Documento eliminado. Puedes crear uno nuevo.";
  error.value = "";
}
async function historyDialog() {
  await loadStoragePath();
  await run(async () => {
    showHistory.value = true;
  });
}
function beforeUnload(event: BeforeUnloadEvent) {
  if (dirty.value || settingsDirty.value) event.preventDefault();
}
onMounted(() => {
  window.addEventListener("beforeunload", beforeUnload);
  void loadStoragePath();
  void loadSettings();
});
onBeforeUnmount(() => window.removeEventListener("beforeunload", beforeUnload));
useNotification(error, 'error');
useNotification(notice, 'success');
</script>

<template>
  <Notifications />
  <div class="app-shell">
    <aside class="sidebar">
      <div class="brand">
        <span class="brand-mark"><i class="pi pi-clone"></i></span>
        <div>Evidence<span>GENERATOR</span></div>
      </div>
      <div class="workspace-label">ESPACIO DE TRABAJO</div>
      <button class="side-link profile-link" @click="changeStep('settings')">
        <img v-if="settings?.photo" :src="settings.photo.dataUrl" alt="Tu foto de perfil" />
        <i v-else class="pi pi-user"></i><span>{{ settings?.name || 'Crear mi perfil' }}<small>Perfil y configuración</small></span>
      </button>
      <button class="side-link" @click="create">
        <i class="pi pi-plus-circle"></i> Nuevo documento
      </button>
      <button class="side-link" :disabled="busy" @click="historyDialog">
        <i class="pi pi-folder-open"></i> Borradores locales
      </button>
      <div class="sidebar-divider"></div>
      <div class="workspace-label">CONSTRUCTOR DE DOCUMENTOS</div>
      <nav aria-label="Secciones del documento">
        <button
          v-for="(item, index) in steps"
          :key="item.id"
          :class="['step', { active: step === item.id }]"
          :aria-current="step === item.id ? 'step' : undefined"
          @click="changeStep(item.id)"
        >
          <i :class="item.icon"></i><span>{{ item.label }}</span
          ><small>{{ index + 1 }}</small>
        </button>
      </nav>
      <ThemePicker />
      <div class="sidebar-footer">
        <span class="status-dot"></span> Almacenamiento local<small
          >Plantilla institucional · v1</small
        >
      </div>
    </aside>
    <div class="main-shell">
      <header
        class="document-bar"
        aria-label="Resumen y acciones del documento"
      >
        <div class="document-bar-main">
          <div class="document-identity">
            <span class="eyebrow">EVIDENCE GENERATOR</span>
            <h1>{{ doc.requirement || "Nuevo documento" }}</h1>
          </div>
          <div class="draft-status">
            <span class="save-state"
              ><i
                :class="dirty ? 'pi pi-circle-fill' : 'pi pi-check-circle'"
              ></i
              >{{
                dirty
                  ? "Cambios sin guardar"
                  : doc.revision
                    ? `Guardado · v${doc.revision}`
                    : "Borrador nuevo"
              }}</span
            >
          </div>
          <div class="actions">
            <Button
              label="Guardar borrador"
              icon="pi pi-save"
              severity="secondary"
              :disabled="busy"
              @click="save"
            /><Button
              label="Guardar Excel"
              icon="pi pi-download"
              :loading="busy"
              @click="prepareExcel"
            />
            <select v-model="excelMode" aria-label="Modo de guardado del Excel" :disabled="busy"><option value="ask">Preguntar ubicación</option><option value="configured">Carpeta configurada</option></select>
          </div>
        </div>
        <div class="document-bar-meta">
          <span
            ><strong>{{ evidenceCount }}</strong> evidencias · SQL +
            Oracle</span
          ><span
            ><strong>{{ imageCount }}</strong> capturas</span
          ><span>FO_GDS_07 · v4.0</span>
          <details ref="storageDetails" class="storage-detail">
            <summary>Ubicación de borradores</summary>
            <div class="storage-options">
            <p class="storage-location"><strong>Borradores:</strong> {{ storagePath }}</p>
            <p class="storage-location"><strong>Carpeta Excel configurada:</strong> {{ settings?.excelDirectory || 'Sin configurar' }}</p>
            <p class="storage-location"><strong>Último Excel de este requerimiento:</strong> {{ excelLocation?.path || 'Todavía no se ha guardado' }}</p>
            </div>
          </details>
        </div>
      </header>
      <main>
        <template v-if="step === 'general'">
          <div class="section-heading">
            <div>
              <span class="eyebrow">PASO 01</span>
              <h2>Información general</h2>
            </div>
          </div>
          <section class="panel">
            <div class="fields two">
              <label
                >Requerimiento *<input
                  v-model="doc.requirement"
                  placeholder="MD 00000"
                  maxlength="80"
                  required /></label
              ><label
                >Fecha de elaboración<input
                  v-model="doc.date"
                  type="date"
                  required /></label
              ><label
                >Elaborado por *<input
                  v-model="doc.author"
                  placeholder="Nombre del responsable"
                  maxlength="150" /></label
              ><label
                >Cliente<input
                  v-model="doc.client"
                  placeholder="Nombre del cliente"
                  maxlength="150" /></label
              ><label class="full" for="description"
                >Descripción del requerimiento *<Textarea
                  id="description"
                  v-model="doc.description"
                  rows="5"
                  auto-resize
                  maxlength="4000"
                  placeholder="¿Qué cambio se implementó y qué se debe validar?"
              /></label>
            </div>
            <div class="description-tools">
              <small>{{ doc.description.length }} / 4000 caracteres</small
              ><VoiceButton
                @text="
                  doc.description = `${doc.description} ${$event}`
                    .trim()
                    .slice(0, 4000)
                "
              />
            </div>
            <div class="panel-footer">
              <span class="subtle"
                >* Obligatorios para generar el documento</span
              ><Button
                label="Continuar con SQL"
                icon="pi pi-arrow-right"
                icon-pos="right"
                @click="step = 'sql'"
              />
            </div>
          </section>
        </template>
        <EvidenceEditor
          v-else-if="step === 'sql'"
          v-model="doc.sql"
          engine="SQL"
          :environment-urls="settings?.environmentUrls ?? []" :connections="settings?.connections ?? []"
        />
        <EvidenceEditor
          v-else-if="step === 'oracle'"
          v-model="doc.oracle"
          engine="Oracle"
          :environment-urls="settings?.environmentUrls ?? []" :connections="settings?.connections ?? []"
        />
        <template v-else-if="step === 'preview'"
          ><div class="section-heading">
            <div>
              <span class="eyebrow">REVISIÓN</span>
              <h2>Vista previa del contenido</h2>
            </div>
          </div>
          <p class="subtle">
            Resumen de contenido. La paginación y el diseño final se consultan
            en el Excel descargado.
          </p>
          <section class="document-preview panel">
            <div class="document-title">
              <span>PRUEBAS UNITARIAS</span>
              <h2>{{ doc.requirement || "Requerimiento" }}</h2>
              <p>{{ doc.author }} · {{ doc.date }} · {{ doc.client }}</p>
            </div>
            <p class="prewrap">{{ doc.description }}</p>
            <div v-for="{ name, section } in sections" :key="name">
              <h3 class="engine-band">Evidencia de pruebas {{ name }}</h3>
              <p>{{ section.url }} · {{ section.connection }}</p>
              <template v-if="section.loginImage"
                ><h4>Inicio de sesión</h4>
                <img
                  :src="section.loginImage.dataUrl"
                  :alt="`Inicio de sesión ${name}`"
              /></template>
              <p v-if="!section.items.length" class="subtle">
                Sin evidencias registradas para este motor.
              </p>
              <article
                v-for="(evidence, index) in section.items"
                :key="evidence.id"
              >
                <h4>Validación {{ index + 1 }}</h4>
                <p class="prewrap">
                  {{ evidence.description || "Descripción pendiente" }}
                </p>
                <img
                  v-for="(image, i) in evidence.images"
                  :key="i"
                  :src="image.dataUrl"
                  :alt="`Captura ${i + 1}`"
                />
              </article>
            </div></section
        ></template>
        <MailEditor v-else-if="step === 'mail'" v-model="doc" :contacts="settings?.contacts ?? []" :profile="settings" />
        <template v-else-if="step === 'settings'">
          <WorkspaceSettingsPanel v-if="settings" :settings="settings" @saved="settingsSaved" @dirty="settingsDirty = $event" />
          <section v-else class="panel"><p>No se pudo cargar la configuración.</p><Button label="Reintentar" @click="loadSettings" /></section>
        </template>
        <AboutGenerator v-else />
      </main>
    </div>
  </div>
  <Dialog
    v-model:visible="showHistory"
    header="Borradores locales"
    modal
    :style="{ width: '640px', maxWidth: '95vw' }"
  >
    <p class="storage-location">
      Ubicación de almacenamiento: <strong>{{ storagePath }}</strong>
    </p>
    <DraftBrowser
      v-if="showHistory"
      :disabled="busy"
      :active-id="doc.id"
      :active-dirty="dirty"
      @open="open"
      @deleted="documentDeleted"
    />
  </Dialog>
  <Dialog
    v-model:visible="discardDialog"
    header="Cambios sin guardar"
    modal
    :style="{ width: '440px', maxWidth: '95vw' }"
    ><p>Al continuar perderás los cambios pendientes de la sección que estás abandonando.</p>
    <template #footer
      ><Button
        label="Seguir editando"
        severity="secondary"
        @click="discardDialog = false" /><Button
        label="Descartar cambios"
        severity="danger"
        @click="
          discardDialog = false;
          pendingNavigation?.();
        " /></template
  ></Dialog>
  <Dialog v-model:visible="saveExcelDialog" header="Guardar o actualizar Excel" modal :closable="!busy" :style="{width:'620px',maxWidth:'95vw'}">
    <p>El archivo del mismo requerimiento se reemplazará en la carpeta elegida. Los borradores se mantienen en su base de datos local.</p>
    <label>Carpeta de destino (ruta completa)<input v-model="excelDirectory" maxlength="220" :disabled="busy" placeholder="Vacía: usar Descargas del sistema" /></label>
    <p class="subtle">Si dejas la ruta vacía, el archivo se guardará en Descargas del usuario de Windows.</p>
    <p class="storage-location">Archivo: Pruebas Unitarias - {{ doc.requirement.trim() }}.xlsx</p>

    <template #footer><Button label="Cancelar" severity="secondary" :disabled="busy" @click="saveExcelDialog=false" /><Button label="Guardar en esta carpeta" icon="pi pi-save" :loading="busy" @click="writeExcel" /></template>
  </Dialog>
</template>
