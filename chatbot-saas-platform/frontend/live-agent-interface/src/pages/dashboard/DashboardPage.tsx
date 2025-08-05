import { useEffect } from "react";
import { useDispatch, useSelector } from "react-redux";
import { useTranslation } from "react-i18next";
import { AppDispatch, RootState } from "../../store/store";
import { fetchConversations } from "../../store/slices/conversationSlice";
import { fetchAgentProfile, fetchAgentStats, updateAgentStatusRealtime } from "../../store/slices/agentSlice";
import { agentSignalRService } from "../../services/signalr";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "../../components/ui/card";
import { Button } from "../../components/ui/button";
import { Badge } from "../../components/ui/badge";
import { Avatar, AvatarFallback, AvatarImage } from "../../components/ui/avatar";
import { Skeleton } from "../../components/ui/skeleton";
import { Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, LineChart, Line, PieChart, Pie, Cell } from "recharts";
import { MessageSquare, Users, Clock, Star, TrendingUp, Activity, Phone, Mail, BarChart3 } from "lucide-react";

const DashboardPage = () => {
  const { t } = useTranslation();
  const dispatch = useDispatch<AppDispatch>();
  const { conversations, isLoading: conversationsLoading } = useSelector((state: RootState) => state.conversations);
  const { currentAgent, stats: agentStats, isSignalRConnected } = useSelector((state: RootState) => state.agent);

  useEffect(() => {
    dispatch(fetchConversations());
    if (currentAgent?.id) {
      dispatch(fetchAgentProfile(currentAgent.id));
      dispatch(fetchAgentStats(currentAgent.id));

      if (isSignalRConnected) {
        agentSignalRService.setOnAgentStatusChanged((statusUpdate: any) => {
          dispatch(updateAgentStatusRealtime(statusUpdate));
        });
      }

      const interval = setInterval(() => {
        if (!isSignalRConnected) {
          dispatch(fetchAgentStats(currentAgent.id));
        }
      }, 30000);

      return () => clearInterval(interval);
    }
  }, [dispatch, currentAgent?.id, isSignalRConnected]);

  const getStatusBadge = (status: string) => {
    const variants: Record<string, "default" | "secondary" | "destructive"> = {
      active: "default",
      waiting: "secondary",
      resolved: "default",
      escalated: "destructive",
    };
    return (
      <Badge variant={variants[status] || "secondary"}>
        <span>{t(`conversation.status.${status}`)}</span>
      </Badge>
    );
  };

  if (conversationsLoading && !conversations) {
    return (
      <div className="space-y-6">
        <div className="flex items-center justify-between">
          <Skeleton className="h-8 w-[200px]" />
          <Skeleton className="h-10 w-[120px]" />
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

  const recentConversations = conversations?.slice(0, 5) || [];
  const performanceData = [
    { date: "2024-01-01", conversations: 12, avgRating: 4.5 },
    { date: "2024-01-02", conversations: 15, avgRating: 4.7 },
    { date: "2024-01-03", conversations: 8, avgRating: 4.2 },
    { date: "2024-01-04", conversations: 18, avgRating: 4.8 },
    { date: "2024-01-05", conversations: 14, avgRating: 4.6 },
    { date: "2024-01-06", conversations: 16, avgRating: 4.9 },
    { date: "2024-01-07", conversations: 11, avgRating: 4.4 },
  ];

  const channelData = [
    { name: t("dashboard.website"), value: 65, color: "#3b82f6" },
    { name: t("dashboard.mobileApp"), value: 25, color: "#10b981" },
    { name: t("dashboard.socialMedia"), value: 10, color: "#f59e0b" },
  ];

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">{t("dashboard.title")}</h1>
          <p className="text-muted-foreground">{t("dashboard.subtitle")}</p>
        </div>
        <div className="flex items-center space-x-4">
          <div className="flex items-center space-x-1">
            <div className={`w-2 h-2 rounded-full ${isSignalRConnected ? "bg-green-500" : "bg-red-500"}`} />
            <span className="text-xs text-muted-foreground">{isSignalRConnected ? t("dashboard.liveUpdates") : t("dashboard.staticData")}</span>
          </div>
          <div className="flex items-center space-x-2">
            <Badge variant={currentAgent?.status === "online" ? "default" : "secondary"}>
              <Activity className="mr-1 h-3 w-3" />
              {t(`agent.status.${currentAgent?.status || "offline"}`)}
            </Badge>
            <Button variant="outline">{t("dashboard.takeBreak")}</Button>
          </div>
        </div>
      </div>

      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">{t("dashboard.activeConversations")}</CardTitle>
            <MessageSquare className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{conversations?.filter((c: any) => c.status === "active").length || 3}</div>
            <div className="flex items-center text-xs text-muted-foreground">
              <TrendingUp className="mr-1 h-3 w-3" />
              {t("dashboard.currentlyHandling")}
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">{t("dashboard.todayConversations")}</CardTitle>
            <BarChart3 className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{agentStats?.totalConversations || 24}</div>
            <div className="flex items-center text-xs text-muted-foreground">
              <TrendingUp className="mr-1 h-3 w-3" />
              {t("dashboard.changeFromYesterday")}
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">{t("dashboard.avgResponseTime")}</CardTitle>
            <Clock className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{agentStats?.averageResponseTime || 2.3}m</div>
            <div className="flex items-center text-xs text-muted-foreground">
              <TrendingUp className="mr-1 h-3 w-3" />
              {t("dashboard.changeFromLastWeek")}
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">{t("dashboard.satisfactionRating")}</CardTitle>
            <Star className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{agentStats?.averageRating || 4.8}</div>
            <div className="flex items-center text-xs text-muted-foreground">
              <TrendingUp className="mr-1 h-3 w-3" />
              {t("dashboard.ratingChangeFromLastWeek")}
            </div>
          </CardContent>
        </Card>
      </div>

      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
        <Card className="col-span-2">
          <CardHeader>
            <CardTitle>{t("dashboard.performanceTrends")}</CardTitle>
            <CardDescription>{t("dashboard.performanceTrendsDesc")}</CardDescription>
          </CardHeader>
          <CardContent>
            <ResponsiveContainer width="100%" height={300}>
              <LineChart data={performanceData}>
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis dataKey="date" tickFormatter={(value) => new Date(value).toLocaleDateString()} />
                <YAxis yAxisId="left" />
                <YAxis yAxisId="right" orientation="right" />
                <Tooltip />
                <Bar yAxisId="left" dataKey="conversations" fill="#3b82f6" />
                <Line yAxisId="right" type="monotone" dataKey="avgRating" stroke="#10b981" strokeWidth={2} />
              </LineChart>
            </ResponsiveContainer>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>{t("dashboard.channelDistribution")}</CardTitle>
            <CardDescription>{t("dashboard.channelDistributionDesc")}</CardDescription>
          </CardHeader>
          <CardContent>
            <ResponsiveContainer width="100%" height={200}>
              <PieChart>
                <Pie data={channelData} cx="50%" cy="50%" innerRadius={40} outerRadius={80} paddingAngle={5} dataKey="value">
                  {channelData.map((entry, index) => (
                    <Cell key={`cell-${index}`} fill={entry.color} />
                  ))}
                </Pie>
                <Tooltip />
              </PieChart>
            </ResponsiveContainer>
            <div className="flex justify-center space-x-4 mt-4">
              {channelData.map((entry, index) => (
                <div key={index} className="flex items-center space-x-2">
                  <div className="w-3 h-3 rounded-full" style={{ backgroundColor: entry.color }} />
                  <span className="text-sm">{entry.name}</span>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
      </div>

      <div className="grid gap-4 md:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle>{t("dashboard.recentConversations")}</CardTitle>
            <CardDescription>{t("dashboard.recentConversationsDesc")}</CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            {recentConversations.length > 0 ? (
              recentConversations.map((conversation: any) => (
                <div key={conversation.id} className="flex items-center justify-between p-3 border rounded-lg">
                  <div className="flex items-center space-x-3">
                    <Avatar className="h-8 w-8">
                      <AvatarImage src={conversation.customer?.avatar} />
                      <AvatarFallback>{conversation.customer?.name?.charAt(0) || "U"}</AvatarFallback>
                    </Avatar>
                    <div>
                      <p className="font-medium">{conversation.customer?.name || t("dashboard.anonymous")}</p>
                      <p className="text-sm text-muted-foreground">{conversation.lastMessage?.content?.substring(0, 50)}...</p>
                    </div>
                  </div>
                  <div className="flex items-center space-x-2">
                    {getStatusBadge(conversation.status)}
                    <span className="text-xs text-muted-foreground">{new Date(conversation.updatedAt).toLocaleTimeString()}</span>
                  </div>
                </div>
              ))
            ) : (
              <div className="text-center py-8">
                <MessageSquare className="mx-auto h-12 w-12 text-muted-foreground" />
                <h3 className="mt-2 text-sm font-semibold">{t("dashboard.noActiveConversations")}</h3>
                <p className="mt-1 text-sm text-muted-foreground">{t("dashboard.noActiveConversationsDesc")}</p>
              </div>
            )}
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>{t("dashboard.agentProfile")}</CardTitle>
            <CardDescription>{t("dashboard.agentProfileDesc")}</CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            {currentAgent ? (
              <div className="space-y-4">
                <div className="flex items-center space-x-4">
                  <Avatar className="h-16 w-16">
                    <AvatarImage src={currentAgent.avatar} />
                    <AvatarFallback>
                      {currentAgent.name
                        .split(" ")
                        .map((n) => n[0])
                        .join("")}
                    </AvatarFallback>
                  </Avatar>
                  <div>
                    <h3 className="text-lg font-semibold">{currentAgent.name}</h3>
                    <p className="text-sm text-muted-foreground">{currentAgent.email}</p>
                    <Badge variant={currentAgent.status === "online" ? "default" : "secondary"}>{t(`agent.status.${currentAgent.status}`)}</Badge>
                  </div>
                </div>

                <div className="grid gap-3">
                  <div className="flex items-center justify-between">
                    <span className="text-sm font-medium">{t("dashboard.totalConversations")}</span>
                    <span className="text-sm">{agentStats?.totalConversations || 0}</span>
                  </div>
                  <div className="flex items-center justify-between">
                    <span className="text-sm font-medium">{t("dashboard.avgRating")}</span>
                    <div className="flex items-center">
                      <Star className="mr-1 h-4 w-4 text-yellow-400" />
                      <span className="text-sm">{agentStats?.averageRating || 0}</span>
                    </div>
                  </div>
                  <div className="flex items-center justify-between">
                    <span className="text-sm font-medium">{t("dashboard.responseTime")}</span>
                    <span className="text-sm">{agentStats?.averageResponseTime || 0}m</span>
                  </div>
                  <div className="flex items-center justify-between">
                    <span className="text-sm font-medium">{t("dashboard.resolutionRate")}</span>
                    <span className="text-sm">{agentStats?.resolutionRate || 0}%</span>
                  </div>
                </div>

                <div className="pt-4 border-t">
                  <h4 className="font-medium mb-2">{t("dashboard.quickActions")}</h4>
                  <div className="flex space-x-2">
                    <Button size="sm" variant="outline">
                      <Phone className="mr-2 h-4 w-4" />
                      {t("dashboard.makeCall")}
                    </Button>
                    <Button size="sm" variant="outline">
                      <Mail className="mr-2 h-4 w-4" />
                      {t("dashboard.sendEmail")}
                    </Button>
                  </div>
                </div>
              </div>
            ) : (
              <div className="text-center py-8">
                <Users className="mx-auto h-12 w-12 text-muted-foreground" />
                <h3 className="mt-2 text-sm font-semibold">{t("dashboard.profileNotLoaded")}</h3>
                <p className="mt-1 text-sm text-muted-foreground">{t("dashboard.profileNotLoadedDesc")}</p>
              </div>
            )}
          </CardContent>
        </Card>
      </div>
    </div>
  );
};

export default DashboardPage;
