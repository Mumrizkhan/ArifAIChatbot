import { useEffect } from "react";
import { BrowserRouter as Router, Routes, Route, Navigate } from "react-router-dom";
import { Provider } from "react-redux";
import { store } from "./store/store";
import { useTranslation } from "react-i18next";
import { useSelector } from "react-redux";
import { RootState } from "./store/store";
import { ThemeProvider } from "./components/theme/ThemeProvider";
import { Toaster } from "./components/ui/sonner";
import "./i18n";

import Layout from "./components/layout/Layout";
import LoginPage from "./pages/auth/LoginPage";
import DashboardPage from "./pages/dashboard/DashboardPage";
import TenantsPage from "./pages/tenants/TenantsPage";
import UsersPage from "./pages/users/UsersPage";
import AnalyticsPage from "./pages/analytics/AnalyticsPage";
import SubscriptionsPage from "./pages/subscriptions/SubscriptionsPage";
import SettingsPage from "./pages/settings/SettingsPage";
import ProtectedRoute from "./components/auth/ProtectedRoute";

function AppContent() {
  const { i18n } = useTranslation();
  const { language, direction } = useSelector((state: RootState) => state.theme);

  useEffect(() => {
    i18n.changeLanguage(language);
    document.documentElement.dir = direction;
    document.documentElement.lang = language;
  }, [language, direction, i18n]);

  return (
    <Router>
      <div className={`min-h-screen bg-background ${direction === "rtl" ? "rtl" : "ltr"}`}>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route
            path="/*"
            element={
              <ProtectedRoute>
                <Layout>
                  <Routes>
                    <Route path="/" element={<Navigate to="/dashboard" replace />} />
                    <Route path="/dashboard" element={<DashboardPage />} />
                    <Route path="/tenants" element={<TenantsPage />} />
                    <Route path="/users" element={<UsersPage />} />
                    <Route path="/analytics" element={<AnalyticsPage />} />
                    <Route path="/subscriptions" element={<SubscriptionsPage />} />
                    <Route path="/settings" element={<SettingsPage />} />
                  </Routes>
                </Layout>
              </ProtectedRoute>
            }
          />
        </Routes>
        <Toaster />
      </div>
    </Router>
  );
}

function App() {
  return (
    <Provider store={store}>
      <ThemeProvider>
        <AppContent />
      </ThemeProvider>
    </Provider>
  );
}

export default App;
