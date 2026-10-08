<script setup lang="ts">
import { useNotification } from '../services/notifications';
import { ref, watch } from 'vue';
import Button from 'primevue/button';
import ImageInput from './ImageInput.vue';
import type { WorkspaceSettings } from '../domain/settings';
import { saveSettings } from '../services/api';
import { defaultSignature } from '../domain/mailDefaults';
const props = defineProps<{ settings: WorkspaceSettings }>();
const emit = defineEmits<{ saved: [settings: WorkspaceSettings]; dirty: [value: boolean] }>();
const draft = ref<WorkspaceSettings>(JSON.parse(JSON.stringify(props.settings)));
if (!draft.value.revision) draft.value.signature = defaultSignature();
let snapshot = JSON.stringify(draft.value);
watch(draft, value => emit('dirty', JSON.stringify(value) !== snapshot), { deep: true });
const createId = () => crypto.randomUUID();
const busy = ref(false);
const error = ref('');
const status = ref('');
async function save() {
  busy.value = true; error.value = ''; status.value = '';
  try {
    draft.value = await saveSettings(draft.value);
    snapshot = JSON.stringify(draft.value);
    emit('saved', JSON.parse(snapshot)); emit('dirty', false);
    status.value = 'Configuración guardada. Se usará al crear documentos nuevos.';
  } catch (e) { error.value = e instanceof Error ? e.message : 'No se pudo guardar.'; }
  finally { busy.value = false; }
}
useNotification(error, 'error');
useNotification(status, 'success');
</script>
<template>
  <div class="section-heading"><div><span class="eyebrow">TU ESPACIO DE TRABAJO</span><h2>Perfil y configuración</h2></div></div>
  <p class="subtle">Datos locales de este equipo. El perfil y la firma se aplican a documentos nuevos; los borradores existentes conservan su información.</p>
  <form @submit.prevent="save">
    <fieldset :disabled="busy" class="settings-fieldset">
      <section class="panel"><h3>Perfil</h3>
        <label>Nombre del responsable<input v-model="draft.name" maxlength="150" required placeholder="Tu nombre" /></label>
        <div class="profile-photo" v-if="draft.photo"><img :src="draft.photo.dataUrl" alt="Foto de perfil" /><Button label="Quitar foto" text @click="draft.photo = null" /></div>
        <ImageInput label="Cargar foto de perfil" @images="draft.photo = $event[0] ?? null" />
        <p class="subtle">La foto identifica tu perfil en el menú; no se agrega al Excel ni sustituye la firma.</p>
      </section>
      <section class="panel"><h3>Archivos Excel</h3><div class="fields two">
        <label>Carpeta de almacenamiento<input v-model="draft.excelDirectory" maxlength="220" placeholder="Vacía: usar Descargas del sistema" /></label>
        <label>Al guardar Excel<select v-model="draft.saveMode"><option value="ask">Preguntar dónde guardar</option><option value="configured">Usar carpeta configurada</option></select></label>
      </div><p class="subtle">Si dejas la ruta vacía, se usará Descargas del usuario de Windows. También puedes copiar otra ruta desde el Explorador. Si no existe se creará al guardar. El mismo requerimiento actualiza el mismo archivo en esa carpeta, sin sufijos ni copias adicionales. Cierra el Excel antes de actualizarlo.</p></section>
      <details class="panel signature-panel"><summary>Firma predeterminada del correo</summary>
        <div class="settings-content"><label>Ancho de la firma<select v-model.number="draft.signatureWidth"><option :value="280">280 px</option><option :value="360">360 px</option><option :value="420">420 px</option><option :value="480">480 px</option><option :value="600">600 px</option></select></label>
        <ImageInput label="Cargar firma predeterminada" @images="draft.signature = $event[0] ?? null" />
        <img v-if="draft.signature" :src="draft.signature.dataUrl" alt="Firma predeterminada" :style="{ width: `${draft.signatureWidth}px`, maxWidth: '100%' }" />
        <div class="actions"><Button label="Quitar firma" severity="secondary" @click="draft.signature = null" /><Button label="Restaurar firma original" text @click="draft.signature = defaultSignature()" /></div></div>
      </details>
      <section class="panel"><div class="section-heading compact"><h3>Contactos del correo</h3><Button label="Agregar contacto" icon="pi pi-plus" :disabled="draft.contacts.length >= 200" @click="draft.contacts.push({id:createId(), name:'', email:''})" /></div>
        <p class="subtle">Podrás buscar por nombre o correo y seleccionar varios contactos en Para y CC.</p>
        <div v-for="(contact,index) in draft.contacts" :key="contact.id" class="settings-row">
          <label>Nombre<input v-model="contact.name" required maxlength="150" /></label><label>Correo electrónico<input v-model="contact.email" type="email" required maxlength="254" /></label><Button icon="pi pi-trash" :aria-label="`Quitar contacto ${index+1}`" severity="danger" text @click="draft.contacts.splice(index,1)" />
        </div><p v-if="!draft.contacts.length" class="subtle">Aún no tienes contactos guardados.</p>
      </section>
      <section class="panel"><h3>URL de ambientes</h3>
        <p class="subtle">Lista independiente disponible tanto en SQL como en Oracle. Cada documento puede combinar cualquier URL con cualquier conexión.</p>
        <div v-for="(_,index) in draft.environmentUrls" :key="index" class="catalog-row">
          <label :for="`environment-url-${index}`">URL {{ index+1 }}<input :id="`environment-url-${index}`" v-model.trim="draft.environmentUrls[index]" type="url" required maxlength="2048" placeholder="https://desarrollo.oasiscom.com" /></label>
          <Button icon="pi pi-trash" :aria-label="`Quitar URL ${index+1}`" severity="danger" text @click="draft.environmentUrls.splice(index,1)" />
        </div>
        <Button label="Agregar URL" icon="pi pi-plus" severity="secondary" :disabled="draft.environmentUrls.length >= 100" @click="draft.environmentUrls.push('')" />
      </section>
      <section class="panel"><h3>Conexiones</h3>
        <p class="subtle">Sin relación con las URL ni con un motor. Ejemplos: OasisComTest, OasisComTest2, Oracle Test.</p>
        <div v-for="(_,index) in draft.connections" :key="index" class="catalog-row">
          <label :for="`connection-${index}`">Conexión {{ index+1 }}<input :id="`connection-${index}`" v-model.trim="draft.connections[index]" required maxlength="150" placeholder="OasisComTest" /></label>
          <Button icon="pi pi-trash" :aria-label="`Quitar conexión ${index+1}`" severity="danger" text @click="draft.connections.splice(index,1)" />
        </div>
        <Button label="Agregar conexión" icon="pi pi-plus" severity="secondary" :disabled="draft.connections.length >= 100" @click="draft.connections.push('')" />
      </section>
    </fieldset>
    <div class="settings-save"><Button type="submit" label="Guardar configuración" icon="pi pi-save" :loading="busy" /></div>
  </form>
</template>
<style scoped>
.settings-fieldset { border:0; padding:0; margin:0; min-width:0; }
.settings-content { padding-top:20px; }
.settings-row { display:grid; grid-template-columns:1fr 1fr auto; gap:16px; align-items:end; padding:14px 0; }
.catalog-row { display:flex; gap:12px; align-items:end; margin-bottom:16px; } .catalog-row label { flex:1; min-width:0; }
.profile-photo { display:flex; gap:16px; align-items:center; margin:20px 0; }
.profile-photo img { width:72px; height:72px; border-radius:50%; object-fit:cover; }
.settings-save { position:sticky; bottom:12px; background:white; padding:16px; border:1px solid #e4e7ec; border-radius:12px; display:flex; gap:20px; align-items:center; justify-content:flex-end; z-index:2; box-shadow:0 4px 20px #11131814; }
@media(max-width:760px) { .settings-row { grid-template-columns:1fr; } }
</style>
