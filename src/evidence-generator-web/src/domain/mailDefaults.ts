import signatureDataUrl from "../assets/email/firma-correo.png?inline";
import type { EvidenceDocument, MailSettings } from "./document";

export const environmentalNotice =
  "¡Salva un árbol...no imprimas este mail a menos que realmente lo necesites!";
export const confidentialityNotice =
  "La información Contenida es este mensaje es de carácter privado y confidencial, corresponde únicamente a sus destinatarios y no compromete para nada a OasisCom S.A.S. Queda prohibida la reproducción parcial o total de la misma sin autorización expresa del Emisor.  Los mensajes, archivos y datos que contiene el correo han sido revisados por un antivirus, no obstante, OasisCom S.A.S. no se hace responsable por los daños que los equipos receptores puedan sufrir a causa o consecuencia de este mensaje,  Si usted por algún motivo la recibe favor devolverla al correo del emisor.";

export function defaultSignature() {
  return { dataUrl: signatureDataUrl };
}
export function restoreNotices(mail: MailSettings) {
  mail.environmentalNotice = environmentalNotice;
  mail.confidentialityNotice = confidentialityNotice;
}
export function upgradeMailDefaults(doc: EvidenceDocument): boolean {
  if ((doc.mail.templateVersion ?? 0) >= 1) return false;
  doc.mail.signature ??= defaultSignature();
  if (!doc.mail.environmentalNotice.trim())
    doc.mail.environmentalNotice = environmentalNotice;
  if (!doc.mail.confidentialityNotice.trim())
    doc.mail.confidentialityNotice = confidentialityNotice;
  doc.mail.signatureWidth ??= 420;
  doc.mail.templateVersion = 1;
  return true;
}
