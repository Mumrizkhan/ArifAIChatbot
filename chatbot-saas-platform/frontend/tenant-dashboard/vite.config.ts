import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import path from "path";

export default defineConfig(({ mode }) => {
  const envFile = getEnvFileName(mode);
  console.log(`🔧 Loading environment: ${mode} from ${envFile}`);

  return {
    plugins: [react()],
    resolve: {
      alias: {
        "@": path.resolve(__dirname, "./src"),
      },
    },
    define: {
      __APP_ENV__: JSON.stringify(mode),
    },
    envPrefix: "VITE_",
    envDir: "./",
  };
});

function getEnvFileName(mode: string): string {
  switch (mode) {
    case "development":
      return ".env.dev";
    case "staging":
      return ".env.staging";
    case "production":
      return ".env.production";
    default:
      return ".env";
  }
}
