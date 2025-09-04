import { createSlice, PayloadAction } from "@reduxjs/toolkit";

export interface ThemeConfig {
  primaryColor: string;
  secondaryColor: string;
  backgroundColor: string;
  textColor: string;
  borderRadius: string;
  fontFamily: string;
  fontSize: string;
  headerColor: string;
  headerTextColor: string;
  userMessageColor: string;
  botMessageColor: string;
  shadowColor: string;
  position: "bottom-right" | "bottom-left" | "top-right" | "top-left";
  size: "small" | "medium" | "large";
  animation: "slide" | "fade" | "bounce" | "none";
}

interface ThemeState {
  config: ThemeConfig;
  isRTL: boolean;
  language: "en" | "ar";
  customCSS?: string;
  branding: {
    logo?: string;
    companyName?: string;
    welcomeMessage?: string;
    placeholderText?: string;
  };
}

const defaultTheme: ThemeConfig = {
  primaryColor: "#3b82f6",
  secondaryColor: "#64748b",
  backgroundColor: "#ffffff",
  textColor: "#1f2937",
  borderRadius: "12px",
  fontFamily: "Inter, system-ui, sans-serif",
  fontSize: "14px",
  headerColor: "#3b82f6",
  headerTextColor: "#ffffff",
  userMessageColor: "#3b82f6",
  botMessageColor: "#f1f5f9",
  shadowColor: "rgba(0, 0, 0, 0.1)",
  position: "bottom-right",
  size: "medium",
  animation: "slide",
};

const initialState: ThemeState = {
  config: defaultTheme,
  isRTL: false,
  language: "en",
  branding: {
    welcomeMessage: "Hello! How can I help you today?", // Default English
    placeholderText: "Type your message...", // Default English
  },
};

const themeSlice = createSlice({
  name: "theme",
  initialState,
  reducers: {
    updateThemeConfig: (state, action: PayloadAction<Partial<ThemeConfig>>) => {
      state.config = { ...state.config, ...action.payload };
    },
    setLanguage: (state, action: PayloadAction<"en" | "ar">) => {
      state.language = action.payload;
      state.isRTL = action.payload === "ar";
    },
    setBranding: (state, action: PayloadAction<Partial<ThemeState["branding"]>>) => {
      state.branding = { ...state.branding, ...action.payload };
    },
    updateBrandingWithTranslations: (
      state,
      action: PayloadAction<{
        welcomeMessage?: string;
        placeholderText?: string;
        companyName?: string;
        logo?: string;
      }>
    ) => {
      state.branding = { ...state.branding, ...action.payload };
    },
    setCustomCSS: (state, action: PayloadAction<string>) => {
      state.customCSS = action.payload;
    },
    resetTheme: (state) => {
      state.config = defaultTheme;
      state.customCSS = undefined;
    },
    applyTenantTheme: (
      state,
      action: PayloadAction<{
        theme: Partial<ThemeConfig>;
        branding: Partial<ThemeState["branding"]>;
        language?: "en" | "ar";
        customCSS?: string;
      }>
    ) => {
      const { theme, branding, language, customCSS } = action.payload;
      state.config = { ...state.config, ...theme };
      state.branding = { ...state.branding, ...branding };
      if (language) {
        state.language = language;
        state.isRTL = language === "ar";
      }
      if (customCSS) {
        state.customCSS = customCSS;
      }
    },
  },
});

export const { updateThemeConfig, setLanguage, setBranding, updateBrandingWithTranslations, setCustomCSS, resetTheme, applyTenantTheme } =
  themeSlice.actions;

export default themeSlice.reducer;
