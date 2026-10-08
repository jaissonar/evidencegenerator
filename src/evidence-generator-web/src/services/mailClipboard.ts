// Flatten inherited formatting so mail clients do not depend on the outer wrapper.
// Input is the already escaped/sanitized output of mailHtml, never raw pasted HTML.
export function prepareMailClipboard(html: string) {
  const document = new DOMParser().parseFromString(html, "text/html");
  const properties = ["font-family", "font-size", "color", "font-weight", "font-style", "line-height", "text-align", "text-decoration"];
  function inline(element: HTMLElement, inherited: Record<string, string>) {
    const styles = { ...inherited };
    const semantic: Record<string, Record<string, string>> = {
      STRONG: { "font-weight": "bold" }, B: { "font-weight": "bold" },
      EM: { "font-style": "italic" }, I: { "font-style": "italic" },
      U: { "text-decoration": "underline" }, S: { "text-decoration": "line-through" },
    };
    Object.assign(styles, semantic[element.tagName] ?? {});
    for (const property of properties) {
      const explicit = element.style.getPropertyValue(property);
      if (explicit) styles[property] = explicit;
      if (styles[property]) element.style.setProperty(property, styles[property]);
    }
    if (["P", "DIV", "LI", "H1", "H2", "H3"].includes(element.tagName)) {
      if (styles["text-align"]) element.setAttribute("align", styles["text-align"]);
      if (!element.style.margin && element.tagName === "P") element.style.margin = "0 0 12px";
    }
    for (const child of element.children) inline(child as HTMLElement, styles);
  }
  for (const child of document.body.children) inline(child as HTMLElement, {});
  const fragment = document.body.innerHTML;
  const plain = document.body.cloneNode(true) as HTMLElement;
  for (const br of plain.querySelectorAll("br")) br.replaceWith("\n");
  for (const block of plain.querySelectorAll("p,div,li,h1,h2,h3")) block.append("\n");
  return {
    fragment,
    html: `<!doctype html><html lang="es"><head><meta charset="utf-8"></head><body><!--StartFragment-->${fragment}<!--EndFragment--></body></html>`,
    text: (plain.textContent ?? "").replace(/\n{3,}/g, "\n\n").trim(),
  };
}
