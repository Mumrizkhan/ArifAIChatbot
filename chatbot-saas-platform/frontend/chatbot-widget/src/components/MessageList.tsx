import React, { useEffect, useRef, useMemo } from "react";
import { useSelector } from "react-redux";
import { useTranslation } from "react-i18next";
import { RootState } from "../store/store";
import { Message } from "../store/slices/chatSlice";
import { MessageBubble } from "./MessageBubble";
import { TypingIndicator } from "./TypingIndicator";
import { IntentButtons } from "./IntentButtons";
import { Bot } from "lucide-react";

export const MessageList: React.FC = () => {
  const { t } = useTranslation();
  const messagesEndRef = useRef<HTMLDivElement>(null);
  const { currentConversation, isTyping, typingUser } = useSelector((state: RootState) => state.chat);
  const { branding } = useSelector((state: RootState) => state.theme);
  console.log(currentConversation);

  const messages = useMemo(() => currentConversation?.messages || [], [currentConversation?.messages]);
  console.log(messages, "rao");

  useEffect(() => {
    scrollToBottom();
  }, [messages, isTyping]);

  const scrollToBottom = () => {
    messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
  };

  const renderMessage = (message: Message, index: number) => {
    const isFirstInGroup = index === 0 || messages[index - 1].sender !== message.sender;
    const isLastInGroup = index === messages.length - 1 || messages[index + 1]?.sender !== message.sender;

    // Use MessageBubble for pending bot message
    return (
      <MessageBubble key={message.id} message={message} isFirstInGroup={isFirstInGroup} isLastInGroup={isLastInGroup} showAvatar={isLastInGroup} />
    );
  };

  const renderEmptyState = () => (
    <div className="message-list-empty">
      <div className="empty-state-avatar">
        {branding.logo ? (
          <img src={branding.logo} alt={branding.companyName || t("widget.title")} className="w-12 h-12 rounded-full" />
        ) : (
          <div className="w-12 h-12 rounded-full bg-primary flex items-center justify-center">
            <Bot size={24} className="text-white" />
          </div>
        )}
      </div>
      <div className="empty-state-content">
        <h3 className="empty-state-title">{branding.companyName || t("widget.title")}</h3>
        <p className="empty-state-message">{branding.welcomeMessage || t("widget.welcomeMessage")}</p>
        <IntentButtons />
      </div>
    </div>
  );

  const renderConversationStart = () => (
    <div className="conversation-start">
      <div className="conversation-start-line">
        <span className="conversation-start-text">{t("widget.startConversation")}</span>
      </div>
    </div>
  );

  return (
    <div className="message-list h-full min-h-0 overflow-y-auto" role="log" aria-label="Message List" aria-live="polite">
      {messages.length === 0 ? (
        renderEmptyState()
      ) : (
        <div className="messages-container">
          {renderConversationStart()}
          {messages.map(renderMessage)}
          {isTyping && typingUser && <TypingIndicator user={typingUser} />}
        </div>
      )}
      <div ref={messagesEndRef} />
    </div>
  );
};
