import { test, expect } from '@playwright/test';
test('eliminar evidencias SQL y Oracle con confirmación y persistencia', async ({page}) => {
  await page.goto('/');
  const name='MD EVIDENCIA '+Date.now();
  await page.getByLabel('Requerimiento *',{exact:true}).fill(name);
  for (const engine of ['SQL','Oracle']) {
    await page.getByRole('button',{name:`Evidencias ${engine}`}).click();
    await page.getByRole('button',{name:'Crear primera evidencia'}).click();
    await page.getByLabel('Descripción de la validación').fill(`Eliminar ${engine}`);
    await page.getByRole('button',{name:'Agregar evidencia',exact:true}).click();
    await page.getByLabel('Descripción de la validación').nth(1).fill(`Conservar ${engine}`);
    await page.getByRole('button',{name:'Contraer evidencia 1',exact:true}).click();
    await page.locator('.evidence-card').first().getByRole('button',{name:'Eliminar evidencia',exact:true}).click();
    const dialog=page.getByRole('dialog',{name:'Eliminar evidencia',exact:true});
    await expect(dialog).toContainText(`Eliminar ${engine}`);
    await dialog.getByRole('button',{name:'Cancelar'}).click();
    await expect(page.locator('.evidence-card')).toHaveCount(2);
    await page.locator('.evidence-card').first().getByRole('button',{name:'Eliminar evidencia',exact:true}).click();
    await dialog.getByRole('button',{name:'Eliminar evidencia',exact:true}).click();
    await expect(page.locator('.evidence-card')).toHaveCount(1);
    await expect(page.getByLabel('Descripción de la validación')).toHaveValue(`Conservar ${engine}`);
  }
  await page.getByRole('button',{name:'Guardar borrador'}).click();
  await expect(page.locator('.message.success')).toContainText('Borrador guardado');
  await page.reload();
  await page.getByRole('button',{name:'Borradores locales'}).click();
  await page.getByLabel('Buscar borradores').fill(name);
  await page.getByRole('button',{name:'Buscar',exact:true}).click();
  await page.locator('.history-item').first().click();
  for(const engine of ['SQL','Oracle']) {
    await page.getByRole('button',{name:`Evidencias ${engine}`}).click();
    await expect(page.locator('.evidence-card')).toHaveCount(1);
    await expect(page.getByLabel('Descripción de la validación')).toHaveValue(`Conservar ${engine}`);
  }
});
