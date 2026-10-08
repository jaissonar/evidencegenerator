// @vitest-environment jsdom
import { it, expect } from 'vitest';
import { prepareMailClipboard } from './mailClipboard';
import { mailHtml } from './mail';
import { newDocument } from '../domain/document';
it('copies inherited font and color onto each element without losing local styles', () => {
 const doc = newDocument();
 doc.mail.bodyHtml='<p style="text-align:justify"><span style="font-family:Georgia;font-size:18pt;color:#123456;background-color:#ffee00"><strong><em>Personalizado</em></strong></span></p>';
 const result = prepareMailClipboard(mailHtml(doc));
 const body = new DOMParser().parseFromString(result.html,'text/html').body;
 const text = body.querySelector('em')!;
 expect(text.style.fontFamily).toBe('Georgia'); expect(text.style.fontSize).toBe('18pt');
 expect(text.style.color).toBe('rgb(18, 52, 86)'); expect(text.style.fontWeight).toBe('bold'); expect(text.style.fontStyle).toBe('italic');
 expect(body.querySelector('p')!.getAttribute('align')).toBe('justify');
 expect(body.querySelector('span')!.style.backgroundColor).toBe('rgb(255, 238, 0)');
 const disclaimer = [...body.querySelectorAll('strong')].find(e=>e.textContent==='OasisCom S.A.S.')!;
 expect(disclaimer.style.fontSize).toBe('8pt'); expect(disclaimer.style.fontFamily).toContain('Tahoma');
 expect(result.text).toContain('Personalizado'); expect(result.text).toContain('¡Salva un árbol'); expect(result.text).not.toContain('<');
 expect(result.html).toContain('<!--StartFragment-->');
});
