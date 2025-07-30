import React, { useEffect, useState } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { useTranslation } from 'react-i18next';
import { useForm } from 'react-hook-form';
import { AppDispatch, RootState } from '../../store/store';
import { fetchChatbotConfigs, updateChatbotConfig } from '../../store/slices/chatbotSlice';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../../components/ui/card';
import { Button } from '../../components/ui/button';
import { Input } from '../../components/ui/input';
import { Label } from '../../components/ui/label';
import { Textarea } from '../../components/ui/textarea';
import { Switch } from '../../components/ui/switch';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '../../components/ui/tabs';
import { Skeleton } from '../../components/ui/skeleton';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '../../components/ui/select';
import {
  Bot,
  Upload,
  Save,
  RefreshCw,
  MessageSquare,
  Brain,
  Settings,
  FileText,
  Zap,
  Globe,
} from 'lucide-react';

const ChatbotConfigPage = () => {
  const { t } = useTranslation();
  const dispatch = useDispatch<AppDispatch>();
  const { configs, isLoading } = useSelector((state: RootState) => state.chatbot);
  const config = configs && configs.length > 0 ? configs[0] : null;
  const [activeTab, setActiveTab] = useState('personality');

  const { register, handleSubmit, setValue, watch, formState: { errors } } = useForm({
    defaultValues: {
      name: '',
      personality: '',
      welcomeMessage: '',
      fallbackMessage: '',
      language: 'en',
      tone: 'friendly',
      responseLength: 'medium',
      enableEmojis: true,
      enableTypingIndicator: true,
      maxConversationLength: 50,
    },
  });

  useEffect(() => {
    dispatch(fetchChatbotConfigs());
  }, [dispatch]);

  useEffect(() => {
    if (config) {
      setValue('name', config.name || '');
      setValue('personality', config.description || '');
      setValue('welcomeMessage', config.name || '');
      setValue('fallbackMessage', config.name || '');
      setValue('language', 'en');
      setValue('tone', 'friendly');
      setValue('responseLength', 'medium');
      setValue('enableEmojis', true);
      setValue('enableTypingIndicator', true);
      setValue('maxConversationLength', 50);
    }
  }, [config, setValue]);

  const onSubmit = (data: any) => {
    dispatch(updateChatbotConfig(data));
  };

  const handleFileUpload = (event: React.ChangeEvent<HTMLInputElement>) => {
    const files = event.target.files;
    if (files && files.length > 0) {
      const formData = new FormData();
      Array.from(files).forEach(file => {
        formData.append('files', file);
      });
      console.log('Training data upload:', formData);
    }
  };

  const knowledgeBaseStats = {
    totalDocuments: 0,
    totalWords: 0,
    lastUpdated: null,
  };

  if (isLoading && !config) {
    return (
      <div className="space-y-6">
        <div className="flex items-center justify-between">
          <Skeleton className="h-8 w-[200px]" />
          <Skeleton className="h-10 w-[120px]" />
        </div>
        <Card>
          <CardHeader>
            <Skeleton className="h-6 w-[150px]" />
            <Skeleton className="h-4 w-[300px]" />
          </CardHeader>
          <CardContent>
            <div className="space-y-4">
              {[...Array(5)].map((_, i) => (
                <Skeleton key={i} className="h-10 w-full" />
              ))}
            </div>
          </CardContent>
        </Card>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">{t('chatbot.title')}</h1>
          <p className="text-muted-foreground">
            {t('chatbot.subtitle')}
          </p>
        </div>
        <div className="flex space-x-2">
          <Button variant="outline">
            <RefreshCw className="mr-2 h-4 w-4" />
            {t('chatbot.testBot')}
          </Button>
          <Button onClick={handleSubmit(onSubmit)}>
            <Save className="mr-2 h-4 w-4" />
            {t('common.save')}
          </Button>
        </div>
      </div>

      <Tabs value={activeTab} onValueChange={setActiveTab} className="space-y-4">
        <TabsList>
          <TabsTrigger value="personality">
            <Bot className="mr-2 h-4 w-4" />
            {t('chatbot.personality')}
          </TabsTrigger>
          <TabsTrigger value="knowledge">
            <Brain className="mr-2 h-4 w-4" />
            {t('chatbot.knowledgeBase')}
          </TabsTrigger>
          <TabsTrigger value="behavior">
            <Settings className="mr-2 h-4 w-4" />
            {t('chatbot.behavior')}
          </TabsTrigger>
          <TabsTrigger value="responses">
            <MessageSquare className="mr-2 h-4 w-4" />
            {t('chatbot.responses')}
          </TabsTrigger>
        </TabsList>

        <TabsContent value="personality" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle>{t('chatbot.botPersonality')}</CardTitle>
              <CardDescription>
                {t('chatbot.botPersonalityDesc')}
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="grid gap-4 md:grid-cols-2">
                <div className="space-y-2">
                  <Label htmlFor="name">{t('chatbot.botName')}</Label>
                  <Input
                    id="name"
                    {...register('name', { required: t('validation.required') })}
                    placeholder={t('chatbot.botNamePlaceholder')}
                  />
                  {errors.name && (
                    <p className="text-sm text-red-500">{errors.name.message}</p>
                  )}
                </div>

                <div className="space-y-2">
                  <Label htmlFor="language">{t('chatbot.language')}</Label>
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
              </div>

              <div className="space-y-2">
                <Label htmlFor="personality">{t('chatbot.personalityDescription')}</Label>
                <Textarea
                  id="personality"
                  {...register('personality')}
                  placeholder={t('chatbot.personalityPlaceholder')}
                  rows={4}
                />
              </div>

              <div className="grid gap-4 md:grid-cols-2">
                <div className="space-y-2">
                  <Label htmlFor="tone">{t('chatbot.tone')}</Label>
                  <Select value={watch('tone')} onValueChange={(value) => setValue('tone', value)}>
                    <SelectTrigger>
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="friendly">{t('chatbot.toneFriendly')}</SelectItem>
                      <SelectItem value="professional">{t('chatbot.toneProfessional')}</SelectItem>
                      <SelectItem value="casual">{t('chatbot.toneCasual')}</SelectItem>
                      <SelectItem value="formal">{t('chatbot.toneFormal')}</SelectItem>
                    </SelectContent>
                  </Select>
                </div>

                <div className="space-y-2">
                  <Label htmlFor="responseLength">{t('chatbot.responseLength')}</Label>
                  <Select value={watch('responseLength')} onValueChange={(value) => setValue('responseLength', value)}>
                    <SelectTrigger>
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="short">{t('chatbot.responseShort')}</SelectItem>
                      <SelectItem value="medium">{t('chatbot.responseMedium')}</SelectItem>
                      <SelectItem value="long">{t('chatbot.responseLong')}</SelectItem>
                    </SelectContent>
                  </Select>
                </div>
              </div>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="knowledge" className="space-y-4">
          <div className="grid gap-4 md:grid-cols-3">
            <Card>
              <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                <CardTitle className="text-sm font-medium">
                  {t('chatbot.totalDocuments')}
                </CardTitle>
                <FileText className="h-4 w-4 text-muted-foreground" />
              </CardHeader>
              <CardContent>
                <div className="text-2xl font-bold">{knowledgeBaseStats.totalDocuments}</div>
              </CardContent>
            </Card>

            <Card>
              <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                <CardTitle className="text-sm font-medium">
                  {t('chatbot.totalWords')}
                </CardTitle>
                <Zap className="h-4 w-4 text-muted-foreground" />
              </CardHeader>
              <CardContent>
                <div className="text-2xl font-bold">{knowledgeBaseStats.totalWords.toLocaleString()}</div>
              </CardContent>
            </Card>

            <Card>
              <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                <CardTitle className="text-sm font-medium">
                  {t('chatbot.lastUpdated')}
                </CardTitle>
                <Globe className="h-4 w-4 text-muted-foreground" />
              </CardHeader>
              <CardContent>
                <div className="text-sm">
                  {knowledgeBaseStats.lastUpdated 
                    ? new Date(knowledgeBaseStats.lastUpdated).toLocaleDateString()
                    : t('common.never')
                  }
                </div>
              </CardContent>
            </Card>
          </div>

          <Card>
            <CardHeader>
              <CardTitle>{t('chatbot.uploadTrainingData')}</CardTitle>
              <CardDescription>
                {t('chatbot.uploadTrainingDataDesc')}
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="border-2 border-dashed border-muted-foreground/25 rounded-lg p-6 text-center">
                <Upload className="mx-auto h-12 w-12 text-muted-foreground" />
                <div className="mt-4">
                  <Label htmlFor="file-upload" className="cursor-pointer">
                    <span className="text-sm font-medium text-primary hover:text-primary/80">
                      {t('chatbot.clickToUpload')}
                    </span>
                    <span className="text-sm text-muted-foreground"> {t('chatbot.orDragAndDrop')}</span>
                  </Label>
                  <Input
                    id="file-upload"
                    type="file"
                    multiple
                    accept=".txt,.pdf,.docx,.md"
                    onChange={handleFileUpload}
                    className="hidden"
                  />
                </div>
                <p className="text-xs text-muted-foreground mt-2">
                  {t('chatbot.supportedFormats')}
                </p>
              </div>

            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="behavior" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle>{t('chatbot.behaviorSettings')}</CardTitle>
              <CardDescription>
                {t('chatbot.behaviorSettingsDesc')}
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-6">
              <div className="flex items-center justify-between">
                <div className="space-y-0.5">
                  <Label>{t('chatbot.enableEmojis')}</Label>
                  <p className="text-sm text-muted-foreground">
                    {t('chatbot.enableEmojisDesc')}
                  </p>
                </div>
                <Switch
                  checked={watch('enableEmojis')}
                  onCheckedChange={(checked) => setValue('enableEmojis', checked)}
                />
              </div>

              <div className="flex items-center justify-between">
                <div className="space-y-0.5">
                  <Label>{t('chatbot.enableTypingIndicator')}</Label>
                  <p className="text-sm text-muted-foreground">
                    {t('chatbot.enableTypingIndicatorDesc')}
                  </p>
                </div>
                <Switch
                  checked={watch('enableTypingIndicator')}
                  onCheckedChange={(checked) => setValue('enableTypingIndicator', checked)}
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="maxConversationLength">{t('chatbot.maxConversationLength')}</Label>
                <Input
                  id="maxConversationLength"
                  type="number"
                  {...register('maxConversationLength', { min: 1, max: 100 })}
                />
                <p className="text-sm text-muted-foreground">
                  {t('chatbot.maxConversationLengthDesc')}
                </p>
              </div>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="responses" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle>{t('chatbot.customResponses')}</CardTitle>
              <CardDescription>
                {t('chatbot.customResponsesDesc')}
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="space-y-2">
                <Label htmlFor="welcomeMessage">{t('chatbot.welcomeMessage')}</Label>
                <Textarea
                  id="welcomeMessage"
                  {...register('welcomeMessage')}
                  placeholder={t('chatbot.welcomeMessagePlaceholder')}
                  rows={3}
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="fallbackMessage">{t('chatbot.fallbackMessage')}</Label>
                <Textarea
                  id="fallbackMessage"
                  {...register('fallbackMessage')}
                  placeholder={t('chatbot.fallbackMessagePlaceholder')}
                  rows={3}
                />
                <p className="text-sm text-muted-foreground">
                  {t('chatbot.fallbackMessageDesc')}
                </p>
              </div>
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>
    </div>
  );
};

export default ChatbotConfigPage;
