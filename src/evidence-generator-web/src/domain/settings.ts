import type { EvidenceImage, EvidenceDocument } from './document';
export interface Contact { id: string; name: string; email: string }
export interface WorkspaceSettings {
  revision: number; name: string; photo: EvidenceImage | null; excelDirectory: string;
  saveMode: 'ask' | 'configured'; signature: EvidenceImage | null; signatureWidth: number;
  contacts: Contact[]; environmentUrls: string[]; connections: string[];
}
export interface ExportLocation { requirement: string; path: string; savedAt: string }
export function applyProfile(doc: EvidenceDocument, settings: WorkspaceSettings | null) {
  if (!settings) return;
  doc.author = settings.name;
  if (settings.revision > 0) {
    doc.mail.signature = settings.signature ? { ...settings.signature } : null;
    doc.mail.signatureWidth = settings.signatureWidth;
  }
}
