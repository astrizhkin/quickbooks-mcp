'use strict';
const assert = require('node:assert');
const crypto = require('node:crypto');
const fs = require('node:fs');
const os = require('node:os');
const path = require('node:path');
const { test } = require('node:test');
const { target, matchesChecksum, cachePath, ensureExecutable } = require('../lib');

const sha = (data) => crypto.createHash('sha256').update(data).digest('hex');

test('picks the build for the processor on Windows', () => {
  assert.deepStrictEqual(target('win32', 'x64'), { rid: 'win-x64', asset: 'quickbooks-mcp-win-x64.exe' });
  assert.deepStrictEqual(target('win32', 'ia32'), { rid: 'win-x86', asset: 'quickbooks-mcp-win-x86.exe' });
  assert.strictEqual(target('win32', 'arm64').rid, 'win-x64');
});

test('says clearly that other systems are unsupported', () => {
  for (const platform of ['linux', 'darwin']) assert.match(target(platform, 'x64').error, /only on Windows/);
  assert.match(target('win32', 'mips').error, /Unsupported processor/);
});

test('checksums compare case-insensitively and reject changes', () => {
  const data = Buffer.from('hello');
  assert.ok(matchesChecksum(data, sha(data).toUpperCase()));
  assert.ok(!matchesChecksum(Buffer.from('hellp'), sha(data)));
});

function tempEnv() {
  return { LOCALAPPDATA: fs.mkdtempSync(path.join(os.tmpdir(), 'qbmcp-')) };
}

test('downloads once, verifies, and reuses the cached file', async () => {
  const env = tempEnv();
  const data = Buffer.from('exe-bytes');
  let calls = 0;
  const fetch = async () => { calls += 1; return data; };
  const options = { version: '1.2.3', asset: 'a.exe', checksums: { 'a.exe': sha(data) }, fetch, env };

  const first = await ensureExecutable(options);
  const second = await ensureExecutable(options);

  assert.strictEqual(first, cachePath('1.2.3', 'a.exe', env));
  assert.strictEqual(second, first);
  assert.strictEqual(calls, 1);
  assert.deepStrictEqual(fs.readFileSync(first), data);
});

test('refuses a download that does not match the checksum', async () => {
  const env = tempEnv();
  await assert.rejects(
    ensureExecutable({ version: '1.2.3', asset: 'a.exe', checksums: { 'a.exe': sha('good') }, fetch: async () => Buffer.from('evil'), env }),
    /Checksum mismatch/,
  );
  assert.ok(!fs.existsSync(cachePath('1.2.3', 'a.exe', env)));
});

test('refuses to run without a known checksum', async () => {
  await assert.rejects(
    ensureExecutable({ version: '1.2.3', asset: 'a.exe', checksums: {}, fetch: async () => Buffer.from('x'), env: tempEnv() }),
    /No checksum/,
  );
});

test('replaces a cached file that was tampered with', async () => {
  const env = tempEnv();
  const good = Buffer.from('good');
  const file = cachePath('1.2.3', 'a.exe', env);
  fs.mkdirSync(path.dirname(file), { recursive: true });
  fs.writeFileSync(file, 'tampered');

  await ensureExecutable({ version: '1.2.3', asset: 'a.exe', checksums: { 'a.exe': sha(good) }, fetch: async () => good, env });

  assert.deepStrictEqual(fs.readFileSync(file), good);
});

test('parses a sha256sum listing in text and binary mode', () => {
  const { parse } = require('../scripts/write-checksums');
  const a = 'a'.repeat(64);
  const b = 'B'.repeat(64);

  assert.deepStrictEqual(parse(`${a}  one.exe\r\n${b} *two.exe\n\nnot a line\n`), { 'one.exe': a, 'two.exe': 'b'.repeat(64) });
});
