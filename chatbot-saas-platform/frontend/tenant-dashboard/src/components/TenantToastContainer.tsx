import React, { useEffect, useState } from "react";
import { useDispatch } from "react-redux";
import { tenantSignalRService } from "../services/signalr";
import { addTenantNotification, TenantNotification } from "../store/slices/analyticsSlice";
import { X, CheckCircle, AlertCircle, Info } from "lucide-react";

interface ToastNotification extends TenantNotification {
  show: boolean;
}

export const TenantToastContainer: React.FC = () => {
  const dispatch = useDispatch();
  const [toasts, setToasts] = useState<ToastNotification[]>([]);

  useEffect(() => {
    // Set up real-time notification listener for toasts
    tenantSignalRService.setOnTenantNotification((notification) => {
      console.log("🔔 Tenant Toast: TenantNotification received:", notification);
      
      // Add to Redux store
      const serializedNotification = {
        ...notification,
        timestamp: notification.timestamp instanceof Date 
          ? notification.timestamp.toISOString() 
          : new Date(notification.timestamp).toISOString()
      };
      dispatch(addTenantNotification(serializedNotification));

      // Add to toast display
      const toastNotification: ToastNotification = {
        ...serializedNotification,
        show: true
      };
      
      setToasts(prev => [...prev, toastNotification]);

      // Auto-remove toast after 5 seconds
      setTimeout(() => {
        setToasts(prev => prev.filter(toast => toast.id !== notification.id));
      }, 5000);
    });
  }, [dispatch]);

  const removeToast = (id: string) => {
    setToasts(prev => prev.filter(toast => toast.id !== id));
  };

  const getToastIcon = (type: string) => {
    switch (type) {
      case "success":
        return <CheckCircle className="w-5 h-5 text-green-600" />;
      case "warning":
        return <AlertCircle className="w-5 h-5 text-yellow-600" />;
      case "error":
        return <AlertCircle className="w-5 h-5 text-red-600" />;
      case "info":
      default:
        return <Info className="w-5 h-5 text-blue-600" />;
    }
  };

  const getToastStyles = (type: string) => {
    switch (type) {
      case "success":
        return "border-green-200 bg-green-50 text-green-800";
      case "warning":
        return "border-yellow-200 bg-yellow-50 text-yellow-800";
      case "error":
        return "border-red-200 bg-red-50 text-red-800";
      case "info":
      default:
        return "border-blue-200 bg-blue-50 text-blue-800";
    }
  };

  if (toasts.length === 0) return null;

  return (
    <div className="fixed top-4 right-4 z-50 space-y-2">
      {toasts.map((toast) => (
        <div
          key={toast.id}
          className={`
            max-w-sm w-full border rounded-lg shadow-lg p-4 
            transform transition-all duration-300 ease-in-out
            ${toast.show ? 'translate-x-0 opacity-100' : 'translate-x-full opacity-0'}
            ${getToastStyles(toast.type)}
          `}
        >
          <div className="flex items-start">
            <div className="flex-shrink-0">
              {getToastIcon(toast.type)}
            </div>
            
            <div className="ml-3 w-0 flex-1">
              <p className="text-sm font-medium">
                {toast.title}
              </p>
              <p className="mt-1 text-sm">
                {toast.message}
              </p>
            </div>
            
            <div className="ml-4 flex-shrink-0 flex">
              <button
                className="inline-flex text-gray-400 hover:text-gray-600 focus:outline-none"
                onClick={() => removeToast(toast.id)}
              >
                <span className="sr-only">Close</span>
                <X className="h-4 w-4" />
              </button>
            </div>
          </div>
        </div>
      ))}
    </div>
  );
};
