import { test, expect } from '@playwright/test';
test('cabecera fija, destinatarios plegables, cursiva y sección informativa', async ({page}) => {
 await page.goto('/');
 await expect(page.locator('html')).toHaveCSS('font-size','12px');
 await page.getByRole('button',{name:'Preparar correo'}).click();
 await expect(page.getByLabel('Nombre para el saludo')).not.toBeVisible();
 await page.locator('.recipients-panel summary').click();
 await page.getByLabel('Nombre para el saludo').fill('Prueba');
 await page.locator('.recipients-panel summary').click();
 const editor=page.locator('.ql-editor');
 await editor.fill('Texto en cursiva'); await editor.press('Control+a');
 await page.getByRole('button',{name:'Cursiva',exact:true}).click();
 await expect(editor.locator('em')).toHaveCSS('font-style','italic');
 await expect(editor).toHaveCSS('font-synthesis','weight style');
 await expect(page.locator('.email-preview em').filter({hasText:'Texto en cursiva'})).toHaveCSS('font-style','italic');
 await page.getByRole('button',{name:'Cursiva',exact:true}).click();
 await expect(editor.locator('em')).toHaveCount(0);
 await page.evaluate(()=>window.scrollTo(0,document.body.scrollHeight));
 expect((await page.locator('.document-bar').boundingBox())!.y).toBe(0);
 await page.getByRole('button',{name:'Acerca del generador'}).click();
 await expect(page.getByRole('heading',{name:'¿Para qué sirve Evidence Generator?'})).toBeVisible();
 await expect(page.locator('.technology-list')).toContainText('SQLite');
 await page.screenshot({path:'../../work/about-header.png',fullPage:true});
 await page.setViewportSize({width:390,height:844});
 expect(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth)).toBe(true);
 await page.screenshot({path:'../../work/about-mobile.png',fullPage:true});
});
test('rechaza arrastrar múltiples imágenes sin cargar ninguna', async ({page}) => {
 await page.goto('/'); await page.getByRole('button',{name:'Evidencias SQL'}).click();
 await page.getByRole('button',{name:'Crear primera evidencia'}).click();
 await page.locator('.evidence-card .image-input').evaluate(el=>{
   const data=new DataTransfer(); data.items.add(new File(['x'],'a.png',{type:'image/png'})); data.items.add(new File(['y'],'b.png',{type:'image/png'}));
   el.dispatchEvent(new DragEvent('drop',{bubbles:true,cancelable:true,dataTransfer:data}));
 });
 await expect(page.getByRole('alert')).toContainText('una imagen por evidencia');
 await expect(page.locator('.capture img')).toHaveCount(0);
});
