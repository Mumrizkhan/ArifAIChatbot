import React, { useEffect, useState } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { useTranslation } from 'react-i18next';
import { useForm } from 'react-hook-form';
import { AppDispatch, RootState } from '../../store/store';
import { fetchChatbotConfigs, updateChatbotConfig, createChatbotConfig } from '../../store/slices/chatbotSlice';
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
  FileText,
  Zap,
  Globe,
  Send,
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
  const config = configs && configs.length > 0 ? configs[0] : null;
  const isCreateMode = !config;
  const [activeTab, setActiveTab] = useState('personality');
  const [isUploading, setIsUploading] = useState(false);
  const [uploadError, setUploadError] = useState<string | null>(null);
  const [uploadSuccess, setUploadSuccess] = useState<string | null>(null);
  const [isTestModalOpen, setIsTestModalOpen] = useState(false);
  const [testMessages, setTestMessages] = useState<Array<{id: string, content: string, sender: 'user' | 'bot', timestamp: Date}>>([]);
  const [testInput, setTestInput] = useState('');
  const [isTesting, setIsTesting] = useState(false);

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

  const handleTestChatbot = async (message: string) => {
    if (!message.trim()) return;

    setIsTesting(true);
    
    const userMessage = {
      id: `user_${Date.now()}`,
      content: message,
      sender: 'user' as const,
      timestamp: new Date()
    };
    setTestMessages(prev => [...prev, userMessage]);
    setTestInput('');

    try {
      const response = await ChatbotService.testChatbot(message);
      
      if (response.success) {
        const botMessage = {
          id: `bot_${Date.now()}`,
          content: response.data.botMessage.content,
          sender: 'bot' as const,
          timestamp: new Date()
        };
        setTestMessages(prev => [...prev, botMessage]);
      } else {
        const errorMessage = {
          id: `bot_${Date.now()}`,
          content: 'Sorry, I encountered an error while processing your message. Please try again.',
          sender: 'bot' as const,
          timestamp: new Date()
        };
        setTestMessages(prev => [...prev, errorMessage]);
      }
    } catch (error) {
      console.error('Test chatbot error:', error);
      const errorMessage = {
        id: `bot_${Date.now()}`,
        content: error instanceof Error && error.message.includes('conversation') 
          ? 'Failed to create test conversation. Please try again.'
          : 'Sorry, I encountered an error while processing your message. Please try again.',
        sender: 'bot' as const,
        timestamp: new Date()
      };
      setTestMessages(prev => [...prev, errorMessage]);
    }finally {
      setIsTesting(false);
    }
  };

  const handleTestSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (testInput.trim() && !isTesting) {
      handleTestChatbot(testInput);
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
      
      event.target.value = '';
    } catch (error: unknown) {
      setUploadError((error as Error).message || 'Failed to upload files');
    } finally {
      setIsUploading(false);
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
            <Dialog open={isTestModalOpen} onOpenChange={setIsTestModalOpen}>
              <DialogTrigger asChild>
                <Button variant="outline">
                  <RefreshCw className="mr-2 h-4 w-4" />
                  {t('chatbot.testBot')}
                </Button>
              </DialogTrigger>
              <DialogContent className="max-w-2xl max-h-[80vh] flex flex-col">
                <DialogHeader>
                  <DialogTitle>Test Chatbot</DialogTitle>
                  <DialogDescription>
                    Test your chatbot configuration with live messages. This uses your current form settings.
                  </DialogDescription>
                </DialogHeader>
                
                <div className="flex-1 flex flex-col min-h-0">
                  <div className="flex-1 overflow-y-auto border rounded-lg p-4 mb-4 bg-gray-50 dark:bg-gray-900 min-h-[300px]">
                    {testMessages.length === 0 ? (
                      <div className="flex items-center justify-center h-full text-gray-500">
                        <div className="text-center">
                          <MessageSquare className="mx-auto h-12 w-12 mb-2 opacity-50" />
                          <p>Start a conversation to test your chatbot</p>
                        </div>
                      </div>
                    ) : (
                      <div className="space-y-4">
                        {testMessages.map((message) => (
                          <div
                            key={message.id}
                            className={`flex ${message.sender === 'user' ? 'justify-end' : 'justify-start'}`}
                          >
                            <div
                              className={`max-w-[80%] rounded-lg px-4 py-2 ${
                                message.sender === 'user'
                                  ? 'bg-blue-500 text-white'
                                  : 'bg-white dark:bg-gray-800 border'
                              }`}
                            >
                              <p className="text-sm">{message.content}</p>
                              <p className={`text-xs mt-1 opacity-70 ${
                                message.sender === 'user' ? 'text-blue-100' : 'text-gray-500'
                              }`}>
                                {message.timestamp.toLocaleTimeString()}
                              </p>
                            </div>
                          </div>
                        ))}
                        {isTesting && (
                          <div className="flex justify-start">
                            <div className="bg-white dark:bg-gray-800 border rounded-lg px-4 py-2">
                              <div className="flex items-center space-x-1">
                                <div className="w-2 h-2 bg-gray-400 rounded-full animate-bounce"></div>
                                <div className="w-2 h-2 bg-gray-400 rounded-full animate-bounce" style={{animationDelay: '0.1s'}}></div>
                                <div className="w-2 h-2 bg-gray-400 rounded-full animate-bounce" style={{animationDelay: '0.2s'}}></div>
                              </div>
                            </div>
                          </div>
                        )}
                      </div>
                    )}
                  </div>
                  
                  <form onSubmit={handleTestSubmit} className="flex space-x-2">
                    <Input
                      value={testInput}
                      onChange={(e) => setTestInput(e.target.value)}
                      placeholder="Type your message..."
                      disabled={isTesting}
                      className="flex-1"
                    />
                    <Button type="submit" disabled={isTesting || !testInput.trim()}>
                      <Send className="h-4 w-4" />
                    </Button>
                  </form>
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
                {isCreateMode 
                  ? 'Save your chatbot configuration first to upload training documents'
                  : t('chatbot.uploadTrainingDataDesc')
                }
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className={`border-2 border-dashed rounded-lg p-6 text-center ${
                isCreateMode ? 'border-muted-foreground/10 bg-muted/20' : 'border-muted-foreground/25'
              }`}>
                <Upload className={`mx-auto h-12 w-12 ${
                  isCreateMode ? 'text-muted-foreground/50' : 'text-muted-foreground'
                }`} />
                <div className="mt-4">
                  <Label htmlFor="file-upload" className={isCreateMode ? 'cursor-not-allowed' : 'cursor-pointer'}>
                    <span className={`text-sm font-medium ${
                      isCreateMode ? 'text-muted-foreground/50' : 'text-primary hover:text-primary/80'
                    }`}>
                      {isUploading ? 'Uploading...' : t('chatbot.clickToUpload')}
                    </span>
                    <span className="text-sm text-muted-foreground"> {t('chatbot.orDragAndDrop')}</span>
                  </Label>
                  <Input
                    id="file-upload"
                    type="file"
                    multiple
                    accept=".txt,.pdf,.docx,.md"
                    onChange={handleFileUpload}
                    disabled={isUploading || isCreateMode}
                    className="hidden"
                  />
                </div>
                <p className="text-xs text-muted-foreground mt-2">
                  {t('chatbot.supportedFormats')}
                </p>
              </div>

              {uploadError && (
                <div className="p-3 text-sm text-red-600 bg-red-50 border border-red-200 rounded-md">
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
                  Uploading files...
                </div>
              )}

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
