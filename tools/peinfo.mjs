// peinfo.mjs - read the CLR metadata version string out of a managed assembly
// without loading it. Tells us whether SHVDN targets .NET Framework (v4.0.30319)
// or modern .NET (v8.0 / v9.0 ...).
import { readFileSync } from 'node:fs';

const file = process.argv[2];
if (!file) {
  console.error('usage: node peinfo.mjs <assembly.dll>');
  process.exit(2);
}
const b = readFileSync(file);

const peOff = b.readUInt32LE(0x3c);
const sig = b.readUInt32LE(peOff);
if (sig !== 0x00004550) throw new Error('not a PE file');
const coff = peOff + 4;
const numSections = b.readUInt16LE(coff + 2);
const optSize = b.readUInt16LE(coff + 16);
const optOff = coff + 20;
const magic = b.readUInt16LE(optOff);
const pe32plus = magic === 0x20b;
const ddOff = optOff + (pe32plus ? 112 : 96);
const clrRva = b.readUInt32LE(ddOff + 14 * 8);
if (clrRva === 0) throw new Error('not a managed assembly (no CLR directory)');

const sections = [];
const secOff = optOff + optSize;
for (let i = 0; i < numSections; i++) {
  const s = secOff + i * 40;
  sections.push({
    name: b.toString('ascii', s, s + 8).replace(/\0/g, ''),
    vaddr: b.readUInt32LE(s + 12),
    vsize: b.readUInt32LE(s + 8),
    raw: b.readUInt32LE(s + 20),
  });
}
const rva2off = (rva) => {
  for (const s of sections) {
    if (rva >= s.vaddr && rva < s.vaddr + Math.max(s.vsize, 0x1000)) return s.raw + (rva - s.vaddr);
  }
  return -1;
};

const clrOff = rva2off(clrRva);
const metaRva = b.readUInt32LE(clrOff + 8);
const metaOff = rva2off(metaRva);
const verLen = b.readUInt32LE(metaOff + 12);
const ver = b.toString('utf8', metaOff + 16, metaOff + 16 + verLen).replace(/\0/g, '');
const flags = b.readUInt32LE(clrOff + 16); // COMIMAGE_FLAGS_ILONLY etc.

console.log(JSON.stringify({
  file,
  size: b.length,
  pe: pe32plus ? 'PE32+' : 'PE32',
  runtimeVersion: ver,
  clrFlags: '0x' + flags.toString(16),
  ilOnly: (flags & 1) === 1,
  requires32Bit: (flags & 2) === 2,
}, null, 2));
