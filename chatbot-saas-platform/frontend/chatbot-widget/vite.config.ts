import path from "path";
import react from "@vitejs/plugin-react";
import { defineConfig } from "vite";

export default defineConfig(({ mode }) => {
  console.log('Building in mode:', mode);
  
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
        name: "ArifChatWidget",
        fileName: (format) => `arif-chat-widget.${format}.js`,
        formats: ["es", "umd", "iife"],
      },
      rollupOptions: {
        external: [],
        input: path.resolve(__dirname, "src/widget.ts"),
        output: [
          {
            format: "es",
            entryFileNames: "arif-chat-widget.es.js",
            assetFileNames: "arif-chat-widget.[ext]",
            exports: "named",
            inlineDynamicImports: true,
          },
          {
            format: "umd",
            name: "ArifChatWidget",
            entryFileNames: "arif-chat-widget.umd.js",
            assetFileNames: "arif-chat-widget.[ext]",
            exports: "named",
            inlineDynamicImports: true,
          },
          {
            format: "iife",
            name: "ArifChatWidget",
            entryFileNames: "arif-chat-widget.min.js",
            assetFileNames: "arif-chat-widget.[ext]",
            exports: "named",
            inlineDynamicImports: true,
          },
        ],
      },
      minify: "terser",
      terserOptions: {
        compress: {
          drop_console: mode === 'production',
          drop_debugger: true,
          pure_funcs: ['console.log', 'console.info', 'console.debug'],
          passes: 2,
        },
        mangle: {
          safari10: true,
        },
      },
      target: "es2015",
      cssCodeSplit: false,
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
      'process.env.NODE_ENV': JSON.stringify(mode === 'development' ? 'development' : 'production'),
    }
  };
});
