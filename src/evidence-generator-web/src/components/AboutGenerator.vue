<script setup lang="ts">
const technologies = [
  ['Vue 3 y TypeScript', 'Organizan la interfaz, los formularios y la información del documento.'],
  ['PrimeVue y Quill', 'Proporcionan los controles visuales y el editor de correo con formato.'],
  ['Vite', 'Inicia la interfaz durante el desarrollo y prepara sus archivos para la instalación.'],
  ['ASP.NET Core / .NET 10', 'Valida los documentos, gestiona los borradores y genera el Excel.'],
  ['SQLite', 'Guarda localmente borradores, evidencias y ajustes del correo en este equipo.'],
  ['Plantilla Excel y OOXML', 'Permiten crear bloques de evidencias conservando el formato institucional.'],
  ['DOMPurify', 'Limpia el HTML del editor antes de mostrarlo y prepararlo para copiar.'],
];
</script>
<template>
  <div class="section-heading"><div><span class="eyebrow">ACERCA DE LA APLICACIÓN</span><h2>¿Para qué sirve Evidence Generator?</h2></div></div>
  <section class="panel"><h3>Documentar pruebas con menos trabajo manual</h3><p>El generador reúne los datos de un requerimiento, sus evidencias SQL y Oracle y el correo de validación en un mismo lugar. Su objetivo es ayudarte a entregar documentación clara y consistente sin armar el Excel y el correo desde cero.</p><ol><li>Completa la información del requerimiento y los entornos.</li><li>Agrega una evidencia por validación, con descripción y una imagen.</li><li>Revisa y descarga el Excel con la plantilla institucional.</li><li>Personaliza el correo y cópialo para enviarlo desde Outlook.</li></ol><p>Los borradores se guardan en este equipo al pulsar «Guardar borrador». El generador documenta las pruebas; no ejecuta consultas ni se conecta a las bases de datos SQL/Oracle. El envío directo con Microsoft 365 todavía no está habilitado.</p></section>
  <section class="panel"><h3>Cómo está construido</h3><dl class="technology-list"><div v-for="[name, purpose] in technologies" :key="name"><dt>{{ name }}</dt><dd>{{ purpose }}</dd></div></dl></section>
  <section class="panel"><h3>Tu información</h3><p>Las imágenes y el contenido se guardan junto al borrador local. El tema de color se recuerda en una cookie del navegador. El dictado depende del servicio de voz del navegador y puede utilizar conexión a internet. Puedes consultar la ubicación del archivo de borradores en la barra superior.</p></section>
</template>
<style scoped>
li { margin: 10px 0; line-height: 1.6; }
.technology-list { margin: 0; }
.technology-list div { display: grid; grid-template-columns: 210px 1fr; gap: 16px; padding: 14px 0; border-bottom: 1px solid #e6e8ed; }
dt { font-weight: bold; } dd { margin: 0; line-height: 1.6; color: #667085; }
@media (max-width:760px) { .technology-list div { grid-template-columns: 1fr; gap: 6px; } }
</style>
