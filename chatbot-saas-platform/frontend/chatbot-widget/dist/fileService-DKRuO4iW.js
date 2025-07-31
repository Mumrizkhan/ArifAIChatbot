var __defProp = Object.defineProperty;
var __defNormalProp = (obj, key, value) => key in obj ? __defProp(obj, key, { enumerable: true, configurable: true, writable: true, value }) : obj[key] = value;
var __publicField = (obj, key, value) => __defNormalProp(obj, typeof key !== "symbol" ? key + "" : key, value);
var __async = (__this, __arguments, generator) => {
  return new Promise((resolve, reject) => {
    var fulfilled = (value) => {
      try {
        step(generator.next(value));
      } catch (e) {
        reject(e);
      }
    };
    var rejected = (value) => {
      try {
        step(generator.throw(value));
      } catch (e) {
        reject(e);
      }
    };
    var step = (x) => x.done ? resolve(x.value) : Promise.resolve(x.value).then(fulfilled, rejected);
    step((generator = generator.apply(__this, __arguments)).next());
  });
};
class FileService {
  constructor() {
    __publicField(this, "apiUrl", "");
  }
  initialize(apiUrl) {
    this.apiUrl = apiUrl;
  }
  uploadFile(file, conversationId) {
    return __async(this, null, function* () {
      const formData = new FormData();
      formData.append("file", file);
      formData.append("conversationId", conversationId);
      try {
        const response = yield fetch(`${this.apiUrl}/files/upload`, {
          method: "POST",
          body: formData
        });
        const data = yield response.json();
        return data.fileUrl;
      } catch (error) {
        console.error("File upload error:", error);
        throw new Error("Failed to upload file");
      }
    });
  }
  isValidFileType(file, allowedTypes) {
    return allowedTypes.some((type) => {
      if (type.startsWith(".")) {
        return file.name.toLowerCase().endsWith(type.toLowerCase());
      }
      return file.type.match(type);
    });
  }
  isValidFileSize(file, maxSize) {
    return file.size <= maxSize;
  }
  formatFileSize(bytes) {
    if (bytes === 0) return "0 Bytes";
    const k = 1024;
    const sizes = ["Bytes", "KB", "MB", "GB"];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + " " + sizes[i];
  }
  getFileIcon(fileType) {
    if (fileType.startsWith("image/")) return "image";
    if (fileType.includes("pdf")) return "pdf";
    if (fileType.includes("word") || fileType.includes("document")) return "document";
    if (fileType.includes("excel") || fileType.includes("spreadsheet")) return "spreadsheet";
    return "file";
  }
}
const fileService = new FileService();
export {
  fileService
};
