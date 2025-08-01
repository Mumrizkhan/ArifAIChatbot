import React, { useEffect, useState } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { useTranslation } from 'react-i18next';
import { AppDispatch, RootState } from '../../store/store';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../../components/ui/card';
import { Button } from '../../components/ui/button';
import { Badge } from '../../components/ui/badge';
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
  BarChart3,
  TrendingUp,
  Users,
  MessageSquare,
  Clock,
  Star,
  Download,
  Calendar,
  Activity,
  Target,
  Award,
} from 'lucide-react';

const AnalyticsPage = () => {
  const { t } = useTranslation();
  const dispatch = useDispatch<AppDispatch>();
  const [activeTab, setActiveTab] = useState('performance');
  const [selectedTimeRange, setSelectedTimeRange] = useState('7d');

  const isLoading = false;

  const handleTimeRangeChange = (timeRange: string) => {
    setSelectedTimeRange(timeRange);
  };

  const handleExport = (format: 'csv' | 'pdf' | 'excel') => {
    console.log(`Exporting analytics as ${format}`);
  };

  const performanceData = [
    { date: '2024-01-01', conversations: 12, avgRating: 4.5, responseTime: 2.3 },
    { date: '2024-01-02', conversations: 15, avgRating: 4.7, responseTime: 2.1 },
    { date: '2024-01-03', conversations: 8, avgRating: 4.2, responseTime: 2.8 },
    { date: '2024-01-04', conversations: 18, avgRating: 4.8, responseTime: 1.9 },
    { date: '2024-01-05', conversations: 14, avgRating: 4.6, responseTime: 2.2 },
    { date: '2024-01-06', conversations: 16, avgRating: 4.9, responseTime: 1.8 },
    { date: '2024-01-07', conversations: 11, avgRating: 4.4, responseTime: 2.5 },
  ];

  const satisfactionData = [
    { rating: '5 Stars', count: 68, percentage: 68 },
    { rating: '4 Stars', count: 22, percentage: 22 },
    { rating: '3 Stars', count: 7, percentage: 7 },
    { rating: '2 Stars', count: 2, percentage: 2 },
    { rating: '1 Star', count: 1, percentage: 1 },
  ];

  const channelData = [
    { name: 'Website', value: 65, color: '#3b82f6' },
    { name: 'Mobile App', value: 25, color: '#10b981' },
    { name: 'Social Media', value: 10, color: '#f59e0b' },
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
            {t('analytics.subtitle')}
          </p>
        </div>
        <div className="flex items-center space-x-2">
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
          <Select onValueChange={handleExport}>
            <SelectTrigger className="w-[120px]">
              <Download className="mr-2 h-4 w-4" />
              <SelectValue placeholder={t('common.export')} />
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
          <TabsTrigger value="performance">{t('analytics.performance')}</TabsTrigger>
          <TabsTrigger value="satisfaction">{t('analytics.satisfaction')}</TabsTrigger>
          <TabsTrigger value="productivity">{t('analytics.productivity')}</TabsTrigger>
          <TabsTrigger value="goals">{t('analytics.goals')}</TabsTrigger>
        </TabsList>

        <TabsContent value="performance" className="space-y-4">
          <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
            <MetricCard
              title={t('analytics.totalConversations')}
              value="94"
              change="+12% from last period"
              icon={MessageSquare}
              description="This week"
            />
            <MetricCard
              title={t('analytics.avgResponseTime')}
              value="2.1m"
              change="-15% from last period"
              icon={Clock}
              description="First response time"
            />
            <MetricCard
              title={t('analytics.satisfactionScore')}
              value="4.7"
              change="+0.3 from last period"
              icon={Star}
              description="Average customer rating"
            />
            <MetricCard
              title={t('analytics.resolutionRate')}
              value="94%"
              change="+5% from last period"
              icon={Target}
              description="Successfully resolved"
            />
          </div>

          <div className="grid gap-4 md:grid-cols-2">
            <Card>
              <CardHeader>
                <CardTitle>{t('analytics.conversationTrends')}</CardTitle>
                <CardDescription>
                  Daily conversation volume and performance
                </CardDescription>
              </CardHeader>
              <CardContent>
                <ResponsiveContainer width="100%" height={300}>
                  <AreaChart data={performanceData}>
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
                <CardTitle>{t('analytics.responseTimeAnalysis')}</CardTitle>
                <CardDescription>
                  Average response time trends
                </CardDescription>
              </CardHeader>
              <CardContent>
                <ResponsiveContainer width="100%" height={300}>
                  <LineChart data={performanceData}>
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
            <MetricCard
              title="Overall Rating"
              value="4.7"
              change="+0.3 from last period"
              icon={Star}
              description="Average customer rating"
            />
            <MetricCard
              title="5-Star Ratings"
              value="68%"
              change="+8% from last period"
              icon={Award}
              description="Excellent ratings"
            />
            <MetricCard
              title="Customer Retention"
              value="92%"
              change="+3% from last period"
              icon={Users}
              description="Return customers"
            />
            <MetricCard
              title="Recommendation Score"
              value="8.9"
              change="+0.5 from last period"
              icon={TrendingUp}
              description="Net Promoter Score"
            />
          </div>

          <div className="grid gap-4 md:grid-cols-2">
            <Card>
              <CardHeader>
                <CardTitle>Rating Distribution</CardTitle>
                <CardDescription>
                  Customer satisfaction breakdown
                </CardDescription>
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
                <CardTitle>Satisfaction Trends</CardTitle>
                <CardDescription>
                  Customer satisfaction over time
                </CardDescription>
              </CardHeader>
              <CardContent>
                <ResponsiveContainer width="100%" height={300}>
                  <LineChart data={performanceData}>
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
              title="Conversations/Hour"
              value="3.2"
              change="+8% from last period"
              icon={Activity}
              description="Average productivity"
            />
            <MetricCard
              title="Active Hours"
              value="7.5h"
              change="+0.5h from yesterday"
              icon={Clock}
              description="Daily active time"
            />
            <MetricCard
              title="Multitasking Score"
              value="85%"
              change="+5% from last period"
              icon={Target}
              description="Concurrent conversations"
            />
            <MetricCard
              title="Efficiency Rating"
              value="A+"
              change="Maintained from last period"
              icon={Award}
              description="Overall performance grade"
            />
          </div>

          <Card>
            <CardHeader>
              <CardTitle>Daily Productivity</CardTitle>
              <CardDescription>
                Conversations handled per day
              </CardDescription>
            </CardHeader>
            <CardContent>
              <ResponsiveContainer width="100%" height={400}>
                <BarChart data={performanceData}>
                  <CartesianGrid strokeDasharray="3 3" />
                  <XAxis dataKey="date" tickFormatter={(value) => new Date(value).toLocaleDateString()} />
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
                <CardTitle>Daily Goal</CardTitle>
                <CardDescription>Conversations target</CardDescription>
              </CardHeader>
              <CardContent>
                <div className="space-y-2">
                  <div className="flex justify-between">
                    <span className="text-sm">Progress</span>
                    <span className="text-sm font-medium">14/15</span>
                  </div>
                  <div className="w-full bg-gray-200 rounded-full h-2">
                    <div className="bg-blue-600 h-2 rounded-full" style={{ width: '93%' }}></div>
                  </div>
                  <p className="text-xs text-muted-foreground">1 more to reach daily goal</p>
                </div>
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle>Weekly Goal</CardTitle>
                <CardDescription>Response time target</CardDescription>
              </CardHeader>
              <CardContent>
                <div className="space-y-2">
                  <div className="flex justify-between">
                    <span className="text-sm">Average</span>
                    <span className="text-sm font-medium">2.1m / 2.5m</span>
                  </div>
                  <div className="w-full bg-gray-200 rounded-full h-2">
                    <div className="bg-green-600 h-2 rounded-full" style={{ width: '84%' }}></div>
                  </div>
                  <p className="text-xs text-muted-foreground">Exceeding target by 16%</p>
                </div>
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle>Monthly Goal</CardTitle>
                <CardDescription>Satisfaction target</CardDescription>
              </CardHeader>
              <CardContent>
                <div className="space-y-2">
                  <div className="flex justify-between">
                    <span className="text-sm">Rating</span>
                    <span className="text-sm font-medium">4.7 / 4.5</span>
                  </div>
                  <div className="w-full bg-gray-200 rounded-full h-2">
                    <div className="bg-yellow-600 h-2 rounded-full" style={{ width: '100%' }}></div>
                  </div>
                  <p className="text-xs text-muted-foreground">Goal achieved! 🎉</p>
                </div>
              </CardContent>
            </Card>
          </div>

          <Card>
            <CardHeader>
              <CardTitle>Goal Progress Over Time</CardTitle>
              <CardDescription>
                Track your progress towards monthly targets
              </CardDescription>
            </CardHeader>
            <CardContent>
              <ResponsiveContainer width="100%" height={300}>
                <LineChart data={performanceData}>
                  <CartesianGrid strokeDasharray="3 3" />
                  <XAxis dataKey="date" tickFormatter={(value) => new Date(value).toLocaleDateString()} />
                  <YAxis />
                  <Tooltip />
                  <Line type="monotone" dataKey="conversations" stroke="#3b82f6" strokeWidth={2} name="Conversations" />
                  <Line type="monotone" dataKey="avgRating" stroke="#10b981" strokeWidth={2} name="Rating" />
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
