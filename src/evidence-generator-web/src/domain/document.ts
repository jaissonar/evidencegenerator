import {
  defaultSignature,
  environmentalNotice,
  confidentialityNotice,
} from "./mailDefaults";

export interface EvidenceImage {
  dataUrl: string;
}
export interface Evidence {
  id: string;
  description: string;
  images: EvidenceImage[];
}
export interface EvidenceSection {
  url: string;
  connection: string;
  loginImage: EvidenceImage | null;
  items: Evidence[];
}
export interface MailSettings {
  bodyHtml?: string | null;
  recipientName: string;
  to: string;
  cc: string;
  subject: string;
  fontSize: number;
  environmentalNotice: string;
  confidentialityNotice: string;
  signature: EvidenceImage | null;
  signatureWidth: number;
  templateVersion: number;
}
export interface EvidenceDocument {
  id: string;
  revision: number;
  requirement: string;
  description: string;
  author: string;
  client: string;
  date: string;
  sql: EvidenceSection;
  oracle: EvidenceSection;
  mail: MailSettings;
}
export interface DocumentSummary {
  client: string;
  description: string;
  date: string;
  id: string;
  requirement: string;
  revision: number;
  updatedAt: string;
}
export const sites = [
  "https://app.oasiscom.com/",
  "https://pruebas.oasiscom.com/",
  "https://desarrollo.oasiscom.com/",
];
const section = (): EvidenceSection => ({
  url: sites[2]!,
  connection: "",
  loginImage: null,
  items: [],
});
export function newDocument(): EvidenceDocument {
  const date = new Date();
  return {
    id: crypto.randomUUID(),
    revision: 0,
    requirement: "",
    description: "",
    author: "",
    client: "",
    date: `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, "0")}-${String(date.getDate()).padStart(2, "0")}`,
    sql: section(),
    oracle: section(),
    mail: {
      recipientName: "",
      to: "",
      cc: "",
      subject: "",
      fontSize: 11,
      environmentalNotice,
      confidentialityNotice,
      signature: defaultSignature(),
      signatureWidth: 420,
      templateVersion: 1,
      bodyHtml: null,
    },
  };
}
export function moveItem<T>(items: T[], index: number, delta: number) {
  const destination = index + delta;
  if (destination < 0 || destination >= items.length) return;
  const item = items.splice(index, 1)[0];
  if (item !== undefined) items.splice(destination, 0, item);
}

// Old drafts remain untouched on disk until the user saves the converted document.
export function splitLegacyEvidenceImages(doc: EvidenceDocument): boolean {
  let changed = false;
  for (const section of [doc.sql, doc.oracle]) {
    section.items = section.items.flatMap(item => {
      if (item.images.length <= 1) return [item];
      changed = true;
      return item.images.map((image, index) => ({
        ...item, id: index === 0 ? item.id : crypto.randomUUID(), images: [image],
      }));
    });
  }
  return changed;
}
