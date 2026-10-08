import { test, expect } from '@playwright/test';
import path from 'node:path';
import fs from 'node:fs/promises';

test('notificaciones: pausa con cursor y errores hasta cierre manual', async ({ page }) => {
  await page.goto('/');
  await expect(page.getByLabel('Requerimiento *',{exact:true})).toBeVisible();
  await page.clock.install();
  await page.getByLabel('Requerimiento *',{exact:true}).fill('MD TOAST');
  await page.getByRole('button',{name:'Guardar borrador',exact:true}).click();
  const success=page.locator('.notification.success').last();
  await expect(success).toContainText('Borrador guardado');
  await success.hover();
  await page.clock.runFor(6000);
  await expect(success).toBeVisible();
  await page.mouse.move(0,0);
  await page.clock.runFor(5001);
  await expect(page.locator('.notification.success')).toHaveCount(0);
  await page.getByLabel('Requerimiento *',{exact:true}).fill('../inválido');
  await page.getByRole('button',{name:'Guardar borrador',exact:true}).click();
  const error=page.locator('.notification.error');
  await expect(error).toBeVisible();
  await page.mouse.move(0,0);
  await page.clock.runFor(15000);
  await expect(error).toBeVisible();
  await error.getByRole('button',{name:'Cerrar notificación'}).click();
  await expect(error).toHaveCount(0);
});

test('perfil, contactos, ambientes, guardado sin duplicados y vista previa de solo lectura', async ({ page, request }) => {
  const original = await (await request.get('/api/settings')).json();
  await request.put('/api/settings', {data:{...original, name:'Perfil QA', contacts:[], environmentUrls:[], connections:[]}});
  const folder = path.resolve('../../work/workspace-e2e', String(Date.now()));
  const failures: string[] = []; page.on('pageerror', e => failures.push(e.message));
  try {
    await page.goto('/');
    await page.getByRole('button', { name: /Perfil y configuración/ }).click();
    await page.getByLabel('Nombre del responsable').fill('Responsable de prueba');
    const photo = await page.evaluate(() => { const canvas=document.createElement('canvas'); canvas.width=80; canvas.height=80; const context=canvas.getContext('2d')!; context.fillStyle='#5B3FA8'; context.fillRect(0,0,80,80); return canvas.toDataURL('image/png').split(',')[1]!; });
    await page.locator('input[type=file]').first().setInputFiles({ name:'photo.png', mimeType:'image/png', buffer:Buffer.from(photo,'base64') });
    await expect(page.getByAltText('Foto de perfil')).toBeVisible();
    await page.getByLabel('Carpeta de almacenamiento').fill(folder);
    await page.getByLabel('Al guardar Excel').selectOption('ask');
    await page.getByRole('button', { name: 'Agregar contacto' }).click();
    const row = page.locator('.settings-row').last();
    await row.getByLabel('Nombre', { exact: true }).fill('Ana pruebas');
    await row.getByLabel('Correo electrónico').fill('ana@example.com');
    await page.getByRole('button', { name: 'Agregar contacto' }).click();
    await page.locator('.settings-row').last().getByLabel('Nombre', {exact: true}).fill('Luis QA');
    await page.locator('.settings-row').last().getByLabel('Correo electrónico').fill('luis@example.com');
    for (const [index,url] of ['https://desarrollo.oasiscom.com','https://pruebas.oasiscom.com'].entries()) {
      await page.getByRole('button',{name:'Agregar URL',exact:true}).click();
      await page.getByLabel(`URL ${index+1}`,{exact:true}).fill(url);
    }
    for (const [index,connection] of ['OasisComTest','Oracle Test'].entries()) {
      await page.getByRole('button',{name:'Agregar conexión',exact:true}).click();
      await page.getByLabel(`Conexión ${index+1}`,{exact:true}).fill(connection);
    }
    await expect(page.getByLabel('Nombre del ambiente')).toHaveCount(0);
    await expect(page.getByLabel('Motor',{exact:true})).toHaveCount(0);
    await page.screenshot({path:'../../work/workspace-settings.png',fullPage:true});
    await page.getByRole('button',{name:'Guardar configuración'}).click();
    await expect(page.locator('.notification.success').last()).toContainText('Configuración guardada');
    await page.reload();
    await expect(page.getByLabel('Elaborado por *')).toHaveValue('Responsable de prueba');
    await expect(page.getByAltText('Tu foto de perfil')).toBeVisible();
    await page.getByLabel('Requerimiento *',{exact:true}).fill('MD WORKSPACE');
    await page.getByLabel('Descripción del requerimiento *').fill('Guardado local');
    for (const engine of ['SQL', 'Oracle']) {
      await page.getByRole('button',{name:`Evidencias ${engine}`}).click();
      await page.locator('.p-select-dropdown').nth(0).click();
      await page.getByRole('option',{name:'https://pruebas.oasiscom.com',exact:true}).click();
      await page.locator('.p-select-dropdown').nth(1).click();
      await page.getByRole('option',{name:'Oracle Test',exact:true}).click();
      await expect(page.getByLabel('URL / Sitio',{exact:true})).toHaveValue('https://pruebas.oasiscom.com');
      await expect(page.getByLabel('Conexión / Ambiente',{exact:true})).toHaveValue('Oracle Test');
      await page.locator('.p-select-dropdown').nth(0).click();
      await page.getByRole('option',{name:'https://desarrollo.oasiscom.com',exact:true}).click();
      await expect(page.getByLabel('Conexión / Ambiente',{exact:true})).toHaveValue('Oracle Test');
    }
    await page.getByRole('button',{name:'Evidencias SQL'}).click();
    for (let i=0; i<5; i++) {
      await page.getByRole('button',{name:'Agregar evidencia',exact:true}).click();
      await page.getByLabel('Descripción de la validación').last().fill(`Validación ${i}`);
    }
    await page.evaluate(() => window.scrollTo(0, document.body.scrollHeight));
    const button = await page.getByRole('button',{name:'Agregar evidencia',exact:true}).boundingBox();
    expect(button!.y).toBeGreaterThan(0); expect(button!.y + button!.height).toBeLessThan(1050);
    await page.getByRole('button',{name:'Guardar Excel',exact:true}).click();
    await page.getByLabel('Carpeta de destino (ruta completa)').fill(folder);
    await page.getByRole('button',{name:'Guardar en esta carpeta'}).click();
    await expect(page.locator('.message.success').last()).toContainText(folder);
    await page.getByLabel('Modo de guardado del Excel').selectOption('configured');
    await page.getByRole('button',{name:'Guardar Excel',exact:true}).click();
    await expect(page.locator('.message.success').last()).toContainText(folder);
    expect((await fs.readdir(folder)).filter(f=>f.endsWith('.xlsx'))).toHaveLength(1);
    await page.locator('.storage-detail summary').click();
    await expect(page.locator('.storage-detail')).toContainText(path.join(folder,'Pruebas Unitarias - MD WORKSPACE.xlsx'));
    const locations = await page.locator('.storage-options p').evaluateAll(elements => elements.map(e => ({top:e.getBoundingClientRect().top,bottom:e.getBoundingClientRect().bottom})));
    expect(locations[1]!.top).toBeGreaterThan(locations[0]!.bottom);
    expect(locations[2]!.top).toBeGreaterThan(locations[1]!.bottom);
    await page.getByRole('button',{name:'Preparar correo'}).click();
    await page.locator('.recipients-panel summary').click();
    await page.locator('.recipients-panel .p-multiselect').nth(0).click();
    await page.getByRole('searchbox').fill('Ana');
    await page.getByRole('option', {name:'Ana pruebas · ana@example.com'}).click();
    await page.keyboard.press('Escape');
    await expect(page.locator('.p-multiselect-overlay')).toHaveCount(0);
    await page.locator('.recipients-panel .p-multiselect').nth(0).click();
    await page.getByRole('searchbox').fill('Luis');
    await page.getByRole('option', {name:'Luis QA · luis@example.com'}).click();
    await page.keyboard.press('Escape');
    await expect(page.locator('.p-multiselect-overlay')).toHaveCount(0);
    await page.locator('.recipients-panel .p-multiselect').nth(1).click();
    await page.getByRole('option', {name:'Luis QA · luis@example.com'}).click();
    await page.keyboard.press('Escape');
    await expect(page.locator('.p-multiselect-overlay')).toHaveCount(0);
    await page.getByRole('button',{name:'Vista previa del correo',exact:true}).click();
    await expect(page.locator('.ql-editor')).not.toBeVisible();
    await expect(page.locator('.mail-paper')).toContainText('ana@example.com; luis@example.com');
    await expect(page.locator('.mail-paper [contenteditable=true]')).toHaveCount(0);
    await page.screenshot({path:'../../work/workspace-mail.png',fullPage:true});
    await page.setViewportSize({width:390,height:844});
    await expect(page.getByRole('button',{name:/Perfil y configuración/})).toBeVisible();
    expect(await page.evaluate(()=>document.documentElement.scrollWidth <= innerWidth)).toBe(true);
    expect(failures).toEqual([]);
  } finally {
    const current = await request.get('/api/settings').then(r=>r.json()).catch(()=>null);
    if (current) await request.put('/api/settings',{data:{...original, revision:current.revision, name:original.name || 'Perfil QA'}});
  }
});
