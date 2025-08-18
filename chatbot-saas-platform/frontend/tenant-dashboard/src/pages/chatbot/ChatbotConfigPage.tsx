import { useTranslation } from 'react-i18next';
import { useDispatch, useSelector } from 'react-redux';
import { useForm } from 'react-hook-form';
import { useState, useEffect } from 'react';
import { AppDispatch, RootState } from '../../store/store';
import { fetchChatbotConfigs, createChatbotConfig, updateChatbotConfig } from '../../store/slices/chatbotSlice';
import { ChatbotService } from '../../services/chatbotService';
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
} from 'lucide-react';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '../../components/ui/dialog';

const ChatbotConfigPage = () => {
  const { t } = useTranslation();
  const dispatch = useDispatch<AppDispatch>();
  const { configs, isLoading } = useSelector((state: RootState) => state.chatbot);
  const { user } = useSelector((state: RootState) => state.auth);
  const config = configs && configs.length > 0 ? configs[0] : null;
  const isCreateMode = !config;
  const [activeTab, setActiveTab] = useState('personality');
  const [isUploading, setIsUploading] = useState(false);
  const [uploadError, setUploadError] = useState<string | null>(null);
  const [uploadSuccess, setUploadSuccess] = useState<string | null>(null);
  const [isTestModalOpen, setIsTestModalOpen] = useState(false);
  const [widgetInstance, setWidgetInstance] = useState<{ init: (config: unknown) => void; destroy: () => void } | null>(null);

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

  const onSubmit = (data: Record<string, unknown>) => {
    if (isCreateMode) {
      const createRequest = {
        name: (data.name as string) || 'My Chatbot',
        description: (data.personality as string) || 'AI Assistant',
        isActive: true,
        appearance: {
          position: "bottom-right" as const,
          size: "medium" as const,
          primaryColor: "#007bff",
          secondaryColor: "#6c757d",
          borderRadius: "8px",
          animation: "slide" as const
        },
        behavior: {
          autoOpen: false,
          autoOpenDelay: 3000,
          showWelcomeMessage: true,
          welcomeMessage: (data.welcomeMessage as string) || "Hello! How can I help you today?",
          placeholderText: "Type your message...",
          maxFileSize: 10485760,
          allowedFileTypes: [".pdf", ".docx", ".txt", ".md"]
        },
        features: {
          fileUpload: true,
          voiceMessages: false,
          typing: true,
          readReceipts: true,
          agentHandoff: true,
          conversationRating: true,
          conversationTranscript: true
        },
        aiSettings: {
          model: "gpt-3.5-turbo",
          temperature: 0.7,
          maxTokens: 1000,
          systemPrompt: (data.personality as string) || "You are a helpful AI assistant.",
          fallbackMessage: (data.fallbackMessage as string) || "I'm sorry, I didn't understand that. Could you please rephrase your question?"
        },
        integrations: {
          knowledgeBase: true,
          crm: false,
          analytics: true
        }
      };
      dispatch(createChatbotConfig(createRequest));
    } else {
      if (!config?.id) {
        console.error('No chatbot configuration found. Please refresh the page.');
        return;
      }
      dispatch(updateChatbotConfig({ id: config.id, config: data }));
    }
  };

  const initializeWidget = () => {
    if (typeof window !== 'undefined' && (window as unknown as { ChatbotWidget: new () => { init: (config: unknown) => void; destroy: () => void } }).ChatbotWidget && user?.tenantId) {
      if (widgetInstance) {
        widgetInstance.destroy();
      }

      const widget = new (window as unknown as { ChatbotWidget: new () => { init: (config: unknown) => void; destroy: () => void } }).ChatbotWidget();
      const formData = watch();
      
      widget.init({
        tenantId: user.tenantId,
        apiUrl: 'http://localhost:8000',
        websocketUrl: 'ws://localhost:8000/chatHub',
        authToken: localStorage.getItem('token'),
        theme: {
          primaryColor: '#007bff',
          position: 'bottom-right',
          size: 'medium',
          animation: 'slide'
        },
        branding: {
          companyName: formData.name || 'Test Chatbot',
          welcomeMessage: formData.welcomeMessage || 'Hello! How can I help you today?',
          placeholderText: 'Type your message...'
        },
        language: formData.language || 'en',
        features: {
          fileUpload: true,
          typing: formData.enableTypingIndicator,
          readReceipts: true,
          agentHandoff: true,
          conversationRating: true,
          conversationTranscript: true
        },
        behavior: {
          autoOpen: true,
          showWelcomeMessage: true,
          persistConversation: false,
          maxFileSize: 10485760,
          allowedFileTypes: ['.pdf', '.docx', '.txt', '.md']
        }
      });
      
      setWidgetInstance(widget);
      console.log('✅ TestBot widget initialized');
    } else {
      console.error('❌ ChatbotWidget not available or missing tenant context');
    }
  };

  const destroyWidget = () => {
    if (widgetInstance) {
      widgetInstance.destroy();
      setWidgetInstance(null);
      console.log('✅ TestBot widget destroyed');
    }
  };

  const handleFileUpload = async (event: React.ChangeEvent<HTMLInputElement>) => {
    const files = event.target.files;
    if (!files || files.length === 0) return;
    
    if (!config?.id) {
      setUploadError('No chatbot configuration found. Please save your configuration first.');
      return;
    }

    setUploadError(null);
    setUploadSuccess(null);
    setIsUploading(true);

    try {
      const uploadPromises = Array.from(files).map(async (file: File) => {
        const allowedTypes = ['.pdf', '.docx', '.txt', '.md'];
        const fileExtension = '.' + file.name.split('.').pop()?.toLowerCase();
        if (!allowedTypes.includes(fileExtension)) {
          throw new Error(`File type ${fileExtension} is not supported. Allowed types: ${allowedTypes.join(', ')}`);
        }

        if (file.size > 10 * 1024 * 1024) {
          throw new Error(`File ${file.name} exceeds 10MB size limit`);
        }

        return ChatbotService.uploadKnowledgeBaseDocument(config.id, file);
      });

      await Promise.all(uploadPromises);
      setUploadSuccess(`Successfully uploaded ${files.length} file(s) to knowledge base`);
      
      setTimeout(() => {
        setUploadSuccess(null);
      }, 5000);
    } catch (error) {
      console.error('Upload error:', error);
      setUploadError(error instanceof Error ? error.message : 'Failed to upload files');
    } finally {
      setIsUploading(false);
    }
  };

  const knowledgeBaseStats = {
    totalDocuments: 0,
    totalSize: '0 MB'
  };

  if (isLoading) {
    return (
      <div className="space-y-6">
        <div className="flex items-center justify-between">
          <div>
            <Skeleton className="h-8 w-64 mb-2" />
            <Skeleton className="h-4 w-96" />
          </div>
          <Skeleton className="h-10 w-24" />
        </div>
        <div className="space-y-4">
          <Skeleton className="h-64 w-full" />
          <Skeleton className="h-64 w-full" />
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">
            {isCreateMode ? t('chatbot.createTitle') || 'Create Chatbot' : t('chatbot.title')}
          </h1>
          <p className="text-muted-foreground">
            {isCreateMode 
              ? t('chatbot.createSubtitle') || 'Set up your first AI chatbot configuration'
              : t('chatbot.subtitle')
            }
          </p>
        </div>
        <div className="flex space-x-2">
          {!isCreateMode && (
            <Dialog open={isTestModalOpen} onOpenChange={(open) => {
              setIsTestModalOpen(open);
              if (open) {
                setTimeout(initializeWidget, 100);
              } else {
                destroyWidget();
              }
            }}>
              <DialogTrigger asChild>
                <Button variant="outline">
                  <RefreshCw className="mr-2 h-4 w-4" />
                  {t('chatbot.testBot')}
                </Button>
              </DialogTrigger>
              <DialogContent className="max-w-4xl max-h-[90vh] p-0 overflow-hidden">
                <DialogHeader className="p-6 pb-0">
                  <DialogTitle>Test Chatbot</DialogTitle>
                  <DialogDescription>
                    Testing your chatbot with the actual widget. This provides an authentic experience of how users will interact with your chatbot.
                  </DialogDescription>
                </DialogHeader>
                
                <div className="p-6 pt-4">
                  <div className="bg-gray-50 dark:bg-gray-900 rounded-lg p-4 min-h-[500px] relative">
                    <div className="text-center text-gray-500 text-sm">
                      <MessageSquare className="mx-auto h-8 w-8 mb-2 opacity-50" />
                      <p>The chatbot widget will appear here when initialized.</p>
                      <p className="text-xs mt-1">Look for the chat button in the bottom-right corner of this area.</p>
                    </div>
                  </div>
                  
                  <div className="mt-4 text-xs text-gray-500 text-center">
                    <p>💡 This uses your current form settings and provides the same experience as your website visitors.</p>
                  </div>
                </div>
              </DialogContent>
            </Dialog>
          )}
          <Button onClick={handleSubmit(onSubmit)}>
            <Save className="mr-2 h-4 w-4" />
            {isCreateMode ? t('common.create') || 'Create' : t('common.save')}
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
              <div className="space-y-2">
                <Label htmlFor="name">{t('chatbot.name')}</Label>
                <Input
                  id="name"
                  {...register('name', { required: t('validation.required') })}
                  placeholder={t('chatbot.namePlaceholder')}
                />
                {errors.name && (
                  <p className="text-sm text-destructive">{errors.name.message as string}</p>
                )}
              </div>

              <div className="space-y-2">
                <Label htmlFor="personality">{t('chatbot.personality')}</Label>
                <Textarea
                  id="personality"
                  {...register('personality')}
                  placeholder={t('chatbot.personalityPlaceholder')}
                  rows={4}
                />
                <p className="text-sm text-muted-foreground">
                  {t('chatbot.personalityDesc')}
                </p>
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div className="space-y-2">
                  <Label htmlFor="language">{t('chatbot.language')}</Label>
                  <Select defaultValue="en">
                    <SelectTrigger>
                      <SelectValue placeholder={t('chatbot.selectLanguage')} />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="en">English</SelectItem>
                      <SelectItem value="es">Spanish</SelectItem>
                      <SelectItem value="fr">French</SelectItem>
                      <SelectItem value="de">German</SelectItem>
                      <SelectItem value="it">Italian</SelectItem>
                    </SelectContent>
                  </Select>
                </div>

                <div className="space-y-2">
                  <Label htmlFor="tone">{t('chatbot.tone')}</Label>
                  <Select defaultValue="friendly">
                    <SelectTrigger>
                      <SelectValue placeholder={t('chatbot.selectTone')} />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="professional">Professional</SelectItem>
                      <SelectItem value="friendly">Friendly</SelectItem>
                      <SelectItem value="casual">Casual</SelectItem>
                      <SelectItem value="formal">Formal</SelectItem>
                    </SelectContent>
                  </Select>
                </div>
              </div>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="knowledge" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle>{t('chatbot.knowledgeBase')}</CardTitle>
              <CardDescription>
                {t('chatbot.knowledgeBaseDesc')}
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="border-2 border-dashed border-muted-foreground/25 rounded-lg p-6">
                <div className="text-center">
                  <Upload className="mx-auto h-12 w-12 text-muted-foreground/50" />
                  <div className="mt-4">
                    <Label htmlFor="file-upload" className="cursor-pointer">
                      <span className="text-sm font-medium text-primary hover:text-primary/80">
                        {t('chatbot.uploadFiles')}
                      </span>
                      <Input
                        id="file-upload"
                        type="file"
                        multiple
                        accept=".pdf,.docx,.txt,.md"
                        onChange={handleFileUpload}
                        className="sr-only"
                        disabled={isUploading}
                      />
                    </Label>
                    <p className="text-xs text-muted-foreground mt-1">
                      {t('chatbot.supportedFormats')}
                    </p>
                  </div>
                </div>
              </div>

              {uploadError && (
                <div className="p-3 text-sm text-destructive bg-destructive/10 border border-destructive/20 rounded-md">
                  {uploadError}
                </div>
              )}

              {uploadSuccess && (
                <div className="p-3 text-sm text-green-600 bg-green-50 border border-green-200 rounded-md">
                  {uploadSuccess}
                </div>
              )}

              {isUploading && (
                <div className="p-3 text-sm text-blue-600 bg-blue-50 border border-blue-200 rounded-md">
                  {t('chatbot.uploading')}...
                </div>
              )}

              <div className="grid grid-cols-2 gap-4 pt-4">
                <div className="text-center">
                  <div className="text-2xl font-bold">{knowledgeBaseStats.totalDocuments}</div>
                  <div className="text-sm text-muted-foreground">{t('chatbot.totalDocuments')}</div>
                </div>
                <div className="text-center">
                  <div className="text-2xl font-bold">{knowledgeBaseStats.totalSize}</div>
                  <div className="text-sm text-muted-foreground">{t('chatbot.totalSize')}</div>
                </div>
              </div>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="behavior" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle>{t('chatbot.behavior')}</CardTitle>
              <CardDescription>
                {t('chatbot.behaviorDesc')}
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
                <Switch {...register('enableEmojis')} />
              </div>

              <div className="flex items-center justify-between">
                <div className="space-y-0.5">
                  <Label>{t('chatbot.enableTypingIndicator')}</Label>
                  <p className="text-sm text-muted-foreground">
                    {t('chatbot.enableTypingIndicatorDesc')}
                  </p>
                </div>
                <Switch {...register('enableTypingIndicator')} />
              </div>

              <div className="space-y-2">
                <Label htmlFor="responseLength">{t('chatbot.responseLength')}</Label>
                <Select defaultValue="medium">
                  <SelectTrigger>
                    <SelectValue placeholder={t('chatbot.selectResponseLength')} />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="short">Short</SelectItem>
                    <SelectItem value="medium">Medium</SelectItem>
                    <SelectItem value="long">Long</SelectItem>
                  </SelectContent>
                </Select>
              </div>

              <div className="space-y-2">
                <Label htmlFor="maxConversationLength">{t('chatbot.maxConversationLength')}</Label>
                <Input
                  id="maxConversationLength"
                  type="number"
                  {...register('maxConversationLength')}
                  placeholder="50"
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
              <CardTitle>{t('chatbot.welcomeMessage')}</CardTitle>
              <CardDescription>
                {t('chatbot.welcomeMessageDesc')}
              </CardDescription>
            </CardHeader>
            <CardContent>
              <div className="space-y-2">
                <Label htmlFor="welcomeMessage">{t('chatbot.welcomeMessage')}</Label>
                <Textarea
                  id="welcomeMessage"
                  {...register('welcomeMessage')}
                  placeholder={t('chatbot.welcomeMessagePlaceholder')}
                  rows={3}
                />
                <p className="text-sm text-muted-foreground">
                  {t('chatbot.welcomeMessageDesc')}
                </p>
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>{t('chatbot.fallbackMessage')}</CardTitle>
              <CardDescription>
                {t('chatbot.fallbackMessageDesc')}
              </CardDescription>
            </CardHeader>
            <CardContent>
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
