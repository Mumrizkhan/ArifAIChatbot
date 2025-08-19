const { copyFileSync, unlinkSync, existsSync } = require("fs");
const { resolve } = require("path");

const projectRoot = resolve(__dirname, "..");
const distIndex = resolve(projectRoot, "dist", "index.html");
const rootIndex = resolve(projectRoot, "index.html");

if (existsSync(distIndex)) {
  copyFileSync(distIndex, rootIndex);
  console.log("Copied dist/index.html -> index.html");
}

const demo = resolve(projectRoot, "dist", "demo.html");
if (existsSync(demo)) {
  try {
    unlinkSync(demo);
    console.log("Removed dist/demo.html");
  } catch (e) {
    /*ignore*/
  }
}
