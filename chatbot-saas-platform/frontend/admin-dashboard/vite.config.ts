import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import path from "path";

export default defineConfig(() => {
  const indexHtml = path.resolve(__dirname, "index.html");

  return {
    plugins: [react()],
    resolve: {
      alias: {
        "@": path.resolve(__dirname, "src"),
      },
    },
    build: {
      rollupOptions: {
        input: { main: indexHtml },
      },
    },
  };
});
