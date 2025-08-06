import React, { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useForm } from 'react-hook-form';
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
  Palette,
  Upload,
  Save,
  Eye,
  Monitor,
  Smartphone,
  Type,
  Image,
  Code,
} from 'lucide-react';

const BrandingPage = () => {
  const { t } = useTranslation();
  const [activeTab, setActiveTab] = useState('colors');
  const [previewMode, setPreviewMode] = useState('desktop');

  const { register, handleSubmit, setValue, watch } = useForm({
    defaultValues: {
      primaryColor: '#3b82f6',
      secondaryColor: '#10b981',
      accentColor: '#f59e0b',
      backgroundColor: '#ffffff',
      textColor: '#1f2937',
      fontFamily: 'Inter',
      fontSize: 'medium',
      borderRadius: 'medium',
      logoUrl: '',
      faviconUrl: '',
      customCss: '',
      enableDarkMode: false,
      enableAnimations: true,
      companyName: '',
      tagline: '',
    },
  });

  const onSubmit = (data: any) => {
    console.log('Branding data:', data);
  };

  const handleLogoUpload = (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (file) {
      const reader = new FileReader();
      reader.onload = (e) => {
        setValue('logoUrl', e.target?.result as string);
      };
      reader.readAsDataURL(file);
    }
  };

  const colorPresets = [
    { name: 'Blue', primary: '#3b82f6', secondary: '#10b981', accent: '#f59e0b' },
    { name: 'Purple', primary: '#8b5cf6', secondary: '#06b6d4', accent: '#f97316' },
    { name: 'Green', primary: '#10b981', secondary: '#3b82f6', accent: '#ef4444' },
    { name: 'Orange', primary: '#f97316', secondary: '#8b5cf6', accent: '#10b981' },
  ];

  const fontOptions = [
    { name: 'Inter', value: 'Inter' },
    { name: 'Roboto', value: 'Roboto' },
    { name: 'Open Sans', value: 'Open Sans' },
    { name: 'Lato', value: 'Lato' },
    { name: 'Poppins', value: 'Poppins' },
  ];

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">{t('branding.title')}</h1>
          <p className="text-muted-foreground">
            {t('branding.subtitle')}
          </p>
        </div>
        <div className="flex items-center space-x-2">
          <Button variant="outline" onClick={() => setPreviewMode(previewMode === 'desktop' ? 'mobile' : 'desktop')}>
            {previewMode === 'desktop' ? <Smartphone className="mr-2 h-4 w-4" /> : <Monitor className="mr-2 h-4 w-4" />}
            {previewMode === 'desktop' ? t('branding.mobilePreview') : t('branding.desktopPreview')}
          </Button>
          <Button onClick={handleSubmit(onSubmit)}>
            <Save className="mr-2 h-4 w-4" />
            {t('common.save')}
          </Button>
        </div>
      </div>

      <div className="grid gap-6 lg:grid-cols-3">
        <div className="lg:col-span-2">
          <Tabs value={activeTab} onValueChange={setActiveTab} className="space-y-4">
            <TabsList>
              <TabsTrigger value="colors">
                <Palette className="mr-2 h-4 w-4" />
                {t('branding.colors')}
              </TabsTrigger>
              <TabsTrigger value="typography">
                <Type className="mr-2 h-4 w-4" />
                {t('branding.typography')}
              </TabsTrigger>
              <TabsTrigger value="assets">
                <Image className="mr-2 h-4 w-4" />
                {t('branding.assets')}
              </TabsTrigger>
              <TabsTrigger value="custom">
                <Code className="mr-2 h-4 w-4" />
                {t('branding.custom')}
              </TabsTrigger>
            </TabsList>

            <TabsContent value="colors" className="space-y-4">
              <Card>
                <CardHeader>
                  <CardTitle>{t('branding.colorScheme')}</CardTitle>
                  <CardDescription>
                    {t('branding.colorSchemeDesc')}
                  </CardDescription>
                </CardHeader>
                <CardContent className="space-y-4">
                  <div className="grid gap-4 md:grid-cols-2">
                    <div className="space-y-2">
                      <Label htmlFor="primaryColor">{t('branding.primaryColor')}</Label>
                      <div className="flex items-center space-x-2">
                        <Input
                          id="primaryColor"
                          type="color"
                          {...register('primaryColor')}
                          className="w-16 h-10 p-1 border rounded"
                        />
                        <Input
                          {...register('primaryColor')}
                          placeholder="#3b82f6"
                          className="flex-1"
                        />
                      </div>
                    </div>

                    <div className="space-y-2">
                      <Label htmlFor="secondaryColor">{t('branding.secondaryColor')}</Label>
                      <div className="flex items-center space-x-2">
                        <Input
                          id="secondaryColor"
                          type="color"
                          {...register('secondaryColor')}
                          className="w-16 h-10 p-1 border rounded"
                        />
                        <Input
                          {...register('secondaryColor')}
                          placeholder="#10b981"
                          className="flex-1"
                        />
                      </div>
                    </div>
                  </div>

                  <div className="space-y-3">
                    <Label>{t('branding.colorPresets')}</Label>
                    <div className="grid gap-2 md:grid-cols-2">
                      {colorPresets.map((preset, index) => (
                        <Button
                          key={index}
                          variant="outline"
                          className="justify-start h-auto p-3"
                          onClick={() => {
                            setValue('primaryColor', preset.primary);
                            setValue('secondaryColor', preset.secondary);
                            setValue('accentColor', preset.accent);
                          }}
                        >
                          <div className="flex items-center space-x-3">
                            <div className="flex space-x-1">
                              <div
                                className="w-4 h-4 rounded-full"
                                style={{ backgroundColor: preset.primary }}
                              />
                              <div
                                className="w-4 h-4 rounded-full"
                                style={{ backgroundColor: preset.secondary }}
                              />
                              <div
                                className="w-4 h-4 rounded-full"
                                style={{ backgroundColor: preset.accent }}
                              />
                            </div>
                            <span>{preset.name}</span>
                          </div>
                        </Button>
                      ))}
                    </div>
                  </div>
                </CardContent>
              </Card>
            </TabsContent>

            <TabsContent value="typography" className="space-y-4">
              <Card>
                <CardHeader>
                  <CardTitle>{t('branding.typography')}</CardTitle>
                  <CardDescription>
                    {t('branding.typographyDesc')}
                  </CardDescription>
                </CardHeader>
                <CardContent className="space-y-4">
                  <div className="grid gap-4 md:grid-cols-2">
                    <div className="space-y-2">
                      <Label htmlFor="fontFamily">{t('branding.fontFamily')}</Label>
                      <Select value={watch('fontFamily')} onValueChange={(value) => setValue('fontFamily', value)}>
                        <SelectTrigger>
                          <SelectValue />
                        </SelectTrigger>
                        <SelectContent>
                          {fontOptions.map((font) => (
                            <SelectItem key={font.value} value={font.value}>
                              {font.name}
                            </SelectItem>
                          ))}
                        </SelectContent>
                      </Select>
                    </div>

                    <div className="space-y-2">
                      <Label htmlFor="fontSize">{t('branding.fontSize')}</Label>
                      <Select value={watch('fontSize')} onValueChange={(value) => setValue('fontSize', value)}>
                        <SelectTrigger>
                          <SelectValue />
                        </SelectTrigger>
                        <SelectContent>
                          <SelectItem value="small">{t('branding.fontSizeSmall')}</SelectItem>
                          <SelectItem value="medium">{t('branding.fontSizeMedium')}</SelectItem>
                          <SelectItem value="large">{t('branding.fontSizeLarge')}</SelectItem>
                        </SelectContent>
                      </Select>
                    </div>
                  </div>
                </CardContent>
              </Card>
            </TabsContent>

            <TabsContent value="assets" className="space-y-4">
              <Card>
                <CardHeader>
                  <CardTitle>{t('branding.brandAssets')}</CardTitle>
                  <CardDescription>
                    {t('branding.brandAssetsDesc')}
                  </CardDescription>
                </CardHeader>
                <CardContent className="space-y-4">
                  <div className="space-y-2">
                    <Label htmlFor="companyName">{t('branding.companyName')}</Label>
                    <Input
                      id="companyName"
                      {...register('companyName')}
                      placeholder={t('branding.companyNamePlaceholder')}
                    />
                  </div>

                  <div className="space-y-2">
                    <Label>{t('branding.logo')}</Label>
                    <div className="border-2 border-dashed border-muted-foreground/25 rounded-lg p-6 text-center">
                      {watch('logoUrl') ? (
                        <div className="space-y-4">
                          <img
                            src={watch('logoUrl')}
                            alt="Logo preview"
                            className="mx-auto max-h-20 object-contain"
                          />
                          <Button
                            variant="outline"
                            onClick={() => setValue('logoUrl', '')}
                          >
                            {t('branding.removeLogo')}
                          </Button>
                        </div>
                      ) : (
                        <div>
                          <Upload className="mx-auto h-12 w-12 text-muted-foreground" />
                          <div className="mt-4">
                            <Label htmlFor="logo-upload" className="cursor-pointer">
                              <span className="text-sm font-medium text-primary hover:text-primary/80">
                                {t('branding.uploadLogo')}
                              </span>
                            </Label>
                            <Input
                              id="logo-upload"
                              type="file"
                              accept="image/*"
                              onChange={handleLogoUpload}
                              className="hidden"
                            />
                          </div>
                        </div>
                      )}
                    </div>
                  </div>
                </CardContent>
              </Card>
            </TabsContent>

            <TabsContent value="custom" className="space-y-4">
              <Card>
                <CardHeader>
                  <CardTitle>{t('branding.customStyling')}</CardTitle>
                  <CardDescription>
                    {t('branding.customStylingDesc')}
                  </CardDescription>
                </CardHeader>
                <CardContent className="space-y-4">
                  <div className="space-y-2">
                    <Label htmlFor="customCss">{t('branding.customCss')}</Label>
                    <Textarea
                      id="customCss"
                      {...register('customCss')}
                      placeholder={t('branding.customCssPlaceholder')}
                      rows={10}
                      className="font-mono text-sm"
                    />
                  </div>

                  <div className="flex items-center justify-between">
                    <div className="space-y-0.5">
                      <Label>{t('branding.enableAnimations')}</Label>
                      <p className="text-sm text-muted-foreground">
                        {t('branding.enableAnimationsDesc')}
                      </p>
                    </div>
                    <Switch
                      checked={watch('enableAnimations')}
                      onCheckedChange={(checked) => setValue('enableAnimations', checked)}
                    />
                  </div>
                </CardContent>
              </Card>
            </TabsContent>
          </Tabs>
        </div>

        <div className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center">
                <Eye className="mr-2 h-4 w-4" />
                {t('branding.preview')}
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div className={`border rounded-lg p-4 ${previewMode === 'mobile' ? 'max-w-sm mx-auto' : ''}`}>
                <div
                  className="space-y-4"
                  style={{
                    backgroundColor: watch('backgroundColor'),
                    color: watch('textColor'),
                    fontFamily: watch('fontFamily'),
                  }}
                >
                  {watch('logoUrl') && (
                    <img
                      src={watch('logoUrl')}
                      alt="Logo"
                      className="h-8 object-contain"
                    />
                  )}
                  <div>
                    <h3
                      className="text-lg font-semibold"
                      style={{ color: watch('primaryColor') }}
                    >
                      {watch('companyName') || 'Your Company'}
                    </h3>
                  </div>
                  <div className="space-y-2">
                    <button
                      className="px-4 py-2 rounded text-white text-sm"
                      style={{ backgroundColor: watch('primaryColor') }}
                    >
                      Primary Button
                    </button>
                    <button
                      className="px-4 py-2 rounded text-white text-sm ml-2"
                      style={{ backgroundColor: watch('secondaryColor') }}
                    >
                      Secondary Button
                    </button>
                  </div>
                </div>
              </div>
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  );
};

export default BrandingPage;
