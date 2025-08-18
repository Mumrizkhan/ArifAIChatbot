import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import path from "path";

export default defineConfig(({ mode }) => ({
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
      input: mode === "production" ? path.resolve(__dirname, "index.html") : path.resolve(__dirname, "dev.html"), // Dynamically set entry point
      output: {
        assetFileNames: (asset) => (asset.name && asset.name.endsWith(".css") ? "arif-chat-widget.css" : asset.name || "asset.[ext]"),
      },
    },
  },
  server: {
    open: mode === "development" ? "/dev.html" : "/index.html", // Automatically open the correct entry point
  },
}));
