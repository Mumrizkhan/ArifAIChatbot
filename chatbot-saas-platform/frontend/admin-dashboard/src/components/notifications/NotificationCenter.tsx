import React from "react";
import { useSelector, useDispatch } from "react-redux";
import { RootState } from "../../store/store";
import { removeSystemNotification, clearSystemNotifications } from "../../store/slices/analyticsSlice";
import { Card, CardContent, CardHeader, CardTitle } from "../ui/card";
import { Button } from "../ui/button";
import { Badge } from "../ui/badge";
import { Bell, X, Trash2 } from "lucide-react";
import { formatDistanceToNow } from "date-fns";
import { useTranslation } from "react-i18next";

const NotificationCenter: React.FC = () => {
  const { t } = useTranslation();
  const dispatch = useDispatch();
  const { systemNotifications } = useSelector((state: RootState) => state.analytics);

  const handleRemoveNotification = (id: string) => {
    dispatch(removeSystemNotification(id));
  };

  const handleClearAll = () => {
    dispatch(clearSystemNotifications());
  };

  const getNotificationColor = (type: string) => {
    switch (type) {
      case "error":
        return "destructive";
      case "warning":
        return "secondary";
      case "success":
        return "default";
      default:
        return "outline";
    }
  };

  if (systemNotifications.length === 0) {
    return (
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center">
            <Bell className="mr-2 h-5 w-5" />
            {t("settings.notifications.title")}
          </CardTitle>
        </CardHeader>
        <CardContent>
          <p className="text-muted-foreground text-center py-4">{t("settings.notifications.noNotifications")}</p>
        </CardContent>
      </Card>
    );
  }

  return (
    <Card>
      <CardHeader>
        <div className="flex items-center justify-between">
          <CardTitle className="flex items-center">
            <Bell className="mr-2 h-5 w-5" />
            {t("settings.notifications.title")} ({systemNotifications.length})
          </CardTitle>
          <Button variant="outline" size="sm" onClick={handleClearAll} className="flex items-center">
            <Trash2 className="mr-1 h-3 w-3" />
            {t("settings.notifications.clearAll")}
          </Button>
        </div>
      </CardHeader>
      <CardContent className="space-y-3 max-h-96 overflow-y-auto">
        {systemNotifications.map((notification) => (
          <div key={notification.id} className="flex items-start justify-between p-3 border rounded-lg">
            <div className="flex-1">
              <div className="flex items-center space-x-2 mb-1">
                <Badge variant={getNotificationColor(notification.type)}>
                  {t(`settings.notifications.types.${notification.type}`, notification.type)}
                </Badge>
                <span className="text-xs text-muted-foreground">{formatDistanceToNow(new Date(notification.timestamp), { addSuffix: true })}</span>
              </div>
              <h4 className="font-medium text-sm">{notification.title}</h4>
              <p className="text-sm text-muted-foreground">{notification.message}</p>
              {notification.tenantId && (
                <p className="text-xs text-muted-foreground mt-1">
                  {t("settings.notifications.tenant")}: {notification.tenantId}
                </p>
              )}
            </div>
            <Button variant="ghost" size="sm" onClick={() => handleRemoveNotification(notification.id)} className="ml-2">
              <X className="h-3 w-3" />
            </Button>
          </div>
        ))}
      </CardContent>
    </Card>
  );
};

export default NotificationCenter;
