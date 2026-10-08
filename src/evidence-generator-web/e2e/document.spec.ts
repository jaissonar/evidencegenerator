import path from 'node:path';
import { test, expect } from "@playwright/test";
test("documento completo: imágenes, orden, persistencia, Excel, correo y móvil", async ({
  page,
}) => {
  const errors: string[] = [];
  page.on("pageerror", (error) => errors.push(error.message));
  await page.goto("/");
  await expect(page.getByText("Invalid PrimeUI License")).toHaveCount(0);
  await page.getByLabel("Requerimiento *", { exact: true }).fill("MD QA 12345");
  await page.getByLabel("Elaborado por *").fill("Prueba automatizada");
  await page.getByLabel("Cliente", { exact: true }).fill("Cliente de prueba");
  await page
    .getByLabel("Descripción del requerimiento *")
    .fill(
      "Validar traducción del programa y conservar el diseño institucional.",
    );
  await page.screenshot({ path: "../../work/ui-general.png", fullPage: true });
  await page.getByRole("button", { name: "Continuar con SQL" }).click();
  await page.getByLabel("Conexión / Ambiente").fill("SQL-QA");
  await page.getByRole("button", { name: "Crear primera evidencia" }).click();
  await page
    .getByLabel("Descripción de la validación")
    .fill("Primera validación SQL: título traducido.");
  await page.getByRole("button", { name: "Limpiar", exact: true }).click();
  await expect(page.getByLabel("Descripción de la validación")).toHaveValue("");
  await page
    .getByLabel("Descripción de la validación")
    .fill("Primera validación SQL: título traducido.");
  const dataUrl = await page.evaluate(() => {
    const canvas = document.createElement("canvas");
    canvas.width = 960;
    canvas.height = 540;
    const ctx = canvas.getContext("2d")!;
    ctx.fillStyle = "#f7f8fa";
    ctx.fillRect(0, 0, 960, 540);
    ctx.fillStyle = "#111318";
    ctx.fillRect(0, 0, 960, 70);
    ctx.fillStyle = "#ffffff";
    ctx.font = "24px Tahoma";
    ctx.fillText("Validación funcional · Evidence Generator", 32, 44);
    ctx.fillStyle = "#5b3fa8";
    ctx.font = "32px Tahoma";
    ctx.fillText("Novedades de Flete", 60, 170);
    ctx.fillStyle = "#185b63";
    ctx.font = "20px Tahoma";
    ctx.fillText("Resultado esperado: traducción disponible", 60, 235);
    return canvas.toDataURL("image/png");
  });
  const image = {
    name: "evidencia.png",
    mimeType: "image/png",
    buffer: Buffer.from(dataUrl.split(",")[1]!, "base64"),
  };
  await page.locator(".evidence-card input[type=file]").setInputFiles(image);
  await expect(page.locator(".capture img")).toHaveCount(1);
  await expect(page.locator(".evidence-card .image-input")).toHaveCount(0);
  await page.getByRole("button", { name: "Quitar imagen", exact: true }).click();
  await page
    .locator(".evidence-card .image-input")
    .evaluate(async (el, url) => {
      const blob = await (await fetch(url)).blob();
      const transfer = new DataTransfer();
      transfer.items.add(new File([blob], "pegada.png", { type: "image/png" }));
      el.dispatchEvent(
        new ClipboardEvent("paste", {
          clipboardData: transfer,
          bubbles: true,
          cancelable: true,
        }),
      );
    }, dataUrl);
  await expect(page.locator(".capture img")).toHaveCount(1);
  await page
    .getByRole("button", { name: "Agregar evidencia", exact: true })
    .click();
  await page
    .getByLabel("Descripción de la validación")
    .nth(1)
    .fill("Segunda validación SQL: revisión de permisos.");
  await page.locator(".evidence-card input[type=file]").setInputFiles(image);
  await page.getByRole("button", { name: "Subir evidencia" }).nth(1).click();
  await expect(
    page.getByLabel("Descripción de la validación").first(),
  ).toHaveValue("Segunda validación SQL: revisión de permisos.");
  await page.screenshot({ path: "../../work/ui-sql.png", fullPage: true });
  await page.getByRole("button", { name: "Evidencias Oracle" }).click();
  await page.getByLabel("Conexión / Ambiente").fill("ORACLE-QA");
  await page.getByRole("button", { name: "Crear primera evidencia" }).click();
  await page
    .getByLabel("Descripción de la validación")
    .fill("Validación de compatibilidad Oracle.");
  await page.locator(".evidence-card input[type=file]").setInputFiles(image);
  await expect(page.locator(".capture img")).toHaveCount(1);
  await page.getByRole("button", { name: "Guardar borrador" }).click();
  await expect(page.locator(".message.success").last()).toContainText("Borrador guardado");
  await page.getByLabel("Modo de guardado del Excel").selectOption("ask");
  await page.getByRole("button", { name: "Guardar Excel", exact:true }).click();
  await page.getByLabel("Carpeta de destino (ruta completa)").fill(path.resolve("../../work/e2e-exports"));
  await page.getByRole("button", {name:"Guardar en esta carpeta"}).click();
  await expect(page.locator(".message.success").last()).toContainText("Excel guardado");
  await page.getByRole("button", { name: "Preparar correo" }).click();
  await page.locator(".recipients-panel summary").click();
  await page.getByLabel("Nombre para el saludo").fill("Nicolás");
  await page.getByLabel("Tamaño de fuente").selectOption("12");
  await expect(page.locator(".email-preview")).toContainText(
    "Buen día, Nicolás.",
  );
  await expect(page.locator(".email-preview")).not.toContainText("ORACLE-QA");
  await expect(page.locator(".email-preview")).toContainText("Entorno SQL:");
  await expect(page.locator(".email-preview img")).toHaveAttribute(
    "width",
    "420",
  );
  await page.locator(".signature-panel summary").click();
  await page.getByLabel("Ancho de la firma").selectOption("360");
  await expect(page.locator(".email-preview img")).toHaveAttribute(
    "width",
    "360",
  );
  await expect(
    page
      .locator(".email-preview strong")
      .filter({ hasText: "requerimiento MD QA" }),
  ).toHaveCSS("color", "rgb(23, 78, 134)");
  await expect(
    page.locator(".email-preview p").filter({ hasText: "¡Salva un árbol" }),
  ).toHaveCSS("font-size", "10.6667px");
  await page.getByRole("button",{name:"Vista previa del correo",exact:true}).click();
  await page.screenshot({ path: "../../work/ui-mail.png", fullPage: true });
  await page
    .locator(".email-preview")
    .screenshot({ path: "../../work/mail-preview.png" });
  await page.getByRole("button",{name:"Volver al editor"}).click();
  await page.locator("input[type=file]").setInputFiles(image);
  await expect(page.locator(".email-preview img")).toHaveCount(1);
  await page.getByRole("button", { name: "Usar firma predeterminada" }).click();
  await page.getByRole("button", { name: "Guardar borrador" }).click();
  await expect(page.locator(".message.success").last()).toContainText("Borrador guardado");
  await page.reload();
  await page.getByRole("button", { name: "Borradores locales" }).click();
  await page.locator(".history-item").first().click();
  await page.locator(".storage-detail summary").click();
  await expect(page.locator(".storage-detail")).toContainText("evidence.db");
  await expect(page.getByLabel("Requerimiento *", { exact: true })).toHaveValue(
    "MD QA 12345",
  );
  await page.getByRole("button", { name: "Vista previa" }).click();
  await expect(page.locator(".document-preview img")).toHaveCount(3);
  await page.setViewportSize({ width: 390, height: 844 });
  await page.screenshot({ path: "../../work/ui-mobile.png", fullPage: true });
  expect(
    await page.evaluate(
      () => document.documentElement.scrollWidth <= window.innerWidth,
    ),
  ).toBeTruthy();
  expect(errors).toEqual([]);
});



