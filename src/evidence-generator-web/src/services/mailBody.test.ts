// @vitest-environment jsdom
import { it, expect } from 'vitest';
import { sanitizeMailBody } from './mailBody';
import { mailHtml } from './mail';
import { newDocument } from '../domain/document';
it('retains formatting and converts alignment/bullets to email HTML without executable content', () => {
  const html = sanitizeMailBody('<p class="ql-align-justify" style="position:fixed;color:rgb(12, 34, 56);font-size:18pt"><strong>Texto</strong><img src="https://tracker" onerror="alert(1)"><script>alert(1)</script><a href="javascript:alert(1)">enlace</a></p><ol><li data-list="bullet">Uno</li><li data-list="ordered">Dos</li></ol>');
  expect(html).toContain('text-align: justify'); expect(html).toContain('font-size: 18pt');
  expect(html).toContain('<ul><li>Uno</li></ul>'); expect(html).toContain('<ol><li>Dos</li></ol>');
  expect(html).not.toMatch(/script|onerror|javascript:|position|<img|https:\/\/tracker/);
});
it('uses custom body while keeping fixed notices regardless of old saved values', () => {
  const doc = newDocument(); doc.mail.bodyHtml = '<p><em>Personalizado</em></p>';
  doc.mail.environmentalNotice = 'MODIFICADO'; doc.mail.confidentialityNotice = 'MODIFICADO';
  const html = mailHtml(doc);
  expect(html).toContain('<em>Personalizado</em>'); expect(html).not.toContain('MODIFICADO');
  expect(html).toContain('¡Salva un árbol'); expect(html).toContain('La información Contenida');
  expect(html.indexOf('Personalizado')).toBeLessThan(html.indexOf('alt="Firma"'));
  doc.mail.bodyHtml = ''; expect(mailHtml(doc)).not.toContain('Buen día');
  doc.mail.bodyHtml = null; expect(mailHtml(doc)).toContain('Buen día');
});
