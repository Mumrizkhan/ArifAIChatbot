import React, { useEffect, useState } from "react";
import { useDispatch, useSelector } from "react-redux";
import { useTranslation } from "react-i18next";
import { useForm } from "react-hook-form";
import { AppDispatch, RootState } from "../../store/store";
import { fetchAgentProfile, updateAgentProfile } from "../../store/slices/agentSlice";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "../../components/ui/card";
import { Button } from "../../components/ui/button";
import { Input } from "../../components/ui/input";
import { Label } from "../../components/ui/label";
import { Textarea } from "../../components/ui/textarea";
import { Switch } from "../../components/ui/switch";
import { Badge } from "../../components/ui/badge";
import { Avatar, AvatarFallback, AvatarImage } from "../../components/ui/avatar";
import { Skeleton } from "../../components/ui/skeleton";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "../../components/ui/select";
import { User, Save, Upload, Star, Clock, MessageSquare, Award, TrendingUp, Calendar, MapPin, Phone, Mail, Globe } from "lucide-react";

const ProfilePage = () => {
  const { t } = useTranslation();
  const dispatch = useDispatch<AppDispatch>();
  const { currentAgent, stats: agentStats, isLoading } = useSelector((state: RootState) => state.agent);
  const { user, token } = useSelector((state: RootState) => state.auth); // Fetch user from auth slice
  const currentAgentId = user?.id; // Extract the agent's ID
  console.log(currentAgent, "currentAgent");
  console.log(currentAgentId, "currentAgent");

  const {
    register,
    handleSubmit,
    setValue,
    watch,
    formState: { errors },
  } = useForm({
    defaultValues: {
      name: "",
      email: "",
      phone: "",
      avatar: "",
      bio: "",
      location: "",
      timezone: "UTC",
      language: "en",
      skills: "",
      specializations: "",
      availability: "available",
      workingHours: {
        start: "09:00",
        end: "17:00",
        timezone: "UTC",
      },
      enablePublicProfile: true,
      enableSkillsDisplay: true,
      enableStatsDisplay: true,
    },
  });

  useEffect(() => {
    if (currentAgentId) {
      dispatch(fetchAgentProfile(currentAgentId));
    }
  }, [dispatch, currentAgentId]);

  useEffect(() => {
    if (currentAgent) {
      setValue("name", currentAgent.name || "");
      setValue("email", currentAgent.email || "");
      setValue("phone", currentAgent.phone || "");
      setValue("avatar", currentAgent.avatar || "");
      setValue("bio", currentAgent.bio || "");
      setValue("location", currentAgent.location || "");
      setValue("timezone", currentAgent.timezone || "UTC");
      setValue("language", currentAgent.language || "en");
      setValue("skills", currentAgent.skills?.join(", ") || "");
      setValue("specializations", currentAgent.specializations?.join(", ") || "");
      setValue("availability", currentAgent.status || "available");
    }
  }, [currentAgent, setValue]);

  const onSubmit = (data: any) => {
    const profileData = {
      ...data,
      skills: data.skills
        .split(",")
        .map((s: string) => s.trim())
        .filter(Boolean),
      specializations: data.specializations
        .split(",")
        .map((s: string) => s.trim())
        .filter(Boolean),
    };
    if (currentAgentId) {
      dispatch(updateAgentProfile({ id: currentAgentId, profileData }));
    }
  };

  const handleAvatarUpload = (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (file) {
      const reader = new FileReader();
      reader.onload = (e) => {
        setValue("avatar", e.target?.result as string);
      };
      reader.readAsDataURL(file);
    }
  };

  // Replace hardcoded timezone labels
  const timezones = [
    { value: "UTC", label: t("profile.timezones.utc") },
    { value: "America/New_York", label: t("profile.timezones.easternTime") },
    { value: "America/Chicago", label: t("profile.timezones.centralTime") },
    { value: "America/Denver", label: t("profile.timezones.mountainTime") },
    { value: "America/Los_Angeles", label: t("profile.timezones.pacificTime") },
    { value: "Europe/London", label: t("profile.timezones.london") },
    { value: "Europe/Paris", label: t("profile.timezones.paris") },
    { value: "Asia/Tokyo", label: t("profile.timezones.tokyo") },
    { value: "Asia/Dubai", label: t("profile.timezones.dubai") },
  ];

  if (isLoading && !currentAgent) {
    return (
      <div className="space-y-6">
        <div className="flex items-center justify-between">
          <Skeleton className="h-8 w-[200px]" />
          <Skeleton className="h-10 w-[120px]" />
        </div>
        <div className="grid gap-6 md:grid-cols-2">
          <Card>
            <CardHeader>
              <Skeleton className="h-6 w-[150px]" />
              <Skeleton className="h-4 w-[300px]" />
            </CardHeader>
            <CardContent>
              <div className="space-y-4">
                <Skeleton className="h-20 w-20 rounded-full" />
                <Skeleton className="h-10 w-full" />
                <Skeleton className="h-10 w-full" />
              </div>
            </CardContent>
          </Card>
          <Card>
            <CardHeader>
              <Skeleton className="h-6 w-[150px]" />
            </CardHeader>
            <CardContent>
              <div className="space-y-3">
                {[...Array(4)].map((_, i) => (
                  <Skeleton key={i} className="h-12 w-full" />
                ))}
              </div>
            </CardContent>
          </Card>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">{t("profile.title")}</h1>
          <p className="text-muted-foreground">{t("profile.subtitle")}</p>
        </div>
        <Button onClick={handleSubmit(onSubmit)}>
          <Save className="mr-2 h-4 w-4" />
          {t("common.save")}
        </Button>
      </div>

      <div className="grid gap-6 md:grid-cols-2">
        <div className="space-y-6">
          <Card>
            <CardHeader>
              <CardTitle>{t("profile.basicInformation")}</CardTitle>
              <CardDescription>{t("profile.basicInformationDesc")}</CardDescription>
            </CardHeader>
            <CardContent className="space-y-6">
              <div className="flex items-center space-x-4">
                <Avatar className="h-20 w-20">
                  <AvatarImage src={watch("avatar")} />
                  <AvatarFallback>
                    {(watch("name") || "")
                      .split(" ")
                      .map((n: string) => n[0])
                      .join("") || "A"}
                  </AvatarFallback>
                </Avatar>
                <div>
                  <Label htmlFor="avatar-upload" className="cursor-pointer">
                    <Button variant="outline" asChild>
                      <span>
                        <Upload className="mr-2 h-4 w-4" />
                        {t("profile.uploadAvatar")}
                      </span>
                    </Button>
                  </Label>
                  <Input id="avatar-upload" type="file" accept="image/*" onChange={handleAvatarUpload} className="hidden" />
                  <p className="text-xs text-muted-foreground mt-2">{t("profile.avatarFormats")}</p>
                </div>
              </div>

              <div className="grid gap-4 md:grid-cols-2">
                <div className="space-y-2">
                  <Label htmlFor="name">{t("profile.fullName")}</Label>
                  <Input
                    id="name"
                    {...register("name", { required: t("profile.validation.required") })}
                    placeholder={t("profile.fullNamePlaceholder")}
                  />
                  {errors.name && <p className="text-sm text-red-500">{errors.name.message}</p>}
                </div>

                <div className="space-y-2">
                  <Label htmlFor="email">{t("profile.email")}</Label>
                  <Input
                    id="email"
                    type="email"
                    {...register("email", { required: t("profile.validation.required") })}
                    placeholder={t("profile.emailPlaceholder")}
                  />
                  {errors.email && <p className="text-sm text-red-500">{errors.email.message}</p>}
                </div>
              </div>

              <div className="grid gap-4 md:grid-cols-2">
                <div className="space-y-2">
                  <Label htmlFor="phone">{t("profile.phone")}</Label>
                  <Input id="phone" {...register("phone")} placeholder={t("profile.phonePlaceholder")} />
                </div>

                <div className="space-y-2">
                  <Label htmlFor="location">{t("profile.location")}</Label>
                  <Input id="location" {...register("location")} placeholder={t("profile.locationPlaceholder")} />
                </div>
              </div>

              <div className="space-y-2">
                <Label htmlFor="bio">{t("profile.bio")}</Label>
                <Textarea id="bio" {...register("bio")} placeholder={t("profile.bioPlaceholder")} rows={3} />
              </div>

              <div className="grid gap-4 md:grid-cols-2">
                <div className="space-y-2">
                  <Label htmlFor="timezone">{t("profile.timezone")}</Label>
                  <Select value={watch("timezone")} onValueChange={(value) => setValue("timezone", value)}>
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

                <div className="space-y-2">
                  <Label htmlFor="language">{t("profile.language")}</Label>
                  <Select value={watch("language")} onValueChange={(value) => setValue("language", value)}>
                    <SelectTrigger>
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="en">{t("profile.languages.english")}</SelectItem>
                      <SelectItem value="ar">{t("profile.languages.arabic")}</SelectItem>
                    </SelectContent>
                  </Select>
                </div>
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>{t("profile.professionalInformation")}</CardTitle>
              <CardDescription>{t("profile.professionalInformationDesc")}</CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="space-y-2">
                <Label htmlFor="skills">{t("profile.skills")}</Label>
                <Input id="skills" {...register("skills")} placeholder={t("profile.skillsPlaceholder")} />
                <p className="text-xs text-muted-foreground">{t("profile.skillsDesc")}</p>
              </div>

              <div className="space-y-2">
                <Label htmlFor="specializations">{t("profile.specializations")}</Label>
                <Input id="specializations" {...register("specializations")} placeholder={t("profile.specializationsPlaceholder")} />
                <p className="text-xs text-muted-foreground">{t("profile.specializationsDesc")}</p>
              </div>

              <div className="space-y-2">
                <Label htmlFor="availability">{t("profile.availability")}</Label>
                <Select value={watch("availability")} onValueChange={(value) => setValue("availability", value)}>
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="available">{t("profile.statuses.available")}</SelectItem>
                    <SelectItem value="busy">{t("profile.statuses.busy")}</SelectItem>
                    <SelectItem value="away">{t("profile.statuses.away")}</SelectItem>
                    <SelectItem value="offline">{t("profile.statuses.offline")}</SelectItem>
                  </SelectContent>
                </Select>
              </div>

              <div className="space-y-3">
                <Label>{t("profile.workingHours")}</Label>
                <div className="grid gap-4 md:grid-cols-2">
                  <div className="space-y-2">
                    <Label htmlFor="workingHoursStart">{t("profile.startTime")}</Label>
                    <Input id="workingHoursStart" type="time" {...register("workingHours.start")} />
                  </div>

                  <div className="space-y-2">
                    <Label htmlFor="workingHoursEnd">{t("profile.endTime")}</Label>
                    <Input id="workingHoursEnd" type="time" {...register("workingHours.end")} />
                  </div>
                </div>
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>{t("profile.privacySettings")}</CardTitle>
              <CardDescription>{t("profile.privacySettingsDesc")}</CardDescription>
            </CardHeader>
            <CardContent className="space-y-6">
              <div className="flex items-center justify-between">
                <div className="space-y-0.5">
                  <Label>{t("profile.enablePublicProfile")}</Label>
                  <p className="text-sm text-muted-foreground">{t("profile.enablePublicProfileDesc")}</p>
                </div>
                <Switch checked={watch("enablePublicProfile")} onCheckedChange={(checked) => setValue("enablePublicProfile", checked)} />
              </div>

              <div className="flex items-center justify-between">
                <div className="space-y-0.5">
                  <Label>{t("profile.enableSkillsDisplay")}</Label>
                  <p className="text-sm text-muted-foreground">{t("profile.enableSkillsDisplayDesc")}</p>
                </div>
                <Switch checked={watch("enableSkillsDisplay")} onCheckedChange={(checked) => setValue("enableSkillsDisplay", checked)} />
              </div>

              <div className="flex items-center justify-between">
                <div className="space-y-0.5">
                  <Label>{t("profile.enableStatsDisplay")}</Label>
                  <p className="text-sm text-muted-foreground">{t("profile.enableStatsDisplayDesc")}</p>
                </div>
                <Switch checked={watch("enableStatsDisplay")} onCheckedChange={(checked) => setValue("enableStatsDisplay", checked)} />
              </div>
            </CardContent>
          </Card>
        </div>

        <div className="space-y-6">
          <Card>
            <CardHeader>
              <CardTitle>{t("profile.performanceOverview")}</CardTitle>
              <CardDescription>{t("profile.performanceOverviewDesc")}</CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="grid gap-4 md:grid-cols-2">
                <div className="flex items-center space-x-3">
                  <div className="p-2 bg-blue-100 rounded-lg">
                    <MessageSquare className="h-5 w-5 text-blue-600" />
                  </div>
                  <div>
                    <p className="text-sm font-medium">{t("profile.totalConversations")}</p>
                    <p className="text-2xl font-bold">{agentStats?.totalConversations || 0}</p>
                  </div>
                </div>

                <div className="flex items-center space-x-3">
                  <div className="p-2 bg-green-100 rounded-lg">
                    <Star className="h-5 w-5 text-green-600" />
                  </div>
                  <div>
                    <p className="text-sm font-medium">{t("profile.avgRating")}</p>
                    <p className="text-2xl font-bold">{agentStats?.averageRating || 0}</p>
                  </div>
                </div>

                <div className="flex items-center space-x-3">
                  <div className="p-2 bg-yellow-100 rounded-lg">
                    <Clock className="h-5 w-5 text-yellow-600" />
                  </div>
                  <div>
                    <p className="text-sm font-medium">{t("profile.avgResponseTime")}</p>
                    <p className="text-2xl font-bold">{agentStats?.averageResponseTime || 0}m</p>
                  </div>
                </div>

                <div className="flex items-center space-x-3">
                  <div className="p-2 bg-purple-100 rounded-lg">
                    <Award className="h-5 w-5 text-purple-600" />
                  </div>
                  <div>
                    <p className="text-sm font-medium">{t("profile.resolutionRate")}</p>
                    <p className="text-2xl font-bold">{agentStats?.resolutionRate || 0}%</p>
                  </div>
                </div>
              </div>

              <div className="pt-4 border-t">
                <div className="flex items-center justify-between mb-2">
                  <span className="text-sm font-medium">{t("profile.thisMonth")}</span>
                  <Badge variant="outline">
                    <TrendingUp className="mr-1 h-3 w-3" />
                    {t("profile.performanceTrend")}
                  </Badge>
                </div>
                <div className="space-y-2">
                  <div className="flex justify-between text-sm">
                    <span>{t("profile.conversations")}</span>
                    <span>{agentStats?.totalConversations || 0}</span>
                  </div>
                  <div className="flex justify-between text-sm">
                    <span>{t("profile.satisfaction")}</span>
                    <span>{agentStats?.monthlySatisfaction || 0}</span>
                  </div>
                  <div className="flex justify-between text-sm">
                    <span>{t("profile.responseTime")}</span>
                    <span>{agentStats?.monthlyResponseTime || 0}m</span>
                  </div>
                </div>
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>{t("profile.recentAchievements")}</CardTitle>
              <CardDescription>{t("profile.recentAchievementsDesc")}</CardDescription>
            </CardHeader>
            <CardContent className="space-y-3">
              <div className="flex items-center space-x-3 p-3 border rounded-lg">
                <div className="p-2 bg-gold-100 rounded-lg">
                  <Award className="h-5 w-5 text-yellow-600" />
                </div>
                <div>
                  <p className="font-medium">{t("profile.topPerformer")}</p>
                  <p className="text-sm text-muted-foreground">{t("profile.topPerformerDesc")}</p>
                </div>
                <Badge variant="secondary">{t("profile.badges.new")}</Badge>
              </div>

              <div className="flex items-center space-x-3 p-3 border rounded-lg">
                <div className="p-2 bg-blue-100 rounded-lg">
                  <Star className="h-5 w-5 text-blue-600" />
                </div>
                <div>
                  <p className="font-medium">{t("profile.customerFavorite")}</p>
                  <p className="text-sm text-muted-foreground">{t("profile.customerFavoriteDesc")}</p>
                </div>
                <Badge variant="outline">{t("profile.badges.thisWeek")}</Badge>
              </div>

              <div className="flex items-center space-x-3 p-3 border rounded-lg">
                <div className="p-2 bg-green-100 rounded-lg">
                  <Clock className="h-5 w-5 text-green-600" />
                </div>
                <div>
                  <p className="font-medium">{t("profile.quickResponder")}</p>
                  <p className="text-sm text-muted-foreground">{t("profile.quickResponderDesc")}</p>
                </div>
                <Badge variant="outline">{t("profile.badges.lastMonth")}</Badge>
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>{t("profile.contactInformation")}</CardTitle>
              <CardDescription>{t("profile.contactInformationDesc")}</CardDescription>
            </CardHeader>
            <CardContent className="space-y-3">
              <div className="flex items-center space-x-3">
                <Mail className="h-4 w-4 text-muted-foreground" />
                <span className="text-sm">{watch("email") || t("profile.notProvided")}</span>
              </div>
              <div className="flex items-center space-x-3">
                <Phone className="h-4 w-4 text-muted-foreground" />
                <span className="text-sm">{watch("phone") || t("profile.notProvided")}</span>
              </div>
              <div className="flex items-center space-x-3">
                <MapPin className="h-4 w-4 text-muted-foreground" />
                <span className="text-sm">{watch("location") || t("profile.notProvided")}</span>
              </div>
              <div className="flex items-center space-x-3">
                <Globe className="h-4 w-4 text-muted-foreground" />
                <span className="text-sm">{watch("timezone")}</span>
              </div>
              <div className="flex items-center space-x-3">
                <Calendar className="h-4 w-4 text-muted-foreground" />
                <span className="text-sm">
                  {watch("workingHours.start")} - {watch("workingHours.end")}
                </span>
              </div>
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  );
};

export default ProfilePage;
