import { environmentalNotice, confidentialityNotice } from "../domain/mailDefaults";
import { sanitizeMailBody } from "./mailBody";
import type { EvidenceDocument } from "../domain/document";

export const escapeHtml = (value: string) =>
  value.replace(
    /[&<>"']/g,
    (c) =>
      ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" })[
        c
      ]!,
  );
const lines = (value: string) => escapeHtml(value).replace(/\n/g, "<br>");
export const mailColors = {
  requirement: "#174E86",
  environmental: "#0F7001",
  text: "#000000",
  link: "#0000FF",
};

export function mailHtml(doc: EvidenceDocument) {
  const m = doc.mail;

  const width = Number.isFinite(m.signatureWidth)
    ? Math.min(600, Math.max(240, m.signatureWidth))
    : 420;
  const signature = m.signature?.dataUrl.match(
    /^data:image\/png;base64,[A-Za-z0-9+/=]+$/,
  )
    ? `<p style="margin:0 0 12px"><img alt="Firma" width="${width}" style="display:block;width:${width}px;max-width:100%;height:auto;border:0" src="${m.signature.dataUrl}"></p>`
    : "";
  const confidentiality = lines(confidentialityNotice).replace(
    /OasisCom S\.A\.S\./g,
    "<strong>OasisCom S.A.S.</strong>",
  );
  return `<div style="font-family:Tahoma,Arial,sans-serif;font-size:${m.fontSize === 12 ? 12 : 11}pt;color:${mailColors.text};line-height:1.5">
${m.bodyHtml == null ? defaultMailBody(doc) : sanitizeMailBody(m.bodyHtml)}
${signature}
${environmentalNotice ? `<p style="font-family:Tahoma,Arial,sans-serif;font-size:8pt;line-height:1.8;margin:0 0 2px;color:${mailColors.environmental}">${lines(environmentalNotice)}</p>` : ""}
${confidentialityNotice ? `<p style="font-family:Tahoma,Arial,sans-serif;font-size:8pt;line-height:1.8;margin:0;color:${mailColors.text}">${confidentiality}</p>` : ""}
</div>`;
}

export function defaultMailBody(doc: EvidenceDocument) {
  const m = doc.mail; const env = doc.sql;
  const url = /^https?:\/\//i.test(env.url)
    ? `<a style="color:${mailColors.link};text-decoration:underline" href="${escapeHtml(env.url)}">${escapeHtml(env.url)}</a>`
    : escapeHtml(env.url);
  return `<p style="margin:0 0 16px">Buen día${m.recipientName ? ", " + escapeHtml(m.recipientName) : ""}.</p>
<p style="margin:0 0 16px">Se encuentra disponible para validación el <strong style="color:${mailColors.requirement}">requerimiento ${escapeHtml(doc.requirement)}</strong>.</p>
<p style="margin:0 0 16px"><strong>Descripción:</strong><br>${lines(doc.description)}</p>
<p style="margin:0 0 12px"><strong>Entorno SQL:</strong></p>
<ul style="margin:0 0 16px;padding-left:30px"><li style="margin-bottom:8px"><strong>URL:</strong> ${url}</li><li style="margin-bottom:8px"><strong>Conexión:</strong> <em>${escapeHtml(env.connection)}</em></li><li><strong>Cliente:</strong> <em>${escapeHtml(doc.client)}</em></li></ul>
<p style="margin:0 0 28px">Se solicita validar comportamiento esperado y notificar cualquier anomalía identificada durante la ejecución de las pruebas.</p>
<p style="margin:0 0 12px;color:#333333">Cordialmente,</p>
`;
}

