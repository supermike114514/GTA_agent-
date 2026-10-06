// Download ScriptHookV release zip (needs a browser-ish UA + referer; the site
// serves an HTML page to bare clients).
import { writeFileSync } from 'node:fs';

const target = 'H:/deepseek/GTA5Buddy/_runtime/ScriptHookV.zip';
const urls = [
  'https://www.dev-c.com/files/ScriptHookV_3889.0_1158.13.zip',
  'https://dev-c.com/files/ScriptHookV_3889.0_1158.13.zip',
];

let saved = false;
for (const u of urls) {
  try {
    const r = await fetch(u, {
      redirect: 'follow',
      headers: {
        'user-agent':
          'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126 Safari/537.36',
        referer: 'https://www.dev-c.com/gtav/scripthookv/',
        accept: '*/*',
      },
    });
    const b = Buffer.from(await r.arrayBuffer());
    const magic = b.subarray(0, 2).toString('hex');
    console.log(`${u} -> ${r.status} ${r.headers.get('content-type')} bytes=${b.length} magic=${magic}`);
    if (magic === '504b') {
      writeFileSync(target, b);
      console.log(`SAVED ${b.length} bytes -> ${target}`);
      saved = true;
      break;
    }
  } catch (e) {
    console.log(`${u} -> ERR ${e.message}`);
  }
}
process.exit(saved ? 0 : 1);
