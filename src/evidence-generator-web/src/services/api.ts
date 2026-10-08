import type { EvidenceDocument, DocumentSummary } from "../domain/document";
import type { WorkspaceSettings, ExportLocation } from '../domain/settings';
export const openExcelLocation = async (requirement: string): Promise<void> => {
  await request('/exports/open-location', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ requirement }) });
};
export const getSettings = async (): Promise<WorkspaceSettings> => (await request('/settings')).json();
export const saveSettings = async (settings: WorkspaceSettings): Promise<WorkspaceSettings> =>
  (await request('/settings', { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(settings) })).json();
export const getExcelLocation = async (requirement: string): Promise<ExportLocation | null> =>
  (await request(`/exports/location?${new URLSearchParams({ requirement })}`)).json();
export const saveExcel = async (document: EvidenceDocument, directory: string): Promise<ExportLocation> =>
  (await request('/exports/save', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ document, directory }) })).json();
export const getStorageInfo = async (): Promise<{
  databasePath: string;
  provider: string;
}> => (await request("/storage")).json();
async function request(path: string, options?: RequestInit) {
  let response: Response;
  try {
    response = await fetch(`/api${path}`, options);
  } catch {
    throw new Error(
      "No se pudo conectar con la API local. Comprueba que esté iniciada.",
    );
  }
  if (!response.ok) {
    const problem = await response.json().catch(() => ({}));
    throw new Error(
      problem.detail ||
        (response.status === 413
          ? "El documento supera el límite de 35 MB."
          : `No se pudo completar la operación (${response.status}).`),
    );
  }
  return response;
}
export const listDocuments = async (filters: Record<string, string> = {}): Promise<DocumentSummary[]> =>
  (await request(`/documents?${new URLSearchParams(filters)}`)).json();
export const getDocument = async (id: string): Promise<EvidenceDocument> =>
  (await request(`/documents/${id}`)).json();
export const deleteDocument = async (id: string, revision: number): Promise<void> => {
  await request(`/documents/${id}?revision=${revision}`, { method: "DELETE" });
};
export const saveDocument = async (
  doc: EvidenceDocument,
): Promise<EvidenceDocument> =>
  (
    await request(`/documents/${doc.id}`, {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(doc),
    })
  ).json();
export async function exportExcel(doc: EvidenceDocument) {
  const response = await request("/exports/excel", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(doc),
  });
  download(
    await response.blob(),
    `Pruebas Unitarias - ${doc.requirement}.xlsx`,
  );
}
export function download(blob: Blob, name: string) {
  const url = URL.createObjectURL(blob);
  const a = document.createElement("a");
  a.href = url;
  a.download = name;
  a.click();
  setTimeout(() => URL.revokeObjectURL(url), 1000);
}
