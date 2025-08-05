import { useState } from 'react';
import { useDispatch } from 'react-redux';
import { useTranslation } from 'react-i18next';
import { useForm } from 'react-hook-form';
import { AppDispatch } from '../../store/store';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../../components/ui/card';
import { Button } from '../../components/ui/button';
import { Input } from '../../components/ui/input';
import { Label } from '../../components/ui/label';
import { Textarea } from '../../components/ui/textarea';
import { Switch } from '../../components/ui/switch';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '../../components/ui/tabs';
import { Badge } from '../../components/ui/badge';
import { Avatar, AvatarFallback, AvatarImage } from '../../components/ui/avatar';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '../../components/ui/select';
import {
  User,
  Bell,
  Settings,
  Palette,
  Shield,
  Save,
  Upload,
  Monitor,
} from 'lucide-react';

const SettingsPage = () => {
  const { t } = useTranslation();
  useDispatch<AppDispatch>();
  const [activeTab, setActiveTab] = useState('profile');

  const { register, handleSubmit, setValue, watch, formState: { errors } } = useForm({
    defaultValues: {
      name: '',
      email: '',
      phone: '',
      avatar: '',
      bio: '',
      timezone: 'UTC',
      language: 'en',
      theme: 'light',
      enableNotifications: true,
      enableSoundNotifications: true,
      enableDesktopNotifications: true,
      enableEmailNotifications: false,
      notificationFrequency: 'immediate',
      autoAwayTime: 15,
      maxConcurrentChats: 5,
      enableTypingIndicator: true,
      enableReadReceipts: true,
      enableQuickReplies: true,
      workingHours: {
        start: '09:00',
        end: '17:00',
        timezone: 'UTC',
      },
    },
  });

  const onSubmit = (data: any) => {
    console.log('Settings data:', data);
  };

  const handleAvatarUpload = (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (file) {
      const reader = new FileReader();
      reader.onload = (e) => {
        setValue('avatar', e.target?.result as string);
      };
      reader.readAsDataURL(file);
    }
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

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">{t('settings.title')}</h1>
          <p className="text-muted-foreground">
            {t('settings.subtitle')}
          </p>
        </div>
        <Button onClick={handleSubmit(onSubmit)}>
          <Save className="mr-2 h-4 w-4" />
          {t('common.save')}
        </Button>
      </div>

      <Tabs value={activeTab} onValueChange={setActiveTab} className="space-y-4">
        <TabsList>
          <TabsTrigger value="profile">
            <User className="mr-2 h-4 w-4" />
            {t('settings.profile')}
          </TabsTrigger>
          <TabsTrigger value="notifications">
            <Bell className="mr-2 h-4 w-4" />
            {t('settings.notifications')}
          </TabsTrigger>
          <TabsTrigger value="preferences">
            <Settings className="mr-2 h-4 w-4" />
            {t('settings.preferences')}
          </TabsTrigger>
          <TabsTrigger value="appearance">
            <Palette className="mr-2 h-4 w-4" />
            {t('settings.appearance')}
          </TabsTrigger>
          <TabsTrigger value="security">
            <Shield className="mr-2 h-4 w-4" />
            {t('settings.security')}
          </TabsTrigger>
        </TabsList>

        <TabsContent value="profile" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle>{t('settings.profileInformation')}</CardTitle>
              <CardDescription>
                {t('settings.profileInformationDesc')}
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-6">
              <div className="flex items-center space-x-4">
                <Avatar className="h-20 w-20">
                  <AvatarImage src={watch('avatar')} />
                  <AvatarFallback>
                    {(watch('name') || '').split(' ').map((n: string) => n[0]).join('') || 'A'}
                  </AvatarFallback>
                </Avatar>
                <div>
                  <Label htmlFor="avatar-upload" className="cursor-pointer">
                    <Button variant="outline" asChild>
                      <span>
                        <Upload className="mr-2 h-4 w-4" />
                        {t('settings.uploadAvatar')}
                      </span>
                    </Button>
                  </Label>
                  <Input
                    id="avatar-upload"
                    type="file"
                    accept="image/*"
                    onChange={handleAvatarUpload}
                    className="hidden"
                  />
                  <p className="text-xs text-muted-foreground mt-2">
                    {t('settings.avatarFormats')}
                  </p>
                </div>
              </div>

              <div className="grid gap-4 md:grid-cols-2">
                <div className="space-y-2">
                  <Label htmlFor="name">{t('settings.fullName')}</Label>
                  <Input
                    id="name"
                    {...register('name', { required: t('validation.required') })}
                    placeholder={t('settings.fullNamePlaceholder')}
                  />
                  {errors.name && (
                    <p className="text-sm text-red-500">{errors.name.message}</p>
                  )}
                </div>

                <div className="space-y-2">
                  <Label htmlFor="email">{t('settings.email')}</Label>
                  <Input
                    id="email"
                    type="email"
                    {...register('email', { required: t('validation.required') })}
                    placeholder={t('settings.emailPlaceholder')}
                  />
                  {errors.email && (
                    <p className="text-sm text-red-500">{errors.email.message}</p>
                  )}
                </div>
              </div>

              <div className="space-y-2">
                <Label htmlFor="phone">{t('settings.phone')}</Label>
                <Input
                  id="phone"
                  {...register('phone')}
                  placeholder={t('settings.phonePlaceholder')}
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="bio">{t('settings.bio')}</Label>
                <Textarea
                  id="bio"
                  {...register('bio')}
                  placeholder={t('settings.bioPlaceholder')}
                  rows={3}
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
                  <Label>{t('settings.enableSoundNotifications')}</Label>
                  <p className="text-sm text-muted-foreground">
                    {t('settings.enableSoundNotificationsDesc')}
                  </p>
                </div>
                <Switch
                  checked={watch('enableSoundNotifications')}
                  onCheckedChange={(checked) => setValue('enableSoundNotifications', checked)}
                />
              </div>

              <div className="flex items-center justify-between">
                <div className="space-y-0.5">
                  <Label>{t('settings.enableDesktopNotifications')}</Label>
                  <p className="text-sm text-muted-foreground">
                    {t('settings.enableDesktopNotificationsDesc')}
                  </p>
                </div>
                <Switch
                  checked={watch('enableDesktopNotifications')}
                  onCheckedChange={(checked) => setValue('enableDesktopNotifications', checked)}
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

              <div className="space-y-2">
                <Label htmlFor="notificationFrequency">{t('settings.notificationFrequency')}</Label>
                <Select value={watch('notificationFrequency')} onValueChange={(value) => setValue('notificationFrequency', value)}>
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="immediate">{t('settings.immediate')}</SelectItem>
                    <SelectItem value="every5min">{t('settings.every5Minutes')}</SelectItem>
                    <SelectItem value="every15min">{t('settings.every15Minutes')}</SelectItem>
                    <SelectItem value="hourly">{t('settings.hourly')}</SelectItem>
                  </SelectContent>
                </Select>
              </div>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="preferences" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle>{t('settings.workspacePreferences')}</CardTitle>
              <CardDescription>
                {t('settings.workspacePreferencesDesc')}
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-6">
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
                <Label htmlFor="autoAwayTime">{t('settings.autoAwayTime')}</Label>
                <div className="flex items-center space-x-2">
                  <Input
                    id="autoAwayTime"
                    type="number"
                    {...register('autoAwayTime', { min: 5, max: 60 })}
                    className="w-24"
                  />
                  <span className="text-sm text-muted-foreground">{t('settings.minutes')}</span>
                </div>
                <p className="text-sm text-muted-foreground">
                  {t('settings.autoAwayTimeDesc')}
                </p>
              </div>

              <div className="space-y-2">
                <Label htmlFor="maxConcurrentChats">{t('settings.maxConcurrentChats')}</Label>
                <Input
                  id="maxConcurrentChats"
                  type="number"
                  {...register('maxConcurrentChats', { min: 1, max: 20 })}
                  className="w-24"
                />
                <p className="text-sm text-muted-foreground">
                  {t('settings.maxConcurrentChatsDesc')}
                </p>
              </div>

              <div className="flex items-center justify-between">
                <div className="space-y-0.5">
                  <Label>{t('settings.enableTypingIndicator')}</Label>
                  <p className="text-sm text-muted-foreground">
                    {t('settings.enableTypingIndicatorDesc')}
                  </p>
                </div>
                <Switch
                  checked={watch('enableTypingIndicator')}
                  onCheckedChange={(checked) => setValue('enableTypingIndicator', checked)}
                />
              </div>

              <div className="flex items-center justify-between">
                <div className="space-y-0.5">
                  <Label>{t('settings.enableReadReceipts')}</Label>
                  <p className="text-sm text-muted-foreground">
                    {t('settings.enableReadReceiptsDesc')}
                  </p>
                </div>
                <Switch
                  checked={watch('enableReadReceipts')}
                  onCheckedChange={(checked) => setValue('enableReadReceipts', checked)}
                />
              </div>

              <div className="flex items-center justify-between">
                <div className="space-y-0.5">
                  <Label>{t('settings.enableQuickReplies')}</Label>
                  <p className="text-sm text-muted-foreground">
                    {t('settings.enableQuickRepliesDesc')}
                  </p>
                </div>
                <Switch
                  checked={watch('enableQuickReplies')}
                  onCheckedChange={(checked) => setValue('enableQuickReplies', checked)}
                />
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>{t('settings.workingHours')}</CardTitle>
              <CardDescription>
                {t('settings.workingHoursDesc')}
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="grid gap-4 md:grid-cols-2">
                <div className="space-y-2">
                  <Label htmlFor="workingHoursStart">{t('settings.startTime')}</Label>
                  <Input
                    id="workingHoursStart"
                    type="time"
                    {...register('workingHours.start')}
                  />
                </div>

                <div className="space-y-2">
                  <Label htmlFor="workingHoursEnd">{t('settings.endTime')}</Label>
                  <Input
                    id="workingHoursEnd"
                    type="time"
                    {...register('workingHours.end')}
                  />
                </div>
              </div>

              <div className="space-y-2">
                <Label htmlFor="workingHoursTimezone">{t('settings.timezone')}</Label>
                <Select value={watch('workingHours.timezone')} onValueChange={(value) => setValue('workingHours.timezone', value)}>
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
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="appearance" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle>{t('settings.appearanceSettings')}</CardTitle>
              <CardDescription>
                {t('settings.appearanceSettingsDesc')}
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-6">
              <div className="space-y-2">
                <Label htmlFor="theme">{t('settings.theme')}</Label>
                <Select value={watch('theme')} onValueChange={(value) => setValue('theme', value)}>
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="light">{t('settings.lightTheme')}</SelectItem>
                    <SelectItem value="dark">{t('settings.darkTheme')}</SelectItem>
                    <SelectItem value="system">{t('settings.systemTheme')}</SelectItem>
                  </SelectContent>
                </Select>
              </div>

              <div className="space-y-3">
                <Label>{t('settings.themePreview')}</Label>
                <div className="p-4 border rounded-lg bg-background">
                  <div className="space-y-3">
                    <div className="flex items-center space-x-3">
                      <div className="w-8 h-8 rounded-full bg-primary"></div>
                      <div>
                        <div className="h-4 bg-foreground rounded w-24 mb-1"></div>
                        <div className="h-3 bg-muted-foreground rounded w-16"></div>
                      </div>
                    </div>
                    <div className="space-y-2">
                      <div className="h-3 bg-muted rounded w-full"></div>
                      <div className="h-3 bg-muted rounded w-3/4"></div>
                    </div>
                  </div>
                </div>
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
              <div className="space-y-4">
                <div>
                  <h4 className="font-medium mb-2">{t('settings.changePassword')}</h4>
                  <div className="space-y-3">
                    <div className="space-y-2">
                      <Label htmlFor="currentPassword">{t('settings.currentPassword')}</Label>
                      <Input
                        id="currentPassword"
                        type="password"
                        placeholder={t('settings.currentPasswordPlaceholder')}
                      />
                    </div>
                    <div className="space-y-2">
                      <Label htmlFor="newPassword">{t('settings.newPassword')}</Label>
                      <Input
                        id="newPassword"
                        type="password"
                        placeholder={t('settings.newPasswordPlaceholder')}
                      />
                    </div>
                    <div className="space-y-2">
                      <Label htmlFor="confirmPassword">{t('settings.confirmPassword')}</Label>
                      <Input
                        id="confirmPassword"
                        type="password"
                        placeholder={t('settings.confirmPasswordPlaceholder')}
                      />
                    </div>
                    <Button variant="outline">
                      {t('settings.updatePassword')}
                    </Button>
                  </div>
                </div>

                <div className="pt-4 border-t">
                  <h4 className="font-medium mb-2">{t('settings.activeSessions')}</h4>
                  <div className="space-y-3">
                    <div className="flex items-center justify-between p-3 border rounded-lg">
                      <div className="flex items-center space-x-3">
                        <Monitor className="h-5 w-5" />
                        <div>
                          <p className="font-medium">{t('settings.currentSession')}</p>
                          <p className="text-sm text-muted-foreground">Chrome on Windows • Active now</p>
                        </div>
                      </div>
                      <Badge variant="default">{t('settings.current')}</Badge>
                    </div>
                    <div className="flex items-center justify-between p-3 border rounded-lg">
                      <div className="flex items-center space-x-3">
                        <Monitor className="h-5 w-5" />
                        <div>
                          <p className="font-medium">Firefox on macOS</p>
                          <p className="text-sm text-muted-foreground">Last active 2 hours ago</p>
                        </div>
                      </div>
                      <Button variant="outline" size="sm">
                        {t('settings.revoke')}
                      </Button>
                    </div>
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
