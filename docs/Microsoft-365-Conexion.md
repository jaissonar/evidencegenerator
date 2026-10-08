# Evidence Generator — conexión futura a Microsoft 365

## Estado

La aplicación prepara, edita, copia y descarga el correo. Esta entrega no inicia sesiones de Microsoft, no envía correos y no almacena credenciales. La conexión descrita aquí es el siguiente paso de implementación.

## Viabilidad con doble factor

Sí: Microsoft Graph con inicio de sesión interactivo en Microsoft Entra ID permite que Microsoft aplique MFA y Acceso Condicional. No se desactiva el doble factor. Una política corporativa puede exigir aprobación administrativa, dispositivo administrado, ubicación autorizada o bloquear la aplicación; debe revisarlo TI.

## Enfoque recomendado

Para el generador local de un solo usuario: aplicación de un solo tenant, MSAL Browser en Vue y flujo Authorization Code con PKCE. La pantalla de Microsoft recibe la contraseña y el segundo factor. No se solicita contraseña ni códigos al generador, no se utiliza una contraseña de aplicación y no se incluye un client secret en el frontend. El cliente solicita tokens para Graph; la API local sigue generando el Excel. Mantener tokens fuera de SQLite, documentos, logs y cookies de preferencias; preferir caché en memoria y reautenticación cuando Microsoft lo requiera.

## Lo que se necesita de TI

1. Registrar Evidence Generator en Microsoft Entra ID, restringida a la organización.
2. Proporcionar Directory/Tenant ID y Application/Client ID. Son identificadores de configuración, no contraseñas.
3. Configurar plataforma SPA y una URI de redirección exacta. Propuesta de desarrollo: `http://localhost:5173/`; para la aplicación local publicada: `http://localhost:5080/`. Acordar una dirección de uso consistente; las cookies de localhost y 127.0.0.1 son independientes. Para alojamiento remoto, HTTPS y actualización de los controles de acceso actuales de la API son necesarios.
4. Autorizar permisos delegados y revisar las políticas de consentimiento de usuarios, MFA, Acceso Condicional y asignación de usuarios.
5. Confirmar si se enviará desde el buzón del usuario o desde un buzón compartido. La primera integración debería utilizar el buzón propio; los compartidos requieren autorización y permisos adicionales específicos.
6. Confirmar límites de tamaño de mensajes/adjuntos de Exchange Online y un destinatario de pruebas autorizado.

## Permisos por modalidad

| Modalidad | Permisos de correo | Observación |
|---|---|---|
| Enviar en nombre del usuario conectado | Mail.Send delegado | POST /me/sendMail; no requiere leer todo el buzón para el envío directo |
| Crear un borrador en Outlook para revisión | Mail.ReadWrite delegado | Permite escribir el borrador; implica acceso más amplio, requiere valoración de TI |
| Adjuntos grandes mediante sesión de carga | Mail.ReadWrite delegado | Necesita un mensaje borrador; para enviarlo desde la aplicación se añade Mail.Send |

Para este producto recomiendo el flujo borrador + adjunto + revisión, porque las evidencias pueden generar archivos grandes y permite revisar destinatarios antes de enviar. Si TI solo concede Mail.Send, limitar la primera integración a envío directo con adjuntos pequeños y revisión explícita dentro del generador. No solicitar permisos de aplicación para envío desatendido ni acceso a todos los buzones.

## Flujo propuesto

Conectar Microsoft 365 → inicio de sesión y MFA → preparar el correo → generar Excel → revisar destinatarios, asunto y adjuntos → crear borrador o confirmar envío → mostrar resultado.

La firma debe convertirse en adjunto inline con Content-ID y el HTML debe referenciar `cid:` para mejorar compatibilidad; no depender del soporte de imágenes data URI del cliente de correo. Los estilos del cuerpo se enviarán inline, sin CSS de la página. Mantener los avisos fijos en Tahoma 8 pt.

Los adjuntos inferiores a 3 MB admiten el mecanismo simple; entre 3 y 150 MB Graph ofrece sesiones de carga. Los límites efectivos de Exchange/tenant y el tamaño total del mensaje pueden ser menores. El soporte para archivos grandes debe implementarse expresamente, con progreso, cancelación y manejo de fallos. Un `202 Accepted` confirma aceptación de la petición de envío, no entrega al destinatario; no mostrar «entregado». Evitar reintentos automáticos indiscriminados que puedan duplicar correos.

## Validación antes de activar

Probar sesión con MFA, denegación de consentimiento, expiración/cierre de sesión, firma inline, adjunto pequeño/grande, cancelación, política de acceso y revisión de destinatarios. Verificar que no se guardan tokens en los borradores ni en logs. La aprobación del tenant y las pruebas reales no se han realizado en esta entrega.

## Fuentes oficiales consultadas

- [MSAL Browser y Authorization Code con PKCE](https://learn.microsoft.com/en-us/entra/msal/javascript/browser/about-msal-browser).
- [Acceso Condicional para desarrolladores](https://learn.microsoft.com/en-us/entra/identity-platform/v2-conditional-access-dev-guide).
- [Enviar correo con Graph y permiso Mail.Send](https://learn.microsoft.com/en-us/graph/api/user-sendmail?view=graph-rest-1.0).
- [Adjuntos grandes y sesiones de carga](https://learn.microsoft.com/en-us/graph/outlook-large-attachments).
