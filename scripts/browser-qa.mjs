import {chromium} from 'playwright';
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
const base=process.env.DEMO_URL||'http://127.0.0.1:5080';
const cache=path.join(process.env.LOCALAPPDATA||'', 'ms-playwright');
const installed=fs.existsSync(cache)?fs.readdirSync(cache).find(x=>/^chromium-\d+$/.test(x)):null;
const executablePath=installed?path.join(cache,installed,'chrome-win64','chrome.exe'):undefined;
const browser=await chromium.launch({headless:true,executablePath});
const pages=[['home','/'],['about','/gioi-thieu'],['products','/san-pham'],['product-detail','/san-pham/fresenious-medicalcare-fmc'],['news','/tin-tuc'],['article','/tin-tuc/tim-hieu-ve-tui-nang-nguc'],['activities','/hoat-dong'],['careers','/tuyen-dung'],['contact','/lien-he'],['proposal','/de-xuat']];
const results=[],errors=[],networkErrors=[],external=[];
try{
for(const width of [1440,768,390]){
 const page=await browser.newPage({viewport:{width,height:1000},reducedMotion:'reduce'});
 page.on('pageerror',e=>errors.push({width,url:page.url(),message:e.message}));
 page.on('console',m=>{if(m.type()==='error')errors.push({width,url:page.url(),message:m.text()});});
 page.on('response',r=>{if(r.status()>=400)networkErrors.push({url:r.url(),status:r.status()});});
 page.on('request',r=>{if(!r.url().startsWith(base)&&!r.url().startsWith('data:'))external.push(r.url());});
 for(const [name,route] of pages){
  const response=await page.goto(base+route,{waitUntil:'networkidle'});
  await page.evaluate(()=>document.fonts.ready);await page.waitForTimeout(200);
  for(let y=0;y<await page.evaluate(()=>document.body.scrollHeight);y+=950){await page.evaluate(y=>window.scrollTo(0,y),y);await page.waitForTimeout(35);}
  await page.evaluate(()=>window.scrollTo(0,0));
  const signals=await page.evaluate(()=>({title:document.title,h1:[...document.querySelectorAll('h1')].map(x=>x.textContent),scrollWidth:document.documentElement.scrollWidth,width:innerWidth,brokenImages:[...document.images].filter(x=>x.complete&&x.naturalWidth===0).map(x=>x.currentSrc),errorBanner:getComputedStyle(document.querySelector('#blazor-error-ui')).display}));
  assert.equal(response.status(),200,route);assert.equal(signals.h1.length,1,route+' has one H1');assert.ok(signals.scrollWidth<=width+1,route+' horizontal overflow '+signals.scrollWidth);assert.equal(signals.brokenImages.length,0,JSON.stringify(signals.brokenImages));assert.equal(signals.errorBanner,'none');
  const file=`RECON/screenshots/clone-${name}-${width}.png`;
  await page.screenshot({path:file,fullPage:true});
  if(name==='home'||name==='products'||name==='contact')await page.screenshot({path:`RECON/screenshots/clone-${name}-${width}-top.png`});
  results.push({route,width,status:response.status(),signals,screenshot:file});
  console.log('PASS',width,route);
 }
 await page.close();
}
const page=await browser.newPage({viewport:{width:1440,height:1000}});
await page.goto(base+'/san-pham',{waitUntil:'networkidle'});
assert.equal(await page.locator('.catalog-products .product-card').count(),8);
await page.getByRole('button',{name:'Cấy ghép ngực'}).click();
await page.waitForTimeout(200);assert.equal(await page.locator('.catalog-products .product-card').count(),5);
await page.getByRole('button',{name:'Tất cả sản phẩm'}).click();
await page.getByRole('textbox',{name:'Tìm sản phẩm'}).fill('Medtronic');
await page.waitForTimeout(200);assert.equal(await page.locator('.catalog-products .product-card').count(),1);
await page.getByRole('textbox',{name:'Tìm sản phẩm'}).fill('no-matching-product');
await page.waitForTimeout(200);assert.equal(await page.locator('.catalog-products .product-card').count(),0);
await page.getByRole('button',{name:'Xem tất cả sản phẩm'}).click();
await page.waitForTimeout(200);assert.equal(await page.locator('.catalog-products .product-card').count(),8);
await page.goto(base+'/san-pham/c-i-d',{waitUntil:'networkidle'});await page.getByRole('tab',{name:'Hình ảnh sản phẩm'}).click();await page.locator('.image-gallery').waitFor();assert.ok(await page.locator('.image-gallery img').count()>0);
await page.goto(base+'/lien-he?sanpham=c-i-d',{waitUntil:'networkidle'});
assert.equal(await page.locator('select').inputValue(),'Can thiệp mạch');
await page.getByRole('button',{name:'Xem trước yêu cầu'}).click();await page.waitForTimeout(150);assert.equal(await page.locator('.validation-message').count(),3);
await page.getByPlaceholder('Họ và tên của bạn').fill('Khách xem demo');await page.getByPlaceholder('email@congty.vn').fill('demo@example.com');await page.getByPlaceholder('Chia sẻ nhu cầu của bạn...').fill('Tôi muốn tham khảo danh mục thiết bị.');await page.getByRole('button',{name:'Xem trước yêu cầu'}).click();await page.getByRole('status').waitFor();assert.ok((await page.getByRole('status').textContent()).includes('chưa được gửi'));await page.getByRole('button',{name:'Chỉnh sửa yêu cầu'}).click();assert.equal(await page.getByPlaceholder('Họ và tên của bạn').inputValue(),'Khách xem demo');
await page.setViewportSize({width:390,height:844});await page.goto(base+'/',{waitUntil:'networkidle'});await page.getByRole('button',{name:'Mở menu'}).click();await page.locator('nav.main-nav a[href="/san-pham"]').click();await page.waitForURL(base+'/san-pham');assert.equal(await page.locator('nav.main-nav').evaluate(x=>getComputedStyle(x).display),'none');
await page.close();
const archive=JSON.parse(fs.readFileSync('data/anpha-content.json','utf8'));
const context=await browser.newContext();
for(const item of archive){const response=await context.request.get(base+'/noi-dung/'+item.slug);assert.equal(response.status(),200,item.slug);const html=await response.text();assert.ok(!html.includes('An unhandled error has occurred'),item.slug);}
await context.close();
assert.equal(errors.length,0,JSON.stringify(errors));assert.equal(networkErrors.length,0,JSON.stringify(networkErrors));assert.equal(external.length,0,JSON.stringify([...new Set(external)]));
fs.writeFileSync('RECON/clone-qa.json',JSON.stringify({passed:true,captures:results,interactionTests:['category filter','search','empty-state reset','product gallery tab','contact preselection','required validation','local-only form preview','form editing','mobile menu navigation'],archiveRoutes:archive.length,errors,networkErrors,externalRequests:external},null,2));
console.log('All QA passed: 30 responsive captures, 9 interactions, '+archive.length+' archive routes.');
}catch(error){fs.writeFileSync('RECON/clone-qa.json',JSON.stringify({passed:false,error:String(error),captures:results,errors,networkErrors,externalRequests:[...new Set(external)]},null,2));throw error;}finally{await browser.close();}
