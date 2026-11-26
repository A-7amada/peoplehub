// Local fictional fixture only. Checks the sidebar-linked pages of each role at a 390px phone width:
// no page-level horizontal scroll and no failed response. Layout audit, not workflow acceptance.
const p=require('puppeteer-core'),fs=require('fs');
const base=process.env.PEOPLEHUB_TEST_URL||'http://localhost:5123';
if(!['localhost','127.0.0.1'].includes(new URL(base).hostname))throw new Error('local only');
const roles=[['hr@peoplehub.local','Admin123!'],['employee@peoplehub.local','Demo123!'],['manager@peoplehub.local','Demo123!'],['payroll@peoplehub.local','Demo123!']];
(async()=>{const b=await p.launch({executablePath:process.env.CHROME||'/usr/bin/google-chrome',args:['--no-sandbox']});
const bad=[];let n=0;
for(const [e,pw] of roles){const c=await b.createBrowserContext();const g=await c.newPage();await g.setViewport({width:390,height:844,isMobile:true,deviceScaleFactor:2});
await g.goto(base+'/Account/Login');await g.type('input[name=email]',e);await g.type('input[name=password]',pw);await Promise.all([g.waitForNavigation(),g.click('button')]);
if(g.url().includes('Login'))throw new Error('login failed '+e);
const links=await g.$$eval('.sidebar .nav a',xs=>xs.map(x=>x.getAttribute('href')));
for(const lang of ['en','ar'])for(const path of links){await g.goto(base+'/Account/Lang?to='+lang+'&returnUrl='+encodeURIComponent(path));
const r=await g.evaluate(()=>({sw:document.documentElement.scrollWidth,cw:document.documentElement.clientWidth,t:document.title}));n++;
if(r.sw>r.cw+1)bad.push(`${e} ${lang} ${path} scrollWidth ${r.sw} > ${r.cw}`);}
await c.close();}
await b.close();console.log('checked',n);console.log(bad.length?bad.join('\n'):'NO OVERFLOW');})();
