import { useEffect, useState } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { useTranslation } from 'react-i18next';
import { LucideIcon } from 'lucide-react';
import { AppDispatch, RootState } from '../../store/store';
import { 
  fetchAnalytics, 
  fetchRealtimeAnalytics, 
  exportAnalytics, 
  setDateRange,
  updateAnalyticsDataRealtime,
  updateRealtimeData,
  setSignalRConnectionStatus,
  addTenantNotification
} from '../../store/slices/analyticsSlice';
import { tenantSignalRService } from '../../services/signalr';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../../components/ui/card';
import { Button } from '../../components/ui/button';
import { Skeleton } from '../../components/ui/skeleton';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '../../components/ui/tabs';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '../../components/ui/select';
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
  TrendingUp,
  Users,
  MessageSquare,
  Clock,
  Star,
  Download,
  Calendar,
  Bot,
  UserCheck,
  Activity,
} from 'lucide-react';

const AnalyticsPage = () => {
  const { t } = useTranslation();
  const dispatch = useDispatch<AppDispatch>();
  const { data: analytics, isLoading, dateRange, isSignalRConnected } = useSelector((state: RootState) => state.analytics);
  const { user } = useSelector((state: RootState) => state.auth);
  const [activeTab, setActiveTab] = useState('overview');
  const [selectedTimeRange, setSelectedTimeRange] = useState('7d');

  useEffect(() => {
    dispatch(fetchAnalytics({ startDate: new Date(dateRange.start), endDate: new Date(dateRange.end) }));
    dispatch(fetchRealtimeAnalytics());

    const token = localStorage.getItem('token');
    if (token && user?.tenantId) {
      tenantSignalRService.connect(token, user.tenantId).then((connected) => {
        dispatch(setSignalRConnectionStatus(connected));
        
        if (connected) {
          tenantSignalRService.setOnAnalyticsUpdate((analytics) => {
            dispatch(updateAnalyticsDataRealtime(analytics));
          });

          tenantSignalRService.setOnRealtimeUpdate((realtime) => {
            dispatch(updateRealtimeData(realtime));
          });

          tenantSignalRService.setOnTenantNotification((notification) => {
            // Convert Date timestamp to string if needed
            const serializedNotification = {
              ...notification,
              timestamp: notification.timestamp instanceof Date 
                ? notification.timestamp.toISOString() 
                : notification.timestamp
            };
            dispatch(addTenantNotification(serializedNotification));
          });

          tenantSignalRService.setOnConnectionStatusChange((isConnected) => {
            dispatch(setSignalRConnectionStatus(isConnected));
          });

          tenantSignalRService.requestTenantAnalyticsUpdate(new Date(dateRange.start), new Date(dateRange.end));
        }
      });
    }

    const interval = setInterval(() => {
      if (isSignalRConnected) {
        tenantSignalRService.requestTenantRealtimeUpdate();
      } else {
        dispatch(fetchRealtimeAnalytics());
      }
    }, 30000);

    return () => clearInterval(interval);
  }, [dispatch, dateRange, user?.tenantId, isSignalRConnected]);

  const handleTimeRangeChange = (timeRange: string) => {
    setSelectedTimeRange(timeRange);
    const days = timeRange === '7d' ? 7 : timeRange === '30d' ? 30 : timeRange === '90d' ? 90 : 365;
    const startDate = new Date(Date.now() - days * 24 * 60 * 60 * 1000);
    const endDate = new Date();
    
    dispatch(setDateRange({ start: startDate.toISOString(), end: endDate.toISOString() }));
    dispatch(fetchAnalytics({ startDate, endDate }));
    
    if (isSignalRConnected) {
      tenantSignalRService.requestTenantAnalyticsUpdate(startDate, endDate);
    }
  };

  const handleExport = (format: 'csv' | 'pdf' | 'excel') => {
    dispatch(exportAnalytics({ 
      format, 
      startDate: new Date(dateRange.start), 
      endDate: new Date(dateRange.end)
    }));
  };

  const conversationTrendData = [
    { date: '2024-01-01', conversations: 45, resolved: 42, satisfaction: 4.2 },
    { date: '2024-01-02', conversations: 52, resolved: 48, satisfaction: 4.5 },
    { date: '2024-01-03', conversations: 38, resolved: 35, satisfaction: 4.1 },
    { date: '2024-01-04', conversations: 61, resolved: 58, satisfaction: 4.7 },
    { date: '2024-01-05', conversations: 49, resolved: 46, satisfaction: 4.3 },
    { date: '2024-01-06', conversations: 55, resolved: 52, satisfaction: 4.6 },
    { date: '2024-01-07', conversations: 43, resolved: 41, satisfaction: 4.4 },
  ];

  const channelData = [
    { name: 'Website', value: 65, color: '#3b82f6' },
    { name: 'Mobile App', value: 25, color: '#10b981' },
    { name: 'Social Media', value: 10, color: '#f59e0b' },
  ];

  const agentPerformanceData = [
    { name: 'John Doe', conversations: 45, avgRating: 4.8, responseTime: 2.3 },
    { name: 'Jane Smith', conversations: 38, avgRating: 4.9, responseTime: 1.8 },
    { name: 'Mike Johnson', conversations: 42, avgRating: 4.6, responseTime: 3.1 },
    { name: 'Sarah Wilson', conversations: 35, avgRating: 4.7, responseTime: 2.5 },
  ];

interface MetricCardProps {
  title: string;
  value: string | number;
  change?: string;
  icon: LucideIcon;
  description?: string;
}

  const MetricCard = ({ title, value, change, icon: Icon, description }: MetricCardProps) => (
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

  if (isLoading && !analytics) {
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
            {t('analytics.subtitle')}
          </p>
        </div>
        <div className="flex items-center space-x-4">
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
              <SelectItem value="7d">Last 7 days</SelectItem>
              <SelectItem value="30d">Last 30 days</SelectItem>
              <SelectItem value="90d">Last 90 days</SelectItem>
              <SelectItem value="1y">Last year</SelectItem>
            </SelectContent>
          </Select>
          <div className="flex items-center space-x-2">
            <Button
              variant="outline"
              onClick={() => handleExport('csv')}
              disabled={isLoading}
            >
              <Download className="mr-2 h-4 w-4" />
              {t('analytics.exportCSV')}
            </Button>
            <Button
              variant="outline"
              onClick={() => handleExport('pdf')}
              disabled={isLoading}
            >
              <Download className="mr-2 h-4 w-4" />
              {t('analytics.exportPDF')}
            </Button>
          </div>
        </div>
      </div>

      <Tabs value={activeTab} onValueChange={setActiveTab} className="space-y-4">
        <TabsList>
          <TabsTrigger value="overview">{t('analytics.overview')}</TabsTrigger>
          <TabsTrigger value="conversations">{t('analytics.conversations')}</TabsTrigger>
          <TabsTrigger value="agents">{t('analytics.agents')}</TabsTrigger>
          <TabsTrigger value="satisfaction">{t('analytics.satisfaction')}</TabsTrigger>
        </TabsList>

        <TabsContent value="overview" className="space-y-4">
          <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
            <MetricCard
              title={t('analytics.totalConversations')}
              value={analytics?.conversations?.total || 1247}
              change="+12% from last period"
              icon={MessageSquare}
              description="All conversation channels"
            />
            <MetricCard
              title={t('analytics.activeAgents')}
              value={analytics?.agents?.online || 8}
              change="Currently online"
              icon={Users}
              description="Online right now"
            />
            <MetricCard
              title={t('analytics.avgResponseTime')}
              value={`${analytics?.agents?.averageResponseTime || 2.4}m`}
              change="-15% from last period"
              icon={Clock}
              description="First response time"
            />
            <MetricCard
              title={t('analytics.satisfactionScore')}
              value={analytics?.conversations?.satisfactionScore || 4.6}
              change="+0.2 from last period"
              icon={Star}
              description="Average customer rating"
            />
          </div>

          <div className="grid gap-4 md:grid-cols-2">
            <Card>
              <CardHeader>
                <CardTitle>{t('analytics.conversationTrends')}</CardTitle>
                <CardDescription>
                  {t('analytics.conversationTrendsDesc')}
                </CardDescription>
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
                <CardTitle>{t('analytics.channelDistribution')}</CardTitle>
                <CardDescription>
                  {t('analytics.channelDistributionDesc')}
                </CardDescription>
              </CardHeader>
              <CardContent>
                <ResponsiveContainer width="100%" height={200}>
                  <PieChart>
                    <Pie
                      data={channelData}
                      cx="50%"
                      cy="50%"
                      innerRadius={40}
                      outerRadius={80}
                      paddingAngle={5}
                      dataKey="value"
                    >
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

        <TabsContent value="conversations" className="space-y-4">
          <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
            <MetricCard
              title="Total Conversations"
              value={analytics?.conversations?.total || 1247}
              change="+12% from last period"
              icon={MessageSquare}
              description="All conversation channels"
            />
            <MetricCard
              title="Average Duration"
              value={`${analytics?.conversations?.averageDuration || 15.2} min`}
              change="-3% from last period"
              icon={Clock}
              description="Time to resolution"
            />
            <MetricCard
              title="Resolution Rate"
              value={`${analytics?.performance?.resolutionRate || 92}%`}
              change="+5% from last period"
              icon={TrendingUp}
              description="Successfully resolved"
            />
            <MetricCard
              title="Bot Accuracy"
              value={`${analytics?.performance?.botAccuracy || 87}%`}
              change="+3% from last period"
              icon={Bot}
              description="Correct responses"
            />
          </div>

          <Card>
            <CardHeader>
              <CardTitle>Conversation Volume</CardTitle>
              <CardDescription>Daily conversation trends and patterns</CardDescription>
            </CardHeader>
            <CardContent>
              <ResponsiveContainer width="100%" height={400}>
                <LineChart data={conversationTrendData}>
                  <CartesianGrid strokeDasharray="3 3" />
                  <XAxis dataKey="date" tickFormatter={(value) => new Date(value).toLocaleDateString()} />
                  <YAxis />
                  <Tooltip />
                  <Line type="monotone" dataKey="conversations" stroke="#3b82f6" strokeWidth={2} />
                  <Line type="monotone" dataKey="resolved" stroke="#10b981" strokeWidth={2} />
                </LineChart>
              </ResponsiveContainer>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="agents" className="space-y-4">
          <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
            <MetricCard
              title="Total Agents"
              value={(analytics?.agents?.online || 0) + (analytics?.agents?.busy || 0) || 24}
              change="+2 from last month"
              icon={Users}
              description="Registered agents"
            />
            <MetricCard
              title="Online Agents"
              value={analytics?.agents?.online || 18}
              change="Currently online"
              icon={UserCheck}
              description="Available right now"
            />
            <MetricCard
              title="Avg Response Time"
              value={`${analytics?.agents?.averageResponseTime || 2.4} min`}
              change="-15% from last period"
              icon={Clock}
              description="First response time"
            />
            <MetricCard
              title="Agent Utilization"
              value={`${analytics?.agents?.utilization || 75}%`}
              change="+5% from last period"
              icon={Activity}
              description="Average workload"
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

        <TabsContent value="satisfaction" className="space-y-4">
          <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
            <MetricCard
              title="Overall Satisfaction"
              value={analytics?.conversations?.satisfactionScore || 4.6}
              change="+0.2 from last period"
              icon={Star}
              description="Average customer rating"
            />
            <MetricCard
              title="5-Star Ratings"
              value="68%"
              change="+8% from last period"
              icon={Star}
              description="Excellent ratings"
            />
            <MetricCard
              title="NPS Score"
              value="72"
              change="+5 from last period"
              icon={TrendingUp}
              description="Net Promoter Score"
            />
            <MetricCard
              title="Repeat Customers"
              value={`${analytics?.customers?.returning || 45}%`}
              change="+3% from last period"
              icon={Users}
              description="Customer retention"
            />
          </div>

          <Card>
            <CardHeader>
              <CardTitle>Satisfaction Trends</CardTitle>
              <CardDescription>Customer satisfaction over time</CardDescription>
            </CardHeader>
            <CardContent>
              <ResponsiveContainer width="100%" height={400}>
                <LineChart data={conversationTrendData}>
                  <CartesianGrid strokeDasharray="3 3" />
                  <XAxis dataKey="date" tickFormatter={(value) => new Date(value).toLocaleDateString()} />
                  <YAxis domain={[0, 5]} />
                  <Tooltip />
                  <Line type="monotone" dataKey="satisfaction" stroke="#f59e0b" strokeWidth={3} />
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
