import React, { useEffect } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { useTranslation } from 'react-i18next';
import { AppDispatch, RootState } from '../../store/store';
import { fetchDashboardStats } from '../../store/slices/analyticsSlice';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../../components/ui/card';
import { Badge } from '../../components/ui/badge';
import { Button } from '../../components/ui/button';
import { Skeleton } from '../../components/ui/skeleton';
import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
  PieChart,
  Pie,
  Cell,
} from 'recharts';
import {
  Users,
  Building2,
  MessageSquare,
  UserCheck,
  TrendingUp,
  Activity,
  AlertCircle,
} from 'lucide-react';

const DashboardPage: React.FC = () => {
  const { t } = useTranslation();
  const dispatch = useDispatch<AppDispatch>();
  const { dashboardStats, isLoading } = useSelector((state: RootState) => state.analytics);

  useEffect(() => {
    dispatch(fetchDashboardStats());
  }, [dispatch]);

  const mockChartData = [
    { name: 'Jan', conversations: 400, resolved: 350 },
    { name: 'Feb', conversations: 300, resolved: 280 },
    { name: 'Mar', conversations: 500, resolved: 450 },
    { name: 'Apr', conversations: 280, resolved: 250 },
    { name: 'May', conversations: 590, resolved: 520 },
    { name: 'Jun', conversations: 320, resolved: 300 },
  ];

  const mockPieData = [
    { name: 'Active', value: 65, color: '#10b981' },
    { name: 'Inactive', value: 25, color: '#f59e0b' },
    { name: 'Suspended', value: 10, color: '#ef4444' },
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
        {description && (
          <p className="text-xs text-muted-foreground mt-1">{description}</p>
        )}
      </CardContent>
    </Card>
  );

  if (isLoading) {
    return (
      <div className="space-y-6">
        <div>
          <h1 className="text-3xl font-bold">{t('dashboard.title')}</h1>
          <p className="text-muted-foreground">{t('dashboard.overview')}</p>
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
          <p className="text-muted-foreground">{t('dashboard.overview')}</p>
        </div>
        <Button>{t('common.refresh')}</Button>
      </div>

      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        <StatCard
          title={t('dashboard.totalTenants')}
          value={dashboardStats?.totalTenants || 156}
          icon={Building2}
          trend="+12% from last month"
          description="Active tenant accounts"
        />
        <StatCard
          title={t('dashboard.totalUsers')}
          value={dashboardStats?.totalUsers || 2847}
          icon={Users}
          trend="+8% from last month"
          description="Registered users"
        />
        <StatCard
          title={t('dashboard.totalConversations')}
          value={dashboardStats?.totalConversations || 12543}
          icon={MessageSquare}
          trend="+23% from last month"
          description="Total conversations"
        />
        <StatCard
          title={t('dashboard.activeAgents')}
          value={dashboardStats?.activeAgents || 89}
          icon={UserCheck}
          trend="+5% from last month"
          description="Currently online"
        />
      </div>

      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-7">
        <Card className="col-span-4">
          <CardHeader>
            <CardTitle>Conversation Trends</CardTitle>
            <CardDescription>
              Monthly conversation volume and resolution rates
            </CardDescription>
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
            <CardTitle>Tenant Status</CardTitle>
            <CardDescription>Distribution of tenant statuses</CardDescription>
          </CardHeader>
          <CardContent>
            <ResponsiveContainer width="100%" height={300}>
              <PieChart>
                <Pie
                  data={mockPieData}
                  cx="50%"
                  cy="50%"
                  innerRadius={60}
                  outerRadius={100}
                  paddingAngle={5}
                  dataKey="value"
                >
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
            <CardTitle className="flex items-center">
              <Activity className="mr-2 h-5 w-5" />
              {t('dashboard.systemHealth')}
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-3">
            <div className="flex items-center justify-between">
              <span>API Gateway</span>
              <Badge variant="default">Healthy</Badge>
            </div>
            <div className="flex items-center justify-between">
              <span>Database</span>
              <Badge variant="default">Healthy</Badge>
            </div>
            <div className="flex items-center justify-between">
              <span>Redis Cache</span>
              <Badge variant="default">Healthy</Badge>
            </div>
            <div className="flex items-center justify-between">
              <span>Message Queue</span>
              <Badge variant="secondary">Warning</Badge>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="flex items-center">
              <AlertCircle className="mr-2 h-5 w-5" />
              {t('dashboard.recentActivity')}
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-3">
            <div className="text-sm">
              <p className="font-medium">New tenant registered</p>
              <p className="text-muted-foreground">Acme Corp - 2 minutes ago</p>
            </div>
            <div className="text-sm">
              <p className="font-medium">System maintenance completed</p>
              <p className="text-muted-foreground">Database optimization - 1 hour ago</p>
            </div>
            <div className="text-sm">
              <p className="font-medium">High conversation volume detected</p>
              <p className="text-muted-foreground">Peak traffic - 3 hours ago</p>
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
};

export default DashboardPage;
