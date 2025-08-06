import React, { useEffect } from "react";
import { useDispatch, useSelector } from "react-redux";
import { useTranslation } from "react-i18next";
import { AppDispatch, RootState } from "../../store/store";
import { fetchPlans, fetchSubscriptions, fetchBillingStats } from "../../store/slices/subscriptionSlice";
import { Button } from "../../components/ui/button";
import { Input } from "../../components/ui/input";
import { Badge } from "../../components/ui/badge";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "../../components/ui/table";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "../../components/ui/card";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "../../components/ui/tabs";
import { Skeleton } from "../../components/ui/skeleton";
import { XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, LineChart, Line, PieChart, Pie, Cell } from "recharts";
import { Plus, Search, DollarSign, Users, TrendingUp, CreditCard, CheckCircle, XCircle, Clock, Calendar, Download } from "lucide-react";

const SubscriptionsPage: React.FC = () => {
  const { t } = useTranslation();
  const dispatch = useDispatch<AppDispatch>();
  const { plans, subscriptions, billingStats, isLoading, error } = useSelector((state: RootState) => state.subscription);

  useEffect(() => {
    dispatch(fetchPlans());
    dispatch(fetchSubscriptions({ page: 1, pageSize: 20 }));
    dispatch(fetchBillingStats());
  }, [dispatch]);

  const mockSubscriptions = [
    {
      id: "1",
      tenantName: "Acme Corp",
      plan: t("subscriptions.planTypes.enterprise"),
      status: "Active",
      mrr: 299,
      nextBilling: "2024-02-15",
      users: 150,
    },
    {
      id: "2",
      tenantName: "TechStart Inc",
      plan: t("subscriptions.planTypes.pro"),
      status: "Active",
      mrr: 99,
      nextBilling: "2024-02-20",
      users: 25,
    },
    {
      id: "3",
      tenantName: "Global Solutions",
      plan: t("subscriptions.planTypes.basic"),
      status: "Past Due",
      mrr: 29,
      nextBilling: "2024-01-30",
      users: 5,
    },
  ];

  const mockPlans = [
    {
      id: "1",
      name: t("subscriptions.planTypes.basic"),
      price: 29,
      features: [
        t("subscriptions.features.upTo5Users"),
        t("subscriptions.features.thousandConversations"),
        t("subscriptions.features.basicAnalytics"),
      ],
      subscribers: 45,
    },
    {
      id: "2",
      name: t("subscriptions.planTypes.pro"),
      price: 99,
      features: [
        t("subscriptions.features.upTo50Users"),
        t("subscriptions.features.tenThousandConversations"),
        t("subscriptions.features.advancedAnalytics"),
        t("subscriptions.features.customBranding"),
      ],
      subscribers: 128,
    },
    {
      id: "3",
      name: t("subscriptions.planTypes.enterprise"),
      price: 299,
      features: [
        t("subscriptions.features.unlimitedUsers"),
        t("subscriptions.features.unlimitedConversations"),
        t("subscriptions.features.fullAnalytics"),
        t("subscriptions.features.whiteLabelSolution"),
      ],
      subscribers: 67,
    },
  ];

  const revenueData = billingStats
    ? [
        {
          month: t("subscriptions.months.jan"),
          revenue: billingStats.monthlyRecurringRevenue * 0.8,
          subscriptions: billingStats.activeSubscriptions * 0.7,
        },
        {
          month: t("subscriptions.months.feb"),
          revenue: billingStats.monthlyRecurringRevenue * 0.85,
          subscriptions: billingStats.activeSubscriptions * 0.75,
        },
        {
          month: t("subscriptions.months.mar"),
          revenue: billingStats.monthlyRecurringRevenue * 0.9,
          subscriptions: billingStats.activeSubscriptions * 0.8,
        },
        {
          month: t("subscriptions.months.apr"),
          revenue: billingStats.monthlyRecurringRevenue * 0.95,
          subscriptions: billingStats.activeSubscriptions * 0.9,
        },
        { month: t("subscriptions.months.may"), revenue: billingStats.monthlyRecurringRevenue, subscriptions: billingStats.activeSubscriptions },
        {
          month: t("subscriptions.months.jun"),
          revenue: billingStats.monthlyRecurringRevenue * 1.1,
          subscriptions: billingStats.activeSubscriptions * 1.05,
        },
      ]
    : [
        { month: t("subscriptions.months.jan"), revenue: 24500, subscriptions: 180 },
        { month: t("subscriptions.months.feb"), revenue: 26800, subscriptions: 195 },
        { month: t("subscriptions.months.mar"), revenue: 28200, subscriptions: 210 },
        { month: t("subscriptions.months.apr"), revenue: 29100, subscriptions: 225 },
        { month: t("subscriptions.months.may"), revenue: 31500, subscriptions: 240 },
        { month: t("subscriptions.months.jun"), revenue: 33200, subscriptions: 256 },
      ];

  const planDistributionData =
    billingStats && Object.keys(billingStats.subscriptionsByPlan).length > 0
      ? Object.entries(billingStats.subscriptionsByPlan).map(([name, value], index) => ({
          name: t(`subscriptions.planTypes.${name.toLowerCase()}`),
          value,
          color: ["#f59e0b", "#3b82f6", "#10b981", "#ef4444"][index % 4],
        }))
      : [
          { name: t("subscriptions.planTypes.basic"), value: 45, color: "#f59e0b" },
          { name: t("subscriptions.planTypes.pro"), value: 128, color: "#3b82f6" },
          { name: t("subscriptions.planTypes.enterprise"), value: 67, color: "#10b981" },
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

  const getStatusBadge = (status: string) => {
    const variants: Record<string, { variant: "default" | "secondary" | "destructive"; icon: any }> = {
      Active: { variant: "default", icon: CheckCircle },
      "Past Due": { variant: "destructive", icon: XCircle },
      Cancelled: { variant: "secondary", icon: XCircle },
      Trialing: { variant: "secondary", icon: Clock },
    };

    const statusTranslations: Record<string, string> = {
      Active: t("subscriptions.status.active"),
      "Past Due": t("subscriptions.status.pastDue"),
      Cancelled: t("subscriptions.status.cancelled"),
      Trialing: t("subscriptions.status.trialing"),
    };

    const config = variants[status] || { variant: "secondary", icon: Clock };
    const Icon = config.icon;

    return (
      <Badge variant={config.variant}>
        <Icon className="mr-1 h-3 w-3" />
        {statusTranslations[status] || status}
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

  if (error) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="text-center">
          <p className="text-red-500 mb-2">
            {t("subscriptions.errors.loadingError")}: {error}
          </p>
          <Button
            onClick={() => {
              dispatch(fetchPlans());
              dispatch(fetchSubscriptions({ page: 1, pageSize: 20 }));
              dispatch(fetchBillingStats());
            }}
          >
            {t("common.retry")}
          </Button>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">{t("subscriptions.title")}</h1>
          <p className="text-muted-foreground">{t("subscriptions.description")}</p>
        </div>
        <Button>
          <Plus className="mr-2 h-4 w-4" />
          {t("subscriptions.createPlan")}
        </Button>
      </div>

      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        <MetricCard
          title={t("subscriptions.metrics.monthlyRevenue")}
          value={`$${billingStats?.monthlyRecurringRevenue?.toLocaleString() || "0"}`}
          change={t("subscriptions.changes.revenueIncrease")}
          icon={DollarSign}
          description={t("subscriptions.descriptions.recurringRevenue")}
        />
        <MetricCard
          title={t("subscriptions.metrics.activeSubscriptions")}
          value={billingStats?.activeSubscriptions?.toString() || "0"}
          change={t("subscriptions.changes.subscriptionsIncrease")}
          icon={Users}
          description={t("subscriptions.descriptions.payingCustomers")}
        />
        <MetricCard
          title={t("subscriptions.metrics.churnRate")}
          value={`${billingStats?.churnRate?.toFixed(1) || "0"}%`}
          change={t("subscriptions.changes.churnDecrease")}
          icon={TrendingUp}
          description={t("subscriptions.descriptions.monthlyChurn")}
        />
        <MetricCard
          title={t("subscriptions.metrics.averageRevenue")}
          value={`$${billingStats?.averageRevenuePerUser?.toFixed(0) || "0"}`}
          change={t("subscriptions.changes.arpuIncrease")}
          icon={CreditCard}
          description={t("subscriptions.descriptions.perUserPerMonth")}
        />
      </div>

      <Tabs defaultValue="overview" className="space-y-4">
        <TabsList>
          <TabsTrigger value="overview">{t("subscriptions.tabs.overview")}</TabsTrigger>
          <TabsTrigger value="plans">{t("subscriptions.tabs.plans")}</TabsTrigger>
          <TabsTrigger value="subscriptions">{t("subscriptions.tabs.subscriptions")}</TabsTrigger>
          <TabsTrigger value="billing">{t("subscriptions.tabs.billing")}</TabsTrigger>
        </TabsList>

        <TabsContent value="overview" className="space-y-4">
          <div className="grid gap-4 md:grid-cols-2">
            <Card>
              <CardHeader>
                <CardTitle>{t("subscriptions.charts.revenueTrends")}</CardTitle>
                <CardDescription>{t("subscriptions.charts.revenueTrendsDescription")}</CardDescription>
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
                <CardTitle>{t("subscriptions.charts.planDistribution")}</CardTitle>
                <CardDescription>{t("subscriptions.charts.planDistributionDescription")}</CardDescription>
              </CardHeader>
              <CardContent>
                <ResponsiveContainer width="100%" height={300}>
                  <PieChart>
                    <Pie data={planDistributionData} cx="50%" cy="50%" innerRadius={60} outerRadius={100} paddingAngle={5} dataKey="value">
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
                      <div className="w-3 h-3 rounded-full" style={{ backgroundColor: entry.color }} />
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
            {(plans.length > 0 ? plans : mockPlans).map((plan) => (
              <Card key={plan.id}>
                <CardHeader>
                  <CardTitle className="flex items-center justify-between">
                    {plan.name}
                    <Badge variant="secondary">
                      {plan.subscribers || 0} {t("subscriptions.subscribers")}
                    </Badge>
                  </CardTitle>
                  <CardDescription>
                    <span className="text-2xl font-bold">${(plan as any).price || (plan as any).monthlyPrice || 0}</span>
                    <span className="text-muted-foreground">/{t("subscriptions.month")}</span>
                  </CardDescription>
                </CardHeader>
                <CardContent>
                  <ul className="space-y-2">
                    {plan.features.map((feature: string, index: number) => (
                      <li key={index} className="flex items-center text-sm">
                        <CheckCircle className="mr-2 h-4 w-4 text-green-500" />
                        {feature}
                      </li>
                    ))}
                  </ul>
                  <Button className="w-full mt-4" variant="outline">
                    {t("subscriptions.editPlan")}
                  </Button>
                </CardContent>
              </Card>
            ))}
          </div>
        </TabsContent>

        <TabsContent value="subscriptions" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle>{t("subscriptions.activeSubscriptions")}</CardTitle>
              <CardDescription>{t("subscriptions.manageSubscriptions")}</CardDescription>
            </CardHeader>
            <CardContent>
              <div className="flex items-center space-x-2 mb-4">
                <div className="relative flex-1">
                  <Search className="absolute left-2 top-2.5 h-4 w-4 text-muted-foreground" />
                  <Input placeholder={t("subscriptions.searchPlaceholder")} className="pl-8" />
                </div>
                <Button>
                  <Download className="mr-2 h-4 w-4" />
                  {t("common.export")}
                </Button>
              </div>

              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>{t("subscriptions.tenant")}</TableHead>
                    <TableHead>{t("subscriptions.plan")}</TableHead>
                    <TableHead>{t("subscriptions.status.label")}</TableHead>
                    <TableHead>{t("subscriptions.mrr")}</TableHead>
                    <TableHead>{t("subscriptions.users")}</TableHead>
                    <TableHead>{t("subscriptions.nextBilling")}</TableHead>
                    <TableHead>{t("subscriptions.actions")}</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {(subscriptions.length > 0 ? subscriptions : mockSubscriptions).map((subscription) => (
                    <TableRow key={subscription.id}>
                      <TableCell className="font-medium">
                        {(subscription as any).tenantName || (subscription as any).tenantId || t("common.unknown")}
                      </TableCell>
                      <TableCell>{(subscription as any).plan || (subscription as any).planName || t("common.unknown")}</TableCell>
                      <TableCell>{getStatusBadge(subscription.status)}</TableCell>
                      <TableCell>${(subscription as any).mrr || (subscription as any).amount || 0}</TableCell>
                      <TableCell>
                        <div className="flex items-center">
                          <Users className="mr-1 h-4 w-4" />
                          {(subscription as any).users || 0}
                        </div>
                      </TableCell>
                      <TableCell>
                        <div className="flex items-center">
                          <Calendar className="mr-1 h-4 w-4" />
                          {new Date((subscription as any).nextBilling || (subscription as any).nextBillingDate || Date.now()).toLocaleDateString()}
                        </div>
                      </TableCell>
                      <TableCell>
                        <Button variant="ghost" size="sm">
                          {t("subscriptions.manage")}
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
                <CardTitle>{t("subscriptions.billing.overview")}</CardTitle>
                <CardDescription>{t("subscriptions.billing.description")}</CardDescription>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="flex items-center justify-between">
                  <span>{t("subscriptions.billing.successfulPayments")}</span>
                  <Badge variant="default">98.5%</Badge>
                </div>
                <div className="flex items-center justify-between">
                  <span>{t("subscriptions.billing.failedPayments")}</span>
                  <Badge variant="destructive">1.5%</Badge>
                </div>
                <div className="flex items-center justify-between">
                  <span>{t("subscriptions.billing.pendingInvoices")}</span>
                  <Badge variant="secondary">12</Badge>
                </div>
                <div className="flex items-center justify-between">
                  <span>{t("subscriptions.billing.overdueInvoices")}</span>
                  <Badge variant="destructive">3</Badge>
                </div>
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle>{t("subscriptions.billing.paymentMethods")}</CardTitle>
                <CardDescription>{t("subscriptions.billing.paymentMethodsDescription")}</CardDescription>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="flex items-center justify-between">
                  <span>{t("subscriptions.billing.creditCard")}</span>
                  <Badge>85%</Badge>
                </div>
                <div className="flex items-center justify-between">
                  <span>{t("subscriptions.billing.bankTransfer")}</span>
                  <Badge>12%</Badge>
                </div>
                <div className="flex items-center justify-between">
                  <span>{t("subscriptions.billing.paypal")}</span>
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
