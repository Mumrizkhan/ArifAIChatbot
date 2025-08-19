import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import path from "path";

// no need for 'mode' here — remove unused param to avoid TS6133
export default defineConfig(() => {
  // avoid using `${mode}/index.html` as the input — always use project root index.html
  const indexHtml = path.resolve(__dirname, "index.html");

  return {
    plugins: [react()],
    server: {
      host: "0.0.0.0",
      port: 5174,
      strictPort: true,
    },
    resolve: {
      alias: {
        "@": path.resolve(__dirname, "./src"),
      },
    },
    build: {
      rollupOptions: {
        input: {
          main: indexHtml,
          // add other page inputs here if you actually have multiple entry html files
        },
      },
    },
  };
});
