// Fictional local fixture only. Creates, approves and cancels leave. Never target live data.
const p=require('puppeteer-core'),a=require('assert/strict'),fs=require('fs');
const base=process.env.PEOPLEHUB_TEST_URL||'http://localhost:5123',out=process.env.PEOPLEHUB_TEST_OUTPUT||'/tmp/peoplehub-leave-evidence',date=process.env.PEOPLEHUB_TEST_LEAVE_DATE||'2026-12-23';
a.ok(['localhost','127.0.0.1'].includes(new URL(base).hostname),'Local fixture required');fs.mkdirSync(out,{recursive:true});
(async()=>{const b=await p.launch({executablePath:'/usr/bin/google-chrome',args:['--no-sandbox']});const checks=[];try{
async function login(role){const g=await(await b.createBrowserContext()).newPage();await g.setViewport({width:1440,height:1100});await g.goto(base+'/Account/Login');await g.type('[name=email]',role+'@peoplehub.local');await g.type('[name=password]',role==='hr'?'Admin123!':'Demo123!');await Promise.all([g.waitForNavigation(),g.click('form button')]);a.equal(new URL(g.url()).pathname,'/');return g;}
async function submit(g,s,d={}){for(const[k,v]of Object.entries(d))await g.$eval(s+' [name="'+k+'"]',(e,v)=>e.value=v,String(v));a.ok(await g.$eval(s,e=>e.checkValidity()),'Valid form required');await Promise.all([g.waitForNavigation(),g.$eval(s+' button',x=>x.click())]);a.equal(await g.$('.flash.err'),null);}
function ok(s){checks.push(s);console.log('PASS',s)}
const e=await login('employee');await e.goto(base+'/Leave');const remaining=await e.$eval('.kpi .val',x=>x.innerText);
await submit(e,'form[action="/Leave/Request"]',{start:date,end:date,reason:'Fictional leave regression'});
const id=await e.evaluate(date=>[...document.querySelectorAll('tbody tr')].find(x=>x.innerText.includes(date))?.querySelector('input[name=id]')?.value,date);a.ok(id);ok('employee submits working-day leave request');
a.equal(await e.$eval('.kpi .val',x=>x.innerText),remaining);ok('pending request leaves balance unchanged');
const m=await login('manager');await m.goto(base+'/Requests?scope=inbox');const approve='form[action="/Requests/Approve"]:has([name=kind][value=Leave]):has([name=id][value="'+id+'"])';
a.ok(await m.$(approve));a.equal(await m.$eval(approve+' [name=comment]',x=>x.required),true);a.equal(await m.$eval(approve,x=>x.checkValidity()),false);ok('configured approval comment required in browser');
await submit(m,approve,{comment:'Fictional independent manager review'});await e.goto(base+'/Leave');a.notEqual(await e.$eval('.kpi .val',x=>x.innerText),remaining);ok('configured final manager approval deducts balance');
await submit(e,'form[action="/Leave/CancelApproved"]:has([name=id][value="'+id+'"])',{reason:'Fictional future leave cancellation'});a.equal(await e.$eval('.kpi .val',x=>x.innerText),remaining);ok('reasoned approved cancellation restores balance');
a.equal(await e.$('form[action="/Leave/CancelApproved"]:has([name=id][value="'+id+'"])'),null);a.ok((await e.$eval('body',x=>x.innerText)).includes('Cancellation'));ok('cancelled request has no repeat cancellation control and ledger records reversal');
for(const lang of ['en','ar']){await e.goto(base+'/Account/Lang?to='+lang+'&returnUrl=/Leave');await e.screenshot({path:out+'/leave-cancelled-'+lang+'.png',fullPage:true});}
fs.writeFileSync(out+'/leave-checks.json',JSON.stringify({at:new Date().toISOString(),id,date,checks},null,2));
}finally{await b.close()}})().catch(e=>{console.error(e);process.exitCode=1});
