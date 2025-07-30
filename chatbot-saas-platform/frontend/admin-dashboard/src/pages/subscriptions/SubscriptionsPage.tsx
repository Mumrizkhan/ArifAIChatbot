import React, { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Button } from '../../components/ui/button';
import { Input } from '../../components/ui/input';
import { Badge } from '../../components/ui/badge';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '../../components/ui/table';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../../components/ui/card';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '../../components/ui/tabs';
import { Skeleton } from '../../components/ui/skeleton';
import {
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
  Plus,
  Search,
  DollarSign,
  Users,
  TrendingUp,
  CreditCard,
  CheckCircle,
  XCircle,
  Clock,
  Calendar,
  Download,
} from 'lucide-react';

const SubscriptionsPage: React.FC = () => {
  const { t } = useTranslation();
  const [isLoading] = useState(false);

  const subscriptions = [
    {
      id: '1',
      tenantName: 'Acme Corp',
      plan: 'Enterprise',
      status: 'Active',
      mrr: 299,
      nextBilling: '2024-02-15',
      users: 150,
    },
    {
      id: '2',
      tenantName: 'TechStart Inc',
      plan: 'Pro',
      status: 'Active',
      mrr: 99,
      nextBilling: '2024-02-20',
      users: 25,
    },
    {
      id: '3',
      tenantName: 'Global Solutions',
      plan: 'Basic',
      status: 'Past Due',
      mrr: 29,
      nextBilling: '2024-01-30',
      users: 5,
    },
  ];

  const plans = [
    {
      id: '1',
      name: 'Basic',
      price: 29,
      features: ['Up to 5 users', '1,000 conversations/month', 'Basic analytics'],
      subscribers: 45,
    },
    {
      id: '2',
      name: 'Pro',
      price: 99,
      features: ['Up to 50 users', '10,000 conversations/month', 'Advanced analytics', 'Custom branding'],
      subscribers: 128,
    },
    {
      id: '3',
      name: 'Enterprise',
      price: 299,
      features: ['Unlimited users', 'Unlimited conversations', 'Full analytics suite', 'White-label solution'],
      subscribers: 67,
    },
  ];

  const revenueData = [
    { month: 'Jan', revenue: 24500, subscriptions: 180 },
    { month: 'Feb', revenue: 26800, subscriptions: 195 },
    { month: 'Mar', revenue: 28200, subscriptions: 210 },
    { month: 'Apr', revenue: 29100, subscriptions: 225 },
    { month: 'May', revenue: 31500, subscriptions: 240 },
    { month: 'Jun', revenue: 33200, subscriptions: 256 },
  ];

  const planDistributionData = [
    { name: 'Basic', value: 45, color: '#f59e0b' },
    { name: 'Pro', value: 128, color: '#3b82f6' },
    { name: 'Enterprise', value: 67, color: '#10b981' },
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

  const getStatusBadge = (status: string) => {
    const variants: Record<string, { variant: 'default' | 'secondary' | 'destructive'; icon: any }> = {
      Active: { variant: 'default', icon: CheckCircle },
      'Past Due': { variant: 'destructive', icon: XCircle },
      Cancelled: { variant: 'secondary', icon: XCircle },
      Trialing: { variant: 'secondary', icon: Clock },
    };
    
    const config = variants[status] || { variant: 'secondary', icon: Clock };
    const Icon = config.icon;
    
    return (
      <Badge variant={config.variant}>
        <Icon className="mr-1 h-3 w-3" />
        {status}
      </Badge>
    );
  };

  if (isLoading) {
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
          <h1 className="text-3xl font-bold">{t('subscriptions.title')}</h1>
          <p className="text-muted-foreground">
            Manage subscription plans, billing, and revenue
          </p>
        </div>
        <Button>
          <Plus className="mr-2 h-4 w-4" />
          Create Plan
        </Button>
      </div>

      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        <MetricCard
          title="Monthly Revenue"
          value="$33,200"
          change="+14% from last month"
          icon={DollarSign}
          description="Recurring revenue"
        />
        <MetricCard
          title="Active Subscriptions"
          value="240"
          change="+12% from last month"
          icon={Users}
          description="Paying customers"
        />
        <MetricCard
          title="Churn Rate"
          value="2.4%"
          change="-0.5% from last month"
          icon={TrendingUp}
          description="Monthly churn"
        />
        <MetricCard
          title="Average Revenue"
          value="$138"
          change="+$8 from last month"
          icon={CreditCard}
          description="Per user per month"
        />
      </div>

      <Tabs defaultValue="overview" className="space-y-4">
        <TabsList>
          <TabsTrigger value="overview">Overview</TabsTrigger>
          <TabsTrigger value="plans">{t('subscriptions.plans')}</TabsTrigger>
          <TabsTrigger value="subscriptions">Subscriptions</TabsTrigger>
          <TabsTrigger value="billing">{t('subscriptions.billing')}</TabsTrigger>
        </TabsList>

        <TabsContent value="overview" className="space-y-4">
          <div className="grid gap-4 md:grid-cols-2">
            <Card>
              <CardHeader>
                <CardTitle>Revenue Trends</CardTitle>
                <CardDescription>Monthly recurring revenue and subscription growth</CardDescription>
              </CardHeader>
              <CardContent>
                <ResponsiveContainer width="100%" height={300}>
                  <LineChart data={revenueData}>
                    <CartesianGrid strokeDasharray="3 3" />
                    <XAxis dataKey="month" />
                    <YAxis />
                    <Tooltip />
                    <Line type="monotone" dataKey="revenue" stroke="#3b82f6" strokeWidth={2} />
                  </LineChart>
                </ResponsiveContainer>
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle>Plan Distribution</CardTitle>
                <CardDescription>Subscription distribution by plan type</CardDescription>
              </CardHeader>
              <CardContent>
                <ResponsiveContainer width="100%" height={300}>
                  <PieChart>
                    <Pie
                      data={planDistributionData}
                      cx="50%"
                      cy="50%"
                      innerRadius={60}
                      outerRadius={100}
                      paddingAngle={5}
                      dataKey="value"
                    >
                      {planDistributionData.map((entry, index) => (
                        <Cell key={`cell-${index}`} fill={entry.color} />
                      ))}
                    </Pie>
                    <Tooltip />
                  </PieChart>
                </ResponsiveContainer>
                <div className="flex justify-center space-x-4 mt-4">
                  {planDistributionData.map((entry, index) => (
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

        <TabsContent value="plans" className="space-y-4">
          <div className="grid gap-4 md:grid-cols-3">
            {plans.map((plan) => (
              <Card key={plan.id}>
                <CardHeader>
                  <CardTitle className="flex items-center justify-between">
                    {plan.name}
                    <Badge variant="secondary">{plan.subscribers} subscribers</Badge>
                  </CardTitle>
                  <CardDescription>
                    <span className="text-2xl font-bold">${plan.price}</span>
                    <span className="text-muted-foreground">/month</span>
                  </CardDescription>
                </CardHeader>
                <CardContent>
                  <ul className="space-y-2">
                    {plan.features.map((feature, index) => (
                      <li key={index} className="flex items-center text-sm">
                        <CheckCircle className="mr-2 h-4 w-4 text-green-500" />
                        {feature}
                      </li>
                    ))}
                  </ul>
                  <Button className="w-full mt-4" variant="outline">
                    Edit Plan
                  </Button>
                </CardContent>
              </Card>
            ))}
          </div>
        </TabsContent>

        <TabsContent value="subscriptions" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle>Active Subscriptions</CardTitle>
              <CardDescription>Manage all customer subscriptions</CardDescription>
            </CardHeader>
            <CardContent>
              <div className="flex items-center space-x-2 mb-4">
                <div className="relative flex-1">
                  <Search className="absolute left-2 top-2.5 h-4 w-4 text-muted-foreground" />
                  <Input placeholder="Search subscriptions..." className="pl-8" />
                </div>
                <Button>
                  <Download className="mr-2 h-4 w-4" />
                  Export
                </Button>
              </div>

              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Tenant</TableHead>
                    <TableHead>Plan</TableHead>
                    <TableHead>Status</TableHead>
                    <TableHead>MRR</TableHead>
                    <TableHead>Users</TableHead>
                    <TableHead>Next Billing</TableHead>
                    <TableHead>Actions</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {subscriptions.map((subscription) => (
                    <TableRow key={subscription.id}>
                      <TableCell className="font-medium">{subscription.tenantName}</TableCell>
                      <TableCell>{subscription.plan}</TableCell>
                      <TableCell>{getStatusBadge(subscription.status)}</TableCell>
                      <TableCell>${subscription.mrr}</TableCell>
                      <TableCell>
                        <div className="flex items-center">
                          <Users className="mr-1 h-4 w-4" />
                          {subscription.users}
                        </div>
                      </TableCell>
                      <TableCell>
                        <div className="flex items-center">
                          <Calendar className="mr-1 h-4 w-4" />
                          {new Date(subscription.nextBilling).toLocaleDateString()}
                        </div>
                      </TableCell>
                      <TableCell>
                        <Button variant="ghost" size="sm">
                          Manage
                        </Button>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="billing" className="space-y-4">
          <div className="grid gap-4 md:grid-cols-2">
            <Card>
              <CardHeader>
                <CardTitle>Billing Overview</CardTitle>
                <CardDescription>Payment processing and invoice management</CardDescription>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="flex items-center justify-between">
                  <span>Successful Payments</span>
                  <Badge variant="default">98.5%</Badge>
                </div>
                <div className="flex items-center justify-between">
                  <span>Failed Payments</span>
                  <Badge variant="destructive">1.5%</Badge>
                </div>
                <div className="flex items-center justify-between">
                  <span>Pending Invoices</span>
                  <Badge variant="secondary">12</Badge>
                </div>
                <div className="flex items-center justify-between">
                  <span>Overdue Invoices</span>
                  <Badge variant="destructive">3</Badge>
                </div>
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle>Payment Methods</CardTitle>
                <CardDescription>Customer payment method distribution</CardDescription>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="flex items-center justify-between">
                  <span>Credit Card</span>
                  <Badge>85%</Badge>
                </div>
                <div className="flex items-center justify-between">
                  <span>Bank Transfer</span>
                  <Badge>12%</Badge>
                </div>
                <div className="flex items-center justify-between">
                  <span>PayPal</span>
                  <Badge>3%</Badge>
                </div>
              </CardContent>
            </Card>
          </div>
        </TabsContent>
      </Tabs>
    </div>
  );
};

export default SubscriptionsPage;
