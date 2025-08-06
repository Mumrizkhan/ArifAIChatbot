import { useEffect, useState } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { useTranslation } from 'react-i18next';
import { useForm } from 'react-hook-form';
import { AppDispatch, RootState } from '../../store/store';
import { fetchSettings, updateSettings } from '../../store/slices/settingsSlice';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../../components/ui/card';
import { Button } from '../../components/ui/button';
import { Input } from '../../components/ui/input';
import { Label } from '../../components/ui/label';
import { Textarea } from '../../components/ui/textarea';
import { Switch } from '../../components/ui/switch';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '../../components/ui/tabs';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '../../components/ui/select';
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogTrigger,
} from '../../components/ui/alert-dialog';
import {
  Settings,
  User,
  Shield,
  Bell,
  Trash2,
  Save,
  AlertTriangle,
  Database,
  Clock,
} from 'lucide-react';

const SettingsPage = () => {
  const { t } = useTranslation();
  const dispatch = useDispatch<AppDispatch>();
  const { settings, isLoading, isSaving, error } = useSelector((state: RootState) => state.settings);
  const [activeTab, setActiveTab] = useState('general');
  const [isDeleteDialogOpen, setIsDeleteDialogOpen] = useState(false);

  const { register, handleSubmit, setValue, watch, formState: { errors }, reset } = useForm({
    defaultValues: settings,
  });

  useEffect(() => {
    dispatch(fetchSettings());
  }, [dispatch]);

  useEffect(() => {
    if (settings) {
      reset(settings);
    }
  }, [settings, reset]);

  const onSubmit = async (data: any) => {
    try {
      await dispatch(updateSettings(data));
    } catch (error) {
      console.error('Failed to save settings:', error);
    }
  };

  const handleDeleteTenant = async () => {
    setIsDeleteDialogOpen(false);
  };

  const timezones = [
    { value: 'UTC', label: 'UTC' },
    { value: 'America/New_York', label: 'Eastern Time' },
    { value: 'America/Chicago', label: 'Central Time' },
    { value: 'America/Denver', label: 'Mountain Time' },
    { value: 'America/Los_Angeles', label: 'Pacific Time' },
    { value: 'Europe/London', label: 'London' },
    { value: 'Europe/Paris', label: 'Paris' },
    { value: 'Asia/Tokyo', label: 'Tokyo' },
    { value: 'Asia/Dubai', label: 'Dubai' },
  ];

  const currencies = [
    { value: 'USD', label: 'US Dollar ($)' },
    { value: 'EUR', label: 'Euro (€)' },
    { value: 'GBP', label: 'British Pound (£)' },
    { value: 'AED', label: 'UAE Dirham (د.إ)' },
    { value: 'SAR', label: 'Saudi Riyal (ر.س)' },
  ];

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-32 w-32 border-b-2 border-blue-600"></div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="text-center">
          <p className="text-red-500 mb-2">Error loading settings: {error}</p>
          <Button onClick={() => dispatch(fetchSettings())}>
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
          <h1 className="text-3xl font-bold">{t('settings.title')}</h1>
          <p className="text-muted-foreground">
            {t('settings.subtitle')}
          </p>
        </div>
        <Button onClick={handleSubmit(onSubmit)} disabled={isSaving}>
          <Save className="mr-2 h-4 w-4" />
          {isSaving ? 'Saving...' : t('common.save')}
        </Button>
      </div>

      <Tabs value={activeTab} onValueChange={setActiveTab} className="space-y-4">
        <TabsList>
          <TabsTrigger value="general">
            <Settings className="mr-2 h-4 w-4" />
            {t('settings.general')}
          </TabsTrigger>
          <TabsTrigger value="notifications">
            <Bell className="mr-2 h-4 w-4" />
            {t('settings.notifications')}
          </TabsTrigger>
          <TabsTrigger value="security">
            <Shield className="mr-2 h-4 w-4" />
            {t('settings.security')}
          </TabsTrigger>
          <TabsTrigger value="data">
            <Database className="mr-2 h-4 w-4" />
            {t('settings.data')}
          </TabsTrigger>
          <TabsTrigger value="danger">
            <AlertTriangle className="mr-2 h-4 w-4" />
            {t('settings.danger')}
          </TabsTrigger>
        </TabsList>

        <TabsContent value="general" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle>{t('settings.tenantInformation')}</CardTitle>
              <CardDescription>
                {t('settings.tenantInformationDesc')}
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="grid gap-4 md:grid-cols-2">
                <div className="space-y-2">
                  <Label htmlFor="tenantName">{t('settings.tenantName')}</Label>
                  <Input
                    id="tenantName"
                    {...register('tenantName', { required: t('validation.required') })}
                    placeholder={t('settings.tenantNamePlaceholder')}
                  />
                  {errors.tenantName && (
                    <p className="text-sm text-red-500">{errors.tenantName.message}</p>
                  )}
                </div>

                <div className="space-y-2">
                  <Label htmlFor="contactEmail">{t('settings.contactEmail')}</Label>
                  <Input
                    id="contactEmail"
                    type="email"
                    {...register('contactEmail', { required: t('validation.required') })}
                    placeholder={t('settings.contactEmailPlaceholder')}
                  />
                  {errors.contactEmail && (
                    <p className="text-sm text-red-500">{errors.contactEmail.message}</p>
                  )}
                </div>
              </div>

              <div className="space-y-2">
                <Label htmlFor="tenantDescription">{t('settings.tenantDescription')}</Label>
                <Textarea
                  id="tenantDescription"
                  {...register('tenantDescription')}
                  placeholder={t('settings.tenantDescriptionPlaceholder')}
                  rows={3}
                />
              </div>

              <div className="grid gap-4 md:grid-cols-2">
                <div className="space-y-2">
                  <Label htmlFor="contactPhone">{t('settings.contactPhone')}</Label>
                  <Input
                    id="contactPhone"
                    {...register('contactPhone')}
                    placeholder={t('settings.contactPhonePlaceholder')}
                  />
                </div>

                <div className="space-y-2">
                  <Label htmlFor="timezone">{t('settings.timezone')}</Label>
                  <Select value={watch('timezone')} onValueChange={(value) => setValue('timezone', value)}>
                    <SelectTrigger>
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      {timezones.map((tz) => (
                        <SelectItem key={tz.value} value={tz.value}>
                          {tz.label}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>{t('settings.localization')}</CardTitle>
              <CardDescription>
                {t('settings.localizationDesc')}
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="grid gap-4 md:grid-cols-3">
                <div className="space-y-2">
                  <Label htmlFor="language">{t('settings.language')}</Label>
                  <Select value={watch('language')} onValueChange={(value) => setValue('language', value)}>
                    <SelectTrigger>
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="en">English</SelectItem>
                      <SelectItem value="ar">العربية</SelectItem>
                    </SelectContent>
                  </Select>
                </div>

                <div className="space-y-2">
                  <Label htmlFor="dateFormat">{t('settings.dateFormat')}</Label>
                  <Select value={watch('dateFormat')} onValueChange={(value) => setValue('dateFormat', value)}>
                    <SelectTrigger>
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="MM/DD/YYYY">MM/DD/YYYY</SelectItem>
                      <SelectItem value="DD/MM/YYYY">DD/MM/YYYY</SelectItem>
                      <SelectItem value="YYYY-MM-DD">YYYY-MM-DD</SelectItem>
                    </SelectContent>
                  </Select>
                </div>

                <div className="space-y-2">
                  <Label htmlFor="currency">{t('settings.currency')}</Label>
                  <Select value={watch('currency')} onValueChange={(value) => setValue('currency', value)}>
                    <SelectTrigger>
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      {currencies.map((currency) => (
                        <SelectItem key={currency.value} value={currency.value}>
                          {currency.label}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
              </div>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="notifications" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle>{t('settings.notificationPreferences')}</CardTitle>
              <CardDescription>
                {t('settings.notificationPreferencesDesc')}
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-6">
              <div className="flex items-center justify-between">
                <div className="space-y-0.5">
                  <Label>{t('settings.enableNotifications')}</Label>
                  <p className="text-sm text-muted-foreground">
                    {t('settings.enableNotificationsDesc')}
                  </p>
                </div>
                <Switch
                  checked={watch('enableNotifications')}
                  onCheckedChange={(checked) => setValue('enableNotifications', checked)}
                />
              </div>

              <div className="flex items-center justify-between">
                <div className="space-y-0.5">
                  <Label>{t('settings.enableEmailNotifications')}</Label>
                  <p className="text-sm text-muted-foreground">
                    {t('settings.enableEmailNotificationsDesc')}
                  </p>
                </div>
                <Switch
                  checked={watch('enableEmailNotifications')}
                  onCheckedChange={(checked) => setValue('enableEmailNotifications', checked)}
                />
              </div>

              <div className="flex items-center justify-between">
                <div className="space-y-0.5">
                  <Label>{t('settings.enableSmsNotifications')}</Label>
                  <p className="text-sm text-muted-foreground">
                    {t('settings.enableSmsNotificationsDesc')}
                  </p>
                </div>
                <Switch
                  checked={watch('enableSmsNotifications')}
                  onCheckedChange={(checked) => setValue('enableSmsNotifications', checked)}
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="notificationFrequency">{t('settings.notificationFrequency')}</Label>
                <Select value={watch('notificationFrequency')} onValueChange={(value) => setValue('notificationFrequency', value)}>
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="immediate">{t('settings.immediate')}</SelectItem>
                    <SelectItem value="hourly">{t('settings.hourly')}</SelectItem>
                    <SelectItem value="daily">{t('settings.daily')}</SelectItem>
                    <SelectItem value="weekly">{t('settings.weekly')}</SelectItem>
                  </SelectContent>
                </Select>
              </div>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="security" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle>{t('settings.securitySettings')}</CardTitle>
              <CardDescription>
                {t('settings.securitySettingsDesc')}
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-6">
              <div className="flex items-center justify-between">
                <div className="space-y-0.5">
                  <Label>{t('settings.enableTwoFactor')}</Label>
                  <p className="text-sm text-muted-foreground">
                    {t('settings.enableTwoFactorDesc')}
                  </p>
                </div>
                <Switch
                  checked={watch('enableTwoFactor')}
                  onCheckedChange={(checked) => setValue('enableTwoFactor', checked)}
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="sessionTimeout">{t('settings.sessionTimeout')}</Label>
                <div className="flex items-center space-x-2">
                  <Input
                    id="sessionTimeout"
                    type="number"
                    {...register('sessionTimeout', { min: 5, max: 480 })}
                    className="w-24"
                  />
                  <span className="text-sm text-muted-foreground">{t('settings.minutes')}</span>
                </div>
                <p className="text-sm text-muted-foreground">
                  {t('settings.sessionTimeoutDesc')}
                </p>
              </div>

              <div className="space-y-2">
                <Label htmlFor="allowedDomains">{t('settings.allowedDomains')}</Label>
                <Textarea
                  id="allowedDomains"
                  {...register('allowedDomains')}
                  placeholder={t('settings.allowedDomainsPlaceholder')}
                  rows={3}
                />
                <p className="text-sm text-muted-foreground">
                  {t('settings.allowedDomainsDesc')}
                </p>
              </div>

              <div className="space-y-2">
                <Label htmlFor="ipWhitelist">{t('settings.ipWhitelist')}</Label>
                <Textarea
                  id="ipWhitelist"
                  {...register('ipWhitelist')}
                  placeholder={t('settings.ipWhitelistPlaceholder')}
                  rows={3}
                />
                <p className="text-sm text-muted-foreground">
                  {t('settings.ipWhitelistDesc')}
                </p>
              </div>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="data" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle>{t('settings.dataManagement')}</CardTitle>
              <CardDescription>
                {t('settings.dataManagementDesc')}
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-6">
              <div className="space-y-2">
                <Label htmlFor="dataRetentionDays">{t('settings.dataRetentionDays')}</Label>
                <div className="flex items-center space-x-2">
                  <Input
                    id="dataRetentionDays"
                    type="number"
                    {...register('dataRetentionDays', { min: 30, max: 2555 })}
                    className="w-32"
                  />
                  <span className="text-sm text-muted-foreground">{t('settings.days')}</span>
                </div>
                <p className="text-sm text-muted-foreground">
                  {t('settings.dataRetentionDesc')}
                </p>
              </div>

              <div className="flex items-center justify-between">
                <div className="space-y-0.5">
                  <Label>{t('settings.enableDataExport')}</Label>
                  <p className="text-sm text-muted-foreground">
                    {t('settings.enableDataExportDesc')}
                  </p>
                </div>
                <Switch
                  checked={watch('enableDataExport')}
                  onCheckedChange={(checked) => setValue('enableDataExport', checked)}
                />
              </div>

              <div className="flex items-center justify-between">
                <div className="space-y-0.5">
                  <Label>{t('settings.enableAuditLogs')}</Label>
                  <p className="text-sm text-muted-foreground">
                    {t('settings.enableAuditLogsDesc')}
                  </p>
                </div>
                <Switch
                  checked={watch('enableAuditLogs')}
                  onCheckedChange={(checked) => setValue('enableAuditLogs', checked)}
                />
              </div>

              <div className="space-y-3">
                <Label>{t('settings.dataExportActions')}</Label>
                <div className="flex space-x-2">
                  <Button variant="outline">
                    <Database className="mr-2 h-4 w-4" />
                    {t('settings.exportConversations')}
                  </Button>
                  <Button variant="outline">
                    <User className="mr-2 h-4 w-4" />
                    {t('settings.exportUsers')}
                  </Button>
                  <Button variant="outline">
                    <Clock className="mr-2 h-4 w-4" />
                    {t('settings.exportAnalytics')}
                  </Button>
                </div>
              </div>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="danger" className="space-y-4">
          <Card className="border-red-200">
            <CardHeader>
              <CardTitle className="text-red-600">{t('settings.dangerZone')}</CardTitle>
              <CardDescription>
                {t('settings.dangerZoneDesc')}
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="p-4 border border-red-200 rounded-lg bg-red-50">
                <div className="flex items-start space-x-3">
                  <AlertTriangle className="h-5 w-5 text-red-500 mt-0.5" />
                  <div className="flex-1">
                    <h4 className="font-medium text-red-800">{t('settings.deleteTenant')}</h4>
                    <p className="text-sm text-red-700 mt-1">
                      {t('settings.deleteTenantDesc')}
                    </p>
                    <AlertDialog open={isDeleteDialogOpen} onOpenChange={setIsDeleteDialogOpen}>
                      <AlertDialogTrigger asChild>
                        <Button variant="destructive" className="mt-3">
                          <Trash2 className="mr-2 h-4 w-4" />
                          {t('settings.deleteTenant')}
                        </Button>
                      </AlertDialogTrigger>
                      <AlertDialogContent>
                        <AlertDialogHeader>
                          <AlertDialogTitle>{t('settings.confirmDelete')}</AlertDialogTitle>
                          <AlertDialogDescription>
                            {t('settings.confirmDeleteDesc')}
                          </AlertDialogDescription>
                        </AlertDialogHeader>
                        <AlertDialogFooter>
                          <AlertDialogCancel>{t('common.cancel')}</AlertDialogCancel>
                          <AlertDialogAction
                            onClick={handleDeleteTenant}
                            className="bg-red-600 hover:bg-red-700"
                          >
                            {t('settings.deleteForever')}
                          </AlertDialogAction>
                        </AlertDialogFooter>
                      </AlertDialogContent>
                    </AlertDialog>
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

export default SettingsPage;
