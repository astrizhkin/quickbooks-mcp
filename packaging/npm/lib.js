'use strict';
const crypto = require('node:crypto');
const fs = require('node:fs');
const https = require('node:https');
const os = require('node:os');
const path = require('node:path');

const REPO = 'devwithmj/quickbooks-mcp';

/** Which release asset matches this machine, or an error message for the user. */
function target(platform, arch) {
  if (platform !== 'win32') {
    return { error: 'quickbooks-mcp works only on Windows, because it talks to QuickBooks Desktop through its Windows COM interface.' };
  }
  // Windows on ARM runs the x64 build under emulation.
  const rid = arch === 'ia32' ? 'win-x86' : arch === 'x64' || arch === 'arm64' ? 'win-x64' : null;
  return rid ? { rid, asset: `quickbooks-mcp-${rid}.exe` } : { error: `Unsupported processor architecture: ${arch}.` };
}

/** True if the SHA-256 of `data` equals `expected` (hex, any case). */
function matchesChecksum(data, expected) {
  return crypto.createHash('sha256').update(data).digest('hex') === String(expected).toLowerCase();
}

/** Where a version's executable is cached. */
function cachePath(version, asset, env = process.env) {
  const base = env.LOCALAPPDATA || path.join(os.homedir(), '.cache');
  return path.join(base, 'quickbooks-mcp', version, asset);
}

/** GET a URL following redirects (release downloads redirect to a CDN). */
function download(url, redirects = 5) {
  return new Promise((resolve, reject) => {
    https.get(url, { headers: { 'user-agent': 'quickbooks-mcp-launcher' } }, (res) => {
      if (res.statusCode >= 300 && res.statusCode < 400 && res.headers.location && redirects > 0) {
        res.resume();
        resolve(download(new URL(res.headers.location, url).toString(), redirects - 1));
        return;
      }
      if (res.statusCode !== 200) {
        res.resume();
        reject(new Error(`Download failed (${res.statusCode}): ${url}`));
        return;
      }
      const chunks = [];
      res.on('data', (c) => chunks.push(c));
      res.on('end', () => resolve(Buffer.concat(chunks)));
      res.on('error', reject);
    }).on('error', reject);
  });
}

/**
 * Returns the path of the verified executable for `version`, downloading it on first use.
 * The expected checksum ships inside this package (checksums.json), separate from the download.
 */
async function ensureExecutable({ version, asset, checksums, fetch = download, env = process.env }) {
  const file = cachePath(version, asset, env);
  const expected = checksums && checksums[asset];
  if (!expected) throw new Error(`No checksum for ${asset} in this package; refusing to run an unverified download.`);

  if (fs.existsSync(file) && matchesChecksum(fs.readFileSync(file), expected)) return file;

  const data = await fetch(`https://github.com/${REPO}/releases/download/v${version}/${asset}`);
  if (!matchesChecksum(data, expected)) throw new Error(`Checksum mismatch for ${asset}; the download was discarded.`);
  fs.mkdirSync(path.dirname(file), { recursive: true });
  const temp = `${file}.${process.pid}.tmp`;
  fs.writeFileSync(temp, data);
  fs.renameSync(temp, file);
  return file;
}

module.exports = { target, matchesChecksum, cachePath, ensureExecutable, download };
