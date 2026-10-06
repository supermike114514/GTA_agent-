// apidump.mjs - list documented members of a type from an SHVDN XML doc file.
// usage: node apidump.mjs <doc.xml> <TypeName> [filterRegex]
import { readFileSync } from 'node:fs';

const [xmlPath, typeName, filter] = process.argv.slice(2);
if (!xmlPath || !typeName) {
  console.error('usage: node apidump.mjs <doc.xml> <TypeName> [filterRegex]');
  process.exit(2);
}
const xml = readFileSync(xmlPath, 'utf8');
const re = new RegExp(filter ?? '.', 'i');
const nameRe = new RegExp(`[.:]${typeName}(?:\\.|$)`);
const out = [];
const memberRe = /<member name="([^"]+)">([\s\S]*?)<\/member>/g;
let m;
while ((m = memberRe.exec(xml)) !== null) {
  const id = m[1];
  if (!nameRe.test(id) && !id.includes(typeName)) continue;
  if (!re.test(id)) continue;
  const summary = (m[2].match(/<summary>([\s\S]*?)<\/summary>/) ?? [, ''])[1]
    .replace(/<[^>]+>/g, ' ')
    .replace(/\s+/g, ' ')
    .trim();
  out.push(`${id}\n    ${summary.slice(0, 150)}`);
}
console.log(out.join('\n'));
console.log(`\n[${out.length} members matched for ${typeName}]`);
