import { chromium, expect } from '@playwright/test';
const browser=await chromium.launch({channel:'msedge',headless:true});
const page=await browser.newPage();
await page.goto('http://127.0.0.1:5173/');
for(const engine of ['SQL','Oracle']) {
 await page.getByRole('button',{name:`Evidencias ${engine}`}).click();
 await page.getByRole('button',{name:'Crear primera evidencia'}).click();
 await page.getByLabel('Descripción de la validación').fill('Evidencia que se elimina');
 await page.getByRole('button',{name:'Agregar evidencia',exact:true}).click();
 await page.getByLabel('Descripción de la validación').nth(1).fill('Evidencia que se conserva');
 await page.getByRole('button',{name:'Contraer evidencia 1',exact:true}).click();
 await page.getByRole('button',{name:'Eliminar evidencia',exact:true}).first().click();
 const dialog=page.getByRole('dialog',{name:'Eliminar evidencia',exact:true});
 await expect(dialog).toContainText(engine);
 await expect(dialog).toContainText('Evidencia que se elimina');
 await dialog.getByRole('button',{name:'Cancelar'}).click();
 await expect(page.locator('.evidence-card')).toHaveCount(2);
 await page.getByRole('button',{name:'Eliminar evidencia',exact:true}).first().click();
 await dialog.getByRole('button',{name:'Eliminar evidencia',exact:true}).click();
 await expect(page.locator('.evidence-card')).toHaveCount(1);
 await expect(page.getByLabel('Descripción de la validación')).toHaveValue('Evidencia que se conserva');
 await expect(page.locator('.save-state')).toContainText('Cambios sin guardar');
}
console.log('Cancelación y eliminación de evidencias SQL/Oracle verificadas sin guardar ni modificar borradores reales.');
await browser.close();
