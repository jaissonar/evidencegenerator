import DOMPurify from "dompurify";

// Keep only email formatting; pasted HTML cannot add active content or remote images.
export function sanitizeMailBody(html: string): string {
  const safe = DOMPurify.sanitize(html, {
    ALLOWED_TAGS: ['p','br','strong','b','em','i','u','s','span','div','a','ul','ol','li','blockquote','h1','h2','h3','sub','sup'],
    ALLOWED_ATTR: ['style','href','class','data-list'],
    ALLOW_DATA_ATTR: false,
  });
  const root = new DOMParser().parseFromString(safe, 'text/html').body;
  for (const node of root.querySelectorAll<HTMLElement>('*')) {
    const original = node.style;
    const allowed = new Map<string,string>();
    for (const property of ['color','background-color','font-family','font-size','font-weight','font-style','text-decoration','text-align','margin','margin-bottom','padding-left','line-height']) {
      const value = original.getPropertyValue(property);
      if (value && !/url\s*\(|expression|var\s*\(|[<>]/i.test(value)) allowed.set(property, value);
    }
    const align = [...node.classList].find(value => /^ql-align-(center|right|justify)$/.test(value));
    if (align) allowed.set('text-align', align.replace('ql-align-',''));
    const indent = [...node.classList].find(value => /^ql-indent-[1-8]$/.test(value));
    if (indent) allowed.set('padding-left', `${Number(indent.slice(-1))*24}px`);
    node.removeAttribute('class'); node.removeAttribute('style');
    allowed.forEach((value,property) => node.style.setProperty(property,value));
    if (node.tagName === 'A') {
      if (!/^(https?:\/\/|mailto:)/i.test(node.getAttribute('href') ?? '')) node.removeAttribute('href');
      node.setAttribute('rel','noopener noreferrer');
    }
  }
  // Quill 2 uses ol + data-list for bullets; email clients require real ul elements.
  for (const list of [...root.querySelectorAll('ol')]) {
    let group: HTMLElement | undefined;
    for (const item of [...list.children]) {
      const kind = item.getAttribute('data-list') === 'bullet' ? 'ul' : 'ol';
      item.removeAttribute('data-list');
      if (!group || group.tagName.toLowerCase() !== kind) { group = root.ownerDocument.createElement(kind); list.before(group); }
      group.append(item);
    }
    list.remove();
  }
  return root.innerHTML;
}
