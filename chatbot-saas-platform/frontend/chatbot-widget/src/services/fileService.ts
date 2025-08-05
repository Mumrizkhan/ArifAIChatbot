class FileService {
  private apiUrl: string = import.meta.env.VITE_WEBSOCKET_URL || "";
  initialize(apiUrl: string) {
    this.apiUrl = apiUrl;
  }

  async uploadFile(file: File, conversationId: string): Promise<string> {
    const formData = new FormData();
    formData.append("file", file);
    formData.append("conversationId", conversationId);

    try {
      const response = await fetch(`${this.apiUrl}/files/upload`, {
        method: "POST",
        body: formData,
      });
      const data = await response.json();
      return data.fileUrl;
    } catch (error) {
      console.error("File upload error:", error);
      throw new Error("Failed to upload file");
    }
  }

  isValidFileType(file: File, allowedTypes: string[]): boolean {
    return allowedTypes.some((type) => {
      if (type.startsWith(".")) {
        return file.name.toLowerCase().endsWith(type.toLowerCase());
      }
      return file.type.match(type);
    });
  }

  isValidFileSize(file: File, maxSize: number): boolean {
    return file.size <= maxSize;
  }

  formatFileSize(bytes: number): string {
    if (bytes === 0) return "0 Bytes";
    const k = 1024;
    const sizes = ["Bytes", "KB", "MB", "GB"];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + " " + sizes[i];
  }

  getFileIcon(fileType: string): string {
    if (fileType.startsWith("image/")) return "image";
    if (fileType.includes("pdf")) return "pdf";
    if (fileType.includes("word") || fileType.includes("document")) return "document";
    if (fileType.includes("excel") || fileType.includes("spreadsheet")) return "spreadsheet";
    return "file";
  }
}

export const fileService = new FileService();
