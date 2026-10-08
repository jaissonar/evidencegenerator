import { it, expect } from 'vitest';
import { newDocument, splitLegacyEvidenceImages } from './document';
it('separates legacy captures without dropping descriptions or images and is idempotent', () => {
 const doc = newDocument(); const images = [{dataUrl:'one'},{dataUrl:'two'},{dataUrl:'three'}];
 doc.sql.items=[{id:'legacy',description:'Descripción original',images}];
 expect(splitLegacyEvidenceImages(doc)).toBe(true);
 expect(doc.sql.items).toHaveLength(3);
 expect(doc.sql.items.map(e=>e.images[0])).toEqual(images);
 expect(doc.sql.items.every(e=>e.description==='Descripción original')).toBe(true);
 expect(new Set(doc.sql.items.map(e=>e.id)).size).toBe(3);
 expect(splitLegacyEvidenceImages(doc)).toBe(false);
});
