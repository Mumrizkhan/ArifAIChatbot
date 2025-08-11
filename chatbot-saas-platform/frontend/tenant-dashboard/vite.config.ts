import { defineConfig, loadEnv } from "vite";
import react from "@vitejs/plugin-react";
import path from "path";

export default defineConfig(({ command, mode }) => {
  // Determine which env file to load based on mode
  //const envFile = getEnvFileName(mode);

  // Load env file based on mode
  //const env = loadEnv(mode, process.cwd(), "");

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
    // Specify custom env file names
    envDir: "./",
    // Load environment files in this order:
    // .env.[mode].local (highest priority)
    // .env.[mode]
    // .env.local
    // .env (lowest priority)
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
