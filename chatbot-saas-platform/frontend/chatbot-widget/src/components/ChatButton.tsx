import React from 'react';
import { useTranslation } from 'react-i18next';
import { useSelector } from 'react-redux';
import { RootState } from '../store/store';
import { MessageCircle, Wifi, WifiOff } from 'lucide-react';

interface ChatButtonProps {
  onClick: () => void;
  unreadCount: number;
  isConnected: boolean;
}

export const ChatButton: React.FC<ChatButtonProps> = ({
  onClick,
  unreadCount,
  isConnected,
}) => {
  const { t } = useTranslation();
  const { branding } = useSelector((state: RootState) => state.theme);

  return (
    <button
      className="chat-button"
      onClick={onClick}
      aria-label={t('widget.startConversation')}
      title={t('widget.startConversation')}
    >
      <div className="chat-button-content">
        {branding.logo ? (
          <img 
            src={branding.logo} 
            alt={branding.companyName || t('widget.title')}
            className="chat-button-logo"
          />
        ) : (
          <MessageCircle size={24} className="chat-button-icon" />
        )}
        
        {unreadCount > 0 && (
          <span className="unread-badge" aria-label={`${unreadCount} unread messages`}>
            {unreadCount > 99 ? '99+' : unreadCount}
          </span>
        )}
        
        <div className="connection-indicator">
          {isConnected ? (
            <Wifi size={12} className="connection-icon connected" />
          ) : (
            <WifiOff size={12} className="connection-icon disconnected" />
          )}
        </div>
      </div>
      
      {branding.welcomeMessage && (
        <div className="chat-button-tooltip">
          {branding.welcomeMessage}
        </div>
      )}
    </button>
  );
};
