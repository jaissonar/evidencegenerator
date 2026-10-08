<script setup lang="ts">
import { computed, ref } from 'vue';
import Editor from 'primevue/editor';
import Button from 'primevue/button';
import Dialog from 'primevue/dialog';
import Quill from 'quill';
import 'quill/dist/quill.core.css';
import type { StyleAttributor } from 'parchment';
import type { EvidenceDocument } from '../domain/document';
import { defaultMailBody } from '../services/mail';
import { sanitizeMailBody } from '../services/mailBody';
const fonts = ['Tahoma','Arial','Calibri','Verdana','Georgia','Times New Roman'];
const sizes = ['8pt','9pt','10pt','11pt','12pt','14pt','16pt','18pt','24pt','32pt'];
const font = Quill.import('attributors/style/font') as StyleAttributor;
font.whitelist = fonts; Quill.register(font, true);
const size = Quill.import('attributors/style/size') as StyleAttributor;
size.whitelist = sizes; Quill.register(size, true);
const doc = defineModel<EvidenceDocument>({required:true});
const resetDialog = ref(false);
const editorKey = ref(0);
const body = computed({
  get: () => sanitizeMailBody(doc.value.mail.bodyHtml ?? defaultMailBody(doc.value)),
  set: value => { doc.value.mail.bodyHtml = sanitizeMailBody(value ?? ''); },
});
function reset() { doc.value.mail.bodyHtml = null; editorKey.value++; resetDialog.value = false; }
</script>
<template>
  <section class="panel mail-composer">
    <div class="section-heading compact"><h3>Editor del cuerpo del correo</h3><Button label="Regenerar desde los datos" severity="secondary" @click="resetDialog = true" /></div>
    <p class="subtle">Selecciona el texto para aplicar formato. Al personalizar el cuerpo, los cambios posteriores del documento se incorporan con «Regenerar desde los datos». La firma y los avisos fijos se añaden al final.</p>
    <Editor :key="editorKey" v-model="body" editor-style="height: 360px" :style="{fontFamily:'Tahoma',fontSize:doc.mail.fontSize+'pt'}" :formats="['font','size','bold','italic','underline','strike','color','background','align','list','indent','link']">
      <template #toolbar>
        <span class="ql-formats"><select class="ql-font" aria-label="Fuente"><option v-for="name in fonts" :key="name" :value="name" :selected="name === 'Tahoma'">{{ name }}</option></select><select class="ql-size" aria-label="Tamaño"><option v-for="value in sizes" :key="value" :value="value" :selected="value === '11pt'">{{ value }}</option></select></span>
        <span class="ql-formats"><button class="ql-bold" aria-label="Negrita" title="Negrita"></button><button class="ql-italic" aria-label="Cursiva" title="Cursiva"></button><button class="ql-underline" aria-label="Subrayado" title="Subrayado"></button><button class="ql-strike" aria-label="Tachado" title="Tachado"></button></span>
        <span class="ql-formats"><select class="ql-color" aria-label="Color de texto"></select><select class="ql-background" aria-label="Resaltado"></select></span>
        <span class="ql-formats"><select class="ql-align" aria-label="Alineación"><option selected></option><option value="center"></option><option value="right"></option><option value="justify"></option></select><button class="ql-list" value="ordered" aria-label="Lista numerada"></button><button class="ql-list" value="bullet" aria-label="Viñetas"></button><button class="ql-indent" value="-1" aria-label="Reducir sangría"></button><button class="ql-indent" value="+1" aria-label="Aumentar sangría"></button></span>
        <span class="ql-formats"><button class="ql-link" aria-label="Insertar enlace"></button><button class="ql-clean" aria-label="Quitar formato"></button></span>
      </template>
    </Editor>
    <p v-if="(doc.mail.bodyHtml?.length ?? 0) > 100000" role="alert" class="error-text">El cuerpo supera el límite de 100.000 caracteres HTML. Reduce su contenido antes de guardar.</p>
    <Dialog v-model:visible="resetDialog" header="Regenerar cuerpo" modal :style="{width:'440px',maxWidth:'95vw'}"><p>Se reemplazará el texto y formato personalizado por los datos actuales del documento. La firma se conserva.</p><template #footer><Button label="Cancelar" severity="secondary" @click="resetDialog=false" /><Button label="Regenerar" @click="reset" /></template></Dialog>
  </section>
</template>
<style>
.mail-composer .ql-editor { font-family: Tahoma,Arial,sans-serif; font-size: inherit; line-height: 1.5; }
.mail-composer .ql-picker.ql-font { width: 145px; }
.mail-composer .ql-picker.ql-size { width: 65px; }
.mail-composer .ql-picker.ql-font .ql-picker-label::before,
.mail-composer .ql-picker.ql-font .ql-picker-item::before,
.mail-composer .ql-picker.ql-size .ql-picker-label::before,
.mail-composer .ql-picker.ql-size .ql-picker-item::before { content: attr(data-value); }
</style>


