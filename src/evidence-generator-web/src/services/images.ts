import type { EvidenceImage } from "../domain/document";
export async function normalizeImage(file: File): Promise<EvidenceImage> {
  if (
    !["image/png", "image/jpeg", "image/webp"].includes(file.type) ||
    file.size > 10 * 1024 * 1024
  )
    throw new Error("Usa una imagen PNG, JPG o WebP de hasta 10 MB.");
  const bitmap = await createImageBitmap(file);
  try {
    const ratio = Math.min(1, 2400 / Math.max(bitmap.width, bitmap.height));
    const canvas = document.createElement("canvas");
    canvas.width = Math.max(1, Math.round(bitmap.width * ratio));
    canvas.height = Math.max(1, Math.round(bitmap.height * ratio));
    canvas
      .getContext("2d")!
      .drawImage(bitmap, 0, 0, canvas.width, canvas.height);
    const dataUrl = canvas.toDataURL("image/png");
    if (dataUrl.length > 6_990_000)
      throw new Error(
        "La captura sigue superando 5 MB. Recorta el área antes de subirla.",
      );
    return { dataUrl };
  } finally {
    bitmap.close();
  }
}
