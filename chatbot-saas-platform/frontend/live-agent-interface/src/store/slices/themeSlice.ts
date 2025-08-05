import { createSlice, PayloadAction } from "@reduxjs/toolkit";

interface ThemeConfig {
  primaryColor: string;
  secondaryColor: string;
  backgroundColor: string;
  textColor: string;
  borderRadius: string;
  fontFamily: string;
  fontSize: string;
  headerColor: string;
  headerTextColor: string;
  sidebarColor: string;
  sidebarTextColor: string;
  accentColor: string;
  successColor: string;
  warningColor: string;
  errorColor: string;
  infoColor: string;
}

interface BrandingConfig {
  logo?: string;
  companyName?: string;
  favicon?: string;
  customCSS?: string;
}

interface ThemeState {
  isDarkMode: boolean;
  language: "en" | "ar";
  isRTL: boolean;
  theme: ThemeConfig;
  branding: BrandingConfig;
  customCSS?: string;
}

const defaultTheme: ThemeConfig = {
  primaryColor: "#3b82f6",
  secondaryColor: "#64748b",
  backgroundColor: "#ffffff",
  textColor: "#1f2937",
  borderRadius: "0.5rem",
  fontFamily: "Inter, system-ui, sans-serif",
  fontSize: "14px",
  headerColor: "#ffffff",
  headerTextColor: "#1f2937",
  sidebarColor: "#f8fafc",
  sidebarTextColor: "#64748b",
  accentColor: "#10b981",
  successColor: "#10b981",
  warningColor: "#f59e0b",
  errorColor: "#ef4444",
  infoColor: "#3b82f6",
};

const initialState: ThemeState = {
  isDarkMode: false,
  language: "en",
  isRTL: false,
  theme: defaultTheme,
  branding: {},
};

const themeSlice = createSlice({
  name: "theme",
  initialState,
  reducers: {
    toggleDarkMode: (state) => {
      state.isDarkMode = !state.isDarkMode;
    },
    setLanguage: (state, action: PayloadAction<"en" | "ar">) => {
      state.language = action.payload;
      state.isRTL = action.payload === "ar";
    },
    updateTheme: (state, action: PayloadAction<Partial<ThemeConfig>>) => {
      state.theme = { ...state.theme, ...action.payload };
    },
    updateBranding: (state, action: PayloadAction<Partial<BrandingConfig>>) => {
      state.branding = { ...state.branding, ...action.payload };
    },
    applyTenantTheme: (
      state,
      action: PayloadAction<{
        theme?: Partial<ThemeConfig>;
        branding?: Partial<BrandingConfig>;
        language?: "en" | "ar";
        customCSS?: string;
      }>
    ) => {
      const { theme, branding, language, customCSS } = action.payload;

      if (theme) {
        state.theme = { ...state.theme, ...theme };
      }

      if (branding) {
        state.branding = { ...state.branding, ...branding };
      }

      if (language) {
        state.language = language;
        state.isRTL = language === "ar";
      }

      if (customCSS) {
        state.customCSS = customCSS;
      }
    },
    resetTheme: (state) => {
      state.theme = defaultTheme;
      state.branding = {};
      state.customCSS = undefined;
    },
  },
});

export const { toggleDarkMode, setLanguage, updateTheme, updateBranding, applyTenantTheme, resetTheme } = themeSlice.actions;

export default themeSlice.reducer;
