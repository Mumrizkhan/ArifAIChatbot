import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import { resolve } from "path";

export default defineConfig(() => ({
  base: "./", // relative asset URLs
  plugins: [react()],
  build: {
    outDir: "dist",
    emptyOutDir: true,
    // force a single HTML entry: index.html
    rollupOptions: {
      input: {
        index: resolve(__dirname, "index.html"),
      },
    },
    // if you build as a library instead, use the lib option (no HTML emitted)
  },
}));
