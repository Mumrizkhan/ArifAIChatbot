import React, { useEffect, useRef } from "react";
import { useDispatch, useSelector } from "react-redux";
import { useTranslation } from "react-i18next";
import { RootState, AppDispatch } from "../store/store";
import { toggleChat, minimizeChat, maximizeChat, closeChat, markAsRead, startConversation } from "../store/slices/chatSlice";
import { trackEvent } from "../store/slices/configSlice";
import { ChatHeader } from "./ChatHeader";
import { MessageList } from "./MessageList";
import { MessageInput } from "./MessageInput";
import { ChatButton } from "./ChatButton";
import { ConnectionStatus } from "./ConnectionStatus";
import { signalRService } from "../services/websocket";
import { useTranslatedTheme } from "../hooks/useTranslatedTheme";
import "../styles/widget.css";

export const ChatWidget: React.FC = () => {
  const { t, i18n } = useTranslation();
  const dispatch = useDispatch<AppDispatch>();
  const widgetRef = useRef<HTMLDivElement>(null);

  // Use the translation hook to automatically update theme with translations
  useTranslatedTheme();

  const { isOpen, isMinimized, unreadCount, connectionStatus, currentConversation } = useSelector((state: RootState) => state.chat);
  const { config, isRTL, language } = useSelector((state: RootState) => state.theme);
  const { widget, isInitialized } = useSelector((state: RootState) => state.config);

  useEffect(() => {
    if (isInitialized && widget.tenantId) {
      dispatch(trackEvent({ event: "widget_initialized" }));
    }

    return () => {
      signalRService.disconnect();
    };
  }, [isInitialized, widget.tenantId, dispatch]);

  useEffect(() => {
    i18n.changeLanguage(language);
    document.documentElement.dir = isRTL ? "rtl" : "ltr";
  }, [language, isRTL, i18n]);

  useEffect(() => {
    if (isOpen && unreadCount > 0) {
      dispatch(markAsRead());
    }
  }, [isOpen, unreadCount, dispatch]);

  useEffect(() => {
    if (widget.behavior.autoOpen && !isOpen) {
      const timer = setTimeout(() => {
        dispatch(toggleChat());
        dispatch(trackEvent({ event: "widget_auto_opened" }));
      }, widget.behavior.autoOpenDelay);

      return () => clearTimeout(timer);
    }
  }, [widget.behavior.autoOpen, widget.behavior.autoOpenDelay, isOpen, dispatch]);

  // Create conversation when chat is opened for the first time
  useEffect(() => {
    console.log("🔍 Chat conversation creation check:", {
      isOpen,
      currentConversation: currentConversation?.id,
      isInitialized,
      tenantId: widget.tenantId,
      shouldCreate: isOpen && !currentConversation && isInitialized && widget.tenantId,
    });

    if (isOpen && !currentConversation && isInitialized && widget.tenantId) {
      console.log("🚀 Chat opened - creating conversation for tenant:", widget.tenantId);
      dispatch(startConversation(widget.tenantId));
    }
  }, [isOpen, currentConversation, isInitialized, widget.tenantId, dispatch]);

  const handleToggleChat = () => {
    console.log("🎯 Toggle chat clicked - current state:", { isOpen, currentConversation: currentConversation?.id });
    dispatch(toggleChat());
    dispatch(
      trackEvent({
        event: isOpen ? "widget_closed" : "widget_opened",
        data: { source: "user_action" },
      })
    );
  };

  const handleMinimize = () => {
    dispatch(minimizeChat());
    dispatch(trackEvent({ event: "widget_minimized" }));
  };

  const handleMaximize = () => {
    dispatch(maximizeChat());
    dispatch(trackEvent({ event: "widget_maximized" }));
  };

  const handleClose = () => {
    dispatch(closeChat());
    dispatch(trackEvent({ event: "widget_closed", data: { source: "close_button" } }));
  };

  const getWidgetClasses = () => {
    const baseClasses = "chat-widget";
    const positionClasses = {
      "bottom-right": "bottom-4 right-4",
      "bottom-left": "bottom-4 left-4",
      "top-right": "top-4 right-4",
      "top-left": "top-4 left-4",
    };

    const sizeClasses = {
      small: "w-80 h-96",
      medium: "w-96 h-[500px]",
      large: "w-[420px] h-[600px]",
    };

    const animationClasses = {
      slide: "animate-slide-up",
      fade: "animate-fade-in",
      bounce: "animate-bounce-in",
      none: "",
    };

    return [
      baseClasses,
      positionClasses[config.position],
      isOpen ? sizeClasses[config.size] : "w-auto h-auto",
      isOpen && !isMinimized ? animationClasses[config.animation] : "",
      isRTL ? "rtl" : "ltr",
    ]
      .filter(Boolean)
      .join(" ");
  };

  const getWidgetStyles = () =>
    ({
      "--primary-color": config.primaryColor,
      "--secondary-color": config.secondaryColor,
      "--background-color": config.backgroundColor,
      "--text-color": config.textColor,
      "--border-radius": config.borderRadius,
      "--font-family": config.fontFamily,
      "--font-size": config.fontSize,
      "--header-color": config.headerColor,
      "--header-text-color": config.headerTextColor,
      "--user-message-color": config.userMessageColor,
      "--bot-message-color": config.botMessageColor,
      "--shadow-color": config.shadowColor,
      position: "fixed",
      bottom: config.position.includes("bottom") ? "1rem" : "auto",
    } as React.CSSProperties);

  if (!isInitialized) {
    return null;
  }

  return (
    <div
      ref={widgetRef}
      className={getWidgetClasses()}
      style={getWidgetStyles()}
      role="dialog"
      aria-label={t("accessibility.chatWidget")}
      aria-expanded={isOpen}
    >
      {!isOpen ? (
        <ChatButton onClick={handleToggleChat} unreadCount={unreadCount} />
      ) : (
        <div className="chat-container">
          {!isMinimized && (
            <>
              <ChatHeader onMinimize={handleMinimize} onClose={handleClose} connectionStatus={connectionStatus} />
              <div className="chat-body ">
                <ConnectionStatus status={connectionStatus} />
                <MessageList />
                <MessageInput />
              </div>
            </>
          )}

          {isMinimized && (
            <div
              className="chat-minimized"
              onClick={handleMaximize}
              role="button"
              tabIndex={0}
              aria-label={t("widget.maximize")}
              onKeyDown={(e) => {
                if (e.key === "Enter" || e.key === " ") {
                  handleMaximize();
                }
              }}
            >
              <div className="minimized-header">
                <span className="minimized-title">{t("widget.title")}</span>
                {unreadCount > 0 && <span className="unread-badge">{unreadCount}</span>}
              </div>
            </div>
          )}
        </div>
      )}
    </div>
  );
};
