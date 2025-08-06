import React, { useEffect } from "react";
import { useLocation, Link } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { useSelector, useDispatch } from "react-redux";
import { RootState, AppDispatch } from "../../store/store";
import { toggleSidebar } from "../../store/slices/themeSlice";
import { cn } from "../../lib/utils";
import { Button } from "../ui/button";
import { LayoutDashboard, Users, Building2, BarChart3, CreditCard, Settings, ChevronLeft, ChevronRight } from "lucide-react";

const Sidebar: React.FC = () => {
  const { t, i18n } = useTranslation();
  const location = useLocation();
  const dispatch = useDispatch<AppDispatch>();
  const { sidebarCollapsed } = useSelector((state: RootState) => state.theme);

  // Check if current language is RTL
  const isRTL = i18n.language === "ar" || i18n.dir() === "rtl";

  // Set document direction when language changes
  useEffect(() => {
    document.documentElement.dir = isRTL ? "rtl" : "ltr";
    document.documentElement.lang = i18n.language;
  }, [isRTL, i18n.language]);

  const menuItems = [
    {
      icon: LayoutDashboard,
      label: t("navigation.dashboard"),
      path: "/dashboard",
    },
    {
      icon: Building2,
      label: t("navigation.tenants"),
      path: "/tenants",
    },
    {
      icon: Users,
      label: t("navigation.users"),
      path: "/users",
    },
    {
      icon: BarChart3,
      label: t("navigation.analytics"),
      path: "/analytics",
    },
    {
      icon: CreditCard,
      label: t("navigation.subscriptions"),
      path: "/subscriptions",
    },
    {
      icon: Settings,
      label: t("navigation.settings"),
      path: "/settings",
    },
  ];

  // Get positioning classes based on language direction
  const getPositionClasses = () => {
    const baseClasses = "fixed top-0 h-full bg-card transition-all duration-300 z-50";
    const width = sidebarCollapsed ? "w-16" : "w-64";

    if (isRTL) {
      // For RTL (Arabic): sidebar on the right
      return cn(baseClasses, width, "right-0 border-l border-border");
    } else {
      // For LTR (English): sidebar on the left
      return cn(baseClasses, width, "left-0 border-r border-border");
    }
  };

  // Get appropriate chevron icon based on direction and collapse state
  const getChevronIcon = () => {
    if (isRTL) {
      return sidebarCollapsed ? ChevronLeft : ChevronRight;
    } else {
      return sidebarCollapsed ? ChevronRight : ChevronLeft;
    }
  };

  const ChevronIcon = getChevronIcon();

  return (
    <div className={getPositionClasses()}>
      <div className={cn("flex items-center justify-between p-4 border-b border-border", isRTL ? "flex-row-reverse" : "")}>
        {!sidebarCollapsed && <h1 className="text-xl font-bold text-foreground">ChatBot Admin</h1>}
        <Button variant="ghost" size="sm" onClick={() => dispatch(toggleSidebar())} className="p-2">
          <ChevronIcon size={16} />
        </Button>
      </div>

      <nav className="mt-6">
        <ul className="space-y-2 px-3">
          {menuItems.map((item) => {
            const Icon = item.icon;
            const isActive = location.pathname === item.path;

            return (
              <li key={item.path}>
                <Link
                  to={item.path}
                  className={cn(
                    "flex items-center gap-3 py-2 rounded-lg transition-colors",
                    "hover:bg-accent hover:text-accent-foreground",
                    isActive && "bg-primary text-primary-foreground",
                    sidebarCollapsed && "justify-center",
                    // RTL flex direction and alignment
                    isRTL && !sidebarCollapsed && "flex-row-reverse text-right justify-end pr-3",
                    !isRTL && !sidebarCollapsed && "justify-start pl-3",
                    // Ensure proper text direction for Arabic
                    isRTL && "dir-rtl"
                  )}
                  style={{
                    direction: isRTL ? "rtl" : "ltr",
                    textAlign: isRTL ? "right" : "left",
                  }}
                >
                  {isRTL ? (
                    <>
                      {!sidebarCollapsed && (
                        <span
                          className={cn("font-medium", "text-right")}
                          style={{
                            direction: "rtl",
                          }}
                        >
                          {item.label}
                        </span>
                      )}
                      <Icon size={20} />
                    </>
                  ) : (
                    <>
                      <Icon size={20} />
                      {!sidebarCollapsed && (
                        <span
                          className={cn("font-medium", "text-left")}
                          style={{
                            direction: "ltr",
                          }}
                        >
                          {item.label}
                        </span>
                      )}
                    </>
                  )}
                </Link>
              </li>
            );
          })}
        </ul>
      </nav>
    </div>
  );
};

export default Sidebar;
