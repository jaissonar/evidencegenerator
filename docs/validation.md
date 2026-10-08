# Verificación de Evidence Generator 0.2.0

Fecha: 7 de octubre de 2026.

## Resultado

- Backend: 13 pruebas correctas.
- Frontend: 5 pruebas correctas.
- Microsoft Edge: 1 recorrido integral correcto.
- Compilación de frontend y publicación Release de la API: correctas.
- Publicación local dependiente del runtime: iniciada y verificada en el navegador, con frontend estático, API y ruta de almacenamiento correctos.

## Cobertura del cambio

Se verificó generación con 0, 1, 7 y 17 evidencias, conservación del instructivo y estilos, ausencia de nuevos errores OOXML respecto de la plantilla, anclajes de 42 filas completas, desplazamiento final cero, borde de 12700 EMU (1 pt) y color 252932. Las imágenes panorámicas y verticales se probaron en Oracle con altura equivalente a las 42 filas originales y ancho proporcional sin límite en N. Se comprueba la relación de aspecto, el ancho físico del anclaje y que el área de impresión contiene las capturas completas. Se verifica que los atributos de altura de cada fila permanecen iguales a los de la plantilla, también en bloques SQL repetidos. No se reducen las filas para acomodar las imágenes. El encabezado exacto Plan de Pruebas se comprobó en ambas hojas.

La exportación descargada por el navegador se inspeccionó además con un lector independiente: las capturas SQL y Oracle abarcan 42 filas y ambas hojas conservan el título esperado.

Las pruebas de correo comprobaron un único bloque Entorno SQL, exclusión del bloque Oracle, colores de referencia, ancho de firma, avisos a 8 pt, orden después de la firma, negritas y escape de entradas. Se comprobó la incorporación de defaults en borradores anteriores sin reemplazar personalizaciones ni reponer firmas eliminadas intencionalmente en documentos actualizados.

El recorrido de navegador comprobó también carga y pegado, botón Limpiar, reordenamiento, guardado, recuperación, exportación, configuración de firma y comportamiento responsive. Se revisaron capturas de escritorio, móvil y correo. El modo compilado se verificó por separado en 127.0.0.1:5080.

## Límites de la verificación

No se verificó la impresión dentro de Microsoft Excel ni el pegado de imágenes en una instalación real de Outlook. El dictado requiere una comprobación con micrófono real. La publicación autocontenida se ofrece como opción del script, pero la variante efectivamente ejecutada fue la dependiente del runtime.

La firma original ya está incorporada. Microsoft Graph, envío automático y autenticación siguen fuera de esta versión. Los datos de prueba permanecen separados de los borradores de uso real.



Actualización búsqueda/correo: 14 pruebas backend, 7 frontend y 2 recorridos de navegador correctos. La búsqueda se verifica más allá de los primeros 100 documentos, con fechas inclusivas y texto parametrizado. Se comprueban HTML seguro, avisos fijos, persistencia y regeneración del cuerpo. En navegador se comprobaron negrita, fuente Georgia, 18 pt, justificación y recuperación del borrador. Auditoría npm sin vulnerabilidades tras fijar Quill 2.0.2; no se utilizó la versión 2.0.3 afectada por el aviso de exportación HTML. Microsoft 365 quedó documentado, no conectado ni probado con un tenant real.

Última revisión: destinatarios plegables, una imagen por evidencia, cabecera fija, interfaz de 12 px y Acerca del generador. Correctas 14 pruebas backend, 9 frontend y 6 recorridos de navegador. Se verificó la síntesis de cursiva en Tahoma, activar/desactivar el botón, ancho móvil, posición fija de la cabecera al desplazarse y rechazo de varias imágenes sin cargas parciales. La conversión de borradores antiguos preserva imágenes, descripciones e identificadores únicos.

## Perfil y almacenamiento — 8 de octubre de 2026

- 16 pruebas backend, 9 frontend y 7 recorridos de navegador correctos (los seis recorridos anteriores y el nuevo recorrido de configuración, corregido y vuelto a ejecutar).
- Compilación Release .NET sin errores ni advertencias; frontend compila con el aviso conocido de tamaño del paquete.
- Preferencias persistentes, revisión optimista, validación de contactos/ambientes, bloqueo de orígenes externos.
- Reexportación a la misma carpeta produce un único XLSX actualizado; un bloqueo exclusivo del archivo devuelve error, conserva los bytes anteriores y no deja temporales.
- Foto/nombre tras recargar, autocompletado del responsable, selección independiente SQL/Oracle, botón fijo, multiselección y filtrado por contacto, Para/CC en la hoja y editor oculto durante la vista previa.
- Se revisaron capturas de la configuración y de la hoja de correo. Se verificó que las tres rutas del desplegable no se superponen y que el perfil permanece accesible en móvil.
- Los datos de cada ejecución del navegador quedan en una subcarpeta propia de work/e2e-data; los Excel de prueba quedan en work. No se modificaron borradores reales.
- No se probó impresión real ni envío por Outlook. La ubicación se escribe o pega como ruta local; no se implementó un diálogo nativo del Explorador. La API sigue siendo exclusiva para uso local.

## Catálogos independientes y notificaciones — 8 de octubre de 2026

17 pruebas backend, 11 frontend y 8 recorridos de navegador correctos (seis recorridos generales y dos de configuración/notificaciones). Se verificó migración de ambientes antiguos a URL/conexiones sin relación, deduplicación, conservación de listas vaciadas intencionalmente, rechazo de URL no HTTP/HTTPS y conexiones duplicadas. En navegador se comprobó seleccionar Oracle Test en ambas secciones, cambiar URL sin alterar conexión y ausencia de nombre/motor en configuración. Temporizadores: cinco segundos activos, pausa/reanudación por cursor y foco, errores persistentes y X de cierre; comprobados con temporizadores controlados y eventos reales del navegador. El desplegable de ubicaciones se cierra al navegar para no tapar acciones. Compilación de frontend y publicación Release correctas, con el aviso conocido de tamaño del JavaScript.

Ajuste Descargas predeterminadas: 19 pruebas backend correctas y compilación frontend correcta. Verificados perfil sin ruta, ruta vacía o con espacios, guardado usando la ubicación de Descargas simulada en una carpeta temporal, conservación de rutas personalizadas, rechazo de rutas relativas y consulta real de la carpeta conocida de Windows sin crear archivos en Descargas.

## Dictado y ubicación del Excel

20 pruebas backend correctas, compilación frontend correcta y tres nuevos recorridos de navegador correctos. Pruebas de dictado con SpeechRecognition simulado: modo continuo, resultados parciales/finales, no duplicación, conservación del fragmento al detener, liberación al navegar, reintentos limitados por no-speech, error de permisos y conservación del sobrante de 4.000 caracteres. Se verificaron el indicador visual y el botón de ubicación, incluida la ausencia de descarga adicional. El backend prueba rutas registradas, archivo ausente, origen externo rechazado y uso de un abridor simulado para no abrir ventanas durante las pruebas. No se validó acústicamente un micrófono real ni la exactitud del proveedor de reconocimiento; tampoco la selección visual de Explorer dentro de una sesión real del usuario.
