// @vitest-environment jsdom
import { describe, it, expect } from "vitest";
import { newDocument, moveItem } from "../domain/document";
import {
  confidentialityNotice,
  environmentalNotice,
  upgradeMailDefaults,
} from "../domain/mailDefaults";
import { mailHtml } from "./mail";
const preview = (doc: ReturnType<typeof newDocument>) =>
  mailHtml(doc).replace(/data:image\/png;base64,[A-Za-z0-9+/=]+/g, "[PNG]");

describe("Correo", () => {
  it("escapa texto y rechaza enlaces y firmas ejecutables", () => {
    const doc = newDocument();
    doc.description = "<img src=x onerror=alert(1)>";
    doc.sql.url = "javascript:alert(1)";
    doc.mail.signature = { dataUrl: "javascript:alert(1)" };
    const html = preview(doc);
    expect(html).toContain("&lt;img");
    expect(html).not.toContain("<img");
    expect(html).not.toContain('href="javascript:');
  });
  it("muestra únicamente Entorno SQL y conserva Oracle fuera del correo", () => {
    const doc = newDocument();
    doc.requirement = "MD 123";
    doc.sql.connection = "SQL-X";
    doc.oracle.connection = "ORACLE-Y";
    doc.mail.fontSize = 12;
    const html = preview(doc);
    expect(html).toContain("MD 123");
    expect(html).toContain("SQL-X");
    expect(html).not.toContain("ORACLE-Y");
    expect(html).not.toContain("Oracle");
    expect(html).toContain("Entorno SQL:");
    expect(html).toContain("font-size:12pt");
    expect(doc.oracle.connection).toBe("ORACLE-Y");
  });
  it("reproduce colores, firma compacta y avisos a 8 pt después de la firma", () => {
    const doc = newDocument();
    const html = preview(doc);
    expect(
      doc.mail.signature?.dataUrl.startsWith("data:image/png;base64,"),
    ).toBe(true);
    expect(html).toContain('width="420"');
    expect(html).toContain("color:#174E86");
    expect(html).toContain("color:#0F7001");
    expect(html.match(/font-size:8pt/g)).toHaveLength(2);
    expect(html.match(/<strong>OasisCom S.A.S.<\/strong>/g)).toHaveLength(2);
    expect(html.indexOf('alt="Firma"')).toBeLessThan(
      html.indexOf("¡Salva un árbol"),
    );
    expect(html.indexOf("¡Salva un árbol")).toBeLessThan(
      html.indexOf("La información Contenida"),
    );
    doc.mail.signatureWidth = 280;
    expect(preview(doc)).toContain('width="280"');
  });
  it("completa borradores antiguos sin reemplazar textos ni firmas personalizados", () => {
    const old = newDocument();
    old.mail.templateVersion = 0;
    old.mail.signature = null;
    old.mail.environmentalNotice = "";
    old.mail.confidentialityNotice = "";
    expect(upgradeMailDefaults(old)).toBe(true);
    expect(old.mail.environmentalNotice).toBe(environmentalNotice);
    expect(old.mail.confidentialityNotice).toBe(confidentialityNotice);
    old.mail.signature = null;
    old.mail.confidentialityNotice = "";
    expect(upgradeMailDefaults(old)).toBe(false);
    expect(old.mail.signature).toBeNull();
    expect(old.mail.confidentialityNotice).toBe("");
    const custom = newDocument();
    custom.mail.templateVersion = 0;
    custom.mail.confidentialityNotice = "Personalizado";
    custom.mail.signature = { dataUrl: "custom" };
    upgradeMailDefaults(custom);
    expect(custom.mail.confidentialityNotice).toBe("Personalizado");
    expect(custom.mail.signature.dataUrl).toBe("custom");
  });
});
describe("Orden de evidencias", () => {
  it("conserva elementos y respeta los extremos", () => {
    const items = ["a", "b", "c"];
    moveItem(items, 0, -1);
    expect(items).toEqual(["a", "b", "c"]);
    moveItem(items, 0, 1);
    expect(items).toEqual(["b", "a", "c"]);
  });
});

