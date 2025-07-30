import React from 'react';
import { useTranslation } from 'react-i18next';
import { useSelector } from 'react-redux';
import { RootState } from '../store/store';
import { Minimize2, X, User, Bot, Wifi, WifiOff, AlertCircle } from 'lucide-react';

interface ChatHeaderProps {
  onMinimize: () => void;
  onClose: () => void;
  connectionStatus: 'connecting' | 'connected' | 'disconnected' | 'error';
}

export const ChatHeader: React.FC<ChatHeaderProps> = ({
  onMinimize,
  onClose,
  connectionStatus,
}) => {
  const { t } = useTranslation();
  const { currentConversation, isTyping, typingUser } = useSelector(
    (state: RootState) => state.chat
  );
  const { branding } = useSelector((state: RootState) => state.theme);

  const getStatusIcon = () => {
    switch (connectionStatus) {
      case 'connected':
        return <Wifi size={16} className="text-green-500" />;
      case 'connecting':
        return <Wifi size={16} className="text-yellow-500 animate-pulse" />;
      case 'error':
        return <AlertCircle size={16} className="text-red-500" />;
      default:
        return <WifiOff size={16} className="text-gray-500" />;
    }
  };

  const getStatusText = () => {
    if (isTyping && typingUser) {
      return `${typingUser} ${t('widget.typing')}`;
    }
    
    if (currentConversation?.assignedAgent) {
      return currentConversation.assignedAgent.name;
    }
    
    switch (connectionStatus) {
      case 'connected':
        return t('widget.online');
      case 'connecting':
        return t('widget.connecting');
      case 'error':
        return t('widget.offline');
      default:
        return t('widget.offline');
    }
  };

  const getAvatar = () => {
    if (currentConversation?.assignedAgent?.avatar) {
      return (
        <img
          src={currentConversation.assignedAgent.avatar}
          alt={currentConversation.assignedAgent.name}
          className="w-8 h-8 rounded-full"
        />
      );
    }
    
    if (currentConversation?.assignedAgent) {
      return (
        <div className="w-8 h-8 rounded-full bg-blue-500 flex items-center justify-center">
          <User size={16} className="text-white" />
        </div>
      );
    }
    
    if (branding.logo) {
      return (
        <img
          src={branding.logo}
          alt={branding.companyName || t('widget.title')}
          className="w-8 h-8 rounded-full"
        />
      );
    }
    
    return (
      <div className="w-8 h-8 rounded-full bg-primary flex items-center justify-center">
        <Bot size={16} className="text-white" />
      </div>
    );
  };

  return (
    <div className="chat-header">
      <div className="chat-header-content">
        <div className="chat-header-info">
          <div className="chat-header-avatar">
            {getAvatar()}
          </div>
          <div className="chat-header-text">
            <h3 className="chat-header-title">
              {branding.companyName || t('widget.title')}
            </h3>
            <div className="chat-header-status">
              {getStatusIcon()}
              <span className="chat-header-status-text">
                {getStatusText()}
              </span>
            </div>
          </div>
        </div>
        
        <div className="chat-header-actions">
          <button
            className="chat-header-button"
            onClick={onMinimize}
            aria-label={t('widget.minimize')}
            title={t('widget.minimize')}
          >
            <Minimize2 size={16} />
          </button>
          <button
            className="chat-header-button"
            onClick={onClose}
            aria-label={t('widget.close')}
            title={t('widget.close')}
          >
            <X size={16} />
          </button>
        </div>
      </div>
    </div>
  );
};
