'use strict';
// Turns a sha256sum listing (SHA256SUMS.txt) into checksums.json for the package.
// usage: node scripts/write-checksums.js SHA256SUMS.txt checksums.json
const fs = require('node:fs');

function parse(text) {
  const result = {};
  for (const line of text.split(/\r?\n/)) {
    const match = /^([0-9a-fA-F]{64}) [ *](.+)$/.exec(line.trim());
    if (match) result[match[2].trim()] = match[1].toLowerCase();
  }
  return result;
}

module.exports = { parse };

if (require.main === module) {
  const [input, output] = process.argv.slice(2);
  const checksums = parse(fs.readFileSync(input, 'utf8'));
  if (Object.keys(checksums).length === 0) throw new Error(`No checksums found in ${input}`);
  fs.writeFileSync(output, JSON.stringify(checksums, null, 2) + '\n');
}
