# Perfil, configuración y guardado local

Actualizado: 8 de octubre de 2026.

## Primer uso

1. Abre **Crear mi perfil / Perfil y configuración**, en el menú lateral.
2. Escribe tu nombre y carga una foto opcional. El nombre completa **Elaborado por** en documentos nuevos y puedes corregirlo en cada documento. La foto solo identifica el perfil en el menú.
3. Opcionalmente indica una carpeta local completa para los Excel. Si no hay una ruta o la dejas vacía, se usará Descargas del usuario de Windows. Puedes copiarla desde la barra de direcciones del Explorador. Ejemplo ilustrativo: `C:\Mis documentos\Evidencias`. No se exige la misma carpeta a todos los desarrolladores.
4. Elige el comportamiento inicial: **Preguntar dónde guardar** o **Usar carpeta configurada**.
5. Despliega la firma predeterminada, carga su imagen y elige el ancho. También puedes quitarla o restaurar la imagen original del proyecto.
6. Agrega contactos con nombre y correo. Agrega URL de ambientes y conexiones en dos listas independientes. No se pide nombre ni motor, ni se relacionan las entradas de ambas listas.
7. Pulsa **Guardar configuración**. El perfil se conserva al cerrar el navegador y al reiniciar la API.

Esta configuración es individual, compartida por las ventanas que usan esta instalación local. No es una cuenta con autenticación ni un sistema multiusuario. No se solicitan credenciales Microsoft 365 o de bases de datos.

## Borrador y archivo Excel son guardados diferentes

**Guardar borrador** conserva el documento editable en SQLite. **Guardar Excel** genera el archivo de entrega a partir de lo que se ve en pantalla; no guarda automáticamente el borrador.

La cabecera permite escoger el modo en cada sesión:

| Modo | Qué sucede |
|---|---|
| Preguntar ubicación | Se abre un formulario con la carpeta sugerida. Pega o escribe una ruta completa y confirma Guardar en esta carpeta. Si el requerimiento ya tiene una exportación registrada, se propone su última carpeta. |
| Carpeta configurada | Se guarda directamente en la carpeta del perfil. |

La selección de la cabecera aplica a esa sesión; para conservarla como preferencia usa Configuración. La ubicación se indica mediante un campo de ruta opcional, no mediante un selector nativo de carpetas del sistema. Vacío significa Descargas del usuario de Windows, incluso en el diálogo de guardado. La ruta personalizada existente se conserva. Windows resuelve la ubicación real mediante SHGetKnownFolderPath/FOLDERID_Downloads, respetando traslados a otra unidad; no se fija el nombre de usuario ni se usa la carpeta Documentos. Consulta la [API de carpetas conocidas](https://learn.microsoft.com/en-us/windows/win32/api/shlobj_core/nf-shlobj_core-shgetknownfolderpath).

El nombre siempre es `Pruebas Unitarias - <requerimiento>.xlsx`. En la misma carpeta, exportar de nuevo el mismo requerimiento reemplaza el archivo anterior, sin `(1)` ni fechas adicionales. La comparación ignora espacios exteriores del requerimiento.

- Cierra el archivo en Excel antes de actualizarlo.
- La carpeta se crea si no existe y la cuenta que ejecuta la API tiene permisos.
- Si el archivo está bloqueado o falla la escritura, se muestra un error; no se entrega un archivo incompleto.
- El contenido se escribe primero en un temporal de la misma carpeta y se reemplaza el destino una vez finalizado.
- Cambiar de carpeta o de requerimiento genera una nueva ubicación; no elimina archivos anteriores.
- Dos borradores con el mismo requerimiento y carpeta actualizan el mismo Excel. Si necesitas entregables separados, utiliza carpetas diferentes.
- No se admiten rutas relativas ni UNC de red. La ruta corresponde al equipo donde se ejecuta la API local.

Despliega **Ubicación de borradores** para consultar la base SQLite, la carpeta configurada y la última ruta guardada del requerimiento actual. Esta última se recuerda en la base de datos; si mueves o borras el archivo externamente, la ruta sigue siendo un registro histórico, no una verificación de su existencia.

## Evidencias y ambientes

**Agregar evidencia** permanece fijo abajo a la derecha. Al pulsarlo se crea una evidencia al final, la pantalla se desplaza hasta ella y se enfoca su descripción. Mantiene el límite de 50 evidencias por motor y una imagen por evidencia.

Cada sección SQL u Oracle ofrece dos listas filtrables y editables: **URL / Sitio** y **Conexión / Ambiente**. Cada lista permite elegir un valor. Se comparten los catálogos entre ambos motores; elegir una URL no cambia la conexión y viceversa. Por ejemplo, puedes combinar https://pruebas.oasiscom.com con OasisComTest, OasisComTest2 u Oracle Test. Cambiar o eliminar opciones no modifica documentos anteriores. Los ambientes antiguos se separan automáticamente en URL y conexiones únicas al leer la configuración y la conversión queda persistida al guardarla. Vaciar una lista no recupera entradas antiguas.

## Contactos y correo

En **Preparar correo**, despliega **Destinatarios y asunto**. **Para** y **CC** son controles PrimeVue MultiSelect: permiten filtrar por nombre/correo y elegir varios contactos. El saludo se propone a partir de los nombres seleccionados en Para, pero permanece editable. Si personalizaste el cuerpo HTML, usa Regenerar desde los datos para incorporar un nuevo saludo a ese cuerpo.

Los destinatarios de borradores anteriores se conservan y aparecen como opciones adicionales, aunque ya no existan en el catálogo. No se borran correos al eliminar un contacto de Configuración. Para añadir un destinatario nuevo, regístralo en el catálogo.

**Vista previa del correo** abre una hoja blanca de solo lectura con Para, CC, asunto, cuerpo, firma y avisos. **Volver al editor** permite continuar editando sin perder el contenido. Copiar correo y Descargar HTML incluyen el cuerpo; los encabezados Para/CC/asunto de la hoja son informativos y no se insertan dentro del cuerpo copiado.

La vista es orientativa: Outlook puede representar fuentes, espacios e imágenes de manera diferente. El envío automático sigue fuera de esta implementación.

## Persistencia, respaldo y compatibilidad

SQLite contiene tres tablas: Document (borradores), WorkspaceSettings (perfil/configuración con revisión) y ExcelLocation (última ruta por requerimiento). Las nuevas tablas se crean sin reescribir documentos existentes.

La configuración usa revisión optimista: una ventana desactualizada recibe un conflicto en lugar de sobrescribir los cambios de otra. Sal y vuelve a abrir Configuración para cargar la versión actual. Los formularios advierten cambios pendientes al salir y al cerrar el navegador. El nombre y la firma nuevos se aplican a documentos nuevos; un responsable vacío puede completarse al guardar el perfil. **Usar firma de mi perfil** aplica explícitamente la firma al documento actual.

Respalda App_Data con la aplicación detenida y, por separado, las carpetas de Excel. Borrar un borrador no elimina archivos Excel. La base almacena la foto y firma como PNG; no se depende de la ubicación del archivo de imagen original.

## Implementación

- `components/WorkspaceSettings.vue`: edición del perfil, contactos, ambientes y preferencias.
- `domain/settings.ts`: tipos TypeScript y aplicación del perfil a documentos nuevos.
- `Documents/WorkspaceStore.cs`: validación, revisiones, preferencias y escritura local del Excel.
- `EvidenceEditor.vue`: botón fijo y selectores PrimeVue Select independientes de URL y conexión.
- `MailEditor.vue`: MultiSelect de contactos y hoja visual de solo lectura.
- `App.vue`: modos de guardado, diálogo de ubicación y presentación de la ruta efectiva.

### API local

| Endpoint | Contrato |
|---|---|
| GET `/api/settings` | Preferencias actuales; revision 0 si todavía no se guardaron. |
| PUT `/api/settings` | Guarda preferencias con revision; devuelve la revisión incrementada o 409 si cambió. |
| GET `/api/exports/location?requirement=...` | Última ruta guardada o null. |
| POST `/api/exports/save` | Recibe `{ document, directory }`, valida, genera y guarda; devuelve `{ requirement, path, savedAt }`. |
| POST `/api/exports/excel` | Conservado para compatibilidad: devuelve los bytes XLSX, sin guardado local. |

Se reutilizan los controles de acceso local/origen y las validaciones del documento. La carpeta y el nombre final se validan en servidor. La firma/foto deben ser PNG válidos dentro de los límites del proyecto. Máximos: 200 contactos, 100 URL y 100 conexiones. Los identificadores y correos duplicados se rechazan.

No se agregaron dependencias: se reutilizan Vue, PrimeVue, SQLite y las funciones de normalización de imágenes existentes.


## Notificaciones flotantes

Los avisos de operación aparecen abajo a la derecha sin desplazar el contenido, con fondo suave e iconos según el tipo: verde para confirmaciones, azul para información y rojo para errores. Éxitos e información se cierran a los 5 segundos activos; el contador se pausa al colocar el cursor o enfocar la X con el teclado, y continúa con el tiempo restante al salir. Los errores no tienen temporizador y requieren cerrar la X. Todas las notificaciones pueden cerrarse manualmente. Se anuncian con role=status o role=alert según su tipo. El servicio services/notifications.ts centraliza la temporización; components/Notifications.vue muestra una única pila global, incluso al cambiar de sección.

## Abrir el Excel guardado y dictar

En **Ubicación de borradores**, pulsa **Abrir ubicación del archivo** para abrir el Explorador con el último Excel del requerimiento seleccionado. Solo se permite abrir una ruta registrada por la aplicación y existente. Si se movió o eliminó, se informa para regenerarlo. No se crea otra descarga del navegador, conforme a la preferencia de conservar un único archivo. El endpoint POST /api/exports/open-location recibe `{ requirement }`; no acepta una ruta arbitraria del navegador.

El dictado utiliza modo continuo y resultados provisionales, con indicador de audio activo, voz detectada y duración de sesión. Puede reconectarse cuando el navegador finaliza una sesión; se detiene tras tres sesiones sin resultados confirmados. Los errores de permiso, red y dispositivo se explican por separado. El texto no confirmado o que supera el límite de 4.000 caracteres queda visible para revisarlo, copiarlo o incorporarlo. No se guardan grabaciones; los resultados pendientes son temporales en la sección. La exactitud del reconocimiento y la captura física dependen del navegador, el servicio y el micrófono.
