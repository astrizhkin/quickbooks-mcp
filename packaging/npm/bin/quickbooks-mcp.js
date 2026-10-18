#!/usr/bin/env node
'use strict';
const { spawn } = require('node:child_process');
const { target, ensureExecutable } = require('../lib');
const { version } = require('../package.json');
const checksums = require('../checksums.json');

/** Starts the verified release build with this process's arguments and stdio, so the MCP client talks to it directly. */
async function main() {
  // QB_MCP_EXE runs a local build instead of downloading a release (for development).
  let exe = process.env.QB_MCP_EXE;
  if (!exe) {
    const t = target(process.platform, process.arch);
    if (t.error) throw new Error(t.error);
    exe = await ensureExecutable({ version, asset: t.asset, checksums });
  }

  const child = spawn(exe, process.argv.slice(2), { stdio: 'inherit' });
  child.on('error', (err) => {
    console.error(`Could not start ${exe}: ${err.message}`);
    process.exit(1);
  });
  child.on('exit', (code, signal) => process.exit(code ?? (signal ? 1 : 0)));
}

main().catch((err) => {
  console.error(err.message);
  process.exit(1);
});
