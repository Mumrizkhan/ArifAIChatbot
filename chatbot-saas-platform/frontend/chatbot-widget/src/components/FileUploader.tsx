import React, { useRef } from 'react';
import { useTranslation } from 'react-i18next';
import { useSelector } from 'react-redux';
import { RootState } from '../store/store';
import { Paperclip } from 'lucide-react';
import { fileService } from '../services/fileService';

interface FileUploaderProps {
  onFileSelect: (file: File) => void;
  disabled?: boolean;
  className?: string;
}

export const FileUploader: React.FC<FileUploaderProps> = ({
  onFileSelect,
  disabled = false,
  className = '',
}) => {
  const { t } = useTranslation();
  const fileInputRef = useRef<HTMLInputElement>(null);
  const { widget } = useSelector((state: RootState) => state.config);

  const handleFileSelect = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    if (!fileService.isValidFileSize(file, widget.behavior.maxFileSize)) {
      alert(t('errors.fileTooLarge', { 
        maxSize: Math.round(widget.behavior.maxFileSize / 1024 / 1024) 
      }));
      return;
    }

    if (!fileService.isValidFileType(file, widget.behavior.allowedFileTypes)) {
      alert(t('errors.fileTypeNotAllowed'));
      return;
    }

    onFileSelect(file);
    
    if (fileInputRef.current) {
      fileInputRef.current.value = '';
    }
  };

  const handleClick = () => {
    fileInputRef.current?.click();
  };

  return (
    <>
      <input
        ref={fileInputRef}
        type="file"
        onChange={handleFileSelect}
        accept={widget.behavior.allowedFileTypes.join(',')}
        style={{ display: 'none' }}
      />
      <button
        type="button"
        onClick={handleClick}
        disabled={disabled || !widget.features.fileUpload}
        className={`file-uploader ${className}`}
        aria-label={t('widget.attachFile')}
        title={t('widget.attachFile')}
      >
        <Paperclip size={20} />
      </button>
    </>
  );
};
