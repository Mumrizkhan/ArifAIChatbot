import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import path from "path";

// Library build (staging/prod) + normal dev server
export default defineConfig(({ mode }) => {
  const isProd = mode === "production";
  return {
    plugins: [react()],
    resolve: {
      alias: {
        "@": path.resolve(__dirname, "src"),
      },
    },
    server: {
      port: 5173,
      host: "0.0.0.0",
    },
    build: {
      lib: {
        entry: "src/widget.ts", // must export ChatbotWidget
        name: "ChatbotWidget",
        formats: ["umd", "es"],
        fileName: (format) => (format === "umd" ? "arif-chat-widget.min.js" : "arif-chat-widget.es.js"),
      },
      cssCodeSplit: false,
      rollupOptions: {
        output: {
          assetFileNames: (asset) => (asset.name && asset.name.endsWith(".css") ? "arif-chat-widget.css" : asset.name || "asset.[ext]"),
        },
      },
      minify: "terser",
      sourcemap: !isProd,
    },
  };
});
