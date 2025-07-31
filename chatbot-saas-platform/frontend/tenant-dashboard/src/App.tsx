import { useEffect } from 'react';
import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import { Provider } from 'react-redux';
import { useTranslation } from 'react-i18next';
import { store } from './store/store';
import { useAppSelector, useAppDispatch } from './hooks/redux';
import { getCurrentUser } from './store/slices/authSlice';
import { ThemeProvider } from './components/theme/ThemeProvider';
import { ProtectedRoute } from './components/auth/ProtectedRoute';
import { Layout } from './components/layout/Layout';
import { LoginPage } from './pages/auth/LoginPage';
import DashboardPage from './pages/dashboard/DashboardPage';
import ChatbotConfigPage from './pages/chatbot/ChatbotConfigPage';
import TeamManagementPage from './pages/team/TeamManagementPage';
import AnalyticsPage from './pages/analytics/AnalyticsPage';
import BrandingPage from './pages/branding/BrandingPage';
import IntegrationsPage from './pages/integrations/IntegrationsPage';
import SettingsPage from './pages/settings/SettingsPage';
import WorkflowsPage from './pages/workflows/WorkflowsPage';
import './i18n';

function AppContent() {
  const { i18n } = useTranslation();
  const dispatch = useAppDispatch();
  const { isAuthenticated, isLoading } = useAppSelector((state) => state.auth);
  const { language, isRTL } = useAppSelector((state) => state.theme);

  useEffect(() => {
    const token = localStorage.getItem('token');
    if (token && !isAuthenticated) {
      dispatch(getCurrentUser());
    }
  }, [dispatch, isAuthenticated]);

  useEffect(() => {
    i18n.changeLanguage(language);
    document.documentElement.dir = isRTL ? 'rtl' : 'ltr';
    document.documentElement.lang = language;
  }, [language, isRTL, i18n]);

  if (isLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <div className="animate-spin rounded-full h-32 w-32 border-b-2 border-blue-600"></div>
      </div>
    );
  }

  return (
    <Router>
      <Routes>
        <Route 
          path="/login" 
          element={
            isAuthenticated ? <Navigate to="/dashboard" replace /> : <LoginPage />
          } 
        />
        <Route
          path="/*"
          element={
            <ProtectedRoute>
              <Layout>
                <Routes>
                  <Route path="/" element={<Navigate to="/dashboard" replace />} />
                  <Route path="/dashboard" element={<DashboardPage />} />
                  <Route path="/chatbot" element={<ChatbotConfigPage />} />
                  <Route path="/team" element={<TeamManagementPage />} />
                  <Route path="/analytics" element={<AnalyticsPage />} />
                  <Route path="/branding" element={<BrandingPage />} />
                  <Route path="/integrations" element={<IntegrationsPage />} />
                  <Route path="/workflows" element={<WorkflowsPage />} />
                  <Route path="/settings" element={<SettingsPage />} />
                </Routes>
              </Layout>
            </ProtectedRoute>
          }
        />
      </Routes>
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
