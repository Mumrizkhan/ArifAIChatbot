import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import { resolve } from "path";
import { existsSync, unlinkSync } from "fs";

export default defineConfig(() => ({
  base: "./", // use relative paths so root/index.html can reference ./dist/...
  plugins: [react()],
  build: {
    outDir: "dist",
    // library mode -> no HTML files generated
    lib: {
      entry: resolve(__dirname, "src/widget.ts"),
      name: "ChatbotWidget",
      formats: ["umd", "es"],
      fileName: (format) => (format === "umd" ? "arif-chat-widget.min.js" : "arif-chat-widget.es.js"),
    },
    cssCodeSplit: true,
    emptyOutDir: true,
    rollupOptions: {
      output: {
        // ensure CSS gets a predictable name
        assetFileNames: (assetInfo) => (assetInfo.name && assetInfo.name.endsWith(".css") ? "arif-chat-widget.css" : "asset.[ext]"),
      },
    },
  },
  // remove demo.html if Vite unexpectedly produces it
  buildEnd() {
    const demo = resolve(__dirname, "dist/demo.html");
    if (existsSync(demo)) {
      try {
        unlinkSync(demo);
        console.log("Removed dist/demo.html");
      } catch {}
    }
  },
}));
