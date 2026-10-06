// Minimal downloader that respects the local proxy from env (HTTP_PROXY).
// Usage: node fetch.mjs <url> <outPath>
import { writeFile } from 'node:fs/promises';
import process from 'node:process';

const [, , url, out] = process.argv;
if (!url || !out) {
  console.error('usage: node fetch.mjs <url> <outPath>');
  process.exit(2);
}

const proxy = process.env.HTTPS_PROXY || process.env.HTTP_PROXY || '';
process.env.NODE_USE_ENV_PROXY = '1';
process.env.NODE_TLS_REJECT_UNAUTHORIZED = '0';

async function viaFetch() {
  const res = await fetch(url, {
    redirect: 'follow',
    headers: { 'user-agent': 'Mozilla/5.0 dsh-fetch' },
  });
  if (!res.ok) throw new Error(`HTTP ${res.status} ${res.statusText}`);
  const buf = Buffer.from(await res.arrayBuffer());
  await writeFile(out, buf);
  return buf.length;
}

async function viaFetchFn() {
  const { fetch: fn } = await import('undici');
  const res = await fn(url, { redirect: 'follow' });
  if (!res.ok) throw new Error(`undici HTTP ${res.status}`);
  const buf = Buffer.from(await res.arrayBuffer());
  await writeFile(out, buf);
  return buf.length;
}

let lastErr;
for (const [name, fn] of [['undici', viaFetchFn], ['global-fetch', viaFetch]]) {
  try {
    const n = await fn();
    console.log(`OK ${name} ${n} bytes -> ${out}`);
    process.exit(0);
  } catch (e) {
    lastErr = e;
    console.error(`FAIL ${name}: ${e.message}`);
  }
}
console.error(`all transports failed (proxy=${proxy}): ${lastErr?.message}`);
process.exit(1);
