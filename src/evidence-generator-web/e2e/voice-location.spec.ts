import { test, expect } from '@playwright/test';

async function mockSpeech(page: import('@playwright/test').Page) {
  await page.addInitScript(() => {
    const target = window as any;
    target.recognitions = [];
    target.SpeechRecognition = class {
      lang = ''; continuous = false; interimResults = false;
      onstart?: () => void; onaudiostart?: () => void; onend?: () => void;
      onresult?: (event:any) => void; onerror?: (event:any) => void;
      aborted = false;
      constructor() { target.recognitions.push(this); }
      start() { this.onstart?.(); this.onaudiostart?.(); }
      stop() { this.onend?.(); }
      abort() { this.aborted = true; this.onend?.(); }
    };
  });
}
test('dictado continuo: indicador, resultados parciales, sin duplicados y texto pendiente', async ({page}) => {
  await mockSpeech(page); await page.goto('/');
  await page.getByRole('button',{name:'Dictar',exact:true}).click();
  await expect(page.locator('.voice-status')).toContainText('Micrófono activo');
  expect(await page.evaluate(() => {
    const speech = (window as any).recognitions[0]; return [speech.continuous, speech.interimResults, speech.lang];
  })).toEqual([true,true,'es-CO']);
  await page.evaluate(() => (window as any).recognitions[0].onresult({resultIndex:0,results:[{isFinal:false,0:{transcript:'Primera frase'}}]}));
  await expect(page.locator('.voice-partial')).toContainText('Primera frase');
  await expect(page.getByLabel('Descripción del requerimiento *')).toHaveValue('');
  await page.evaluate(() => {
    const speech=(window as any).recognitions[0];
    speech.onresult({resultIndex:0,results:[{isFinal:true,0:{transcript:'Primera frase'}}]});
    speech.onresult({resultIndex:0,results:[{isFinal:true,0:{transcript:'Primera frase'}},{isFinal:true,0:{transcript:'Segunda frase'}}]});
    speech.onresult({resultIndex:2,results:[{isFinal:true,0:{transcript:'Primera frase'}},{isFinal:true,0:{transcript:'Segunda frase'}},{isFinal:false,0:{transcript:'Final pendiente'}}]});
  });
  await expect(page.getByLabel('Descripción del requerimiento *')).toHaveValue('Primera frase Segunda frase');
  await page.getByRole('button',{name:'Detener',exact:true}).click();
  await expect(page.getByLabel('Texto pendiente de revisar')).toHaveValue('Final pendiente');
  await page.getByRole('button',{name:'Usar texto pendiente'}).click();
  await expect(page.getByLabel('Descripción del requerimiento *')).toHaveValue('Primera frase Segunda frase Final pendiente');
  await page.getByRole('button',{name:'Dictar',exact:true}).click();
  await page.screenshot({path:'../../work/voice-indicator.png',fullPage:true});
  await page.getByRole('button',{name:'Evidencias SQL'}).click();
  expect(await page.evaluate(()=>(window as any).recognitions.at(-1).aborted)).toBe(true);
});

test('no-speech reintenta con límite, los permisos no reintentan y no se pierde texto por longitud', async ({page}) => {
  await mockSpeech(page); await page.goto('/');
  await page.getByRole('button',{name:'Dictar',exact:true}).click();
  for(let i=0;i<3;i++) {
    await expect.poll(()=>page.evaluate(()=>(window as any).recognitions.length)).toBe(i+1);
    await page.evaluate(()=>{const speech=(window as any).recognitions.at(-1); speech.onerror({error:'no-speech'}); speech.onend();});
  }
  await expect(page.getByRole('button',{name:'Dictar',exact:true})).toBeVisible();
  await expect(page.locator('.notification.info')).toContainText('varios intentos');
  await page.getByRole('button',{name:'Dictar',exact:true}).click();
  await page.evaluate(()=>(window as any).recognitions.at(-1).onerror({error:'not-allowed'}));
  await expect(page.locator('.notification.error')).toContainText('Permite el micrófono');
  await page.getByLabel('Descripción del requerimiento *').fill('x'.repeat(3990));
  await page.getByRole('button',{name:'Dictar',exact:true}).click();
  await page.evaluate(()=>(window as any).recognitions.at(-1).onresult({resultIndex:0,results:[{isFinal:true,0:{transcript:'Texto adicional que supera el límite'}}]}));
  await expect(page.getByLabel('Descripción del requerimiento *')).toHaveValue('x'.repeat(3990)+' Texto adi');
  await expect(page.getByLabel('Texto pendiente de revisar')).toHaveValue('cional que supera el límite');
});

test('abrir ubicación usa el requerimiento y conserva un único archivo sin descargar otra copia', async ({page}) => {
  let opened = '';
  let downloads = 0;
  page.on('download',()=>downloads++);
  await page.route('**/api/exports/location?*',route=>route.fulfill({json:{requirement:'MD LOCATION',path:'C:\\Exports\\MD LOCATION.xlsx',savedAt:new Date().toISOString()}}));
  await page.route('**/api/exports/open-location',route=>{opened=route.request().postDataJSON().requirement;return route.fulfill({status:204});});
  await page.goto('/');
  await page.getByLabel('Requerimiento *',{exact:true}).fill('MD LOCATION');
  await page.locator('.storage-detail summary').click();
  await page.getByRole('button',{name:'Abrir ubicación del archivo'}).click();
  await expect.poll(()=>opened).toBe('MD LOCATION');
  await expect(page.locator('.notification.success')).toContainText('Explorador');
  expect(downloads).toBe(0);
});
