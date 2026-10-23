/**
 * TOC IFC → Fragments Converter
 * Uses @thatopen/fragments v3.x IfcImporter.process() — fully headless Node.js.
 *
 * Usage:
 *   node index.js <input.ifc> <output.frag>
 *
 * process() JSDoc (confirmed from library source):
 *   @param data.bytes          - Raw IFC file data as Uint8Array  ← KEY FIX
 *   @param data.raw            - false = compressed output (smaller .frag)
 *   @param data.progressCallback - optional progress reporting
 */

import * as fs   from 'fs';
import * as path from 'path';
import * as url  from 'url';
import { IfcImporter } from '@thatopen/fragments';

const __dirname = path.dirname(url.fileURLToPath(import.meta.url));

async function main() {
    const args = process.argv.slice(2);
    if (args.length < 2) {
        console.error('Usage: node index.js <input.ifc> <output.frag>');
        process.exit(1);
    }

    const inputIfcPath   = args[0];
    const outputFragPath = args[1];

    if (!fs.existsSync(inputIfcPath)) {
        console.error('Input IFC not found:', inputIfcPath);
        process.exit(1);
    }

    console.log(`[TOC] Input : ${inputIfcPath}`);
    console.log(`[TOC] Output: ${outputFragPath}`);

    // ── Configure IfcImporter ───────────────────────────────────────────────
    const importer = new IfcImporter();

    // Absolute path to the WASM files inside node_modules/web-ifc
    const wasmDir = path.join(__dirname, 'node_modules', 'web-ifc') + path.sep;
    importer.wasm = {
        path:     wasmDir,
        absolute: true,
    };

    // ── Read IFC bytes ──────────────────────────────────────────────────────
    console.log('[TOC] Reading IFC...');
    const ifcBuffer = fs.readFileSync(inputIfcPath);
    const ifcBytes  = new Uint8Array(ifcBuffer);  // must be Uint8Array

    // ── Convert IFC → Fragments ─────────────────────────────────────────────
    // IMPORTANT: the property is 'bytes' (not 'ifcFile' or 'data')
    // Confirmed from IfcImporter JSDoc: @param data.bytes Raw IFC file data as Uint8Array
    console.log('[TOC] Converting (this may take 30–120 s for large models)...');

    const fragBytes = await importer.process({
        bytes: ifcBytes,   // ← correct property name confirmed from library source
        raw:   false,      // false = deflate-compressed output (smaller .frag file)
        progressCallback: (progress, info) => {
            const pct = (progress * 100).toFixed(0).padStart(3);
            process.stdout.write(`\r[TOC] ${String(info.process).padEnd(20)} ${pct}%`);
        },
    });

    process.stdout.write('\n');
    console.log('[TOC] Conversion done.');

    // ── Write .frag file ────────────────────────────────────────────────────
    const outDir = path.dirname(outputFragPath);
    if (outDir && !fs.existsSync(outDir))
        fs.mkdirSync(outDir, { recursive: true });

    fs.writeFileSync(outputFragPath, fragBytes);
    console.log('[TOC] Saved:', outputFragPath);

    process.exit(0);
}

main().catch(err => {
    console.error('\n[TOC] Fatal error:', err.message || err);
    process.exit(1);
});
