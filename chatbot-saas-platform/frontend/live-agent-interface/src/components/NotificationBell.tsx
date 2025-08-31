import React, { useState, useEffect } from "react";
import { useDispatch, useSelector } from "react-redux";
import { Bell } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { RootState } from "../store/store";
import { AppDispatch } from "../store/store";
import { fetchNotifications } from "../store/slices/notificationSlice";
import { NotificationPanel } from "./NotificationPanel";
import { agentSignalRService } from "../services/signalr";

export const NotificationBell: React.FC = () => {
  const dispatch = useDispatch<AppDispatch>();
  const { unreadCount } = useSelector((state: RootState) => state.notifications);
  const [isOpen, setIsOpen] = useState(false);

  useEffect(() => {
    // Fetch initial notifications
    dispatch(fetchNotifications({ limit: 50, unreadOnly: false }));

    // Set up real-time notification listener
    const handleNewNotification = () => {
      // Refresh notifications when a new one arrives
      dispatch(fetchNotifications({ limit: 50, unreadOnly: false }));
    };

    agentSignalRService.setOnAgentNotification(handleNewNotification);

    return () => {
      // Cleanup if needed
    };
  }, [dispatch]);

  return (
    <>
      <Button
        variant="ghost"
        size="sm"
        className="relative"
        onClick={() => setIsOpen(!isOpen)}
      >
        <Bell className="w-5 h-5" />
        {unreadCount > 0 && (
          <Badge
            variant="destructive"
            className="absolute -top-1 -right-1 h-5 w-5 flex items-center justify-center text-xs p-0"
          >
            {unreadCount > 99 ? "99+" : unreadCount}
          </Badge>
        )}
      </Button>

      <NotificationPanel isOpen={isOpen} onClose={() => setIsOpen(false)} />
    </>
  );
};
