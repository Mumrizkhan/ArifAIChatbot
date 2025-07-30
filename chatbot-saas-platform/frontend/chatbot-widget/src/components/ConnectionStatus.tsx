import React from 'react';
import { useTranslation } from 'react-i18next';
import { WifiOff, AlertCircle, Loader2 } from 'lucide-react';

interface ConnectionStatusProps {
  status: 'connecting' | 'connected' | 'disconnected' | 'error';
}

export const ConnectionStatus: React.FC<ConnectionStatusProps> = ({ status }) => {
  const { t } = useTranslation();

  if (status === 'connected') {
    return null; // Don't show anything when connected
  }

  const getStatusConfig = () => {
    switch (status) {
      case 'connecting':
        return {
          icon: <Loader2 size={16} className="animate-spin" />,
          text: t('status.connecting'),
          className: 'status-connecting',
        };
      case 'disconnected':
        return {
          icon: <WifiOff size={16} />,
          text: t('status.disconnected'),
          className: 'status-disconnected',
        };
      case 'error':
        return {
          icon: <AlertCircle size={16} />,
          text: t('errors.connectionFailed'),
          className: 'status-error',
        };
      default:
        return {
          icon: <WifiOff size={16} />,
          text: t('status.disconnected'),
          className: 'status-disconnected',
        };
    }
  };

  const config = getStatusConfig();

  return (
    <div className={`connection-status ${config.className}`} role="status" aria-live="polite">
      <div className="status-content">
        <div className="status-icon">
          {config.icon}
        </div>
        <span className="status-text">
          {config.text}
        </span>
      </div>
    </div>
  );
};
