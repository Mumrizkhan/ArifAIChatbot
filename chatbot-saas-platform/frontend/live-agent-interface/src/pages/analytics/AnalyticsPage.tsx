import React, { useEffect, useState } from "react";
import { useDispatch, useSelector } from "react-redux";
import { useTranslation } from "react-i18next";
import { AppDispatch, RootState } from "../../store/store";
import {
  fetchPerformanceData,
  fetchAnalyticsData,
  fetchDashboardStats,
  fetchConversationMetrics,
  fetchAgentMetrics,
  selectPerformanceData,
  selectAnalyticsData,
  selectDashboardStats,
  selectConversationMetrics,
  selectAgentMetrics,
  selectAnalyticsLoading,
} from "../../store/slices/analyticsSlice";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "../../components/ui/card";
import { Skeleton } from "../../components/ui/skeleton";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "../../components/ui/tabs";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "../../components/ui/select";
import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
  LineChart,
  Line,
  PieChart,
  Pie,
  Cell,
  AreaChart,
  Area,
} from "recharts";
import { BarChart3, TrendingUp, Users, MessageSquare, Clock, Star, Download, Calendar, Activity, Target, Award } from "lucide-react";

const AnalyticsPage = () => {
  const { t } = useTranslation();
  const dispatch = useDispatch<AppDispatch>();
  const [activeTab, setActiveTab] = useState("performance");
  const [selectedTimeRange, setSelectedTimeRange] = useState("7d");
  const { user, token } = useSelector((state: RootState) => state.auth); // Fetch user from auth slice
  const currentAgentId = user?.id; // Extract the agent's ID
  // Select data from Redux store
  const performanceData = useSelector(selectPerformanceData);
  const analyticsData = useSelector(selectAnalyticsData);
  const dashboardStats = useSelector(selectDashboardStats);
  const conversationMetrics = useSelector(selectConversationMetrics);
  const agentMetrics = useSelector(selectAgentMetrics);
  const isLoading = useSelector(selectAnalyticsLoading);

  // Helper to get dateFrom and dateTo based on selectedTimeRange
  const getDateRange = () => {
    const now = new Date();
    let dateFrom = new Date();
    if (selectedTimeRange === "7d") dateFrom.setDate(now.getDate() - 6);
    else if (selectedTimeRange === "30d") dateFrom.setDate(now.getDate() - 29);
    else if (selectedTimeRange === "90d") dateFrom.setDate(now.getDate() - 89);
    else if (selectedTimeRange === "1y") dateFrom.setFullYear(now.getFullYear() - 1);
    return {
      dateFrom: dateFrom.toISOString().split("T")[0],
      dateTo: now.toISOString().split("T")[0],
    };
  };

  // Example goalId, replace with your actual logic or state
  const goalId = currentAgentId;

  useEffect(() => {
    const { dateFrom, dateTo } = getDateRange();
    dispatch(fetchPerformanceData({ dateFrom, dateTo }));
    dispatch(fetchAnalyticsData({ dateFrom, dateTo }));
    dispatch(fetchDashboardStats());
    dispatch(fetchConversationMetrics({ timeRange: '30d' }));
    dispatch(fetchAgentMetrics({ timeRange: '30d' }));
  }, [dispatch, selectedTimeRange, goalId]);

  const handleTimeRangeChange = (timeRange: string) => {
    setSelectedTimeRange(timeRange);
  };

  const handleExport = (format: "csv" | "pdf" | "excel") => {
    console.log(`Exporting analytics as ${format}`);
  };

  const satisfactionData = [
    { rating: t("analytics.fiveStars"), count: 68, percentage: 68 },
    { rating: t("analytics.fourStars"), count: 22, percentage: 22 },
    { rating: t("analytics.threeStars"), count: 7, percentage: 7 },
    { rating: t("analytics.twoStars"), count: 2, percentage: 2 },
    { rating: t("analytics.oneStar"), count: 1, percentage: 1 },
  ];

  const channelData = [
    { name: t("analytics.website"), value: 65, color: "#3b82f6" },
    { name: t("analytics.mobileApp"), value: 25, color: "#10b981" },
    { name: t("analytics.socialMedia"), value: 10, color: "#f59e0b" },
  ];

  const MetricCard = ({ title, value, change, icon: Icon, description }: any) => (
    <Card>
      <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
        <CardTitle className="text-sm font-medium">{title}</CardTitle>
        <Icon className="h-4 w-4 text-muted-foreground" />
      </CardHeader>
      <CardContent>
        <div className="text-2xl font-bold">{value}</div>
        {change && (
          <div className="flex items-center text-xs text-muted-foreground">
            <TrendingUp className="mr-1 h-3 w-3" />
            {change}
          </div>
        )}
        {description && <p className="text-xs text-muted-foreground mt-1">{description}</p>}
      </CardContent>
    </Card>
  );

  if (isLoading) {
    return (
      <div className="space-y-6">
        <div className="flex items-center justify-between">
          <Skeleton className="h-8 w-[200px]" />
          <div className="flex space-x-2">
            <Skeleton className="h-10 w-[120px]" />
            <Skeleton className="h-10 w-[120px]" />
          </div>
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
          <h1 className="text-3xl font-bold">{t("analytics.title")}</h1>
          <p className="text-muted-foreground">{t("analytics.subtitle")}</p>
        </div>
        <div className="flex items-center space-x-2">
          <Select value={selectedTimeRange} onValueChange={handleTimeRangeChange}>
            <SelectTrigger className="w-[140px]">
              <Calendar className="mr-2 h-4 w-4" />
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="7d">{t("analytics.last7Days")}</SelectItem>
              <SelectItem value="30d">{t("analytics.last30Days")}</SelectItem>
              <SelectItem value="90d">{t("analytics.last90Days")}</SelectItem>
              <SelectItem value="1y">{t("analytics.thisYear")}</SelectItem>
            </SelectContent>
          </Select>
          <Select onValueChange={handleExport}>
            <SelectTrigger className="w-[120px]">
              <Download className="mr-2 h-4 w-4" />
              <SelectValue placeholder={t("analytics.exportData")} />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="csv">CSV</SelectItem>
              <SelectItem value="excel">Excel</SelectItem>
              <SelectItem value="pdf">PDF</SelectItem>
            </SelectContent>
          </Select>
        </div>
      </div>

      <Tabs value={activeTab} onValueChange={setActiveTab} className="space-y-4">
        <TabsList>
          <TabsTrigger value="performance">{t("analytics.performance")}</TabsTrigger>
          <TabsTrigger value="satisfaction">{t("analytics.satisfaction")}</TabsTrigger>
          <TabsTrigger value="productivity">{t("analytics.productivity")}</TabsTrigger>
          <TabsTrigger value="goals">{t("analytics.goals")}</TabsTrigger>
        </TabsList>

        <TabsContent value="performance" className="space-y-4">
          <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
            <MetricCard
              title={t("analytics.totalConversations")}
              value={conversationMetrics?.total ?? dashboardStats?.totalConversations ?? "—"}
              change={t("analytics.performanceChange1")}
              icon={MessageSquare}
              description={t("analytics.thisWeek")}
            />
            <MetricCard
              title={t("analytics.averageResponseTime")}
              value="2.1m"
              change={t("analytics.performanceChange2")}
              icon={Clock}
              description={t("analytics.firstResponseTime")}
            />
            <MetricCard
              title={t("analytics.satisfactionScore")}
              value="4.7"
              change={t("analytics.performanceChange3")}
              icon={Star}
              description={t("analytics.averageCustomerRating")}
            />
            <MetricCard
              title={t("analytics.resolutionRate")}
              value="94%"
              change={t("analytics.performanceChange4")}
              icon={Target}
              description={t("analytics.successfullyResolved")}
            />
          </div>

          <div className="grid gap-4 md:grid-cols-2">
            <Card>
              <CardHeader>
                <CardTitle>{t("analytics.conversationTrends")}</CardTitle>
                <CardDescription>{t("analytics.dailyConversationVolume")}</CardDescription>
              </CardHeader>
              <CardContent>
                <ResponsiveContainer width="100%" height={300}>
                  <AreaChart data={performanceData ? [
                    { date: "Week 1", responseTime: performanceData.averageResponseTime * 0.8, uptime: performanceData.systemUptime * 0.95 },
                    { date: "Week 2", responseTime: performanceData.averageResponseTime * 0.9, uptime: performanceData.systemUptime * 0.97 },
                    { date: "Week 3", responseTime: performanceData.averageResponseTime * 1.1, uptime: performanceData.systemUptime * 0.98 },
                    { date: "Week 4", responseTime: performanceData.averageResponseTime, uptime: performanceData.systemUptime }
                  ] : []}>
                    <CartesianGrid strokeDasharray="3 3" />
                    <XAxis dataKey="date" tickFormatter={(value) => new Date(value).toLocaleDateString()} />
                    <YAxis />
                    <Tooltip />
                    <Area type="monotone" dataKey="conversations" stackId="1" stroke="#3b82f6" fill="#3b82f6" fillOpacity={0.6} />
                  </AreaChart>
                </ResponsiveContainer>
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle>{t("analytics.responseTimeAnalysis")}</CardTitle>
                <CardDescription>{t("analytics.averageResponseTimeTrends")}</CardDescription>
              </CardHeader>
              <CardContent>
                <ResponsiveContainer width="100%" height={300}>
                  <LineChart data={performanceData ? [
                    { date: "Week 1", conversations: (conversationMetrics?.total || 100) * 0.7, rating: (conversationMetrics?.averageRating || 4.5) * 0.9 },
                    { date: "Week 2", conversations: (conversationMetrics?.total || 100) * 0.8, rating: (conversationMetrics?.averageRating || 4.5) * 0.95 },
                    { date: "Week 3", conversations: (conversationMetrics?.total || 100) * 0.9, rating: (conversationMetrics?.averageRating || 4.5) * 1.05 },
                    { date: "Week 4", conversations: conversationMetrics?.total || 100, rating: conversationMetrics?.averageRating || 4.5 }
                  ] : []}>
                    <CartesianGrid strokeDasharray="3 3" />
                    <XAxis dataKey="date" tickFormatter={(value) => new Date(value).toLocaleDateString()} />
                    <YAxis />
                    <Tooltip />
                    <Line type="monotone" dataKey="responseTime" stroke="#10b981" strokeWidth={3} />
                  </LineChart>
                </ResponsiveContainer>
              </CardContent>
            </Card>
          </div>
        </TabsContent>

        <TabsContent value="satisfaction" className="space-y-4">
          <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
            <MetricCard title="Overall Rating" value="4.7" change="+0.3 from last period" icon={Star} description="Average customer rating" />
            <MetricCard title="5-Star Ratings" value="68%" change="+8% from last period" icon={Award} description="Excellent ratings" />
            <MetricCard title="Customer Retention" value="92%" change="+3% from last period" icon={Users} description="Return customers" />
            <MetricCard title="Recommendation Score" value="8.9" change="+0.5 from last period" icon={TrendingUp} description="Net Promoter Score" />
          </div>

          <div className="grid gap-4 md:grid-cols-2">
            <Card>
              <CardHeader>
                <CardTitle>{t("analytics.ratingDistribution")}</CardTitle>
                <CardDescription>{t("analytics.customerSatisfactionBreakdown")}</CardDescription>
              </CardHeader>
              <CardContent>
                <ResponsiveContainer width="100%" height={300}>
                  <BarChart data={satisfactionData} layout="horizontal">
                    <CartesianGrid strokeDasharray="3 3" />
                    <XAxis type="number" />
                    <YAxis dataKey="rating" type="category" />
                    <Tooltip />
                    <Bar dataKey="percentage" fill="#3b82f6" />
                  </BarChart>
                </ResponsiveContainer>
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle>{t("analytics.satisfactionTrends")}</CardTitle>
                <CardDescription>{t("analytics.customerSatisfactionOverTime")}</CardDescription>
              </CardHeader>
              <CardContent>
                <ResponsiveContainer width="100%" height={300}>
                  <LineChart data={performanceData ? [
                    { date: "Week 1", errorRate: performanceData.errorRate * 1.2, responseTime: performanceData.averageResponseTime * 1.1 },
                    { date: "Week 2", errorRate: performanceData.errorRate * 1.1, responseTime: performanceData.averageResponseTime * 1.05 },
                    { date: "Week 3", errorRate: performanceData.errorRate * 0.9, responseTime: performanceData.averageResponseTime * 0.95 },
                    { date: "Week 4", errorRate: performanceData.errorRate, responseTime: performanceData.averageResponseTime }
                  ] : []}>
                    <CartesianGrid strokeDasharray="3 3" />
                    <XAxis dataKey="date" tickFormatter={(value) => new Date(value).toLocaleDateString()} />
                    <YAxis domain={[0, 5]} />
                    <Tooltip />
                    <Line type="monotone" dataKey="avgRating" stroke="#f59e0b" strokeWidth={3} />
                  </LineChart>
                </ResponsiveContainer>
              </CardContent>
            </Card>
          </div>
        </TabsContent>

        <TabsContent value="productivity" className="space-y-4">
          <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
            <MetricCard
              title={t("analytics.conversationsPerHour")}
              value="3.2"
              change={t("analytics.productivityChange1")}
              icon={Activity}
              description={t("analytics.averageProductivity")}
            />
            <MetricCard
              title={t("analytics.activeHours")}
              value="7.5h"
              change={t("analytics.productivityChange2")}
              icon={Clock}
              description={t("analytics.dailyActiveTime")}
            />
            <MetricCard
              title={t("analytics.multitaskingScore")}
              value="85%"
              change={t("analytics.productivityChange3")}
              icon={Target}
              description={t("analytics.concurrentConversations")}
            />
            <MetricCard
              title={t("analytics.efficiencyRating")}
              value="A+"
              change={t("analytics.productivityChange4")}
              icon={Award}
              description={t("analytics.overallPerformanceGrade")}
            />
          </div>

          <Card>
            <CardHeader>
              <CardTitle>{t("analytics.dailyProductivity")}</CardTitle>
              <CardDescription>{t("analytics.conversationsHandledPerDay")}</CardDescription>
            </CardHeader>
            <CardContent>
              <ResponsiveContainer width="100%" height={400}>
                <BarChart data={agentMetrics ? [
                  { name: "Active Agents", value: agentMetrics.activeAgents },
                  { name: "Total Agents", value: agentMetrics.totalAgents },
                  { name: "Avg Response Time", value: agentMetrics.averageResponseTime },
                  { name: "Avg Rating", value: agentMetrics.averageRating * 20 }
                ] : []}>
                  <CartesianGrid strokeDasharray="3 3" />
                  <XAxis dataKey="name" />
                  <YAxis />
                  <Tooltip />
                  <Bar dataKey="conversations" fill="#3b82f6" />
                </BarChart>
              </ResponsiveContainer>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="goals" className="space-y-4">
          <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
            <Card>
              <CardHeader>
                <CardTitle>{t("analytics.dailyGoal")}</CardTitle>
                <CardDescription>{t("analytics.conversationsTarget")}</CardDescription>
              </CardHeader>
              <CardContent>
                <div className="space-y-2">
                  <div className="flex justify-between">
                    <span className="text-sm">{t("analytics.progress")}</span>
                    <span className="text-sm font-medium">14/15</span>
                  </div>
                  <div className="w-full bg-gray-200 rounded-full h-2">
                    <div className="bg-blue-600 h-2 rounded-full" style={{ width: "93%" }}></div>
                  </div>
                  <p className="text-xs text-muted-foreground">{t("analytics.oneMoreToReachGoal")}</p>
                </div>
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle>{t("analytics.weeklyGoal")}</CardTitle>
                <CardDescription>{t("analytics.responseTimeTarget")}</CardDescription>
              </CardHeader>
              <CardContent>
                <div className="space-y-2">
                  <div className="flex justify-between">
                    <span className="text-sm">{t("analytics.average")}</span>
                    <span className="text-sm font-medium">2.1m / 2.5m</span>
                  </div>
                  <div className="w-full bg-gray-200 rounded-full h-2">
                    <div className="bg-green-600 h-2 rounded-full" style={{ width: "84%" }}></div>
                  </div>
                  <p className="text-xs text-muted-foreground">{t("analytics.exceedingTarget")}</p>
                </div>
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle>{t("analytics.monthlyGoal")}</CardTitle>
                <CardDescription>{t("analytics.satisfactionTarget")}</CardDescription>
              </CardHeader>
              <CardContent>
                <div className="space-y-2">
                  <div className="flex justify-between">
                    <span className="text-sm">{t("analytics.rating")}</span>
                    <span className="text-sm font-medium">4.7 / 4.5</span>
                  </div>
                  <div className="w-full bg-gray-200 rounded-full h-2">
                    <div className="bg-yellow-600 h-2 rounded-full" style={{ width: "100%" }}></div>
                  </div>
                  <p className="text-xs text-muted-foreground">{t("analytics.goalAchieved")}</p>
                </div>
              </CardContent>
            </Card>
          </div>

          <Card>
            <CardHeader>
              <CardTitle>{t("analytics.goalProgressOverTime")}</CardTitle>
              <CardDescription>{t("analytics.trackProgressTowardsTargets")}</CardDescription>
            </CardHeader>
            <CardContent>
              <ResponsiveContainer width="100%" height={300}>
                <LineChart data={conversationMetrics ? [
                  { date: "Week 1", total: conversationMetrics.total * 0.7, completed: conversationMetrics.completed * 0.8, active: conversationMetrics.active * 0.6 },
                  { date: "Week 2", total: conversationMetrics.total * 0.8, completed: conversationMetrics.completed * 0.85, active: conversationMetrics.active * 0.8 },
                  { date: "Week 3", total: conversationMetrics.total * 0.9, completed: conversationMetrics.completed * 0.9, active: conversationMetrics.active * 0.9 },
                  { date: "Week 4", total: conversationMetrics.total, completed: conversationMetrics.completed, active: conversationMetrics.active }
                ] : []}>
                  <CartesianGrid strokeDasharray="3 3" />
                  <XAxis dataKey="date" tickFormatter={(value) => new Date(value).toLocaleDateString()} />
                  <YAxis />
                  <Tooltip />
                  <Line type="monotone" dataKey="conversations" stroke="#3b82f6" strokeWidth={2} name={t("analytics.conversations")} />
                  <Line type="monotone" dataKey="avgRating" stroke="#10b981" strokeWidth={2} name={t("analytics.rating")} />
                </LineChart>
              </ResponsiveContainer>
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>
    </div>
  );
};

export default AnalyticsPage;
