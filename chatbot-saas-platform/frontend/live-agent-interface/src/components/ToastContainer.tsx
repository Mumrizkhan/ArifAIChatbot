import React, { useEffect, useState } from "react";
import { useSelector, useDispatch } from "react-redux";
import { RootState } from "../store/store";
import { removeNotification } from "../store/slices/notificationSlice";
import { X, CheckCircle, AlertCircle, Info, AlertTriangle } from "lucide-react";

interface ToastProps {
  id: string;
  type: "success" | "error" | "warning" | "info";
  title: string;
  message: string;
  duration?: number;
  onClose: (id: string) => void;
}

const Toast: React.FC<ToastProps> = ({ id, type, title, message, duration = 5000, onClose }) => {
  const [isVisible, setIsVisible] = useState(true);

  useEffect(() => {
    const timer = setTimeout(() => {
      setIsVisible(false);
      setTimeout(() => onClose(id), 300); // Wait for animation
    }, duration);

    return () => clearTimeout(timer);
  }, [id, duration, onClose]);

  const getIcon = () => {
    switch (type) {
      case "success":
        return <CheckCircle className="w-5 h-5 text-green-500" />;
      case "error":
        return <AlertCircle className="w-5 h-5 text-red-500" />;
      case "warning":
        return <AlertTriangle className="w-5 h-5 text-yellow-500" />;
      case "info":
      default:
        return <Info className="w-5 h-5 text-blue-500" />;
    }
  };

  const getBorderColor = () => {
    switch (type) {
      case "success":
        return "border-l-green-500";
      case "error":
        return "border-l-red-500";
      case "warning":
        return "border-l-yellow-500";
      case "info":
      default:
        return "border-l-blue-500";
    }
  };

  return (
    <div
      className={`
        transform transition-all duration-300 ease-in-out
        ${isVisible ? "translate-x-0 opacity-100" : "translate-x-full opacity-0"}
        bg-white border-l-4 ${getBorderColor()} shadow-lg rounded-r-lg p-4 mb-2 max-w-sm
      `}
    >
      <div className="flex items-start">
        <div className="flex-shrink-0">{getIcon()}</div>
        <div className="ml-3 flex-1">
          <p className="text-sm font-medium text-gray-900">{title}</p>
          <p className="text-sm text-gray-600 mt-1">{message}</p>
        </div>
        <button
          onClick={() => {
            setIsVisible(false);
            setTimeout(() => onClose(id), 300);
          }}
          className="flex-shrink-0 ml-2 text-gray-400 hover:text-gray-600"
        >
          <X className="w-4 h-4" />
        </button>
      </div>
    </div>
  );
};

export const ToastContainer: React.FC = () => {
  const [toasts, setToasts] = useState<Array<{
    id: string;
    type: "success" | "error" | "warning" | "info";
    title: string;
    message: string;
  }>>([]);
  
  const { agentNotifications } = useSelector((state: RootState) => state.agent);
  const dispatch = useDispatch();

  // Listen for new agent notifications and show as toasts
  useEffect(() => {
    if (agentNotifications.length > 0) {
      const latestNotification = agentNotifications[0];
      
      // Convert notification to toast
      const newToast = {
        id: latestNotification.id,
        type: latestNotification.type as "success" | "error" | "warning" | "info",
        title: latestNotification.title,
        message: latestNotification.message
      };

      // Check if toast already exists
      if (!toasts.find(toast => toast.id === newToast.id)) {
        setToasts(prev => [newToast, ...prev.slice(0, 4)]); // Keep max 5 toasts
      }
    }
  }, [agentNotifications, toasts]);

  const handleRemoveToast = (id: string) => {
    setToasts(prev => prev.filter(toast => toast.id !== id));
  };

  return (
    <div className="fixed top-4 right-4 z-50 space-y-2">
      {toasts.map((toast) => (
        <Toast
          key={toast.id}
          id={toast.id}
          type={toast.type}
          title={toast.title}
          message={toast.message}
          onClose={handleRemoveToast}
        />
      ))}
    </div>
  );
};
