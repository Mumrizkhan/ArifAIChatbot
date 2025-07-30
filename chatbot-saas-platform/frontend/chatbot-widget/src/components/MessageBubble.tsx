import React from 'react';
import { useTranslation } from 'react-i18next';
import { useSelector } from 'react-redux';
import { RootState } from '../store/store';
import { Message } from '../store/slices/chatSlice';
import { Bot, User, UserCheck, Download, FileText, Image } from 'lucide-react';
import { formatDistanceToNow } from 'date-fns';

interface MessageBubbleProps {
  message: Message;
  isFirstInGroup: boolean;
  isLastInGroup: boolean;
  showAvatar: boolean;
}

export const MessageBubble: React.FC<MessageBubbleProps> = ({
  message,
  isFirstInGroup,
  isLastInGroup,
  showAvatar,
}) => {
  const { t } = useTranslation();
  const { currentConversation } = useSelector((state: RootState) => state.chat);
  const { branding, isRTL } = useSelector((state: RootState) => state.theme);

  const isUser = message.sender === 'user';
  const isAgent = message.sender === 'agent';

  const getAvatar = () => {
    if (isUser) {
      return (
        <div className="message-avatar user-avatar">
          <User size={16} />
        </div>
      );
    }

    if (isAgent && currentConversation?.assignedAgent?.avatar) {
      return (
        <img
          src={currentConversation.assignedAgent.avatar}
          alt={currentConversation.assignedAgent.name}
          className="message-avatar agent-avatar"
        />
      );
    }

    if (isAgent) {
      return (
        <div className="message-avatar agent-avatar">
          <UserCheck size={16} />
        </div>
      );
    }

    if (branding.logo) {
      return (
        <img
          src={branding.logo}
          alt={branding.companyName || t('widget.title')}
          className="message-avatar bot-avatar"
        />
      );
    }

    return (
      <div className="message-avatar bot-avatar">
        <Bot size={16} />
      </div>
    );
  };

  const getSenderName = () => {
    if (isUser) return t('accessibility.userMessage');
    if (isAgent && currentConversation?.assignedAgent) {
      return currentConversation.assignedAgent.name;
    }
    if (isAgent) return 'Agent';
    return branding.companyName || 'Bot';
  };

  const renderFileMessage = () => {
    const { fileName, fileSize, fileType } = message.metadata || {};
    
    return (
      <div className="message-file">
        <div className="file-icon">
          {fileType?.startsWith('image/') ? (
            <Image size={20} />
          ) : (
            <FileText size={20} />
          )}
        </div>
        <div className="file-info">
          <div className="file-name">{fileName}</div>
          {fileSize && (
            <div className="file-size">
              {(fileSize / 1024 / 1024).toFixed(2)} MB
            </div>
          )}
        </div>
        <button className="file-download" aria-label={t('widget.downloadFile')}>
          <Download size={16} />
        </button>
      </div>
    );
  };

  const renderImageMessage = () => {
    const { imageUrl, fileName } = message.metadata || {};
    
    return (
      <div className="message-image">
        <img
          src={imageUrl}
          alt={fileName || 'Uploaded image'}
          className="message-image-content"
          loading="lazy"
        />
      </div>
    );
  };

  const renderTextMessage = () => (
    <div className="message-text">
      {message.content}
    </div>
  );

  const renderMessageContent = () => {
    switch (message.type) {
      case 'file':
        return renderFileMessage();
      case 'image':
        return renderImageMessage();
      case 'text':
      default:
        return renderTextMessage();
    }
  };

  const getMessageClasses = () => {
    const baseClasses = 'message-bubble';
    const senderClasses = {
      user: 'message-user',
      bot: 'message-bot',
      agent: 'message-agent',
    };
    
    return [
      baseClasses,
      senderClasses[message.sender],
      isFirstInGroup && 'first-in-group',
      isLastInGroup && 'last-in-group',
      isRTL && 'rtl',
    ].filter(Boolean).join(' ');
  };

  const formatTimestamp = (timestamp: Date) => {
    return formatDistanceToNow(timestamp, { addSuffix: true });
  };

  return (
    <div className={getMessageClasses()}>
      <div className="message-content">
        {showAvatar && !isUser && (
          <div className="message-avatar-container">
            {getAvatar()}
          </div>
        )}
        
        <div className="message-body">
          {isFirstInGroup && !isUser && (
            <div className="message-sender">
              {getSenderName()}
            </div>
          )}
          
          <div 
            className="message-bubble-content"
            role="article"
            aria-label={`${getSenderName()}: ${message.content}`}
          >
            {renderMessageContent()}
          </div>
          
          {isLastInGroup && (
            <div className="message-timestamp">
              <time dateTime={message.timestamp.toISOString()}>
                {formatTimestamp(message.timestamp)}
              </time>
            </div>
          )}
        </div>
      </div>
    </div>
  );
};
