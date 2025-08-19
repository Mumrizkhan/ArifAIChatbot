import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import { resolve } from "path";

export default defineConfig(() => ({
  base: "./",
  plugins: [react()],
  resolve: {
    alias: {
      // use the process polyfill for browser
      process: "process/browser",
    },
  },
  define: {
    // ensure NODE_ENV is available in the bundle
    "process.env.NODE_ENV": JSON.stringify(process.env.NODE_ENV || "production"),
  },
  build: {
    outDir: "dist",
    emptyOutDir: true,
    lib: {
      entry: resolve(__dirname, "src/widget.ts"),
      name: "ChatbotWidget",
      formats: ["umd", "es"],
      fileName: (format) => (format === "umd" ? "arif-chat-widget.min.js" : "arif-chat-widget.es.js"),
    },
    cssCodeSplit: true,
    rollupOptions: {
      output: {
        assetFileNames: (assetInfo) => (assetInfo.name && assetInfo.name.endsWith(".css") ? "arif-chat-widget.css" : "asset.[ext]"),
      },
    },
  },
}));
