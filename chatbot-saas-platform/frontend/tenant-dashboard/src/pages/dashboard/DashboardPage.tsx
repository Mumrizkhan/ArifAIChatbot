import { useEffect } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { useTranslation } from 'react-i18next';
import { AppDispatch, RootState } from '../../store/store';
import { fetchAnalytics, fetchRealtimeAnalytics } from '../../store/slices/analyticsSlice';
import { fetchChatbotConfigs } from '../../store/slices/chatbotSlice';
import { fetchTeamMembers } from '../../store/slices/teamSlice';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../../components/ui/card';
import { Button } from '../../components/ui/button';
import { Skeleton } from '../../components/ui/skeleton';
import {
  BarChart,
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
} from 'recharts';
import {
  MessageSquare,
  Users,
  TrendingUp,
  Bot,
  Clock,
  Star,
  Settings,
  Plus,
  Activity,
  AlertCircle,
} from 'lucide-react';

const DashboardPage = () => {
  const { t } = useTranslation();
  const dispatch = useDispatch<AppDispatch>();
  const { data: analytics, isLoading: analyticsLoading } = useSelector((state: RootState) => state.analytics);
  const { configs: chatbotConfigs } = useSelector((state: RootState) => state.chatbot);
  const { members: teamMembers } = useSelector((state: RootState) => state.team);

  useEffect(() => {
    const startDate = new Date(Date.now() - 7 * 24 * 60 * 60 * 1000);
    const endDate = new Date();
    
    dispatch(fetchAnalytics({ startDate, endDate }));
    dispatch(fetchRealtimeAnalytics());
    dispatch(fetchChatbotConfigs());
    dispatch(fetchTeamMembers());

    const interval = setInterval(() => {
      dispatch(fetchRealtimeAnalytics());
    }, 30000);

    return () => clearInterval(interval);
  }, [dispatch]);

  const quickActions = [
    {
      title: t('dashboard.configureChatbot'),
      description: t('dashboard.configureChatbotDesc'),
      icon: Bot,
      href: '/chatbot',
      color: 'bg-blue-500',
    },
    {
      title: t('dashboard.manageTeam'),
      description: t('dashboard.manageTeamDesc'),
      icon: Users,
      href: '/team',
      color: 'bg-green-500',
    },
    {
      title: t('dashboard.viewAnalytics'),
      description: t('dashboard.viewAnalyticsDesc'),
      icon: BarChart,
      href: '/analytics',
      color: 'bg-purple-500',
    },
    {
      title: t('dashboard.customizeBranding'),
      description: t('dashboard.customizeBrandingDesc'),
      icon: Settings,
      href: '/branding',
      color: 'bg-orange-500',
    },
  ];

  const recentActivity = [
    {
      id: 1,
      type: 'conversation',
      message: 'New conversation started with customer John Doe',
      time: '2 minutes ago',
      icon: MessageSquare,
    },
    {
      id: 2,
      type: 'team',
      message: 'Agent Sarah Wilson joined the team',
      time: '1 hour ago',
      icon: Users,
    },
    {
      id: 3,
      type: 'bot',
      message: 'Chatbot configuration updated',
      time: '3 hours ago',
      icon: Bot,
    },
    {
      id: 4,
      type: 'analytics',
      message: 'Weekly analytics report generated',
      time: '1 day ago',
      icon: TrendingUp,
    },
  ];

  const conversationTrendData = [
    { date: '2024-01-01', conversations: 45, resolved: 42 },
    { date: '2024-01-02', conversations: 52, resolved: 48 },
    { date: '2024-01-03', conversations: 38, resolved: 35 },
    { date: '2024-01-04', conversations: 61, resolved: 58 },
    { date: '2024-01-05', conversations: 49, resolved: 46 },
    { date: '2024-01-06', conversations: 55, resolved: 52 },
    { date: '2024-01-07', conversations: 43, resolved: 41 },
  ];

  const channelData = [
    { name: 'Website', value: 65, color: '#3b82f6' },
    { name: 'Mobile App', value: 25, color: '#10b981' },
    { name: 'Social Media', value: 10, color: '#f59e0b' },
  ];

  if (analyticsLoading && !analytics) {
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

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">{t('dashboard.title')}</h1>
          <p className="text-muted-foreground">
            {t('dashboard.subtitle')}
          </p>
        </div>
        <Button>
          <Plus className="mr-2 h-4 w-4" />
          {t('dashboard.quickSetup')}
        </Button>
      </div>

      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">
              {t('dashboard.totalConversations')}
            </CardTitle>
            <MessageSquare className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">
              {analytics?.conversations?.total || 1247}
            </div>
            <div className="flex items-center text-xs text-muted-foreground">
              <TrendingUp className="mr-1 h-3 w-3" />
              +12% from last week
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">
              {t('dashboard.activeAgents')}
            </CardTitle>
            <Users className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">
              {analytics?.agents?.online || teamMembers?.filter(m => m.status === 'active').length || 8}
            </div>
            <div className="flex items-center text-xs text-muted-foreground">
              <Activity className="mr-1 h-3 w-3" />
              Currently online
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">
              {t('dashboard.avgResponseTime')}
            </CardTitle>
            <Clock className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">
              {analytics?.agents?.averageResponseTime || 2.4}m
            </div>
            <div className="flex items-center text-xs text-muted-foreground">
              <TrendingUp className="mr-1 h-3 w-3" />
              -15% from last week
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">
              {t('dashboard.satisfactionScore')}
            </CardTitle>
            <Star className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">
              {analytics?.conversations?.satisfactionScore || 4.6}
            </div>
            <div className="flex items-center text-xs text-muted-foreground">
              <TrendingUp className="mr-1 h-3 w-3" />
              +0.2 from last week
            </div>
          </CardContent>
        </Card>
      </div>

      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
        <Card className="col-span-2">
          <CardHeader>
            <CardTitle>{t('dashboard.conversationTrends')}</CardTitle>
            <CardDescription>
              {t('dashboard.conversationTrendsDesc')}
            </CardDescription>
          </CardHeader>
          <CardContent>
            <ResponsiveContainer width="100%" height={300}>
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

        <Card>
          <CardHeader>
            <CardTitle>{t('dashboard.channelDistribution')}</CardTitle>
            <CardDescription>
              {t('dashboard.channelDistributionDesc')}
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

      <div className="grid gap-4 md:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle>{t('dashboard.quickActions')}</CardTitle>
            <CardDescription>
              {t('dashboard.quickActionsDesc')}
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            {quickActions.map((action, index) => (
              <div key={index} className="flex items-center space-x-4 p-3 rounded-lg border hover:bg-accent cursor-pointer">
                <div className={`p-2 rounded-md ${action.color}`}>
                  <action.icon className="h-4 w-4 text-white" />
                </div>
                <div className="flex-1">
                  <h4 className="font-medium">{action.title}</h4>
                  <p className="text-sm text-muted-foreground">{action.description}</p>
                </div>
              </div>
            ))}
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>{t('dashboard.recentActivity')}</CardTitle>
            <CardDescription>
              {t('dashboard.recentActivityDesc')}
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            {recentActivity.map((activity) => (
              <div key={activity.id} className="flex items-start space-x-3">
                <div className="p-2 rounded-full bg-muted">
                  <activity.icon className="h-3 w-3" />
                </div>
                <div className="flex-1 space-y-1">
                  <p className="text-sm">{activity.message}</p>
                  <p className="text-xs text-muted-foreground">{activity.time}</p>
                </div>
              </div>
            ))}
          </CardContent>
        </Card>
      </div>

      {chatbotConfigs && chatbotConfigs.length === 0 && (
        <Card className="border-orange-200 bg-orange-50">
          <CardHeader>
            <CardTitle className="flex items-center text-orange-800">
              <AlertCircle className="mr-2 h-5 w-5" />
              {t('dashboard.setupRequired')}
            </CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-orange-700 mb-4">
              {t('dashboard.setupRequiredDesc')}
            </p>
            <Button variant="outline" className="border-orange-300 text-orange-800 hover:bg-orange-100">
              {t('dashboard.startSetup')}
            </Button>
          </CardContent>
        </Card>
      )}
    </div>
  );
};

export default DashboardPage;
