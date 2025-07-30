import React, { useEffect, useState } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { useTranslation } from 'react-i18next';
import { useForm } from 'react-hook-form';
import { AppDispatch, RootState } from '../../store/store';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../../components/ui/card';
import { Button } from '../../components/ui/button';
import { Input } from '../../components/ui/input';
import { Label } from '../../components/ui/label';
import { Textarea } from '../../components/ui/textarea';
import { Switch } from '../../components/ui/switch';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '../../components/ui/tabs';
import { Badge } from '../../components/ui/badge';
import { Skeleton } from '../../components/ui/skeleton';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '../../components/ui/dialog';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '../../components/ui/select';
import {
  Plus,
  Settings,
  Key,
  Webhook,
  ExternalLink,
  Copy,
  Check,
  AlertCircle,
  Zap,
  Mail,
  MessageSquare,
  Database,
  Cloud,
  Code,
} from 'lucide-react';

const IntegrationsPage = () => {
  const { t } = useTranslation();
  const dispatch = useDispatch<AppDispatch>();
  const [activeTab, setActiveTab] = useState('api');
  const [isAddIntegrationOpen, setIsAddIntegrationOpen] = useState(false);
  const [copiedKey, setCopiedKey] = useState<string | null>(null);

  const { register, handleSubmit, setValue, watch, formState: { errors } } = useForm({
    defaultValues: {
      webhookUrl: '',
      apiKey: '',
      secretKey: '',
      enableWebhooks: true,
      enableApiAccess: true,
    },
  });

  const availableIntegrations = [
    {
      id: 'slack',
      name: 'Slack',
      description: 'Send notifications and messages to Slack channels',
      icon: MessageSquare,
      category: 'Communication',
      status: 'available',
      color: 'bg-purple-500',
    },
    {
      id: 'zapier',
      name: 'Zapier',
      description: 'Connect with 5000+ apps through Zapier automation',
      icon: Zap,
      category: 'Automation',
      status: 'available',
      color: 'bg-orange-500',
    },
    {
      id: 'mailchimp',
      name: 'Mailchimp',
      description: 'Sync customer data with Mailchimp for email marketing',
      icon: Mail,
      category: 'Marketing',
      status: 'available',
      color: 'bg-yellow-500',
    },
    {
      id: 'salesforce',
      name: 'Salesforce',
      description: 'Integrate with Salesforce CRM for lead management',
      icon: Database,
      category: 'CRM',
      status: 'available',
      color: 'bg-blue-500',
    },
    {
      id: 'hubspot',
      name: 'HubSpot',
      description: 'Connect with HubSpot for customer relationship management',
      icon: Database,
      category: 'CRM',
      status: 'connected',
      color: 'bg-orange-600',
    },
    {
      id: 'google-analytics',
      name: 'Google Analytics',
      description: 'Track chatbot interactions in Google Analytics',
      icon: Cloud,
      category: 'Analytics',
      status: 'available',
      color: 'bg-green-500',
    },
  ];

  const connectedIntegrations = availableIntegrations.filter(i => i.status === 'connected');
  const availableForConnection = availableIntegrations.filter(i => i.status === 'available');

  const webhookEvents = [
    { id: 'conversation.started', name: 'Conversation Started', enabled: true },
    { id: 'conversation.ended', name: 'Conversation Ended', enabled: true },
    { id: 'message.received', name: 'Message Received', enabled: false },
    { id: 'agent.assigned', name: 'Agent Assigned', enabled: true },
    { id: 'satisfaction.submitted', name: 'Satisfaction Submitted', enabled: true },
  ];

  const apiKeys = [
    {
      id: '1',
      name: 'Production API Key',
      key: 'ak_live_1234567890abcdef',
      created: '2024-01-15',
      lastUsed: '2024-01-20',
      permissions: ['read', 'write'],
    },
    {
      id: '2',
      name: 'Development API Key',
      key: 'ak_test_abcdef1234567890',
      created: '2024-01-10',
      lastUsed: '2024-01-19',
      permissions: ['read'],
    },
  ];

  const copyToClipboard = (text: string, keyId: string) => {
    navigator.clipboard.writeText(text);
    setCopiedKey(keyId);
    setTimeout(() => setCopiedKey(null), 2000);
  };

  const onSubmit = (data: any) => {
    console.log('Integration settings:', data);
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">{t('integrations.title')}</h1>
          <p className="text-muted-foreground">
            {t('integrations.subtitle')}
          </p>
        </div>
        <Dialog open={isAddIntegrationOpen} onOpenChange={setIsAddIntegrationOpen}>
          <DialogTrigger asChild>
            <Button>
              <Plus className="mr-2 h-4 w-4" />
              {t('integrations.addIntegration')}
            </Button>
          </DialogTrigger>
          <DialogContent className="max-w-2xl">
            <DialogHeader>
              <DialogTitle>{t('integrations.addIntegration')}</DialogTitle>
              <DialogDescription>
                {t('integrations.addIntegrationDesc')}
              </DialogDescription>
            </DialogHeader>
            <div className="grid gap-4 py-4">
              <div className="grid gap-3">
                {availableForConnection.map((integration) => (
                  <div
                    key={integration.id}
                    className="flex items-center justify-between p-4 border rounded-lg hover:bg-accent cursor-pointer"
                  >
                    <div className="flex items-center space-x-3">
                      <div className={`p-2 rounded-md ${integration.color}`}>
                        <integration.icon className="h-5 w-5 text-white" />
                      </div>
                      <div>
                        <h4 className="font-medium">{integration.name}</h4>
                        <p className="text-sm text-muted-foreground">{integration.description}</p>
                        <Badge variant="outline" className="mt-1">
                          {integration.category}
                        </Badge>
                      </div>
                    </div>
                    <Button size="sm">
                      {t('integrations.connect')}
                    </Button>
                  </div>
                ))}
              </div>
            </div>
          </DialogContent>
        </Dialog>
      </div>

      <Tabs value={activeTab} onValueChange={setActiveTab} className="space-y-4">
        <TabsList>
          <TabsTrigger value="api">
            <Key className="mr-2 h-4 w-4" />
            {t('integrations.apiKeys')}
          </TabsTrigger>
          <TabsTrigger value="webhooks">
            <Webhook className="mr-2 h-4 w-4" />
            {t('integrations.webhooks')}
          </TabsTrigger>
          <TabsTrigger value="connected">
            <Zap className="mr-2 h-4 w-4" />
            {t('integrations.connected')}
          </TabsTrigger>
          <TabsTrigger value="custom">
            <Code className="mr-2 h-4 w-4" />
            {t('integrations.custom')}
          </TabsTrigger>
        </TabsList>

        <TabsContent value="api" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle>{t('integrations.apiKeyManagement')}</CardTitle>
              <CardDescription>
                {t('integrations.apiKeyManagementDesc')}
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="flex justify-between items-center">
                <div>
                  <h4 className="font-medium">{t('integrations.apiAccess')}</h4>
                  <p className="text-sm text-muted-foreground">
                    {t('integrations.apiAccessDesc')}
                  </p>
                </div>
                <Switch
                  checked={watch('enableApiAccess')}
                  onCheckedChange={(checked) => setValue('enableApiAccess', checked)}
                />
              </div>

              <div className="space-y-3">
                {apiKeys.map((apiKey) => (
                  <div key={apiKey.id} className="flex items-center justify-between p-4 border rounded-lg">
                    <div className="space-y-1">
                      <div className="flex items-center space-x-2">
                        <h4 className="font-medium">{apiKey.name}</h4>
                        <div className="flex space-x-1">
                          {apiKey.permissions.map((permission) => (
                            <Badge key={permission} variant="secondary" className="text-xs">
                              {permission}
                            </Badge>
                          ))}
                        </div>
                      </div>
                      <div className="flex items-center space-x-4 text-sm text-muted-foreground">
                        <span>Created: {new Date(apiKey.created).toLocaleDateString()}</span>
                        <span>Last used: {new Date(apiKey.lastUsed).toLocaleDateString()}</span>
                      </div>
                      <div className="flex items-center space-x-2">
                        <code className="px-2 py-1 bg-muted rounded text-sm font-mono">
                          {apiKey.key.substring(0, 20)}...
                        </code>
                        <Button
                          size="sm"
                          variant="ghost"
                          onClick={() => copyToClipboard(apiKey.key, apiKey.id)}
                        >
                          {copiedKey === apiKey.id ? (
                            <Check className="h-4 w-4" />
                          ) : (
                            <Copy className="h-4 w-4" />
                          )}
                        </Button>
                      </div>
                    </div>
                    <Button variant="outline" size="sm">
                      {t('integrations.regenerate')}
                    </Button>
                  </div>
                ))}
              </div>

              <Button variant="outline">
                <Plus className="mr-2 h-4 w-4" />
                {t('integrations.generateNewKey')}
              </Button>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="webhooks" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle>{t('integrations.webhookConfiguration')}</CardTitle>
              <CardDescription>
                {t('integrations.webhookConfigurationDesc')}
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="flex justify-between items-center">
                <div>
                  <h4 className="font-medium">{t('integrations.enableWebhooks')}</h4>
                  <p className="text-sm text-muted-foreground">
                    {t('integrations.enableWebhooksDesc')}
                  </p>
                </div>
                <Switch
                  checked={watch('enableWebhooks')}
                  onCheckedChange={(checked) => setValue('enableWebhooks', checked)}
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="webhookUrl">{t('integrations.webhookUrl')}</Label>
                <Input
                  id="webhookUrl"
                  {...register('webhookUrl')}
                  placeholder="https://your-app.com/webhook"
                />
              </div>

              <div className="space-y-3">
                <Label>{t('integrations.webhookEvents')}</Label>
                {webhookEvents.map((event) => (
                  <div key={event.id} className="flex items-center justify-between p-3 border rounded-lg">
                    <div>
                      <h4 className="font-medium">{event.name}</h4>
                      <code className="text-sm text-muted-foreground">{event.id}</code>
                    </div>
                    <Switch defaultChecked={event.enabled} />
                  </div>
                ))}
              </div>

              <div className="space-y-2">
                <Label htmlFor="secretKey">{t('integrations.webhookSecret')}</Label>
                <div className="flex items-center space-x-2">
                  <Input
                    id="secretKey"
                    {...register('secretKey')}
                    placeholder="webhook_secret_key"
                    type="password"
                  />
                  <Button
                    type="button"
                    variant="outline"
                    onClick={() => setValue('secretKey', Math.random().toString(36).substring(2, 15))}
                  >
                    {t('integrations.generate')}
                  </Button>
                </div>
                <p className="text-sm text-muted-foreground">
                  {t('integrations.webhookSecretDesc')}
                </p>
              </div>

              <Button onClick={handleSubmit(onSubmit)}>
                {t('integrations.saveWebhookSettings')}
              </Button>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="connected" className="space-y-4">
          <div className="grid gap-4 md:grid-cols-2">
            {connectedIntegrations.length > 0 ? (
              connectedIntegrations.map((integration) => (
                <Card key={integration.id}>
                  <CardHeader>
                    <div className="flex items-center justify-between">
                      <div className="flex items-center space-x-3">
                        <div className={`p-2 rounded-md ${integration.color}`}>
                          <integration.icon className="h-5 w-5 text-white" />
                        </div>
                        <div>
                          <CardTitle className="text-lg">{integration.name}</CardTitle>
                          <Badge variant="default" className="mt-1">
                            {t('integrations.connected')}
                          </Badge>
                        </div>
                      </div>
                      <Button variant="ghost" size="sm">
                        <Settings className="h-4 w-4" />
                      </Button>
                    </div>
                  </CardHeader>
                  <CardContent>
                    <p className="text-sm text-muted-foreground mb-4">
                      {integration.description}
                    </p>
                    <div className="flex justify-between items-center">
                      <span className="text-sm text-muted-foreground">
                        Connected on Jan 15, 2024
                      </span>
                      <div className="flex space-x-2">
                        <Button variant="outline" size="sm">
                          {t('integrations.configure')}
                        </Button>
                        <Button variant="outline" size="sm" className="text-red-600">
                          {t('integrations.disconnect')}
                        </Button>
                      </div>
                    </div>
                  </CardContent>
                </Card>
              ))
            ) : (
              <div className="col-span-2 text-center py-8">
                <Zap className="mx-auto h-12 w-12 text-muted-foreground" />
                <h3 className="mt-2 text-sm font-semibold">No integrations connected</h3>
                <p className="mt-1 text-sm text-muted-foreground">
                  Connect your first integration to get started.
                </p>
                <Button className="mt-4" onClick={() => setIsAddIntegrationOpen(true)}>
                  <Plus className="mr-2 h-4 w-4" />
                  Add Integration
                </Button>
              </div>
            )}
          </div>
        </TabsContent>

        <TabsContent value="custom" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle>{t('integrations.customIntegrations')}</CardTitle>
              <CardDescription>
                {t('integrations.customIntegrationsDesc')}
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="p-4 border rounded-lg bg-muted/50">
                <div className="flex items-start space-x-3">
                  <AlertCircle className="h-5 w-5 text-orange-500 mt-0.5" />
                  <div>
                    <h4 className="font-medium">{t('integrations.customIntegrationNote')}</h4>
                    <p className="text-sm text-muted-foreground mt-1">
                      {t('integrations.customIntegrationNoteDesc')}
                    </p>
                  </div>
                </div>
              </div>

              <div className="space-y-4">
                <div>
                  <h4 className="font-medium mb-2">{t('integrations.apiEndpoints')}</h4>
                  <div className="space-y-2">
                    <div className="flex items-center justify-between p-3 border rounded-lg">
                      <div>
                        <code className="text-sm">GET /api/conversations</code>
                        <p className="text-xs text-muted-foreground">Retrieve conversation data</p>
                      </div>
                      <Button variant="ghost" size="sm">
                        <ExternalLink className="h-4 w-4" />
                      </Button>
                    </div>
                    <div className="flex items-center justify-between p-3 border rounded-lg">
                      <div>
                        <code className="text-sm">POST /api/messages</code>
                        <p className="text-xs text-muted-foreground">Send messages programmatically</p>
                      </div>
                      <Button variant="ghost" size="sm">
                        <ExternalLink className="h-4 w-4" />
                      </Button>
                    </div>
                    <div className="flex items-center justify-between p-3 border rounded-lg">
                      <div>
                        <code className="text-sm">GET /api/analytics</code>
                        <p className="text-xs text-muted-foreground">Access analytics data</p>
                      </div>
                      <Button variant="ghost" size="sm">
                        <ExternalLink className="h-4 w-4" />
                      </Button>
                    </div>
                  </div>
                </div>

                <div>
                  <h4 className="font-medium mb-2">{t('integrations.sdks')}</h4>
                  <div className="grid gap-2 md:grid-cols-3">
                    <Button variant="outline" className="justify-start">
                      <Code className="mr-2 h-4 w-4" />
                      JavaScript SDK
                    </Button>
                    <Button variant="outline" className="justify-start">
                      <Code className="mr-2 h-4 w-4" />
                      Python SDK
                    </Button>
                    <Button variant="outline" className="justify-start">
                      <Code className="mr-2 h-4 w-4" />
                      PHP SDK
                    </Button>
                  </div>
                </div>
              </div>
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>
    </div>
  );
};

export default IntegrationsPage;
