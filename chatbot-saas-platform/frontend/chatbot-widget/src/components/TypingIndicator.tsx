import React from 'react';
import { useTranslation } from 'react-i18next';
import { useSelector } from 'react-redux';
import { RootState } from '../store/store';
import { Bot, UserCheck } from 'lucide-react';

interface TypingIndicatorProps {
  user: string;
}

export const TypingIndicator: React.FC<TypingIndicatorProps> = ({ user }) => {
  const { t } = useTranslation();
  const { currentConversation } = useSelector((state: RootState) => state.chat);
  const { branding } = useSelector((state: RootState) => state.theme);

  const isAgent = currentConversation?.assignedAgent?.name === user;

  const getAvatar = () => {
    if (isAgent && currentConversation?.assignedAgent?.avatar) {
      return (
        <img
          src={currentConversation.assignedAgent.avatar}
          alt={currentConversation.assignedAgent.name}
          className="typing-avatar"
        />
      );
    }

    if (isAgent) {
      return (
        <div className="typing-avatar agent-avatar">
          <UserCheck size={16} />
        </div>
      );
    }

    if (branding.logo) {
      return (
        <img
          src={branding.logo}
          alt={branding.companyName || t('widget.title')}
          className="typing-avatar"
        />
      );
    }

    return (
      <div className="typing-avatar bot-avatar">
        <Bot size={16} />
      </div>
    );
  };

  return (
    <div className="typing-indicator" role="status" aria-live="polite">
      <div className="typing-content">
        <div className="typing-avatar-container">
          {getAvatar()}
        </div>
        
        <div className="typing-bubble">
          <div className="typing-text">
            <span className="typing-user">{user}</span>
            <span className="typing-message">{t('widget.typing')}</span>
          </div>
          
          <div className="typing-dots">
            <div className="typing-dot"></div>
            <div className="typing-dot"></div>
            <div className="typing-dot"></div>
          </div>
        </div>
      </div>
    </div>
  );
};
