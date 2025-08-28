import React, { useEffect } from "react";
import { useDispatch, useSelector } from "react-redux";
import { useTranslation } from "react-i18next";
import { AppDispatch, RootState } from "../../store/store";
import { setThemeMode, setLanguage, setPrimaryColor } from "../../store/slices/themeSlice";
import { fetchSettings, updateSettings, updateSystemSettings, updateNotificationSettings, clearError } from "../../store/slices/settingsSlice";
import { Button } from "../../components/ui/button";
import { Input } from "../../components/ui/input";
import { Label } from "../../components/ui/label";
import { Switch } from "../../components/ui/switch";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "../../components/ui/select";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "../../components/ui/card";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "../../components/ui/tabs";
import { Separator } from "../../components/ui/separator";
import { Badge } from "../../components/ui/badge";
import { Settings, Palette, Globe, Shield, Bell, Zap, Database, Mail, Smartphone, Key, Users, Monitor, Sun, Moon } from "lucide-react";

const SettingsPage: React.FC = () => {
  const { t, i18n } = useTranslation();
  const dispatch = useDispatch<AppDispatch>();
  const { mode, language, primaryColor } = useSelector((state: RootState) => state.theme);
  const { user } = useSelector((state: RootState) => state.auth);
  const { systemSettings, notificationSettings, integrationSettings, isLoading, isSaving, error } = useSelector((state: RootState) => state.settings);

  useEffect(() => {
    dispatch(fetchSettings());
  }, [dispatch]);
  useEffect(() => {
    const direction = language === "ar" ? "rtl" : "ltr";
    document.documentElement.dir = direction; // Set the direction (RTL or LTR)
    document.documentElement.lang = language; // Set the language attribute
    i18n.changeLanguage(language); // Update i18n language
  }, [language, i18n]);

  const handleThemeChange = (newMode: "light" | "dark" | "system") => {
    dispatch(setThemeMode(newMode));
  };

  const handleLanguageChange = (newLanguage: "en" | "ar") => {
    dispatch(setLanguage(newLanguage));
  };

  const handleColorChange = (color: string) => {
    dispatch(setPrimaryColor(color));
  };

  const handleNotificationChange = (key: string, value: boolean) => {
    dispatch(updateNotificationSettings({ [key]: value }));
  };

  const handleSystemSettingChange = (key: string, value: any) => {
    dispatch(updateSystemSettings({ [key]: value }));
  };

  const handleSaveSettings = async () => {
    try {
      await dispatch(
        updateSettings({
          systemSettings: systemSettings,
          notificationSettings: notificationSettings,
          integrationSettings: integrationSettings,
        })
      ).unwrap();
      
      console.log("System settings saved successfully");
    } catch (error) {
      console.error("Failed to save system settings:", error);
    }
  };

  const predefinedColors = [
    { name: t("settings.colors.blue"), value: "#3b82f6" },
    { name: t("settings.colors.green"), value: "#10b981" },
    { name: t("settings.colors.purple"), value: "#8b5cf6" },
    { name: t("settings.colors.red"), value: "#ef4444" },
    { name: t("settings.colors.orange"), value: "#f59e0b" },
    { name: t("settings.colors.pink"), value: "#ec4899" },
  ];

  return (
    <div className={`space-y-6 ${language === "ar" ? "rtl" : "ltr"}`}>
      <div>
        <h1 className="text-3xl font-bold">{t("settings.title")}</h1>
        <p className="text-muted-foreground">{t("settings.description")}</p>
      </div>

      <Tabs dir={language === "ar" ? "rtl" : "ltr"} defaultValue="general" className="space-y-4">
        <TabsList className="grid w-full grid-cols-6">
          <TabsTrigger value="general">{t("settings.tabs.general")}</TabsTrigger>
          <TabsTrigger value="appearance">{t("settings.tabs.appearance")}</TabsTrigger>
          <TabsTrigger value="security">{t("settings.tabs.security")}</TabsTrigger>
          <TabsTrigger value="notifications">{t("settings.tabs.notifications")}</TabsTrigger>
          <TabsTrigger value="integrations">{t("settings.tabs.integrations")}</TabsTrigger>
          <TabsTrigger value="system">{t("settings.tabs.system")}</TabsTrigger>
        </TabsList>

        <TabsContent value="general" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center">
                <Settings className="mr-2 h-5 w-5" />
                {t("settings.general.title")}
              </CardTitle>
              <CardDescription>{t("settings.general.description")}</CardDescription>
            </CardHeader>
            <CardContent className="space-y-6">
              <div className="grid gap-4 md:grid-cols-2">
                <div className="space-y-2">
                  <Label htmlFor="company-name">{t("settings.general.companyName")}</Label>
                  <Input
                    id="company-name"
                    value={systemSettings.companyName}
                    onChange={(e) => handleSystemSettingChange("companyName", e.target.value)}
                  />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="admin-email">{t("settings.general.adminEmail")}</Label>
                  <Input
                    id="admin-email"
                    type="email"
                    value={systemSettings.adminEmail}
                    onChange={(e) => handleSystemSettingChange("adminEmail", e.target.value)}
                  />
                </div>
              </div>

              <div className="space-y-2">
                <Label htmlFor="timezone">{t("settings.general.timezone")}</Label>
                <Select value={systemSettings.timezone} onValueChange={(value) => handleSystemSettingChange("timezone", value)}>
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="utc">{t("settings.general.timezones.utc")}</SelectItem>
                    <SelectItem value="est">{t("settings.general.timezones.est")}</SelectItem>
                    <SelectItem value="pst">{t("settings.general.timezones.pst")}</SelectItem>
                    <SelectItem value="gmt">{t("settings.general.timezones.gmt")}</SelectItem>
                  </SelectContent>
                </Select>
              </div>

              <div className="space-y-2">
                <Label htmlFor="language">{t("settings.general.language")}</Label>
                <Select value={language} onValueChange={handleLanguageChange}>
                  <SelectTrigger>
                    <Globe className="mr-2 h-4 w-4" />
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="en">{t("settings.general.languages.english")}</SelectItem>
                    <SelectItem value="ar">{t("settings.general.languages.arabic")}</SelectItem>
                  </SelectContent>
                </Select>
              </div>

              <Separator />

              <div className="flex items-center justify-between">
                <div className="space-y-0.5">
                  <Label>{t("settings.general.maintenanceMode")}</Label>
                  <p className="text-sm text-muted-foreground">{t("settings.general.maintenanceModeDescription")}</p>
                </div>
                <Switch />
              </div>

              <div className="flex items-center justify-between">
                <div className="space-y-0.5">
                  <Label>{t("settings.general.userRegistration")}</Label>
                  <p className="text-sm text-muted-foreground">{t("settings.general.userRegistrationDescription")}</p>
                </div>
                <Switch defaultChecked />
              </div>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="appearance" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center">
                <Palette className="mr-2 h-5 w-5" />
                {t("settings.appearance.title")}
              </CardTitle>
              <CardDescription>{t("settings.appearance.description")}</CardDescription>
            </CardHeader>
            <CardContent className="space-y-6">
              <div className="space-y-2">
                <Label>{t("settings.appearance.theme")}</Label>
                <div className="flex space-x-2">
                  <Button variant={mode === "light" ? "default" : "outline"} size="sm" onClick={() => handleThemeChange("light")}>
                    <Sun className="mr-2 h-4 w-4" />
                    {t("settings.appearance.lightMode")}
                  </Button>
                  <Button variant={mode === "dark" ? "default" : "outline"} size="sm" onClick={() => handleThemeChange("dark")}>
                    <Moon className="mr-2 h-4 w-4" />
                    {t("settings.appearance.darkMode")}
                  </Button>
                  <Button variant={mode === "system" ? "default" : "outline"} size="sm" onClick={() => handleThemeChange("system")}>
                    <Monitor className="mr-2 h-4 w-4" />
                    {t("settings.appearance.systemMode")}
                  </Button>
                </div>
              </div>

              <div className="space-y-2">
                <Label>{t("settings.appearance.primaryColor")}</Label>
                <div className="flex space-x-2">
                  {predefinedColors.map((color) => (
                    <button
                      key={color.value}
                      className={`w-8 h-8 rounded-full border-2 ${primaryColor === color.value ? "border-foreground" : "border-transparent"}`}
                      style={{ backgroundColor: color.value }}
                      onClick={() => handleColorChange(color.value)}
                      title={color.name}
                    />
                  ))}
                </div>
              </div>

              <div className="space-y-2">
                <Label htmlFor="custom-color">{t("settings.appearance.customColor")}</Label>
                <div className="flex space-x-2">
                  <Input
                    id="custom-color"
                    type="color"
                    value={primaryColor}
                    onChange={(e) => handleColorChange(e.target.value)}
                    className="w-16 h-10"
                  />
                  <Input value={primaryColor} onChange={(e) => handleColorChange(e.target.value)} placeholder="#3b82f6" />
                </div>
              </div>

              <Separator />

              <div className="flex items-center justify-between">
                <div className="space-y-0.5">
                  <Label>{t("settings.appearance.compactMode")}</Label>
                  <p className="text-sm text-muted-foreground">{t("settings.appearance.compactModeDescription")}</p>
                </div>
                <Switch />
              </div>

              <div className="flex items-center justify-between">
                <div className="space-y-0.5">
                  <Label>{t("settings.appearance.animations")}</Label>
                  <p className="text-sm text-muted-foreground">{t("settings.appearance.animationsDescription")}</p>
                </div>
                <Switch defaultChecked />
              </div>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="security" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center">
                <Shield className="mr-2 h-5 w-5" />
                {t("settings.security.title")}
              </CardTitle>
              <CardDescription>{t("settings.security.description")}</CardDescription>
            </CardHeader>
            <CardContent className="space-y-6">
              <div className="flex items-center justify-between">
                <div className="space-y-0.5">
                  <Label>{t("settings.security.twoFactor")}</Label>
                  <p className="text-sm text-muted-foreground">{t("settings.security.twoFactorDescription")}</p>
                </div>
                <Switch checked={systemSettings.twoFactor} onCheckedChange={(checked) => handleSystemSettingChange("twoFactor", checked)} />
              </div>

              <div className="space-y-2">
                <Label htmlFor="session-timeout">{t("settings.security.sessionTimeout")}</Label>
                <Input
                  id="session-timeout"
                  type="number"
                  value={systemSettings.sessionTimeout}
                  onChange={(e) => handleSystemSettingChange("sessionTimeout", parseInt(e.target.value))}
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="password-expiry">{t("settings.security.passwordExpiry")}</Label>
                <Input
                  id="password-expiry"
                  type="number"
                  value={systemSettings.passwordExpiry}
                  onChange={(e) => handleSystemSettingChange("passwordExpiry", parseInt(e.target.value))}
                />
              </div>

              <Separator />

              <div className="space-y-4">
                <h4 className="text-sm font-medium">{t("settings.security.apiKeys")}</h4>
                <div className="space-y-2">
                  <div className="flex items-center justify-between p-3 border rounded-lg">
                    <div>
                      <p className="font-medium">{t("settings.security.productionApiKey")}</p>
                      <p className="text-sm text-muted-foreground">
                        {t("settings.security.lastUsed")}: {t("settings.security.timeAgo.twoHours")}
                      </p>
                    </div>
                    <Badge variant="default">{t("settings.security.status.active")}</Badge>
                  </div>
                  <div className="flex items-center justify-between p-3 border rounded-lg">
                    <div>
                      <p className="font-medium">{t("settings.security.developmentApiKey")}</p>
                      <p className="text-sm text-muted-foreground">
                        {t("settings.security.lastUsed")}: {t("settings.security.timeAgo.oneDay")}
                      </p>
                    </div>
                    <Badge variant="secondary">{t("settings.security.status.active")}</Badge>
                  </div>
                </div>
                <Button variant="outline">
                  <Key className="mr-2 h-4 w-4" />
                  {t("settings.security.generateNewKey")}
                </Button>
              </div>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="notifications" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center">
                <Bell className="mr-2 h-5 w-5" />
                {t("settings.notifications.title")}
              </CardTitle>
              <CardDescription>{t("settings.notifications.description")}</CardDescription>
            </CardHeader>
            <CardContent className="space-y-6">
              <div className="flex items-center justify-between">
                <div className="flex items-center space-x-2">
                  <Mail className="h-4 w-4" />
                  <div className="space-y-0.5">
                    <Label>{t("settings.notifications.email")}</Label>
                    <p className="text-sm text-muted-foreground">{t("settings.notifications.emailDescription")}</p>
                  </div>
                </div>
                <Switch checked={notificationSettings.email} onCheckedChange={(checked) => handleNotificationChange("email", checked)} />
              </div>

              <div className="flex items-center justify-between">
                <div className="flex items-center space-x-2">
                  <Bell className="h-4 w-4" />
                  <div className="space-y-0.5">
                    <Label>{t("settings.notifications.push")}</Label>
                    <p className="text-sm text-muted-foreground">{t("settings.notifications.pushDescription")}</p>
                  </div>
                </div>
                <Switch checked={notificationSettings.push} onCheckedChange={(checked) => handleNotificationChange("push", checked)} />
              </div>

              <div className="flex items-center justify-between">
                <div className="flex items-center space-x-2">
                  <Smartphone className="h-4 w-4" />
                  <div className="space-y-0.5">
                    <Label>{t("settings.notifications.sms")}</Label>
                    <p className="text-sm text-muted-foreground">{t("settings.notifications.smsDescription")}</p>
                  </div>
                </div>
                <Switch checked={notificationSettings.sms} onCheckedChange={(checked) => handleNotificationChange("sms", checked)} />
              </div>

              <div className="flex items-center justify-between">
                <div className="flex items-center space-x-2">
                  <Monitor className="h-4 w-4" />
                  <div className="space-y-0.5">
                    <Label>{t("settings.notifications.inApp")}</Label>
                    <p className="text-sm text-muted-foreground">{t("settings.notifications.inAppDescription")}</p>
                  </div>
                </div>
                <Switch checked={notificationSettings.inApp} onCheckedChange={(checked) => handleNotificationChange("inApp", checked)} />
              </div>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="integrations" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center">
                <Zap className="mr-2 h-5 w-5" />
                {t("settings.integrations.title")}
              </CardTitle>
              <CardDescription>{t("settings.integrations.description")}</CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="space-y-4">
                <div className="flex items-center justify-between p-4 border rounded-lg">
                  <div className="flex items-center space-x-3">
                    <div className="w-10 h-10 bg-blue-100 rounded-lg flex items-center justify-center">
                      <Mail className="h-5 w-5 text-blue-600" />
                    </div>
                    <div>
                      <p className="font-medium">{t("settings.integrations.sendgrid.name")}</p>
                      <p className="text-sm text-muted-foreground">{t("settings.integrations.sendgrid.description")}</p>
                    </div>
                  </div>
                  <Badge variant="default">{t("settings.integrations.status.connected")}</Badge>
                </div>

                <div className="flex items-center justify-between p-4 border rounded-lg">
                  <div className="flex items-center space-x-3">
                    <div className="w-10 h-10 bg-green-100 rounded-lg flex items-center justify-center">
                      <Smartphone className="h-5 w-5 text-green-600" />
                    </div>
                    <div>
                      <p className="font-medium">{t("settings.integrations.twilio.name")}</p>
                      <p className="text-sm text-muted-foreground">{t("settings.integrations.twilio.description")}</p>
                    </div>
                  </div>
                  <Badge variant="default">{t("settings.integrations.status.connected")}</Badge>
                </div>

                <div className="flex items-center justify-between p-4 border rounded-lg">
                  <div className="flex items-center space-x-3">
                    <div className="w-10 h-10 bg-purple-100 rounded-lg flex items-center justify-center">
                      <Database className="h-5 w-5 text-purple-600" />
                    </div>
                    <div>
                      <p className="font-medium">{t("settings.integrations.stripe.name")}</p>
                      <p className="text-sm text-muted-foreground">{t("settings.integrations.stripe.description")}</p>
                    </div>
                  </div>
                  <Badge variant="default">{t("settings.integrations.status.connected")}</Badge>
                </div>

                <div className="flex items-center justify-between p-4 border rounded-lg">
                  <div className="flex items-center space-x-3">
                    <div className="w-10 h-10 bg-orange-100 rounded-lg flex items-center justify-center">
                      <Users className="h-5 w-5 text-orange-600" />
                    </div>
                    <div>
                      <p className="font-medium">{t("settings.integrations.slack.name")}</p>
                      <p className="text-sm text-muted-foreground">{t("settings.integrations.slack.description")}</p>
                    </div>
                  </div>
                  <Badge
                    variant="secondary"
                    className={integrationSettings.slack.connected ? "bg-green-100 text-green-800" : "bg-red-100 text-red-800"}
                  >
                    {integrationSettings.slack.connected
                      ? t("settings.integrations.status.connected")
                      : t("settings.integrations.status.disconnected")}
                  </Badge>
                </div>
              </div>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="system" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center">
                <Database className="mr-2 h-5 w-5" />
                {t("settings.system.title")}
              </CardTitle>
              <CardDescription>{t("settings.system.description")}</CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="grid gap-4 md:grid-cols-2">
                <div className="space-y-2">
                  <Label>{t("settings.system.appVersion")}</Label>
                  <p className="text-sm font-mono">v1.0.0</p>
                </div>
                <div className="space-y-2">
                  <Label>{t("settings.system.databaseVersion")}</Label>
                  <p className="text-sm font-mono">SQL Server 2022</p>
                </div>
                <div className="space-y-2">
                  <Label>{t("settings.system.cacheStatus")}</Label>
                  <Badge variant="default">{t("settings.system.redisConnected")}</Badge>
                </div>
                <div className="space-y-2">
                  <Label>{t("settings.system.messageQueue")}</Label>
                  <Badge variant="default">{t("settings.system.rabbitMqActive")}</Badge>
                </div>
              </div>

              <Separator />

              <div className="space-y-4">
                <h4 className="text-sm font-medium">{t("settings.system.health.title")}</h4>
                <div className="space-y-2">
                  <div className="flex items-center justify-between">
                    <span>{t("settings.system.health.apiGateway")}</span>
                    <Badge variant="default">{t("settings.system.health.status.healthy")}</Badge>
                  </div>
                  <div className="flex items-center justify-between">
                    <span>{t("settings.system.health.database")}</span>
                    <Badge variant="default">{t("settings.system.health.status.healthy")}</Badge>
                  </div>
                  <div className="flex items-center justify-between">
                    <span>{t("settings.system.health.vectorDatabase")}</span>
                    <Badge variant="default">{t("settings.system.health.status.healthy")}</Badge>
                  </div>
                  <div className="flex items-center justify-between">
                    <span>{t("settings.system.health.backgroundJobs")}</span>
                    <Badge variant="secondary">{t("settings.system.health.status.warning")}</Badge>
                  </div>
                </div>
              </div>
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>

      <div className="flex justify-end space-x-2">
        <Button variant="outline">{t("common.reset")}</Button>
        <Button onClick={handleSaveSettings} disabled={isSaving}>
          {isSaving ? t("common.saving") : t("common.save")}
        </Button>
      </div>
      {error && (
        <div className="fixed bottom-4 right-4 bg-red-100 border border-red-400 text-red-700 px-4 py-3 rounded">
          {t("common.error")}: {error}
          <button onClick={() => dispatch(clearError())} className="ml-2 text-red-500 hover:text-red-700">
            ×
          </button>
        </div>
      )}
      {isLoading && (
        <div className="fixed bottom-4 right-4 bg-blue-100 border border-blue-400 text-blue-700 px-4 py-3 rounded">
          {t("settings.loadingSettings")}
        </div>
      )}

      <div className="flex justify-end space-x-4 pt-6 border-t">
        <Button variant="outline" onClick={() => dispatch(clearError())}>
          {t("common.cancel")}
        </Button>
        <Button onClick={handleSaveSettings} disabled={isSaving}>
          {isSaving ? t("settings.saving") : t("settings.saveSettings")}
        </Button>
      </div>
    </div>
  );
};

export default SettingsPage;
