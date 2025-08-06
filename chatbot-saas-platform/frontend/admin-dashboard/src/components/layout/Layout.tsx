import React from "react";
import { useSelector } from "react-redux";
import { useTranslation } from "react-i18next";
import { RootState } from "../../store/store";
import Sidebar from "./Sidebar";
import Header from "./Header";

interface LayoutProps {
  children: React.ReactNode;
}

const Layout: React.FC<LayoutProps> = ({ children }) => {
  const { sidebarCollapsed } = useSelector((state: RootState) => state.theme);
  const { i18n } = useTranslation();

  // Check if current language is RTL (Arabic)
  const isRTL = i18n.language === "ar" || i18n.dir() === "rtl";

  // Get appropriate margin for main content based on sidebar position and language direction
  const getMainContentMargin = () => {
    if (isRTL) {
      // For Arabic (RTL): sidebar is on right, so content needs right margin
      return sidebarCollapsed ? "mr-16" : "mr-64";
    } else {
      // For English (LTR): sidebar is on left, so content needs left margin
      return sidebarCollapsed ? "ml-16" : "ml-64";
    }
  };

  return (
    <div className={`flex h-screen bg-background ${isRTL ? "flex-row-reverse" : ""}`}>
      <Sidebar />
      <div className={`flex-1 flex flex-col transition-all duration-300 ${getMainContentMargin()}`}>
        <Header />
        <main className="flex-1 p-6">{children}</main>
      </div>
    </div>
  );
};

export default Layout;
