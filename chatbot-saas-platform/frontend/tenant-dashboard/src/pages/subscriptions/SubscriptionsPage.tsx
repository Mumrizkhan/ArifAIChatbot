import React, { useEffect, useState } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { useTranslation } from 'react-i18next';
import { AppDispatch, RootState } from '../../store/store';
import { 
  fetchSubscription, 
  fetchPlans, 
  fetchUsage, 
  fetchInvoices, 
  changePlan, 
  cancelSubscription,
  reactivateSubscription,
  clearError 
} from '../../store/slices/subscriptionSlice';
import { Button } from '../../components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../../components/ui/card';
import { Badge } from '../../components/ui/badge';
import { Progress } from '../../components/ui/progress';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '../../components/ui/tabs';
import { Skeleton } from '../../components/ui/skeleton';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '../../components/ui/table';
import { 
  CreditCard, 
  Calendar, 
  Users, 
  MessageSquare, 
  Database, 
  Download,
  CheckCircle,
  AlertCircle,
  Clock,
  TrendingUp,
  Zap
} from 'lucide-react';

const SubscriptionsPage: React.FC = () => {
  const { t } = useTranslation();
  const dispatch = useDispatch<AppDispatch>();
  const { subscription, plans, usage, invoices, isLoading, error } = useSelector(
    (state: RootState) => state.subscription
  );
  const [showCancelDialog, setShowCancelDialog] = useState(false);

  useEffect(() => {
    dispatch(fetchSubscription());
    dispatch(fetchPlans());
    dispatch(fetchUsage());
    dispatch(fetchInvoices());
  }, [dispatch]);

  const handlePlanChange = async (planId: string) => {
    if (window.confirm(t('subscription.confirmPlanChange'))) {
      await dispatch(changePlan({ planId, prorate: true }));
    }
  };

  const handleCancelSubscription = async () => {
    await dispatch(cancelSubscription({ immediately: false }));
    setShowCancelDialog(false);
  };

  const handleReactivateSubscription = async () => {
    await dispatch(reactivateSubscription());
  };

  const getStatusBadge = (status: string) => {
    const variants: Record<string, { variant: 'default' | 'secondary' | 'destructive'; icon: any }> = {
      active: { variant: 'default', icon: CheckCircle },
      past_due: { variant: 'destructive', icon: AlertCircle },
      canceled: { variant: 'secondary', icon: Clock },
      unpaid: { variant: 'destructive', icon: AlertCircle },
    };
    
    const config = variants[status] || { variant: 'secondary', icon: Clock };
    const Icon = config.icon;
    
    return (
      <Badge variant={config.variant} className="flex items-center gap-1">
        <Icon className="h-3 w-3" />
        {status.charAt(0).toUpperCase() + status.slice(1).replace('_', ' ')}
      </Badge>
    );
  };

  const getUsagePercentage = (used: number, limit: number) => {
    return limit > 0 ? Math.min((used / limit) * 100, 100) : 0;
  };


  if (isLoading) {
    return (
      <div className="space-y-6">
        <div className="flex items-center justify-between">
          <div>
            <Skeleton className="h-8 w-48" />
            <Skeleton className="h-4 w-64 mt-2" />
          </div>
        </div>
        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
          {[...Array(4)].map((_, i) => (
            <Card key={i}>
              <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                <Skeleton className="h-4 w-24" />
                <Skeleton className="h-4 w-4" />
              </CardHeader>
              <CardContent>
                <Skeleton className="h-8 w-16" />
                <Skeleton className="h-3 w-32 mt-2" />
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
          <p className="text-red-500 mb-2">Error loading subscription: {error}</p>
          <Button onClick={() => {
            dispatch(clearError());
            dispatch(fetchSubscription());
            dispatch(fetchPlans());
            dispatch(fetchUsage());
            dispatch(fetchInvoices());
          }}>
            Retry
          </Button>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">{t('subscription.title')}</h1>
          <p className="text-muted-foreground">
            {t('subscription.subtitle')}
          </p>
        </div>
      </div>

      {subscription && (
        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
          <Card>
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
              <CardTitle className="text-sm font-medium">Current Plan</CardTitle>
              <CreditCard className="h-4 w-4 text-muted-foreground" />
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold">{subscription.plan.name}</div>
              <p className="text-xs text-muted-foreground">
                ${subscription.plan.price}/{subscription.plan.interval}
              </p>
            </CardContent>
          </Card>

          <Card>
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
              <CardTitle className="text-sm font-medium">Status</CardTitle>
              <CheckCircle className="h-4 w-4 text-muted-foreground" />
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold">{getStatusBadge(subscription.status)}</div>
              <p className="text-xs text-muted-foreground">
                {subscription.cancelAtPeriodEnd ? 'Cancels at period end' : 'Active subscription'}
              </p>
            </CardContent>
          </Card>

          <Card>
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
              <CardTitle className="text-sm font-medium">Next Billing</CardTitle>
              <Calendar className="h-4 w-4 text-muted-foreground" />
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold">
                {new Date(subscription.currentPeriodEnd).toLocaleDateString()}
              </div>
              <p className="text-xs text-muted-foreground">
                Billing cycle end
              </p>
            </CardContent>
          </Card>

          <Card>
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
              <CardTitle className="text-sm font-medium">Trial Status</CardTitle>
              <Zap className="h-4 w-4 text-muted-foreground" />
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold">
                {subscription.trialEnd ? 'Trial' : 'Active'}
              </div>
              <p className="text-xs text-muted-foreground">
                {subscription.trialEnd 
                  ? `Ends ${new Date(subscription.trialEnd).toLocaleDateString()}`
                  : 'No trial period'
                }
              </p>
            </CardContent>
          </Card>
        </div>
      )}

      <Tabs defaultValue="overview" className="space-y-4">
        <TabsList>
          <TabsTrigger value="overview">Overview</TabsTrigger>
          <TabsTrigger value="usage">Usage</TabsTrigger>
          <TabsTrigger value="plans">Plans</TabsTrigger>
          <TabsTrigger value="billing">Billing History</TabsTrigger>
        </TabsList>

        <TabsContent value="overview" className="space-y-4">
          {subscription && (
            <div className="grid gap-4 md:grid-cols-2">
              <Card>
                <CardHeader>
                  <CardTitle>Subscription Details</CardTitle>
                  <CardDescription>
                    Your current subscription information
                  </CardDescription>
                </CardHeader>
                <CardContent className="space-y-4">
                  <div className="flex justify-between">
                    <span className="text-sm font-medium">Plan:</span>
                    <span className="text-sm">{subscription.plan.name}</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-sm font-medium">Price:</span>
                    <span className="text-sm">${subscription.plan.price}/{subscription.plan.interval}</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-sm font-medium">Status:</span>
                    {getStatusBadge(subscription.status)}
                  </div>
                  <div className="flex justify-between">
                    <span className="text-sm font-medium">Started:</span>
                    <span className="text-sm">{new Date(subscription.currentPeriodStart).toLocaleDateString()}</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-sm font-medium">Next billing:</span>
                    <span className="text-sm">{new Date(subscription.currentPeriodEnd).toLocaleDateString()}</span>
                  </div>
                </CardContent>
              </Card>

              <Card>
                <CardHeader>
                  <CardTitle>Plan Features</CardTitle>
                  <CardDescription>
                    What's included in your current plan
                  </CardDescription>
                </CardHeader>
                <CardContent>
                  <ul className="space-y-2">
                    {subscription.plan.features.map((feature, index) => (
                      <li key={index} className="flex items-center text-sm">
                        <CheckCircle className="mr-2 h-4 w-4 text-green-500" />
                        {feature}
                      </li>
                    ))}
                  </ul>
                </CardContent>
              </Card>
            </div>
          )}

          {subscription && (
            <Card>
              <CardHeader>
                <CardTitle>Subscription Actions</CardTitle>
                <CardDescription>
                  Manage your subscription
                </CardDescription>
              </CardHeader>
              <CardContent className="flex gap-4">
                {subscription.status === 'canceled' ? (
                  <Button onClick={handleReactivateSubscription}>
                    Reactivate Subscription
                  </Button>
                ) : (
                  <Button 
                    variant="destructive" 
                    onClick={() => setShowCancelDialog(true)}
                  >
                    Cancel Subscription
                  </Button>
                )}
              </CardContent>
            </Card>
          )}
        </TabsContent>

        <TabsContent value="usage" className="space-y-4">
          {usage && subscription && (
            <div className="grid gap-4 md:grid-cols-2">
              <Card>
                <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                  <CardTitle className="text-sm font-medium">Conversations</CardTitle>
                  <MessageSquare className="h-4 w-4 text-muted-foreground" />
                </CardHeader>
                <CardContent>
                  <div className="text-2xl font-bold">
                    {usage.conversations.toLocaleString()} / {subscription.plan.limits.conversations.toLocaleString()}
                  </div>
                  <Progress 
                    value={getUsagePercentage(usage.conversations, subscription.plan.limits.conversations)} 
                    className="mt-2"
                  />
                  <p className="text-xs text-muted-foreground mt-2">
                    {getUsagePercentage(usage.conversations, subscription.plan.limits.conversations).toFixed(1)}% used
                  </p>
                </CardContent>
              </Card>

              <Card>
                <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                  <CardTitle className="text-sm font-medium">Agents</CardTitle>
                  <Users className="h-4 w-4 text-muted-foreground" />
                </CardHeader>
                <CardContent>
                  <div className="text-2xl font-bold">
                    {usage.agents} / {subscription.plan.limits.agents}
                  </div>
                  <Progress 
                    value={getUsagePercentage(usage.agents, subscription.plan.limits.agents)} 
                    className="mt-2"
                  />
                  <p className="text-xs text-muted-foreground mt-2">
                    {getUsagePercentage(usage.agents, subscription.plan.limits.agents).toFixed(1)}% used
                  </p>
                </CardContent>
              </Card>

              <Card>
                <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                  <CardTitle className="text-sm font-medium">Storage</CardTitle>
                  <Database className="h-4 w-4 text-muted-foreground" />
                </CardHeader>
                <CardContent>
                  <div className="text-2xl font-bold">
                    {(usage.storage / 1024 / 1024).toFixed(1)} GB / {(subscription.plan.limits.storage / 1024 / 1024).toFixed(1)} GB
                  </div>
                  <Progress 
                    value={getUsagePercentage(usage.storage, subscription.plan.limits.storage)} 
                    className="mt-2"
                  />
                  <p className="text-xs text-muted-foreground mt-2">
                    {getUsagePercentage(usage.storage, subscription.plan.limits.storage).toFixed(1)}% used
                  </p>
                </CardContent>
              </Card>

              <Card>
                <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                  <CardTitle className="text-sm font-medium">API Calls</CardTitle>
                  <TrendingUp className="h-4 w-4 text-muted-foreground" />
                </CardHeader>
                <CardContent>
                  <div className="text-2xl font-bold">
                    {usage.apiCalls.toLocaleString()} / {subscription.plan.limits.apiCalls.toLocaleString()}
                  </div>
                  <Progress 
                    value={getUsagePercentage(usage.apiCalls, subscription.plan.limits.apiCalls)} 
                    className="mt-2"
                  />
                  <p className="text-xs text-muted-foreground mt-2">
                    {getUsagePercentage(usage.apiCalls, subscription.plan.limits.apiCalls).toFixed(1)}% used
                  </p>
                </CardContent>
              </Card>
            </div>
          )}
        </TabsContent>

        <TabsContent value="plans" className="space-y-4">
          <div className="grid gap-4 md:grid-cols-3">
            {plans.map((plan) => (
              <Card key={plan.id} className={subscription?.planId === plan.id ? 'border-primary' : ''}>
                <CardHeader>
                  <CardTitle className="flex items-center justify-between">
                    {plan.name}
                    {plan.isPopular && <Badge variant="secondary">Popular</Badge>}
                    {subscription?.planId === plan.id && <Badge>Current</Badge>}
                  </CardTitle>
                  <CardDescription>
                    <span className="text-2xl font-bold">${plan.price}</span>
                    <span className="text-muted-foreground">/{plan.interval}</span>
                  </CardDescription>
                </CardHeader>
                <CardContent>
                  <p className="text-sm text-muted-foreground mb-4">{plan.description}</p>
                  <ul className="space-y-2 mb-4">
                    {plan.features.map((feature, index) => (
                      <li key={index} className="flex items-center text-sm">
                        <CheckCircle className="mr-2 h-4 w-4 text-green-500" />
                        {feature}
                      </li>
                    ))}
                  </ul>
                  <div className="space-y-2 text-xs text-muted-foreground mb-4">
                    <div>• {plan.limits.conversations.toLocaleString()} conversations/month</div>
                    <div>• {plan.limits.agents} agents</div>
                    <div>• {(plan.limits.storage / 1024 / 1024).toFixed(1)} GB storage</div>
                    <div>• {plan.limits.apiCalls.toLocaleString()} API calls/month</div>
                  </div>
                  {subscription?.planId !== plan.id && (
                    <Button 
                      className="w-full" 
                      onClick={() => handlePlanChange(plan.id)}
                    >
                      {subscription ? 'Switch to this plan' : 'Choose this plan'}
                    </Button>
                  )}
                </CardContent>
              </Card>
            ))}
          </div>
        </TabsContent>

        <TabsContent value="billing" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle>Billing History</CardTitle>
              <CardDescription>
                Your past invoices and payments
              </CardDescription>
            </CardHeader>
            <CardContent>
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Invoice</TableHead>
                    <TableHead>Date</TableHead>
                    <TableHead>Amount</TableHead>
                    <TableHead>Status</TableHead>
                    <TableHead>Actions</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {invoices.map((invoice) => (
                    <TableRow key={invoice.id}>
                      <TableCell className="font-medium">{invoice.number}</TableCell>
                      <TableCell>{new Date(invoice.dueDate).toLocaleDateString()}</TableCell>
                      <TableCell>${invoice.amount.toFixed(2)} {invoice.currency.toUpperCase()}</TableCell>
                      <TableCell>
                        <Badge variant={invoice.status === 'paid' ? 'default' : invoice.status === 'pending' ? 'secondary' : 'destructive'}>
                          {invoice.status}
                        </Badge>
                      </TableCell>
                      <TableCell>
                        {invoice.downloadUrl && (
                          <Button variant="ghost" size="sm" asChild>
                            <a href={invoice.downloadUrl} target="_blank" rel="noopener noreferrer">
                              <Download className="h-4 w-4" />
                            </a>
                          </Button>
                        )}
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>

      {showCancelDialog && (
        <div className="fixed inset-0 bg-gray-600 bg-opacity-50 overflow-y-auto h-full w-full z-50">
          <div className="relative top-20 mx-auto p-5 border w-96 shadow-lg rounded-md bg-white dark:bg-gray-800">
            <div className="mt-3 text-center">
              <h3 className="text-lg font-medium text-gray-900 dark:text-white">
                Cancel Subscription
              </h3>
              <div className="mt-2 px-7 py-3">
                <p className="text-sm text-gray-500 dark:text-gray-400">
                  Are you sure you want to cancel your subscription? You will continue to have access until the end of your current billing period.
                </p>
              </div>
              <div className="flex gap-4 px-4 py-3">
                <Button
                  variant="outline"
                  className="flex-1"
                  onClick={() => setShowCancelDialog(false)}
                >
                  Keep Subscription
                </Button>
                <Button
                  variant="destructive"
                  className="flex-1"
                  onClick={handleCancelSubscription}
                >
                  Cancel Subscription
                </Button>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default SubscriptionsPage;
