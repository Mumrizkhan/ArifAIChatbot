import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

export default defineConfig({
  plugins: [react()],
  build: {
    lib: {
      entry: "src/widget.ts",
      name: "ChatbotWidget",
      formats: ["umd", "es"],
      fileName: (f) => (f === "umd" ? "arif-chat-widget.min.js" : "arif-chat-widget.es.js"),
    },
    cssCodeSplit: false,
    rollupOptions: {
      output: {
        assetFileNames: (asset) => (asset.name && asset.name.endsWith(".css") ? "arif-chat-widget.css" : asset.name || "asset.[ext]"),
      },
    },
  },
});
