import path from "path";
import react from "@vitejs/plugin-react";
import { defineConfig } from "vite";

export default defineConfig(({ mode }) => {
  console.log("Building in mode:", mode);

  return {
    plugins: [react()],
    server: {
      host: "0.0.0.0",
      port: 5173,
      strictPort: true,
    },
    build: {
      lib: {
        entry: path.resolve(__dirname, "src/widget.ts"),
        name: "ChatbotWidget",
        fileName: (format) => (format === "umd" ? "arif-chat-widget.min.js" : "arif-chat-widget.es.js"),
        formats: ["umd", "es"],
      },
      cssCodeSplit: false,
      rollupOptions: {
        output: {
          assetFileNames: (asset) => (asset.name && asset.name.endsWith(".css") ? "arif-chat-widget.css" : asset.name || "asset.[ext]"),
        },
      },
      minify: "terser",
      terserOptions: {
        compress: {
          drop_console: mode === "production",
          drop_debugger: true,
          pure_funcs: ["console.log", "console.info", "console.debug"],
          passes: 2,
        },
        mangle: {
          safari10: true,
        },
      },
      target: "es2015",
      sourcemap: true,
      outDir: "dist",
      emptyOutDir: true,
      copyPublicDir: true,
    },
    resolve: {
      alias: {
        "@": path.resolve(__dirname, "./src"),
      },
    },
    define: {
      "process.env.NODE_ENV": JSON.stringify(mode === "development" ? "development" : "production"),
    },
  };
});
