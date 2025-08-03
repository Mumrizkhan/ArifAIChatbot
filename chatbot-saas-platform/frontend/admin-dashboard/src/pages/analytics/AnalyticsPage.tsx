import React, { useEffect, useState } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { useTranslation } from 'react-i18next';
import { AppDispatch, RootState } from '../../store/store';
import {
  fetchConversationMetrics,
  fetchAgentMetrics,
  fetchBotMetrics,
  setTimeRange,
  setSelectedTenant,
  updateConversationMetricsRealtime,
  updateAgentMetricsRealtime,
} from '../../store/slices/analyticsSlice';
import { adminSignalRService } from '../../services/signalr';
import { Button } from '../../components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../../components/ui/card';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '../../components/ui/select';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '../../components/ui/tabs';
import { Badge } from '../../components/ui/badge';
import { Skeleton } from '../../components/ui/skeleton';
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
} from 'recharts';
import {
  BarChart3,
  TrendingUp,
  Users,
  MessageSquare,
  Clock,
  Star,
  Download,
  Filter,
  Calendar,
  Bot,
  UserCheck,
} from 'lucide-react';

const AnalyticsPage: React.FC = () => {
  const { t } = useTranslation();
  const dispatch = useDispatch<AppDispatch>();
  const {
    conversationMetrics,
    agentMetrics,
    isLoading,
    selectedTimeRange,
    selectedTenant,
    isSignalRConnected,
  } = useSelector((state: RootState) => state.analytics);

  const [activeTab, setActiveTab] = useState('conversations');

  useEffect(() => {
    dispatch(fetchConversationMetrics({ timeRange: selectedTimeRange, tenantId: selectedTenant || undefined }));
    dispatch(fetchAgentMetrics({ timeRange: selectedTimeRange, tenantId: selectedTenant || undefined }));
    dispatch(fetchBotMetrics({ timeRange: selectedTimeRange, tenantId: selectedTenant || undefined }));

    if (isSignalRConnected) {
      adminSignalRService.setOnConversationMetricsUpdate((metrics) => {
        dispatch(updateConversationMetricsRealtime(metrics));
      });

      adminSignalRService.setOnAgentMetricsUpdate((metrics) => {
        dispatch(updateAgentMetricsRealtime(metrics));
      });

      adminSignalRService.requestConversationMetricsUpdate(selectedTimeRange, selectedTenant || undefined);
      adminSignalRService.requestAgentMetricsUpdate(selectedTimeRange, selectedTenant || undefined);
    }
  }, [dispatch, selectedTimeRange, selectedTenant, isSignalRConnected]);

  const handleTimeRangeChange = (timeRange: string) => {
    dispatch(setTimeRange(timeRange));
    if (isSignalRConnected) {
      adminSignalRService.requestConversationMetricsUpdate(timeRange, selectedTenant || undefined);
      adminSignalRService.requestAgentMetricsUpdate(timeRange, selectedTenant || undefined);
    }
  };

  const handleTenantChange = (tenantId: string) => {
    const newTenantId = tenantId === 'all' ? null : tenantId;
    dispatch(setSelectedTenant(newTenantId));
    if (isSignalRConnected) {
      adminSignalRService.requestConversationMetricsUpdate(selectedTimeRange, newTenantId || undefined);
      adminSignalRService.requestAgentMetricsUpdate(selectedTimeRange, newTenantId || undefined);
    }
  };

  const conversationTrendData = [
    { date: '2024-01-01', conversations: 120, resolved: 110, avgDuration: 15 },
    { date: '2024-01-02', conversations: 135, resolved: 125, avgDuration: 18 },
    { date: '2024-01-03', conversations: 98, resolved: 88, avgDuration: 12 },
    { date: '2024-01-04', conversations: 156, resolved: 142, avgDuration: 20 },
    { date: '2024-01-05', conversations: 178, resolved: 165, avgDuration: 16 },
    { date: '2024-01-06', conversations: 145, resolved: 138, avgDuration: 14 },
    { date: '2024-01-07', conversations: 167, resolved: 155, avgDuration: 17 },
  ];

  const channelDistributionData = [
    { name: 'Website', value: 45, color: '#3b82f6' },
    { name: 'Mobile App', value: 30, color: '#10b981' },
    { name: 'Social Media', value: 15, color: '#f59e0b' },
    { name: 'Email', value: 10, color: '#ef4444' },
  ];

  const agentPerformanceData = [
    { name: 'John Doe', conversations: 45, avgRating: 4.8, responseTime: 2.3 },
    { name: 'Jane Smith', conversations: 38, avgRating: 4.9, responseTime: 1.8 },
    { name: 'Mike Johnson', conversations: 42, avgRating: 4.6, responseTime: 3.1 },
    { name: 'Sarah Wilson', conversations: 35, avgRating: 4.7, responseTime: 2.5 },
    { name: 'David Brown', conversations: 40, avgRating: 4.5, responseTime: 2.9 },
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
        {description && (
          <p className="text-xs text-muted-foreground mt-1">{description}</p>
        )}
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
          <h1 className="text-3xl font-bold">{t('analytics.title')}</h1>
          <p className="text-muted-foreground">
            Comprehensive analytics and performance insights
          </p>
        </div>
        <div className="flex items-center space-x-2">
          <div className="flex items-center space-x-1">
            <div 
              className={`w-2 h-2 rounded-full ${isSignalRConnected ? 'bg-green-500' : 'bg-red-500'}`}
            />
            <span className="text-xs text-muted-foreground">
              {isSignalRConnected ? 'Live Updates' : 'Static Data'}
            </span>
          </div>
          <Select value={selectedTimeRange} onValueChange={handleTimeRangeChange}>
            <SelectTrigger className="w-[140px]">
              <Calendar className="mr-2 h-4 w-4" />
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="7d">{t('analytics.last7Days')}</SelectItem>
              <SelectItem value="30d">{t('analytics.last30Days')}</SelectItem>
              <SelectItem value="90d">{t('analytics.last90Days')}</SelectItem>
              <SelectItem value="1y">{t('analytics.thisYear')}</SelectItem>
            </SelectContent>
          </Select>
          <Select value={selectedTenant || 'all'} onValueChange={handleTenantChange}>
            <SelectTrigger className="w-[140px]">
              <Filter className="mr-2 h-4 w-4" />
              <SelectValue placeholder="All Tenants" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All Tenants</SelectItem>
              <SelectItem value="tenant1">Acme Corp</SelectItem>
              <SelectItem value="tenant2">TechStart Inc</SelectItem>
              <SelectItem value="tenant3">Global Solutions</SelectItem>
            </SelectContent>
          </Select>
          <Button>
            <Download className="mr-2 h-4 w-4" />
            {t('common.export')}
          </Button>
        </div>
      </div>

      <Tabs value={activeTab} onValueChange={setActiveTab} className="space-y-4">
        <TabsList>
          <TabsTrigger value="conversations">{t('analytics.conversationMetrics')}</TabsTrigger>
          <TabsTrigger value="agents">{t('analytics.agentPerformance')}</TabsTrigger>
          <TabsTrigger value="bot">{t('analytics.botEffectiveness')}</TabsTrigger>
          <TabsTrigger value="custom">{t('analytics.customReports')}</TabsTrigger>
        </TabsList>

        <TabsContent value="conversations" className="space-y-4">
          <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
            <MetricCard
              title="Total Conversations"
              value={conversationMetrics?.totalConversations || 1247}
              change="+12% from last period"
              icon={MessageSquare}
              description="All conversation channels"
            />
            <MetricCard
              title="Average Duration"
              value={`${conversationMetrics?.averageDuration || 15.2} min`}
              change="-3% from last period"
              icon={Clock}
              description="Time to resolution"
            />
            <MetricCard
              title="Resolution Rate"
              value={`${conversationMetrics?.resolutionRate || 92}%`}
              change="+5% from last period"
              icon={TrendingUp}
              description="Successfully resolved"
            />
            <MetricCard
              title="Satisfaction Score"
              value={conversationMetrics?.satisfactionScore || 4.6}
              change="+0.2 from last period"
              icon={Star}
              description="Average customer rating"
            />
          </div>

          <div className="grid gap-4 md:grid-cols-2">
            <Card>
              <CardHeader>
                <CardTitle>Conversation Trends</CardTitle>
                <CardDescription>Daily conversation volume and resolution rates</CardDescription>
              </CardHeader>
              <CardContent>
                <ResponsiveContainer width="100%" height={300}>
                  <AreaChart data={conversationTrendData}>
                    <CartesianGrid strokeDasharray="3 3" />
                    <XAxis dataKey="date" tickFormatter={(value) => new Date(value).toLocaleDateString()} />
                    <YAxis />
                    <Tooltip />
                    <Area type="monotone" dataKey="conversations" stackId="1" stroke="#3b82f6" fill="#3b82f6" fillOpacity={0.6} />
                    <Area type="monotone" dataKey="resolved" stackId="2" stroke="#10b981" fill="#10b981" fillOpacity={0.6} />
                  </AreaChart>
                </ResponsiveContainer>
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle>Channel Distribution</CardTitle>
                <CardDescription>Conversation volume by channel</CardDescription>
              </CardHeader>
              <CardContent>
                <ResponsiveContainer width="100%" height={300}>
                  <PieChart>
                    <Pie
                      data={channelDistributionData}
                      cx="50%"
                      cy="50%"
                      innerRadius={60}
                      outerRadius={100}
                      paddingAngle={5}
                      dataKey="value"
                    >
                      {channelDistributionData.map((entry, index) => (
                        <Cell key={`cell-${index}`} fill={entry.color} />
                      ))}
                    </Pie>
                    <Tooltip />
                  </PieChart>
                </ResponsiveContainer>
                <div className="flex justify-center space-x-4 mt-4">
                  {channelDistributionData.map((entry, index) => (
                    <div key={index} className="flex items-center space-x-2">
                      <div
                        className="w-3 h-3 rounded-full"
                        style={{ backgroundColor: entry.color }}
                      />
                      <span className="text-sm">{entry.name}</span>
                    </div>
                  ))}
                </div>
              </CardContent>
            </Card>
          </div>
        </TabsContent>

        <TabsContent value="agents" className="space-y-4">
          <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
            <MetricCard
              title="Total Agents"
              value={agentMetrics?.totalAgents || 24}
              change="+2 from last month"
              icon={Users}
              description="Registered agents"
            />
            <MetricCard
              title="Active Agents"
              value={agentMetrics?.activeAgents || 18}
              change="Currently online"
              icon={UserCheck}
              description="Online right now"
            />
            <MetricCard
              title="Avg Response Time"
              value={`${agentMetrics?.averageResponseTime || 2.4} min`}
              change="-15% from last period"
              icon={Clock}
              description="First response time"
            />
            <MetricCard
              title="Average Rating"
              value={agentMetrics?.averageRating || 4.7}
              change="+0.1 from last period"
              icon={Star}
              description="Customer satisfaction"
            />
          </div>

          <Card>
            <CardHeader>
              <CardTitle>Agent Performance</CardTitle>
              <CardDescription>Individual agent metrics and performance</CardDescription>
            </CardHeader>
            <CardContent>
              <ResponsiveContainer width="100%" height={400}>
                <BarChart data={agentPerformanceData}>
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

        <TabsContent value="bot" className="space-y-4">
          <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
            <MetricCard
              title="Bot Accuracy"
              value="87%"
              change="+3% from last period"
              icon={Bot}
              description="Correct responses"
            />
            <MetricCard
              title="Fallback Rate"
              value="13%"
              change="-2% from last period"
              icon={TrendingUp}
              description="Escalated to humans"
            />
            <MetricCard
              title="Training Sessions"
              value="156"
              change="+12 this month"
              icon={BarChart3}
              description="Model improvements"
            />
            <MetricCard
              title="Knowledge Base"
              value="2,847"
              change="+45 documents"
              icon={MessageSquare}
              description="Total documents"
            />
          </div>

          <div className="grid gap-4 md:grid-cols-2">
            <Card>
              <CardHeader>
                <CardTitle>Bot Performance Trends</CardTitle>
                <CardDescription>Accuracy and response quality over time</CardDescription>
              </CardHeader>
              <CardContent>
                <ResponsiveContainer width="100%" height={300}>
                  <LineChart data={conversationTrendData}>
                    <CartesianGrid strokeDasharray="3 3" />
                    <XAxis dataKey="date" tickFormatter={(value) => new Date(value).toLocaleDateString()} />
                    <YAxis />
                    <Tooltip />
                    <Line type="monotone" dataKey="conversations" stroke="#3b82f6" strokeWidth={2} />
                  </LineChart>
                </ResponsiveContainer>
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle>Intent Recognition</CardTitle>
                <CardDescription>Most common user intents</CardDescription>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="flex items-center justify-between">
                  <span>Product Information</span>
                  <Badge>34%</Badge>
                </div>
                <div className="flex items-center justify-between">
                  <span>Technical Support</span>
                  <Badge>28%</Badge>
                </div>
                <div className="flex items-center justify-between">
                  <span>Billing Questions</span>
                  <Badge>18%</Badge>
                </div>
                <div className="flex items-center justify-between">
                  <span>Account Management</span>
                  <Badge>12%</Badge>
                </div>
                <div className="flex items-center justify-between">
                  <span>Other</span>
                  <Badge variant="secondary">8%</Badge>
                </div>
              </CardContent>
            </Card>
          </div>
        </TabsContent>

        <TabsContent value="custom" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle>{t('analytics.customReports')}</CardTitle>
              <CardDescription>
                Create and manage custom analytics reports
              </CardDescription>
            </CardHeader>
            <CardContent>
              <div className="text-center py-8">
                <BarChart3 className="mx-auto h-12 w-12 text-muted-foreground" />
                <h3 className="mt-2 text-sm font-semibold">Custom Reports</h3>
                <p className="mt-1 text-sm text-muted-foreground">
                  Build custom reports with drag-and-drop interface.
                </p>
                <Button className="mt-4">
                  {t('analytics.generateReport')}
                </Button>
              </div>
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>
    </div>
  );
};

export default AnalyticsPage;
