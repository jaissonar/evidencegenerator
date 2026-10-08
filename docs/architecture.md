# Arquitectura de Evidence Generator

## Decisión inicial

Monolito modular local: un frontend Vue 3 y una Web API .NET 10. Se separan Documentos, Excel y correo por responsabilidad, sin agregar microservicios, CQRS, repositorios genéricos ni una capa de dominio vacía. El sistema no conecta a SQL Server u Oracle: esos nombres clasifican las evidencias y sus entornos.

```text
Vue / TypeScript / PrimeVue
    ├── Información y evidencias ── PUT /api/documents/{id} ── SQLite
    ├── Vista previa de contenido
    ├── Descargar Excel ── POST /api/exports/excel ── IExcelGenerator
    │                                                  └── Plantilla XLSX v1
    └── Correo HTML seguro ── Portapapeles / descarga HTML
```

## Modelo y persistencia

`EvidenceDocument` contiene la información general, dos `EvidenceSection`, sus evidencias ordenadas e imágenes, y `MailSettings`. La posición en el arreglo define el orden, evitando un campo adicional que pueda quedar desincronizado.

SQLite guarda cada agregado en JSON, con `Id`, `Requirement`, `Revision` y `UpdatedAt` indexados según su uso. Esto reduce complejidad durante la evolución inicial del esquema y permite guardar imágenes junto con el borrador de forma atómica. Las actualizaciones comparan la revisión anterior: un conflicto devuelve HTTP 409. Los datos se guardan solo al pulsar **Guardar borrador**.

Límites: 50 evidencias por motor, 1 imagen por evidencia, 100 imágenes de evidencias por documento, 5 MB por PNG normalizado y 35 MB por solicitud. El navegador normaliza PNG/JPEG/WebP a PNG y reduce el lado mayor a 2400 píxeles. Se preserva la relación de aspecto. Las imágenes viven dentro del JSON; para mayor volumen conviene separar archivos y metadatos y agregar limpieza transaccional. No hay sincronización entre equipos.

## Generador de Excel

`TemplateExcelGenerator` modifica partes específicas del paquete OOXML mediante `ZipArchive` y `XDocument`. No necesita Excel instalado ni servicios externos. Esta elección conserva el XML original del instructivo, estilos, logotipos y configuración institucional, evitando una reconstrucción aproximada de todo el libro.

Contrato de plantilla v1:

| Hoja | Parte | Contenido dinámico |
|---|---|---|
| Instructivo | sheet1.xml | Ninguno |
| Evidencia Pruebas Unitarias SQL | sheet2.xml | Autor, fecha, sitio, conexión, plan, capturas |
| Evidencia Pruebas Unit Oracle | sheet3.xml | Autor, fecha, sitio, conexión, plan, capturas |

- Autor: A5. Fecha: E5. Sitio: D9. Conexión: D10.
- Inicio de sesión: imagen desde fila 15.
- Plan de pruebas: A60:N61, texto exacto Plan de Pruebas.
- Bloque de referencia: filas 63–109; descripción en A63:N65.
- Una captura por bloque de 47 filas: imagen anclada a 42 filas con borde de 1 pt, color #252932. Las filas conservan sus alturas originales; la imagen se expande a la altura total de las 42 filas. El ancho crece proporcionalmente al alto, sin limitarse a A:N; el área de impresión incluye la captura más ancha y se conserva la relación de aspecto. Los borradores antiguos con varias capturas se convierten en la interfaz en evidencias individuales; el archivo original no se modifica hasta guardar.
- La primera descripción incluye la descripción general del requerimiento.
- Las descripciones largas amplían las tres filas combinadas.
- Sin evidencias, la hoja indica que no se registraron evidencias para ese motor.
- Se retiran las capturas antiguas, sus relaciones y archivos de imagen no utilizados. Los textos compartidos sin referencias se vacían.
- Los textos del usuario se escriben como cadenas, nunca como fórmulas.
- El área de impresión se extiende al último bloque. La numeración institucional se conserva como parte del formato original.

El adaptador está vinculado a esta plantilla, no es un diseñador universal. Una nueva distribución requiere una versión nueva y pruebas de compatibilidad. La vista previa web es de contenido, no una representación exacta de la paginación de Excel. Antes de uso formal se debe validar la impresión en la versión de Excel utilizada por el equipo.

Plantilla original recuperada: `Pruebas Unitarias -  MD 11337.xlsx`.
SHA-256: `5ABAB97578EE0B955B28155A6D853C45C805EE7996336298D15C73D535B8FBCF`.

## Correo y voz

El cuerpo HTML se deriva del documento, con escape de texto, URL HTTP/HTTPS y firma PNG. Tahoma 11 o 12 pt. La firma y avisos se guardan por borrador. Para, CC y asunto son metadatos que el usuario traslada manualmente a Outlook. Copiar el correo copia el cuerpo, no adjunta el Excel. Algunas versiones de Outlook no conservan imágenes data URI del portapapeles: comprobar el resultado y, si hace falta, insertar la firma desde Outlook.

El dictado usa SpeechRecognition del navegador, idioma es-CO, con permiso de micrófono y escritura manual como alternativa. Puede requerir red y procesamiento del proveedor del navegador. No hay grabación persistida ni servicio de IA conectado. La prueba automática no activa un micrófono real.

## Seguridad y operación

- Solo localhost/loopback; lista permitida de orígenes. Vite enlaza a 127.0.0.1 y usa proxy a la API.
- Sin autenticación: MVP individual local. No publicar en red sin autenticación, autorización, HTTPS, límites y estrategia de almacenamiento adecuada.
- SQL parametrizado y actualización optimista; no se registran cuerpos con evidencias.
- Validación de tamaños, URLs y cabecera PNG. Para recibir archivos de usuarios no confiables habría que incorporar decodificación estricta y análisis adicional.
- SQLite y las capturas no están cifrados por la aplicación. Se aplican los permisos del usuario de Windows.
- Copias de seguridad: detener la aplicación antes de copiar App_Data para evitar omitir transacciones WAL.
- No hay contraseñas, tokens ni credenciales Microsoft 365. Graph queda pendiente con Microsoft Entra ID y OAuth/OIDC; nunca autenticación por contraseña almacenada.

## Evolución recomendada

1. Validación con evidencias reales, impresión y firma original.
2. Preferencias globales, gestión de plantillas versionadas y archivos fuera del JSON.
3. Publicación controlada: autenticación, autorización, despliegue y respaldo automatizado.
4. Graph con permisos delegados mínimos y creación de borradores con imágenes CID y adjuntos.
5. Histórico consultable y migración de almacenamiento solo si lo exige el volumen o uso compartido.

## Cambios en 0.2.0

El correo muestra solo Entorno SQL. La firma suministrada se inicializa en 420 px y los avisos de referencia usan Tahoma 8 pt. Los borradores anteriores se completan únicamente en campos vacíos mediante TemplateVersion, sin sobrescribir valores personalizados. /api/storage expone la ruta SQLite real para mostrarla en la interfaz. La publicación local incorpora el frontend en wwwroot. El detalle vigente está en Manual-Evidence-Generator.md.





## Preferencias y guardado local (8 de octubre de 2026)

WorkspaceStore comparte evidence.db con DocumentStore: WorkspaceSettings guarda el perfil/configuración con revisión optimista y ExcelLocation registra la última ruta por requerimiento. Document permanece compatible. Contactos y ambientes son catálogos locales; al seleccionar un ambiente se copian sus valores al documento para evitar modificar retrospectivamente borradores. La exportación de la UI usa POST /api/exports/save y escritura temporal con reemplazo en la misma carpeta. El endpoint anterior /api/exports/excel permanece disponible para clientes existentes. Consulta Perfil-y-almacenamiento.md para límites y contratos.
