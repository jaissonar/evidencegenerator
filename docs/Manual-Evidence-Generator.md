# Evidence Generator — Manual técnico y operativo

**Versión de la aplicación:** 0.2.0  
**Fecha:** 7 de octubre de 2026  
**Plataforma inicial:** Windows x64, ejecución local e individual  
**Directorio del proyecto:** `C:\projects-jn\evidence generator`

Este manual describe la implementación real, cómo utilizarla, cómo instalarla en otro equipo, cómo compilar y publicar, dónde se almacenan los datos y qué aspectos requieren validación o evolución. Los comandos suponen PowerShell y la ruta indicada; si se copia el proyecto, se debe sustituir esa ruta por la nueva.

## Índice

1. [Propósito y alcance](#1-propósito-y-alcance)
2. [Funcionalidades y ajustes actuales](#2-funcionalidades-y-ajustes-actuales)
3. [Arquitectura y decisiones](#3-arquitectura-y-decisiones)
4. [Tecnologías y dependencias](#4-tecnologías-y-dependencias)
5. [Organización del repositorio](#5-organización-del-repositorio)
6. [Requisitos e instalación para desarrollo](#6-requisitos-e-instalación-para-desarrollo)
7. [Inicio y detención](#7-inicio-y-detención)
8. [Uso completo de la aplicación](#8-uso-completo-de-la-aplicación)
9. [Motor de Excel y contrato de plantilla](#9-motor-de-excel-y-contrato-de-plantilla)
10. [Correo, firma y avisos](#10-correo-firma-y-avisos)
11. [Persistencia y concurrencia](#11-persistencia-y-concurrencia)
12. [Contrato de API](#12-contrato-de-api)
13. [Compilación y pruebas](#13-compilación-y-pruebas)
14. [Publicación e instalación compilada](#14-publicación-e-instalación-compilada)
15. [Configuración](#15-configuración)
16. [Respaldo, recuperación y actualización](#16-respaldo-recuperación-y-actualización)
17. [Seguridad y privacidad](#17-seguridad-y-privacidad)
18. [Diagnóstico de problemas](#18-diagnóstico-de-problemas)
19. [Mantenimiento y evolución](#19-mantenimiento-y-evolución)
20. [Verificación y límites conocidos](#20-verificación-y-límites-conocidos)
21. [Referencias](#21-referencias)

## 1. Propósito y alcance

Evidence Generator permite capturar una sola vez la información de un requerimiento y utilizarla para producir dos resultados: un Excel institucional de evidencias y el cuerpo de un correo de validación.

La aplicación reemplaza tareas repetitivas: pegar capturas en Excel, dimensionarlas, copiar descripciones, conservar el formato y volver a escribir los datos en Outlook. El archivo se genera a partir del Excel original, no de una reproducción visual desde cero.

### Funciones implementadas

| Área | Comportamiento |
|---|---|
| Información general | Requerimiento, descripción, autor, cliente y fecha |
| Evidencias | SQL y Oracle, paneles plegables, reordenamiento y eliminación confirmada |
| Entorno | Sitio y conexión independientes por motor |
| Imágenes | Subida, arrastre, pegado con Ctrl+V, eliminación y reordenamiento |
| Texto | Escritura, dictado compatible y botón Limpiar por evidencia |
| Borradores | Guardado explícito, búsqueda, fechas, paginación, eliminación confirmada y revisión optimista |
| Vista previa | Resumen web del contenido del documento |
| Excel | Plantilla institucional, bloques dinámicos, anclaje de capturas a 42 filas y borde oscuro de 1 pt |
| Correo | Editor enriquecido, Entorno SQL, fuentes/tamaños/colores, HTML seguro y copia con formato |
| Firma | Imagen suministrada, ancho inicial 420 px y tamaños ajustables |
| Avisos | Textos fijos después de la firma, Tahoma 8 pt y colores originales |
| Ejecución compilada | Frontend estático servido por la API en un único proceso local |

SQL y Oracle son **clasificaciones documentales**. No se abre ninguna conexión a esos motores, no se ejecutan consultas ni se solicitan contraseñas de bases de datos.

### Fuera del alcance actual

No hay usuarios y roles, colaboración entre equipos, sincronización en nube, envío de correo, adjuntos automáticos, diseñador universal de plantillas, firma HTML importada desde Outlook, transcripción propia ni revisión de textos mediante IA.

## 2. Funcionalidades y ajustes actuales

1. Las capturas nuevas del Excel abarcan 42 filas exactas, tanto en inicio de sesión como en evidencias. El borde mide 1 punto y utiliza `#252932`.
2. El encabezado azul de ambas hojas de evidencias contiene exclusivamente **Plan de Pruebas**.
3. El correo utiliza únicamente los datos SQL, bajo el título **Entorno SQL:**. La sección Oracle continúa disponible en el editor y en el Excel.
4. Se incorporó la imagen original de firma como recurso del proyecto. No se redibujó ni se alteró el contenido de la imagen.
5. La ubicación real del archivo de borradores aparece en el estado del documento y en la ventana de borradores.
6. Los avisos se inicializan con el texto de la referencia. Ambos usan Tahoma 8 pt; el ambiental es verde y la confidencialidad negra, con **OasisCom S.A.S.** en negrita.
7. Cada descripción de evidencia tiene un botón **Limpiar**, que borra solo su texto, sin eliminar capturas ni la evidencia.
8. La frase **requerimiento + identificador** usa negrita azul. Los hipervínculos se muestran azules y subrayados; conexión y cliente, en cursiva.

Los colores se obtuvieron de píxeles predominantes de las capturas proporcionadas: azul `#174E86` y verde `#0F7001`. Se tomó el texto de los avisos tal como aparecía, incluida su redacción original.

Además: temas persistentes por cookie, búsqueda y eliminación de borradores, evidencias plegables con eliminación confirmada, editor enriquecido, firma plegable, avisos fijos y copiado con estilos explícitos. Inicio sencillo mediante Iniciar.cmd. La conexión a Microsoft 365 está documentada, todavía no implementada.

## 3. Arquitectura y decisiones

### 3.1 Estructura general

Se implementó un monolito modular sencillo: un cliente web y una API local. La separación principal se realiza por responsabilidad, sin introducir servicios distribuidos ni capas vacías.

```text
Navegador
  └─ Vue 3 + TypeScript + PrimeVue
       ├─ Información general
       ├─ Evidencias SQL / Oracle
       ├─ Normalización de capturas
       ├─ Vista previa del contenido
       ├─ Constructor seguro de correo HTML
       └─ Cliente HTTP /api
                    │
                    ▼
          ASP.NET Core .NET 10
            ├─ Validación del documento
            ├─ DocumentStore ── SQLite
            ├─ IExcelGenerator ── TemplateExcelGenerator
            │                          └─ Plantilla XLSX original
            └─ Archivos web estáticos en la publicación compilada
```

### 3.2 Desarrollo frente a ejecución compilada

En desarrollo, Vite sirve la interfaz en `127.0.0.1:5173`. Las solicitudes a `/api` se redirigen por proxy a la API en `127.0.0.1:5080`. Así el navegador trabaja con un solo origen visible y no hace falta habilitar CORS de forma amplia.

En la publicación local, la API sirve los archivos compilados desde `wwwroot`. La interfaz y la API comparten `127.0.0.1:5080`. El equipo donde se ejecuta esa publicación no necesita Node.js.

### 3.3 Decisiones y trade-offs

| Decisión | Motivo | Límite o evolución |
|---|---|---|
| Monolito modular | Entrega rápida, operación simple y responsabilidades identificables | Dividir proyectos o servicios solo ante necesidades reales |
| Estado Vue con ref/computed | El flujo principal gira alrededor de un único documento | Pinia puede incorporarse si aparecen múltiples flujos independientes |
| Sin Vue Router | Navegación local entre paneles del mismo constructor | Agregar rutas si se requieren enlaces permanentes a documentos |
| SQLite | No requiere servidor ni credenciales | Para acceso compartido se necesita revisar arquitectura y almacenamiento |
| Agregado JSON en SQLite | Evolución inicial flexible y guardado atómico del documento | Consultas analíticas y documentos grandes favorecerán un modelo más normalizado |
| Imágenes dentro del agregado | Evita referencias rotas y simplifica recuperación | Aumenta memoria, tamaño de solicitudes y base de datos |
| Adaptador OOXML específico | Conserva mejor la plantilla institucional | Una plantilla con otra distribución requiere un adaptador nuevo |
| HTML parametrizado | Contenido consistente y escape seguro de entradas | No es un editor enriquecido de HTML arbitrario |
| Reconocimiento del navegador | Dictado sin instalar otro servicio | Compatibilidad, permisos y procesamiento dependen del navegador |

No se incorporaron CQRS, un bus de eventos, repositorios genéricos ni microservicios porque el alcance local no justifica ese costo de mantenimiento.

## 4. Tecnologías y dependencias

### 4.1 Backend

| Elemento | Configuración del proyecto |
|---|---|
| Plataforma | ASP.NET Core Web API, `net10.0` |
| SDK | `global.json`: 10.0.100 con `rollForward: latestFeature` |
| SDK utilizado en este equipo | 10.0.401 |
| SQLite | Microsoft.Data.Sqlite 10.0.12 |
| Proveedor nativo | SQLitePCLRaw.bundle_e_sqlite3 3.0.5 |
| Serialización | System.Text.Json |
| XLSX | ZipArchive y XDocument de .NET |
| Pruebas | xUnit, Microsoft.AspNetCore.Mvc.Testing y DocumentFormat.OpenXml |

DocumentFormat.OpenXml se utiliza en pruebas para comprobar el paquete generado; no es una dependencia del motor de producción.

### 4.2 Frontend

| Elemento | Función |
|---|---|
| Vue 3 | Interfaz reactiva y componentes |
| TypeScript | Modelo tipado, eventos y contratos |
| Vite | Desarrollo, proxy y compilación |
| PrimeVue 4.5.5 | Botones, diálogos y entradas de texto |
| @primeuix/themes 1.2.5 | Tema Aura y tokens visuales |
| PrimeIcons 7.0.0 | Iconografía |
| Vitest | Pruebas unitarias de correo y ordenamiento |
| Playwright | Prueba integral con Microsoft Edge |

Las versiones directas se consultan en `package.json`; las versiones resueltas están fijadas en `package-lock.json`. Para reproducir la instalación debe utilizarse `npm ci`.

PrimeVue se mantiene en la línea 4.5.5 MIT. La actualización a PrimeVue 5 no debe hacerse automáticamente: cambia el esquema de licenciamiento. Esta decisión también evita solicitar claves de licencia para la interfaz actual.

## 5. Organización del repositorio

```text
evidence generator/
├─ EvidenceGenerator.slnx
├─ global.json
├─ .editorconfig
├─ .gitattributes
├─ .gitignore
├─ Start-Dev.ps1
├─ Publish-Local.ps1
├─ README.md
├─ docs/
│  ├─ Manual-Evidence-Generator.md
│  ├─ architecture.md
│  └─ validation.md
├─ src/
│  ├─ EvidenceGenerator.Api/
│  │  ├─ Program.cs
│  │  ├─ appsettings.json
│  │  ├─ Documents/
│  │  │  ├─ Document.cs
│  │  │  └─ DocumentStore.cs
│  │  ├─ Excel/TemplateExcelGenerator.cs
│  │  ├─ Templates/PruebasUnitarias.v1.xlsx
│  │  └─ App_Data/evidence.db
│  └─ evidence-generator-web/
│     ├─ package.json
│     ├─ package-lock.json
│     ├─ vite.config.ts
│     ├─ playwright.config.ts
│     ├─ e2e/document.spec.ts
│     └─ src/
│        ├─ App.vue
│        ├─ main.ts
│        ├─ style.css
│        ├─ assets/email/firma-correo.png
│        ├─ components/
│        │  ├─ EvidenceEditor.vue
│        │  ├─ ImageInput.vue
│        │  ├─ MailEditor.vue
│        │  └─ VoiceButton.vue
│        ├─ domain/
│        │  ├─ document.ts
│        │  └─ mailDefaults.ts
│        └─ services/
│           ├─ api.ts
│           ├─ images.ts
│           ├─ mail.ts
│           └─ mail.test.ts
├─ tests/EvidenceGenerator.Tests/
├─ artifacts/local/       # Resultado de Publish-Local.ps1
└─ work/                  # Logs y comprobaciones locales
```

`bin`, `obj`, `node_modules`, `dist`, `App_Data`, `artifacts` y `work` están excluidos de Git. La plantilla original y la firma sí son recursos del proyecto. Antes de compartir el repositorio debe revisarse si esos recursos corporativos pueden distribuirse a sus destinatarios.

## 6. Requisitos e instalación para desarrollo

### 6.1 Requisitos

- Windows x64 con permisos de escritura en la carpeta del proyecto.
- .NET SDK 10.
- Node.js compatible con Vite; en este equipo se verificó Node 20.20.2 y npm 10.8.2. Esto describe el entorno probado, no una recomendación sobre el ciclo de soporte de Node.
- Navegador actualizado. La prueba automática utiliza Microsoft Edge instalado.
- Acceso inicial a NuGet y al registro npm para descargar dependencias.
- Git si se desea gestionar versiones; no es obligatorio para ejecutar.

No se necesita instalar Microsoft Excel, SQL Server, Oracle ni SQLite como servidor para generar archivos.

### 6.2 Comprobar herramientas

```powershell
dotnet --list-sdks
dotnet --list-runtimes
node --version
npm --version
git --version
```

El SDK compatible debe resolver `net10.0`. Si la restauración indica que no encuentra el SDK, revisar `global.json` y la instalación de .NET antes de modificar el destino del proyecto.

### 6.3 Instalar desde el código fuente

Copiar o clonar el proyecto completo, incluidos la plantilla XLSX, la firma PNG y el archivo de bloqueo de npm. No copiar dependencias de `node_modules` desde otro equipo: instalarlas a partir del archivo de bloqueo.

```powershell
Set-Location 'C:\projects-jn\evidence generator'
dotnet restore EvidenceGenerator.slnx

Set-Location 'C:\projects-jn\evidence generator\src\evidence-generator-web'
npm ci
```

Si se va a ejecutar `npm ci` en una instalación existente, detener primero Vite: en Windows puede mantener bloqueada la biblioteca nativa de Rolldown. Los paquetes se descargan según la configuración local; el proyecto no pide credenciales Microsoft 365.

## 7. Inicio y detención

### 7.0 Inicio sencillo recomendado

Desde PowerShell, sin importar la carpeta actual:

```powershell
& 'C:\projects-jn\evidence generator\Iniciar.cmd'
```

También puedes hacer doble clic en **Iniciar.cmd** desde el Explorador o crear un acceso directo a ese archivo. No requiere privilegios de administrador. El lanzador usa la carpeta del proyecto, comprueba puertos y herramientas, instala dependencias si faltan, inicia la API, espera su respuesta y abre la interfaz en el navegador predeterminado. Mantén la terminal abierta; Ctrl+C detiene los procesos iniciados por esa sesión. En el archivo .cmd puede aparecer una confirmación de Windows para terminar el lote.

Si ambos servicios de Evidence Generator ya responden correctamente, el comando abre la aplicación existente sin iniciar duplicados. Si un puerto está ocupado por otro proceso o solo queda parte de una sesión anterior, muestra un error: no finaliza procesos ajenos automáticamente. Cierra la sesión anterior o usa el inicio manual por componente para completarla.

`Iniciar.cmd` usa PowerShell con `-ExecutionPolicy Bypass` únicamente para ese proceso; no modifica la política global ni el registro. Una política corporativa impuesta puede impedirlo. En ese caso utiliza las dos terminales de la sección 7.2 o solicita a TI la firma/aprobación del script; no desactives las políticas de la organización.

Requiere .NET SDK 10, Node.js/npm y dependencias restauradas (internet en la primera instalación). Para iniciar sin abrir navegador: `.\Iniciar.cmd -NoBrowser`. Este lanzador usa el modo de desarrollo y sus borradores en `src/EvidenceGenerator.Api/App_Data`. La ejecución compilada usa otro directorio de datos salvo configuración explícita: consulta la sección 14 antes de trasladarte a ella.

### 7.1 Inicio con el script

```powershell
Set-Location 'C:\projects-jn\evidence generator'
.\Start-Dev.ps1
```

Abrir **http://127.0.0.1:5173/**.

El script comprueba los puertos, crea `work`, instala dependencias si falta `node_modules`, inicia la API en segundo plano y mantiene Vite en la terminal. La API escribe en `work/api.log` y `work/api.error.log`.

La terminal debe mantenerse abierta. Ctrl+C detiene el frontend y el bloque de limpieza del script solicita detener el proceso de API que inició. Si se fuerza el cierre de la terminal, conviene comprobar los puertos antes de iniciar otra sesión.

### 7.2 Inicio manual en dos terminales

Terminal de API:

```powershell
Set-Location 'C:\projects-jn\evidence generator'
dotnet run --project src/EvidenceGenerator.Api --no-launch-profile --urls http://127.0.0.1:5080
```

Terminal de frontend:

```powershell
Set-Location 'C:\projects-jn\evidence generator\src\evidence-generator-web'
npm run dev
```

Detener cada terminal con Ctrl+C. Esta alternativa no necesita cambiar la política de ejecución de scripts de PowerShell.

### 7.3 Comprobar la API

```powershell
Invoke-RestMethod 'http://127.0.0.1:5080/api/health'
Invoke-RestMethod 'http://127.0.0.1:5080/api/storage'
```

El primer endpoint informa `status: ok`. El segundo devuelve `provider: SQLite` y la ruta absoluta real de `evidence.db`.

## 8. Uso completo de la aplicación

### 8.1 Crear el documento

1. Seleccionar **Nuevo documento**.
2. Ingresar el identificador, por ejemplo `MD 11337`.
3. Confirmar la fecha de elaboración.
4. Completar el responsable y cliente.
5. Escribir la descripción del requerimiento.

Requerimiento, autor y descripción son obligatorios para generar el Excel. El requerimiento admite hasta 80 letras, números, espacios, guiones, guiones bajos y puntos; se utiliza también en el nombre del archivo. La descripción admite 4.000 caracteres.

La fecha se inicializa con la fecha local del navegador. Los datos se mantienen en memoria hasta que se pulsa **Guardar borrador**; no hay autoguardado.

### 8.2 Definir entornos

Entrar a **Evidencias SQL** y completar URL y conexión. Repetir para Oracle si corresponde. La URL debe ser HTTP o HTTPS y puede escribirse libremente; las opciones de OasisCom son sugerencias, no una lista cerrada. Los dos motores conservan campos independientes.

Una sección sin evidencias se documenta como tal en su hoja del Excel. El correo toma únicamente los datos SQL, incluso si el documento también incluye Oracle.

### 8.3 Inicio de sesión

Cada motor permite una captura opcional de inicio de sesión. Se puede cargar, arrastrar o pegar sobre el área correspondiente. **Quitar captura** elimina únicamente esa imagen del borrador en edición.

### 8.4 Agregar y describir evidencias

1. Pulsar **Crear primera evidencia** o **Agregar evidencia**.
2. Describir qué se hizo y qué resultado se observó.
3. Incorporar una única imagen por evidencia. Para documentar otra captura, crear otra evidencia.
4. Usar las flechas para ordenar evidencias. Para reemplazar la imagen, quitar la actual y agregar la nueva.

**Limpiar** vacía únicamente el texto de esa evidencia. Las imágenes, su orden y la tarjeta permanecen. Para recuperar un texto eliminado puede utilizarse el mecanismo de edición disponible en el campo antes de perder el foco, pero la aplicación no incorpora un historial de deshacer propio; no debe asumirse que toda eliminación es recuperable.

Si una descripción está vacía, se puede seguir editando y guardar un borrador, pero se rechazará la exportación de una evidencia sin descripción. Una evidencia puede contener texto sin imágenes.

### 8.5 Pegar y cargar capturas

Para pegar: tomar una captura, por ejemplo con Win+Shift+S, hacer clic en el área de captura y pulsar Ctrl+V. Pegar en un campo de texto no equivale a pegar en el área de imágenes.

Se aceptan PNG, JPEG y WebP de hasta 10 MB como archivo de entrada. El navegador los decodifica, conserva su proporción, reduce el lado mayor a un máximo de 2.400 píxeles si hace falta y los convierte a PNG. El PNG resultante debe ocupar hasta 5 MB. La normalización ocurre en el navegador antes de enviar el documento a la API.

### 8.6 Dictar

Pulsar **Dictar**, autorizar el micrófono si el navegador lo solicita y hablar en español. La configuración de reconocimiento utiliza `es-CO`. El resultado se incorpora como texto y puede corregirse manualmente. **Detener** finaliza la escucha.

Si el navegador no implementa SpeechRecognition, el botón queda deshabilitado. Si el servicio falla o el permiso se deniega, se muestra un mensaje y se puede continuar escribiendo. El navegador puede usar un servicio externo para procesar audio; la aplicación no implementa transcripción offline ni guarda grabaciones.

### 8.7 Guardar y recuperar

Pulsar **Guardar borrador**. Cuando la API confirma la escritura, el estado indica la revisión guardada. La ruta que aparece junto al estado corresponde a la base de datos, no a un Excel individual.

Para recuperar: abrir **Borradores locales**, elegir el documento y continuar. Los cambios sin guardar se advierten antes de cambiar de documento y al intentar cerrar o recargar la página, según el comportamiento del navegador.

En borradores creados antes de esta versión se completan firma y avisos solo si sus campos estaban vacíos, y se solicita guardar para conservar el ajuste. Los valores personalizados no se reemplazan. Una vez actualizada la versión de la plantilla de correo, quitar intencionalmente la firma o vaciar un aviso se respeta en aperturas posteriores.

### 8.8 Revisar y exportar

**Vista previa** permite comprobar contenido, secuencia, motores y capturas. No simula exactamente los saltos de página de Excel.

**Descargar Excel** genera el archivo con el estado actual de la pantalla, incluso si todavía no se guardó como borrador. Exportar no equivale a guardar. El nombre es `Pruebas Unitarias - <requerimiento>.xlsx`, y el navegador controla su ubicación de descarga.

### 8.9 Preparar el correo

Completar nombre del destinatario, Para, CC y asunto. El asunto se propone a partir del requerimiento y puede personalizarse. Elegir Tahoma 11 o 12 pt como base; el editor permite cambiar fuente, tamaño y formato por selección.

La firma incorporada aparece automáticamente en documentos nuevos. Su ancho inicial es 420 px; se ofrecen 280, 360, 420, 480 y 600 px. El alto se ajusta proporcionalmente. El contenedor de vista previa limita el ancho en pantallas pequeñas.

**Usar firma predeterminada** recupera la imagen suministrada. **Cargar firma** permite reemplazarla solo en el documento actual. Los avisos se incorporan automáticamente desde los textos fijos y no son editables. Cada borrador conserva su configuración al guardarse.

### 8.10 Buscar y filtrar borradores

Abre **Borradores locales** y escribe parte del requerimiento, cliente o descripción. La búsqueda ignora mayúsculas y acentos conforme a la comparación española (la ñ conserva su identidad). Puedes combinar texto con **Fecha del documento desde/hasta**, ambas inclusivas; estas fechas corresponden al documento, no a su última actualización. Pulsa Buscar o Enter. Limpiar filtros vuelve al listado general. Los resultados aparecen por última modificación y se cargan en bloques de 100 con Cargar más. Se consulta todo el almacenamiento, incluyendo documentos más antiguos que los primeros 100. La ubicación del archivo SQLite sigue visible.

El endpoint GET /api/documents acepta search, from, to y offset. El filtro se ejecuta antes de paginar, mediante parámetros y extracción de metadatos JSON; no se transfieren imágenes para presentar resultados. No se requiere migración destructiva ni regrabar los borradores previos. Para un volumen muy grande, el siguiente paso sería indexar metadatos en columnas específicas.

### 8.11 Eliminar documentos

En **Borradores locales**, cada documento tiene un botón de papelera. La confirmación muestra requerimiento y cliente. **Cancelar** conserva el documento; **Eliminar definitivamente** elimina el registro con sus evidencias, imágenes y configuración de correo. No hay papelera ni deshacer. Los Excel y HTML descargados no se eliminan.

Si se elimina el documento actualmente abierto, se reinicia el formulario como documento nuevo. La confirmación advierte si también se descartarán cambios locales sin guardar. La lista mantiene sus filtros y actualiza los resultados tras el borrado. Si otra ventana modificó el documento después de cargar el listado, la API rechaza la eliminación: cancela la confirmación y vuelve a buscar para revisar la versión actual.

API: `DELETE /api/documents/{id}?revision={revision}`. Eliminación parametrizada y condicionada a la revisión: 204 si se elimina, 409 si cambió la versión, 404 si ya no existe, 400 para revisión inválida. Aplica los controles de origen y acceso local existentes. Pruebas con almacenamiento aislado: cancelación, confirmación, documento abierto con cambios, ausencia tras recargar, rechazo de revisión incorrecta y origen ajeno. No se eliminaron borradores reales durante la implementación.

### 8.12 Evidencias plegables

Cada evidencia SQL/Oracle puede contraerse pulsando su encabezado, con soporte de teclado. Al contraerla siguen visibles el número, una síntesis de la descripción, la cantidad de capturas y las acciones de ordenar/eliminar. Se abre de nuevo sin perder contenido; el estado plegado acompaña al identificador de la evidencia al reordenar. Las evidencias se muestran abiertas inicialmente. El fondo general pasó de #F7F8FA a #EEF0F4 para aumentar suavemente la separación de los paneles blancos; el correo y el Excel conservan sus fondos.

### 8.13 Eliminar una evidencia

Pulsa la papelera de la evidencia SQL u Oracle. La confirmación muestra el motor, número, descripción y cantidad de capturas. Cancelar no modifica el documento. Eliminar evidencia quita su descripción y todas sus capturas del documento en edición. **Guardar borrador** hace persistente esa eliminación. Si aún no guardaste, puedes volver a abrir la versión guardada descartando los cambios actuales; esto descarta también las otras modificaciones pendientes. No hay deshacer individual. La captura de inicio de sesión y las otras evidencias no se eliminan.

La eliminación utiliza el identificador de la evidencia, no su posición, para evitar errores cuando se reordena. El documento queda marcado como modificado. Las pruebas usan datos separados; no borran evidencias reales.

### 8.14 Cabecera fija y tamaño de la interfaz

La barra superior permanece visible al desplazarse y cambiar de sección. Reúne el requerimiento, estado del guardado, Guardar borrador y Descargar Excel. Una segunda línea compacta muestra cantidades de evidencias/capturas, formato y Ubicación de borradores (desplegable). Sustituye el título y las tarjetas grandes repetidas de la versión anterior. En móvil se organiza en varias líneas para que los botones sigan disponibles. El tamaño base de la interfaz es 12 px; se conserva Tahoma, la jerarquía de títulos y el formato propio del correo (11/12 pt de base y avisos de 8 pt).

### 8.15 Acerca del generador

La opción del menú explica el objetivo del producto, el flujo desde la documentación de pruebas hasta Excel/correo y la función de Vue, TypeScript, PrimeVue, Quill, Vite, .NET, SQLite, OOXML y DOMPurify. No requiere conocimientos técnicos. Aclara que el generador documenta las pruebas, no ejecuta consultas SQL/Oracle, y aún no envía correos directamente.

## 9. Motor de Excel y contrato de plantilla

### 9.1 Origen

Archivo de referencia: `Pruebas Unitarias -  MD 11337.xlsx`.

Recurso de aplicación: `src/EvidenceGenerator.Api/Templates/PruebasUnitarias.v1.xlsx`.

SHA-256 de la plantilla original: `5ABAB97578EE0B955B28155A6D853C45C805EE7996336298D15C73D535B8FBCF`.

El proceso no sobrescribe la plantilla. Abre una copia en memoria, modifica partes concretas y devuelve un nuevo archivo.

### 9.2 Estructura del libro

| Hoja | Papel |
|---|---|
| Instructivo | Documento institucional fijo |
| Evidencia Pruebas Unitarias SQL | Evidencias y entorno SQL |
| Evidencia Pruebas Unit Oracle | Evidencias y entorno Oracle |

El nombre de la hoja Oracle se conserva exactamente como está en el original. La abreviación no es un cambio introducido por la aplicación.

### 9.3 Mapa de celdas

| Ubicación | Contenido |
|---|---|
| A5 | Elaborado por |
| E5 | Fecha de elaboración |
| D9 | URL del entorno |
| D10 | Conexión |
| A60:N61 | Plan de Pruebas, sin identificador ni cliente añadidos |
| A63:N65 | Primera descripción |
| A110:N112 | Segunda descripción |
| A157:N159 | Tercera descripción |

La primera descripción incluye el requerimiento y la descripción general para conservar ese contexto en el documento. Los colores, fuentes, combinaciones y bordes de celdas se toman del bloque original. Las descripciones largas amplían las filas combinadas correspondientes.

### 9.4 Bloques de capturas

Cada bloque conserva el paso de 47 filas. La captura comienza cuatro filas después de la primera fila de descripción y ocupa 42 filas completas:

| Captura | Primera fila | Última fila incluida | Límite inferior del anclaje |
|---|---:|---:|---:|
| Inicio de sesión | 15 | 56 | Inicio de fila 57 |
| Evidencia 1 | 67 | 108 | Inicio de fila 109 |
| Evidencia 2 | 114 | 155 | Inicio de fila 156 |
| Evidencia 3 | 161 | 202 | Inicio de fila 203 |

Fórmula: `última fila incluida = primera fila + 41`. OOXML utiliza índices de fila desde cero, por lo que el marcador final es `filaInicialCero + 42`, con desplazamiento vertical cero. Ese marcador representa el límite inferior, no una fila adicional ocupada.

Se utiliza `twoCellAnchor`, de manera que la captura queda asociada a su inicio y fin de rango. El borde se expresa como `12700` EMU, equivalente a 1 punto, con color `252932`.

**Altura de la imagen:** las 42 filas conservan sus alturas originales de la plantilla. La imagen se expande hasta completar la suma de esas alturas, desde el inicio de la primera fila hasta el final de la fila 42. No se reducen ni se modifican las filas para acomodar la captura. El ancho crece con el mismo factor que el alto: ancho final = alto final × ancho original / alto original. Se conserva la proporción de cada captura sin deformación ni recorte, aunque supere la columna N. El área de impresión se amplía automáticamente hasta incluir la imagen más ancha. El borde oscuro de 1 punto se mantiene.

Cada evidencia admite una imagen. Al abrir un borrador antiguo con varias imágenes por evidencia, la interfaz las separa en evidencias individuales, conservando la descripción y las capturas. La conversión solo se persiste cuando el usuario guarda. Si supera 50 evidencias en un motor, se informa para distribuirlas en varios documentos; no se trunca el contenido. El logotipo institucional no se trata como captura y no recibe el nuevo borde ni el anclaje de 42 filas.

### 9.5 Preservación y limpieza

La implementación conserva las partes institucionales y reemplaza las evidencias antiguas. Retira relaciones de capturas de ejemplo, elimina archivos de imagen sin referencias y vacía textos compartidos que ya no se utilizan. Los enlaces de sitio se actualizan y el área de impresión alcanza el último bloque.

El texto del usuario se escribe como `inlineStr`: una descripción que empieza por `=` se mantiene como texto, no se convierte en fórmula. La numeración institucional y las configuraciones de impresión originales se mantienen; no se calcula una numeración dinámica por página física impresa.

### 9.6 Sustituir la plantilla

No basta con cambiar el nombre de un Excel cualquiera. El generador conoce hojas, partes OOXML, posiciones y bloques de esta versión. Para una plantilla nueva:

1. Conservar el original y crear una nueva versión del recurso.
2. Comparar hojas, celdas, imágenes, relaciones, estilos y configuración de impresión.
3. Ajustar el adaptador o implementar otro `IExcelGenerator`.
4. Agregar pruebas de conservación y de nuevos anclajes.
5. Generar casos de 0, 1 y múltiples evidencias, incluyendo descripciones extensas e imágenes de varias proporciones.
6. Validar apertura e impresión en Excel antes de reemplazar la versión activa.

## 10. Correo, firma y avisos

### 10.1 Estructura del cuerpo

```text
Buen día, <nombre>.

Se encuentra disponible para validación el requerimiento <identificador>.

Descripción:
<descripción general>

Entorno SQL:
  URL: <sitio SQL>
  Conexión: <conexión SQL>
  Cliente: <cliente>

Solicitud de validación

Cordialmente,
<firma>
<aviso ambiental>
<aviso de confidencialidad>
```

### 10.2 Estilos

| Elemento | Estilo |
|---|---|
| Cuerpo | Tahoma, Arial como respaldo; 11 o 12 pt; negro |
| Requerimiento | Negrita, `#174E86` |
| Encabezados y etiquetas | Negrita |
| URL | Azul `#0000FF`, subrayada |
| Conexión y cliente | Cursiva |
| Cordialmente | Gris oscuro `#333333` |
| Ambiental | Tahoma 8 pt, `#0F7001` |
| Confidencialidad | Tahoma 8 pt, negro; nombres de la empresa en negrita |

Los estilos necesarios se escriben dentro del HTML para que el cuerpo no dependa de la hoja de estilos de la aplicación al copiarlo a Outlook. La fuente efectiva depende de que el sistema y el cliente de correo dispongan de Tahoma; Arial es el respaldo declarado.

### 10.3 Seguridad del contenido

El generador escapa `&`, `<`, `>`, comillas simples y dobles antes de formar el HTML. Solo se generan enlaces HTTP/HTTPS y se permiten firmas PNG codificadas como data URI. El texto ingresado no se interpreta como HTML ni JavaScript. La negrita de la empresa se aplica después del escape a una cadena literal conocida.

Los campos de descripción son texto plano. No se ofrece una barra de formato enriquecido para seleccionar palabras arbitrarias en negrita o cursiva.

### 10.4 Copiar y descargar

**Copiar correo** escribe en el portapapeles el cuerpo HTML y una alternativa de texto. **Copiar HTML** copia el código fuente. **Descargar HTML** guarda un documento HTML con la firma incrustada y codificación UTF-8.

Para, CC y asunto no se transfieren automáticamente a Outlook. Tampoco se adjunta el Excel. El usuario crea el mensaje, traslada esos datos, pega el cuerpo y adjunta el archivo descargado.

La compatibilidad de imágenes data URI al pegar depende del cliente y versión de Outlook. Se debe comprobar que la firma se vea en el mensaje antes de enviarlo. Graph con imágenes CID y adjuntos es una evolución prevista, no una función implementada.

### 10.5 Defaults y versiones

`mailDefaults.ts` contiene el aviso ambiental, la confidencialidad y la referencia a la firma. La imagen está importada de forma inline, por lo que incrementa el tamaño del JavaScript inicial; se eligió para disponer de una firma autocontenida en el borrador y en el HTML. La imagen original ocupa aproximadamente 572 KiB, antes de codificarla en base64.

`TemplateVersion` distingue borradores anteriores de aquellos donde se aplicó la nueva configuración. Un borrador anterior se adapta solo en memoria al abrirlo y se marca como modificado. Guardar persiste la adaptación. No hay una modificación masiva silenciosa de la base de datos.

### 10.6 Editor enriquecido

Los campos Para, CC, nombre del destinatario, tamaño base y asunto se agrupan en **Destinatarios y asunto**, contraído inicialmente. Pulsa su encabezado para editarlos; contraerlo no borra los valores. La cursiva se aplica a una selección o al texto que se escriba después de activarla. Se permite síntesis tipográfica en el editor/vista previa para que Tahoma pueda representar la cursiva aun sin una variante cursiva instalada.

En **Preparar correo**, usa el editor del cuerpo: fuentes Tahoma, Arial, Calibri, Verdana, Georgia y Times New Roman; tamaños de 8 a 32 pt; negrita, cursiva, subrayado, tachado, color, resaltado, listas, sangrías, enlaces y alineación izquierda/centro/derecha/justificada. Selecciona texto y aplica formato. El tamaño general 11/12 sigue actuando como base; un tamaño aplicado a una selección lo sobrescribe.

Al editar, el cuerpo personalizado se guarda con el borrador en Mail.BodyHtml. Desde ese momento, los cambios posteriores en los datos generales no sobrescriben automáticamente el texto. **Regenerar desde los datos** pide confirmación y restaura el cuerpo automático; elimina la personalización del cuerpo. La firma sigue siendo independiente. Copiar correo y Descargar HTML incluyen el formato, la firma y los avisos fijos. El pegado final depende de las capacidades del cliente de correo.

Los avisos ambiental y de confidencialidad ya no tienen campos editables ni botón Restaurar avisos. Siempre se generan desde constantes, después de la firma, con Tahoma 8 pt y los colores originales. Los valores históricos permanecen en los JSON antiguos por compatibilidad, pero no sustituyen los avisos fijos en el correo generado.

El editor utiliza PrimeVue Editor + Quill 2.0.2. DOMPurify y una lista de estilos permitidos limpian el HTML antes de mostrarlo o exportarlo; se eliminan contenido activo, imágenes externas del cuerpo y enlaces no HTTP/HTTPS/mailto. Los estilos de alineación/listas se adaptan a HTML independiente de la aplicación. El backend limita el cuerpo a 100.000 caracteres HTML; conserva el límite global de 35 MB por documento. El backend almacena HTML como datos y no lo ejecuta ni lo envía; cualquier futuro servicio de envío deberá aplicar su propia validación de servidor.

La guía **Microsoft-365-Conexion.md** describe requisitos, MFA, permisos y la integración futura. Esta versión todavía no conecta ni envía correos con Microsoft 365.

### 10.7 Firma plegable y copia con formato

La sección Firma aparece contraída. Pulsa su encabezado para abrir o cerrar la configuración; esto no quita la firma del correo. La firma y los avisos siguen presentes en vista previa y al copiar.

Copiar correo coloca HTML enriquecido y una alternativa de texto plano en el portapapeles. Las fuentes, tamaños, colores, negrita, cursiva y alineación heredados se escriben explícitamente en cada elemento; la vista previa y la descarga utilizan el mismo formato preparado. En Outlook, el mensaje debe estar en HTML y se debe usar Ctrl+V con Mantener formato de origen. Pegar como texto o Combinar formato puede descartar o reemplazar estilos. Las imágenes de firma en data URI continúan dependiendo del soporte del cliente; la integración Graph con adjunto inline será la solución para controlar su envío.

Comprobado mediante lectura real del portapapeles en Edge: Georgia, 18 pt, negrita, color rojo y justificación. Ocho pruebas frontend correctas. No se ha validado el pegado en una sesión real de Outlook del usuario.

## 11. Persistencia y concurrencia

### 11.1 Ubicación

Ruta predeterminada en desarrollo:

```text
C:\projects-jn\evidence generator\src\EvidenceGenerator.Api\App_Data\evidence.db
```

En la publicación predeterminada:

```text
C:\projects-jn\evidence generator\artifacts\local\App_Data\evidence.db
```

Son ubicaciones diferentes. Publicar no copia los borradores de desarrollo. Para reutilizarlos se debe hacer un respaldo y traslado explícito o configurar `DataDirectory` con una ubicación común elegida por el usuario. La ruta visible en la aplicación evita confundir ambas bases.

### 11.2 Tabla

```sql
CREATE TABLE Document (
    Id TEXT PRIMARY KEY,
    Requirement TEXT NOT NULL,
    Revision INTEGER NOT NULL,
    UpdatedAt TEXT NOT NULL,
    Payload TEXT NOT NULL
);

CREATE INDEX IX_Document_UpdatedAt ON Document(UpdatedAt);
```

`Payload` contiene el agregado JSON: información general, secciones, imágenes y correo. `UpdatedAt` se genera en UTC. La pantalla convierte la fecha de actualización al formato local del navegador. El identificador es un GUID y las revisiones son enteros incrementales.

SQLite utiliza WAL. Durante el uso pueden aparecer `evidence.db-wal` y `evidence.db-shm`; no deben eliminarse mientras la aplicación está funcionando. La versión de esquema SQLite actual se identifica con `PRAGMA user_version=1`; la versión de plantilla de correo es un campo independiente dentro del JSON.

### 11.3 Control de conflictos

Un nuevo documento tiene revisión cero. Su primer guardado crea la revisión uno. Una actualización solo tiene éxito si coincide la revisión enviada con la almacenada. Si otra ventana guardó antes, se devuelve HTTP 409.

Ante un conflicto, conservar manualmente el contenido pendiente que se necesite, volver a abrir el borrador y aplicar los cambios sobre la versión actual. No hay mezcla automática de documentos.

### 11.4 Límites

| Recurso | Límite |
|---|---:|
| Evidencias por motor | 50 |
| Imágenes por evidencia | 1 |
| Imágenes de evidencias por documento, incluidos inicios de sesión | 100 |
| Archivo original recibido por el navegador | 10 MB |
| PNG normalizado | 5 MB |
| Lado mayor normalizado en el navegador | 2.400 px |
| Lado máximo aceptado por la validación de API | 4.096 px |
| Descripción general o por evidencia | 4.000 caracteres |
| Solicitud HTTP | 35 MiB |
| Ancho de firma aceptado por API | 240 a 600 px |
| Borradores listados | Páginas de 100, con filtros sobre todo el almacenamiento |

Base64 aumenta el tamaño respecto del PNG original. El límite de solicitud puede alcanzarse antes de llegar al máximo de imágenes. La firma también ocupa espacio en el documento, aunque no se cuenta como evidencia.

## 12. Contrato de API

| Método y ruta | Resultado |
|---|---|
| GET `/api/health` | Estado y plantilla activa |
| GET `/api/storage` | Proveedor y ruta absoluta de SQLite |
| GET `/api/documents` | Búsqueda de borradores por texto/fechas, paginada en bloques de 100 |
| GET `/api/documents/{id}` | Documento completo o 404 |
| PUT `/api/documents/{id}` | Crea o actualiza con revisión optimista |
| POST `/api/exports/excel` | Devuelve el XLSX del documento recibido |

El cuerpo JSON utiliza camelCase. El esquema principal tiene `id`, `revision`, `requirement`, `description`, `author`, `client`, `date`, `sql`, `oracle` y `mail`.

Cada sección contiene `url`, `connection`, `loginImage` e `items`. Cada evidencia contiene `id`, `description` e `images`. Una imagen se representa como `{ "dataUrl": "data:image/png;base64,..." }`.

El correo contiene destinatario de saludo, Para, CC, asunto, tamaño, avisos, firma, `signatureWidth` y `templateVersion`. Estos dos últimos campos tienen valores por defecto en backend para conservar compatibilidad con borradores y clientes anteriores.

Estados esperados: 200 para operaciones correctas; 400 para datos inválidos; 403 para host, origen o conexión no permitidos; 404 cuando no existe el recurso; 409 por revisión obsoleta; 413 si se supera el límite HTTP. Un fallo no controlado se trata mediante Problem Details sin registrar el contenido completo de las evidencias.

Eliminación: DELETE /api/documents/{id}?revision={revision}. Respuestas: 204 eliminado, 409 revisión modificada, 404 inexistente, 400 revisión inválida. La eliminación de evidencias se guarda como parte del documento mediante PUT.

## 13. Compilación y pruebas

### 13.1 Backend

Detener la API antes de compilar en Windows: el ejecutable en uso puede bloquear su reemplazo.

```powershell
Set-Location 'C:\projects-jn\evidence generator'
dotnet build EvidenceGenerator.slnx -c Release
dotnet test EvidenceGenerator.slnx
```

### 13.2 Frontend

```powershell
Set-Location 'C:\projects-jn\evidence generator\src\evidence-generator-web'
npm run build
npm test
```

La compilación comprueba tipos con `vue-tsc` y después genera `dist` con Vite. No se debe editar `dist`: es un resultado regenerable. Por la firma autocontenida, Vite puede advertir que el paquete principal supera su umbral orientativo de tamaño; ese aviso no equivale a un error de compilación.

### 13.3 Prueba integral

```powershell
Set-Location 'C:\projects-jn\evidence generator\src\evidence-generator-web'
npm run test:e2e
```

Requiere Microsoft Edge instalado y el puerto 5080 libre. La configuración inicia una API con almacenamiento separado en `work/e2e-data`, y levanta Vite o reutiliza el servidor de desarrollo existente. No se debe reutilizar una API con borradores reales para estas pruebas.

El recorrido crea un requerimiento de prueba, agrega capturas, simula pegado, limpia texto, reordena, guarda, exporta, revisa el correo, ajusta firma, recupera el borrador y comprueba una pantalla de 390 px. El resultado de prueba, los PNG y el XLSX se escriben en `work/`.

### 13.4 Qué verifican las pruebas

- Conservación del instructivo, estilos y dibujo institucional del instructivo.
- Estructura OOXML comparada con la plantilla original.
- Cantidad variable de evidencias, una imagen por evidencia y conversión de borradores antiguos sin pérdida de capturas.
- Anclajes de 42 filas completas, desplazamiento final cero y borde de 1 pt.
- Título exacto Plan de Pruebas.
- Texto que comienza por `=` sin ejecución como fórmula.
- Persistencia de imágenes y rechazo de revisiones obsoletas.
- Rutas y URLs permitidas.
- Correo sin bloque Oracle, colores y avisos de 8 pt.
- Compatibilidad de defaults con borradores anteriores y configuraciones personalizadas.
- Ubicación real de almacenamiento mostrada por la API.

## 14. Publicación e instalación compilada

### 14.1 Publicar para este equipo

Detener API y Vite antes de publicar. El script comprueba los puertos para anticipar bloqueos de archivos en Windows.

```powershell
Set-Location 'C:\projects-jn\evidence generator'
.\Publish-Local.ps1
```

El script instala dependencias con `npm ci`, compila el frontend, publica la API para Windows x64 en Release, copia `dist` a `wwwroot` y crea `Start-Local.ps1`.

Resultado predeterminado: `artifacts/local`.

Esta variante depende de que el equipo tenga instalado un runtime ASP.NET Core 10 compatible. El SDK no es necesario para ejecutar el resultado, aunque sí para construirlo. La publicación a carpeta se realiza con `dotnet publish`, según la herramienta oficial de .NET.

### 14.2 Iniciar la publicación

```powershell
Set-Location 'C:\projects-jn\evidence generator\artifacts\local'
.\Start-Local.ps1
```

Abrir **http://127.0.0.1:5080/**. En este modo no se ejecuta Vite y no se utiliza el puerto 5173. Mantener la terminal abierta y detener con Ctrl+C.

Si PowerShell impide ejecutar el script:

```powershell
Set-Location 'C:\projects-jn\evidence generator\artifacts\local'
.\EvidenceGenerator.Api.exe --urls http://127.0.0.1:5080
```

El directorio actual debe ser el de publicación para encontrar configuración, plantilla y archivos web.

### 14.3 Paquete autocontenido

Para generar un resultado que incluya el runtime:

```powershell
Set-Location 'C:\projects-jn\evidence generator'
.\Publish-Local.ps1 -SelfContained -OutputDirectory 'C:\projects-jn\evidence generator\artifacts\standalone'
```

Incluye más archivos y ocupa más espacio. El script dirige la publicación a `win-x64`. Para otro sistema o arquitectura se debe revisar el destino, proveedor nativo SQLite y las pruebas. El modo verificado en esta entrega es la publicación dependiente del runtime; la opción autocontenida está preparada mediante `dotnet publish`, pero debe validarse también en el equipo receptor.

### 14.4 Instalar en otro equipo

1. Generar la publicación apropiada.
2. Copiar **toda** la carpeta publicada a un directorio con permisos de escritura, por ejemplo `C:\Apps\EvidenceGenerator`.
3. Mantener `Templates`, `wwwroot`, DLL, bibliotecas nativas y archivos de configuración.
4. Instalar el runtime ASP.NET Core 10 si el paquete no es autocontenido.
5. Ejecutar `Start-Local.ps1` y abrir `http://127.0.0.1:5080`.
6. Verificar `/api/health` y la ruta de borradores.
7. Crear un documento pequeño, guardar y descargar un Excel antes de usarlo con información real.

No se configura un servicio de Windows ni inicio automático. Tampoco es un instalador MSI. El paquete es una publicación local portable por carpeta. No copiar únicamente el EXE.

## 15. Configuración

### 15.1 Cambiar el directorio de datos

En desarrollo, utilizar una ruta absoluta:

```powershell
Set-Location 'C:\projects-jn\evidence generator'
dotnet run --project src/EvidenceGenerator.Api --no-launch-profile -- --urls http://127.0.0.1:5080 --DataDirectory 'C:\projects-jn\evidence generator\data'
```

En publicación, añadir `--DataDirectory` al ejecutable de forma equivalente. La ubicación debe existir o poder crearse. Cambiarla selecciona otra base; no traslada datos de la ubicación anterior. Verificar la ruta mostrada después de reiniciar.

### 15.2 Puertos

Los puertos 5173 y 5080 están coordinados entre Vite, scripts, lista de orígenes y pruebas. Para cambiarlos se deben actualizar todos esos puntos; cambiar solo el comando de inicio puede romper el proxy o causar HTTP 403.

### 15.3 Firma y avisos

Para cambiar los defaults de documentos nuevos, actualizar `domain/mailDefaults.ts` o el recurso PNG. Para cambiar un documento existente, utilizar los controles del módulo de correo y guardar. Cambiar los defaults del código no debe sobrescribir automáticamente contenido personalizado de documentos guardados.

### 15.4 Límites

`DocumentValidation` centraliza los límites de backend. `images.ts` controla la normalización de entrada. Si se aumenta el límite de solicitud se debe analizar memoria, almacenamiento y tiempos de generación, y mantener coherencia con el cliente. No basta con subir el límite de Kestrel.

### 15.5 Temas de color

En la barra lateral, la sección **Temas de color** permite elegir Morado (predeterminado), Verde, Azul, Rojo o Naranja. El cambio se aplica inmediatamente a botones, indicadores, enlaces, focos y fondos de acento. Se mantienen los colores neutros, estados de error/éxito y los colores institucionales del Excel y del correo. En móvil el selector aparece debajo de las secciones del documento.

La preferencia se guarda en la cookie `evidence-generator-theme` durante un año desde la última selección, con `Path=/` y `SameSite=Lax`; se agrega `Secure` cuando se utiliza HTTPS. Solo contiene el identificador del tema. No incluye datos del documento ni credenciales. Se restaura antes de montar la aplicación; valores desconocidos vuelven al morado. Si se borran o bloquean las cookies, la preferencia no se conserva. La cookie pertenece al navegador y al host: `localhost` y `127.0.0.1` mantienen preferencias independientes.

Implementación: `services/theme.ts` centraliza paletas, aplicación y cookie; `ThemePicker.vue` presenta las opciones con nombres accesibles y estado seleccionado. Los tokens de PrimeVue y los estilos propios consumen las mismas variables CSS. Los botones principales utilizan tonos oscuros para mantener legibilidad del texto blanco. La preferencia visual es independiente del borrador y no genera cambios pendientes en el documento.

Validación: compilación TypeScript/Vite y cinco pruebas existentes correctas. Se comprobó en Edge que los cinco temas actualizan botones y marca, persisten después de recargar, mantienen una cookie de un año y funcionan en pantalla de 390 px sin desbordamiento horizontal. Una cookie inválida recupera el tema predeterminado.

## 16. Respaldo, recuperación y actualización

### 16.1 Respaldo

Guardar cambios, detener la aplicación y copiar toda la carpeta real de datos indicada en la interfaz. Ejemplo para desarrollo:

```powershell
$backupPath = 'C:\projects-jn\evidence generator\backups\2026-10-07'
New-Item -ItemType Directory -Force $backupPath
Copy-Item -LiteralPath 'C:\projects-jn\evidence generator\src\EvidenceGenerator.Api\App_Data' -Destination $backupPath -Recurse
```

Guardar además el código o publicación de la versión utilizada y su plantilla. No hacer copias parciales de una base activa ignorando WAL. Para copias en caliente futuras conviene implementar la API de backup de SQLite.

### 16.2 Recuperar sin sobrescribir el original

Copiar el respaldo a una carpeta diferente, iniciar la aplicación con `--DataDirectory` apuntando a esa copia y verificar los documentos. Solo después de comprobarla se decide qué carpeta será la activa. Esta estrategia conserva el archivo original ante una recuperación fallida.

### 16.3 Actualizar

1. Guardar y respaldar datos.
2. Detener procesos de API y frontend.
3. Actualizar código o generar una nueva carpeta de publicación.
4. Ejecutar compilación y pruebas.
5. Iniciar y revisar un documento existente y uno nuevo.
6. Comprobar generación de Excel y firma de correo.
7. Conservar temporalmente la versión anterior para retorno.

La versión 0.2.0 no cambia la tabla Document ni exige una migración SQL. Los campos nuevos se almacenan dentro del JSON y tienen defaults compatibles. Para versiones futuras con cambios de esquema debe diseñarse una migración explícita; `CREATE TABLE IF NOT EXISTS` no reemplaza un sistema de migraciones completo.

## 17. Seguridad y privacidad

La API comprueba host, origen y dirección remota para permitir únicamente uso local. No dispone de autenticación; no debe exponerse a una red de trabajo o internet cambiando simplemente el binding.

Las consultas SQLite están parametrizadas. Las revisiones evitan sobrescrituras silenciosas entre ventanas. Las rutas de datos no se reciben desde el cuerpo de cada documento; las define la configuración del proceso.

Las imágenes se validan por tipo de entrada, tamaño y dimensiones. La validación de PNG en backend comprueba cabecera y dimensiones, no sustituye una decodificación estricta ni un escaneo de archivos no confiables. Ese nivel adicional sería necesario antes de abrir la aplicación a múltiples usuarios externos.

Los borradores y capturas no se cifran por la aplicación: dependen de permisos y protección del equipo. La ruta de almacenamiento se expone solo dentro de la API local para ayudar a localizar los datos. No se registran solicitudes completas con imágenes en los logs de aplicación.

El aviso corporativo de confidencialidad afirma que el correo fue revisado por antivirus porque ese es el texto suministrado. **Evidence Generator no realiza por sí mismo un análisis antivirus de correos ni archivos.** La política efectiva de revisión depende del entorno corporativo.

No se solicitan, transmiten ni almacenan credenciales Microsoft 365. Una futura integración con Graph debe utilizar Microsoft Entra ID y OAuth/OIDC con permisos mínimos, sin guardar contraseñas de usuario.

La interfaz responsive puede verse en anchos pequeños, pero `127.0.0.1` apunta al propio dispositivo. Un teléfono no puede acceder a la instalación del PC mediante esa dirección. Habilitar acceso desde otros dispositivos exige revisar primero autenticación, HTTPS, orígenes y red.

## 18. Diagnóstico de problemas

| Síntoma | Causa probable | Acción |
|---|---|---|
| La interfaz no abre | Vite o aplicación compilada detenidos | Confirmar modo y puerto correcto |
| No conecta con la API | API detenida o proxy desalineado | Consultar `/api/health` y logs |
| Puerto ocupado | Otra sesión sigue ejecutándose | Identificar el proceso y cerrar solo la sesión correspondiente |
| Error al copiar EXE durante build | API activa en Windows | Detener API y volver a compilar |
| EPERM en npm ci | Vite bloquea la biblioteca de Rolldown | Detener Vite y repetir npm ci |
| PowerShell bloquea un script | Política del equipo | Usar comandos manuales o seguir la política corporativa |
| HTTP 400 al exportar | Campos obligatorios vacíos o datos inválidos | Completar datos y revisar el mensaje |
| HTTP 409 | Otra ventana guardó una revisión nueva | Recuperar versión vigente y reaplicar cambios |
| HTTP 413 | Documento demasiado grande | Reducir capturas, recortar o dividir documentación |
| No aparece una captura pegada | Foco fuera del área de imágenes | Hacer clic en el área de carga antes de Ctrl+V |
| Dictado deshabilitado | API no soportada por el navegador | Probar navegador compatible o escribir |
| Error de micrófono | Permiso denegado, dispositivo o servicio no disponible | Revisar permiso y configuración del navegador |
| Firma no aparece en Outlook | Restricción del cliente al pegar data URI | Insertar firma desde Outlook y verificar antes de enviar |
| No se ven borradores tras publicar | Se usa una carpeta de datos diferente | Comparar la ruta visible y elegir DataDirectory correcto |
| Cambios de código no se ven en publicación | wwwroot contiene un build anterior | Volver a ejecutar Publish-Local.ps1 con procesos detenidos |
| El Excel no coincide tras cambiar plantilla | Contrato del adaptador incompatible | Restaurar plantilla validada o versionar el adaptador |

Para identificar procesos, sin detenerlos automáticamente:

```powershell
Get-NetTCPConnection -LocalPort 5080,5173 -State Listen -ErrorAction SilentlyContinue |
    Select-Object LocalAddress,LocalPort,OwningProcess
```

No finalizar indiscriminadamente todos los procesos `dotnet` o `node`, ya que pueden pertenecer a otros proyectos.

## 19. Mantenimiento y evolución

### 19.1 Cambios de código

Mantener funciones pequeñas cuando ayude a la comprensión, nombres descriptivos y comentarios concentrados en decisiones no evidentes: índices de filas, unidades EMU, compatibilidad de borradores y control de revisiones. No comentar cada línea de código.

Antes de modificar el generador, revisar su contrato de filas. Antes de cambiar el DTO, revisar el tipo TypeScript, validación C#, serialización y borradores anteriores. Las pruebas deben cubrir el comportamiento que cambia, no repetir literalmente la implementación.

### 19.2 Dependencias

Usar cambios deliberados de versiones y conservar `package-lock.json`. Revisar licencias al actualizar PrimeVue. Auditar con:

```powershell
Set-Location 'C:\projects-jn\evidence generator'
dotnet list package --vulnerable --include-transitive
Set-Location 'C:\projects-jn\evidence generator\src\evidence-generator-web'
npm audit
```

No aplicar `npm audit fix --force` sin evaluar cambios mayores y repetir pruebas.

### 19.3 Roadmap sugerido

| Fase | Objetivo | Resultado esperado |
|---|---|---|
| Validación operativa | Excel real, impresión, Outlook y voz | Ajustes confirmados con el flujo cotidiano |
| Productividad | Defaults globales, duplicar documento y deshacer | Menor tiempo por documento |
| Almacenamiento | Archivos separados del JSON, backup y retención | Menor consumo de memoria y crecimiento controlado |
| Plantillas | Versiones explícitas y contratos por formato | Evolución sin romper documentos anteriores |
| Distribución | Instalador, actualización y autenticación si aplica | Operación administrable |
| Integración | Microsoft Graph con Entra ID, CID y adjuntos | Creación segura de borradores de correo |

Métricas útiles para decidir prioridades: tiempo promedio de documentación, cantidad de correcciones manuales posteriores al Excel, fallos de exportación, tamaño promedio de borrador y tiempo de preparación del correo. Actualmente no se recopila telemetría de estas métricas.

## 20. Verificación y límites conocidos

La revisión de esta versión incluye compilación frontend/backend, pruebas unitarias, pruebas de API y cuatro recorridos automatizados de navegador. La última validación del frontend incluye ocho pruebas unitarias y los cuatro recorridos correctos, incluida la eliminación de evidencias SQL/Oracle y documentos. Se comprobó el inicio desde Iniciar.cmd y la reutilización de una instancia activa sin duplicar procesos. Se verifican los anclajes de 42 filas, bordes, títulos, colores y migración de defaults. Las pruebas usan un almacenamiento separado del uso real.

La apertura e impresión final dentro de Microsoft Excel, el pegado en la versión concreta de Outlook y el micrófono real requieren validación operativa. El renderizador auxiliar de hojas utilizado en la primera entrega no representa imágenes incrustadas; por ello no se utiliza como única evidencia de fidelidad de imágenes. Se comprueban sus partes OOXML, anclajes y contenido independientemente.

La aplicación completa se ejecuta localmente; no se ha desplegado como servicio multiusuario. La generación se realiza en memoria y no está diseñada todavía para grandes volúmenes concurrentes. Los parámetros actuales favorecen el uso individual con documentos acotados.

## 21. Referencias

La descripción de la implementación proviene del código y pruebas de este proyecto. Para profundizar en los mecanismos de publicación y ejecución:

- [dotnet publish — Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-publish).
- [Archivos estáticos en ASP.NET Core — Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/static-files?view=aspnetcore-10.0).
- [Publicación de sitios estáticos con Vite](https://vite.dev/guide/static-deploy).
- [Licencias de PrimeUI y versiones MIT](https://primeui.dev/licenses/community).

El comportamiento documentado debe actualizarse junto con cambios de contrato, rutas, límites, plantilla o distribución. Este archivo es documentación versionable del proyecto.


