const urls=[
 'https://www.dev-c.com/files/ScriptHookV_3889.0_1158.13.zip',
 'http://www.dev-c.com/files/ScriptHookV_3889.0_1158.13.zip',
 'https://dev-c.com/files/ScriptHookV_3889.0_1158.13.zip',
];
for(const u of urls){
  try{
    const r=await fetch(u,{redirect:'follow',headers:{'user-agent':'Mozilla/5.0 (Windows NT 10.0; Win64; x64)','referer':'https://www.dev-c.com/gtav/scripthookv/','accept':'*/*'}});
    const b=Buffer.from(await r.arrayBuffer());
    console.log(u,'->',r.status,r.headers.get('content-type'),r.headers.get('content-length'),'bytes=',b.length,'magic=',b.slice(0,2).toString('hex'));
    if(b.slice(0,2).toString('hex')==='504b'){ require('fs').writeFileSync('H:/deepseek/GTA5Buddy/_runtime/ScriptHookV.zip',b); console.log('SAVED zip'); break; }
  }catch(e){ console.log(u,'-> ERR',e.message); }
}
