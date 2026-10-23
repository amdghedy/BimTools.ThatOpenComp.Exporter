/**
 * serve.js – Zero-dependency static file server for the TOC Fragment Viewer.
 * Uses ONLY Node.js built-in modules (no npm install needed).
 *
 * Run:  node serve.js
 * Then: open http://localhost:3000 in Chrome
 */

import http   from 'http';
import fs     from 'fs';
import path   from 'path';
import { fileURLToPath } from 'url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const PORT      = 3000;
const ROOT      = __dirname;   // serve everything inside the Viewer folder

const MIME = {
  '.html' : 'text/html; charset=utf-8',
  '.js'   : 'application/javascript',
  '.mjs'  : 'application/javascript',
  '.cjs'  : 'application/javascript',
  '.ts'   : 'application/javascript',
  '.css'  : 'text/css',
  '.json' : 'application/json',
  '.wasm' : 'application/wasm',
  '.png'  : 'image/png',
  '.svg'  : 'image/svg+xml',
  '.frag' : 'application/octet-stream',
};

const server = http.createServer((req, res) => {
  // COOP + COEP headers are required for SharedArrayBuffer / WASM threads
  res.setHeader('Cross-Origin-Opener-Policy',   'same-origin');
  res.setHeader('Cross-Origin-Embedder-Policy',  'require-corp');
  res.setHeader('Access-Control-Allow-Origin',   '*');

  // Default to viewer.html
  let urlPath = req.url.split('?')[0];
  if (urlPath === '/') urlPath = '/viewer.html';

  const filePath = path.join(ROOT, urlPath);

  // Security: disallow escaping the ROOT directory
  if (!filePath.startsWith(ROOT)) {
    res.writeHead(403);
    res.end('Forbidden');
    return;
  }

  if (!fs.existsSync(filePath) || fs.statSync(filePath).isDirectory()) {
    res.writeHead(404, { 'Content-Type': 'text/plain' });
    res.end('404 Not Found: ' + urlPath);
    return;
  }

  const ext  = path.extname(filePath).toLowerCase();
  const mime = MIME[ext] || 'application/octet-stream';

  res.writeHead(200, { 'Content-Type': mime });
  fs.createReadStream(filePath).pipe(res);
});

server.listen(PORT, '127.0.0.1', () => {
  console.log('');
  console.log('  ✔  TOC Fragment Viewer is running!');
  console.log('');
  console.log(`  ➜  http://localhost:${PORT}`);
  console.log('');
  console.log('  Open the URL above in Chrome, then load your .frag file.');
  console.log('  Press Ctrl+C to stop the server.');
  console.log('');
});
