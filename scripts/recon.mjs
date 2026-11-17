import { chromium } from 'playwright';
import fs from 'node:fs';
import path from 'node:path';
const root = process.cwd();
const cache = path.join(process.env.LOCALAPPDATA, 'ms-playwright');
const folder = fs.readdirSync(cache).find(x => /^chromium-\d+$/.test(x));
const executablePath = path.join(cache,folder,'chrome-win64','chrome.exe');
const browser = await chromium.launch({headless:true,executablePath});
const captures=[];
for (const [label,url,widths] of [
  ['anpha-original','https://anphahealth.vn/',[1440,768,390]],
  ['chemlabs','https://7oroof.com/demos/chemlabs/index.html',[1440,768,390]],
  ['chemlabs-supplies','https://7oroof.com/demos/chemlabs/home-supllies.html',[1440]],
  ['chemlabs-modern','https://7oroof.com/demos/chemlabs/home-modern.html',[1440]]
]) {
 for(const width of widths){
  const page=await browser.newPage({viewport:{width,height:1000},ignoreHTTPSErrors:true});
  const errors=[];page.on('pageerror',e=>errors.push(e.message));
  await page.goto(url,{waitUntil:'domcontentloaded',timeout:60000});
  await page.waitForTimeout(1800);
  for(let y=0;y<await page.evaluate(()=>document.body.scrollHeight);y+=800){await page.evaluate(y=>window.scrollTo(0,y),y);await page.waitForTimeout(80);}
  await page.evaluate(()=>window.scrollTo(0,0));
  await page.screenshot({path:`RECON/screenshots/${label}-${width}.png`,fullPage:true});
  const signals=await page.evaluate(()=>({title:document.title,height:document.body.scrollHeight,canvas:document.querySelectorAll('canvas').length,fonts:[...new Set([...document.querySelectorAll('h1,h2,h3,p,a')].map(x=>getComputedStyle(x).fontFamily))],colors:[...new Set([...document.querySelectorAll('h1,h2,h3,.btn,header,nav')].map(x=>getComputedStyle(x).color))],css:[...document.styleSheets].map(x=>x.href),h1:[...document.querySelectorAll('h1,h2')].slice(0,5).map(x=>x.innerText)}));
  captures.push({label,url,width,signals,errors});
  console.log(label,width,signals.title);await page.close();
 }
}
fs.writeFileSync(path.join(root,'RECON/original-recon.json'),JSON.stringify(captures,null,2));
await browser.close();
