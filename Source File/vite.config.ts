import { defineConfig } from "vite";

export default defineConfig({
  optimizeDeps: {
    exclude: ["web-ifc"],
  },
  server: {
    headers: {
      // Required for SharedArrayBuffer (used by web-ifc WASM)
      "Cross-Origin-Opener-Policy": "same-origin",
      "Cross-Origin-Embedder-Policy": "require-corp",
    },
  },
});
