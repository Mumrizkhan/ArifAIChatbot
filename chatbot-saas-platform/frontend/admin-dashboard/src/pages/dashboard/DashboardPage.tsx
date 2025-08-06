import React, { useEffect } from "react";
import { useDispatch, useSelector } from "react-redux";
import { useTranslation } from "react-i18next";
import { AppDispatch, RootState } from "../../store/store";
import {
  fetchDashboardStats,
  setSignalRConnectionStatus,
  updateDashboardStatsRealtime,
  addSystemNotification,
} from "../../store/slices/analyticsSlice";
import { adminSignalRService } from "../../services/signalr";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "../../components/ui/card";
import { Badge } from "../../components/ui/badge";
import { Button } from "../../components/ui/button";
import { Skeleton } from "../../components/ui/skeleton";
import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, PieChart, Pie, Cell } from "recharts";
import { Users, Building2, MessageSquare, UserCheck, TrendingUp, Activity, AlertCircle, RefreshCw } from "lucide-react";
import NotificationCenter from "../../components/notifications/NotificationCenter";

const DashboardPage: React.FC = () => {
  const { t } = useTranslation();
  const dispatch = useDispatch<AppDispatch>();
  const { dashboardStats, isLoading, isSignalRConnected } = useSelector((state: RootState) => state.analytics);

  useEffect(() => {
    dispatch(fetchDashboardStats());

    const initializeSignalR = async () => {
      const authToken = localStorage.getItem("authToken") || "demo-admin-token";

      adminSignalRService.setOnConnectionStatusChange((isConnected) => {
        dispatch(setSignalRConnectionStatus(isConnected));
      });

      adminSignalRService.setOnDashboardStatsUpdate((stats) => {
        dispatch(updateDashboardStatsRealtime(stats));
      });

      adminSignalRService.setOnSystemNotification((notification) => {
        dispatch(addSystemNotification(notification));
      });

      try {
        await adminSignalRService.connect(authToken);
      } catch (error) {
        console.error("Failed to initialize SignalR:", error);
      }
    };

    initializeSignalR();

    return () => {
      adminSignalRService.disconnect();
    };
  }, [dispatch]);

  const handleRefresh = async () => {
    dispatch(fetchDashboardStats());
    if (isSignalRConnected) {
      await adminSignalRService.requestDashboardStatsUpdate();
    }
  };

  const mockChartData = [
    { name: t("dashboard.months.jan"), conversations: 400, resolved: 350 },
    { name: t("dashboard.months.feb"), conversations: 300, resolved: 280 },
    { name: t("dashboard.months.mar"), conversations: 500, resolved: 450 },
    { name: t("dashboard.months.apr"), conversations: 280, resolved: 250 },
    { name: t("dashboard.months.may"), conversations: 590, resolved: 520 },
    { name: t("dashboard.months.jun"), conversations: 320, resolved: 300 },
  ];

  const mockPieData = [
    { name: t("dashboard.tenantStatus.active"), value: 65, color: "#10b981" },
    { name: t("dashboard.tenantStatus.inactive"), value: 25, color: "#f59e0b" },
    { name: t("dashboard.tenantStatus.suspended"), value: 10, color: "#ef4444" },
  ];

  const StatCard = ({ title, value, icon: Icon, trend, description }: any) => (
    <Card>
      <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
        <CardTitle className="text-sm font-medium">{title}</CardTitle>
        <Icon className="h-4 w-4 text-muted-foreground" />
      </CardHeader>
      <CardContent>
        <div className="text-2xl font-bold">{value}</div>
        {trend && (
          <div className="flex items-center text-xs text-muted-foreground">
            <TrendingUp className="mr-1 h-3 w-3" />
            {trend}
          </div>
        )}
        {description && <p className="text-xs text-muted-foreground mt-1">{description}</p>}
      </CardContent>
    </Card>
  );

  if (isLoading) {
    return (
      <div className="space-y-6">
        <div>
          <h1 className="text-3xl font-bold">{t("dashboard.title")}</h1>
          <p className="text-muted-foreground">{t("dashboard.overview")}</p>
        </div>

        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
          {[...Array(4)].map((_, i) => (
            <Card key={i}>
              <CardHeader>
                <Skeleton className="h-4 w-[100px]" />
              </CardHeader>
              <CardContent>
                <Skeleton className="h-8 w-[60px]" />
                <Skeleton className="h-3 w-[80px] mt-2" />
              </CardContent>
            </Card>
          ))}
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">{t("dashboard.title")}</h1>
          <p className="text-muted-foreground">{t("dashboard.overview")}</p>
        </div>
        <div className="flex items-center space-x-2">
          <div className="flex items-center space-x-1">
            <div className={`w-2 h-2 rounded-full ${isSignalRConnected ? "bg-green-500" : "bg-red-500"}`} />
            <span className="text-xs text-muted-foreground">{isSignalRConnected ? t("dashboard.status.live") : t("dashboard.status.offline")}</span>
          </div>
          <Button onClick={handleRefresh} className="flex items-center">
            <RefreshCw className="mr-1 h-3 w-3" />
            {t("common.refresh")}
          </Button>
        </div>
      </div>

      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        <StatCard
          title={t("dashboard.stats.totalTenants")}
          value={dashboardStats?.totalTenants || 156}
          icon={Building2}
          trend={t("dashboard.trends.tenants")}
          description={t("dashboard.descriptions.activeTenants")}
        />
        <StatCard
          title={t("dashboard.stats.totalUsers")}
          value={dashboardStats?.totalUsers || 2847}
          icon={Users}
          trend={t("dashboard.trends.users")}
          description={t("dashboard.descriptions.registeredUsers")}
        />
        <StatCard
          title={t("dashboard.stats.totalConversations")}
          value={dashboardStats?.totalConversations || 12543}
          icon={MessageSquare}
          trend={t("dashboard.trends.conversations")}
          description={t("dashboard.descriptions.totalConversations")}
        />
        <StatCard
          title={t("dashboard.stats.activeAgents")}
          value={dashboardStats?.activeAgents || 89}
          icon={UserCheck}
          trend={t("dashboard.trends.agents")}
          description={t("dashboard.descriptions.currentlyOnline")}
        />
      </div>

      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-7">
        <Card className="col-span-4">
          <CardHeader>
            <CardTitle>{t("dashboard.charts.conversationTrends")}</CardTitle>
            <CardDescription>{t("dashboard.charts.conversationTrendsDescription")}</CardDescription>
          </CardHeader>
          <CardContent>
            <ResponsiveContainer width="100%" height={300}>
              <BarChart data={mockChartData}>
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis dataKey="name" />
                <YAxis />
                <Tooltip />
                <Bar dataKey="conversations" fill="#3b82f6" />
                <Bar dataKey="resolved" fill="#10b981" />
              </BarChart>
            </ResponsiveContainer>
          </CardContent>
        </Card>

        <Card className="col-span-3">
          <CardHeader>
            <CardTitle>{t("dashboard.charts.tenantStatus")}</CardTitle>
            <CardDescription>{t("dashboard.charts.tenantStatusDescription")}</CardDescription>
          </CardHeader>
          <CardContent>
            <ResponsiveContainer width="100%" height={300}>
              <PieChart>
                <Pie data={mockPieData} cx="50%" cy="50%" innerRadius={60} outerRadius={100} paddingAngle={5} dataKey="value">
                  {mockPieData.map((entry, index) => (
                    <Cell key={`cell-${index}`} fill={entry.color} />
                  ))}
                </Pie>
                <Tooltip />
              </PieChart>
            </ResponsiveContainer>
            <div className="flex justify-center space-x-4 mt-4">
              {mockPieData.map((entry, index) => (
                <div key={index} className="flex items-center space-x-2">
                  <div className="w-3 h-3 rounded-full" style={{ backgroundColor: entry.color }} />
                  <span className="text-sm">{entry.name}</span>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
      </div>

      <div className="grid gap-4 md:grid-cols-3">
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center">
              <Activity className="mr-2 h-5 w-5" />
              {t("dashboard.systemHealth.title")}
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-3">
            <div className="flex items-center justify-between">
              <span>{t("dashboard.systemHealth.apiGateway")}</span>
              <Badge variant="default">{t("dashboard.systemHealth.healthy")}</Badge>
            </div>
            <div className="flex items-center justify-between">
              <span>{t("dashboard.systemHealth.database")}</span>
              <Badge variant="default">{t("dashboard.systemHealth.healthy")}</Badge>
            </div>
            <div className="flex items-center justify-between">
              <span>{t("dashboard.systemHealth.redisCache")}</span>
              <Badge variant="default">{t("dashboard.systemHealth.healthy")}</Badge>
            </div>
            <div className="flex items-center justify-between">
              <span>{t("dashboard.systemHealth.signalrHub")}</span>
              <Badge variant={isSignalRConnected ? "default" : "destructive"}>
                {isSignalRConnected ? t("dashboard.systemHealth.connected") : t("dashboard.systemHealth.disconnected")}
              </Badge>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="flex items-center">
              <AlertCircle className="mr-2 h-5 w-5" />
              {t("dashboard.recentActivity.title")}
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-3">
            <div className="text-sm">
              <p className="font-medium">{t("dashboard.recentActivity.newTenant")}</p>
              <p className="text-muted-foreground">{t("dashboard.recentActivity.newTenantTime")}</p>
            </div>
            <div className="text-sm">
              <p className="font-medium">{t("dashboard.recentActivity.maintenance")}</p>
              <p className="text-muted-foreground">{t("dashboard.recentActivity.maintenanceTime")}</p>
            </div>
            <div className="text-sm">
              <p className="font-medium">{t("dashboard.recentActivity.highVolume")}</p>
              <p className="text-muted-foreground">{t("dashboard.recentActivity.highVolumeTime")}</p>
            </div>
          </CardContent>
        </Card>

        <NotificationCenter />
      </div>
    </div>
  );
};

export default DashboardPage;
