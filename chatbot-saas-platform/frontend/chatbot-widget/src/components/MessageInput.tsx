import React, { useState, useRef, useEffect } from "react";
import { useDispatch, useSelector } from "react-redux";
import { useTranslation } from "react-i18next";
import { RootState, AppDispatch } from "../store/store";
import { sendMessage, startConversation, requestHumanAgent } from "../store/slices/chatSlice";
import { trackEvent } from "../store/slices/configSlice";
import { signalRService } from "../services/websocket";
import { Send, Mic, MicOff, User } from "lucide-react";
import { addMessage } from "../store/slices/chatSlice";

export const MessageInput: React.FC = () => {
  const { t, i18n } = useTranslation();
  const dispatch = useDispatch<AppDispatch>();
  const inputRef = useRef<HTMLTextAreaElement>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const [message, setMessage] = useState("");
  const [isRecording, setIsRecording] = useState(false);
  const [typingTimeout, setTypingTimeout] = useState<NodeJS.Timeout | null>(null);

  const { currentConversation, isLoading, connectionStatus } = useSelector((state: RootState) => state.chat);
  const { branding } = useSelector((state: RootState) => state.theme);
  const { widget } = useSelector((state: RootState) => state.config);
  console.log(branding, "branding");

  const isConnected = connectionStatus === "connected";
  const canSendMessage = isConnected && !isLoading && message.trim().length > 0;
  const maxLength = widget.behavior.maxMessageLength;

  // Effect to handle language changes
  useEffect(() => {
    if (widget.language && i18n.language !== widget.language) {
      i18n.changeLanguage(widget.language);
    }
  }, [widget.language, i18n]);

  useEffect(() => {
    if (inputRef.current) {
      inputRef.current.focus();
    }
  }, []);

  const handleInputChange = (e: React.ChangeEvent<HTMLTextAreaElement>) => {
    const value = e.target.value;

    if (value.length <= maxLength) {
      setMessage(value);

      if (value.length > 0) {
        const conversationId = currentConversation?.id;
        if (conversationId) {
          signalRService.sendTyping(conversationId, true);
        }

        if (typingTimeout) {
          clearTimeout(typingTimeout);
        }

        const timeout = setTimeout(() => {
          if (conversationId) {
            signalRService.sendTyping(conversationId, false);
          }
        }, 1000);

        setTypingTimeout(timeout);
      } else {
        const conversationId = currentConversation?.id;
        if (conversationId) {
          signalRService.sendTyping(conversationId, false);
        }
        if (typingTimeout) {
          clearTimeout(typingTimeout);
          setTypingTimeout(null);
        }
      }
    }
  };

  const handleKeyDown = (e: React.KeyboardEvent<HTMLTextAreaElement>) => {
    if (e.key === "Enter" && !e.shiftKey) {
      e.preventDefault();
      handleSendMessage();
    }
  };

  const handleSendMessage = async () => {
    if (!canSendMessage) return;

    const messageContent = message.trim();
    if (!messageContent) return;
    setMessage("");

    // Generate a temporary ID for the user message
    const tempUserMessageId = `temp-user-${Date.now()}`;

    // Optimistically add the user message to the UI
    dispatch(
      addMessage({
        id: tempUserMessageId,
        sender: "user",
        content: messageContent,
        type: "text",
        timestamp: new Date().toISOString(),
        // pending: false, // User messages are not pending
      })
    );

    try {
      // Ensure conversation exists (start if needed)
      let conversation = currentConversation;
      if (!conversation && widget.tenantId) {
        conversation = await dispatch(startConversation(widget.tenantId)).unwrap();
      }

      // Send the user message to the API
      await dispatch(sendMessage({ content: messageContent, type: "text" })).unwrap();

      // Optionally track the event
      dispatch(
        trackEvent({
          event: "message_sent",
          data: { type: "text", length: messageContent.length },
        })
      );
    } catch (err) {
      console.error("Failed to send message:", err);

      // Optionally handle errors (e.g., show a retry button or mark the message as failed)
    }
  };

  const handleFileUpload = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    if (file.size > widget.behavior.maxFileSize) {
      alert(t("errors.fileTooLarge", { maxSize: widget.behavior.maxFileSize / 1024 / 1024 }));
      return;
    }

    const isAllowed = widget.behavior.allowedFileTypes.some((type) => {
      if (type.startsWith(".")) {
        return file.name.toLowerCase().endsWith(type.toLowerCase());
      }
      return file.type.match(type);
    });

    if (!isAllowed) {
      alert(t("errors.fileTypeNotAllowed"));
      return;
    }

    console.log("Uploading file:", file);
    dispatch(
      trackEvent({
        event: "file_upload_started",
        data: { fileName: file.name, fileSize: file.size, fileType: file.type },
      })
    );
  };

  const handleRequestAgent = async () => {
    try {
      let conversation = currentConversation;
      if (!conversation && widget.tenantId) {
        conversation = await dispatch(startConversation(widget.tenantId)).unwrap();
      }

      if (conversation) {
        await dispatch(requestHumanAgent(conversation.id)).unwrap();
        await signalRService.requestAgent();

        dispatch(trackEvent({ event: "agent_requested" }));
      }
    } catch (error) {
      console.error("Failed to request agent:", error);
    }
  };

  const handleVoiceRecord = () => {
    if (isRecording) {
      setIsRecording(false);
      dispatch(trackEvent({ event: "voice_recording_stopped" }));
    } else {
      setIsRecording(true);
      dispatch(trackEvent({ event: "voice_recording_started" }));
    }
  };

  const adjustTextareaHeight = () => {
    if (inputRef.current) {
      inputRef.current.style.height = "auto";
      inputRef.current.style.height = `${Math.min(inputRef.current.scrollHeight, 120)}px`;
    }
  };

  useEffect(() => {
    adjustTextareaHeight();
  }, [message]);

  return (
    <div className="message-input-container">
      {widget.features.agentHandoff && !currentConversation?.assignedAgent && (
        <div className="input-actions-top">
          <button className="action-button agent-request" onClick={handleRequestAgent} disabled={!isConnected} title={t("widget.requestAgent")}>
            <User size={16} />
            <span>{t("widget.requestAgent")}</span>
          </button>
        </div>
      )}

      <div className="message-input-wrapper">
        <div className="input-container">
          {/* {widget.features.fileUpload && (
            <button
              className="input-action-button"
              onClick={() => fileInputRef.current?.click()}
              disabled={!isConnected}
              aria-label={t("widget.attachFile")}
              title={t("widget.attachFile")}
            >
              <Paperclip size={18} />
            </button>
          )} */}

          <textarea
            ref={inputRef}
            value={message}
            onChange={handleInputChange}
            onKeyDown={handleKeyDown}
            placeholder={t("widget.placeholder")}
            className="message-textarea"
            style={{
              outline: "none",
              boxShadow: "none",
              border: "none",
              direction: widget.language === "ar" ? "rtl" : "ltr",
              textAlign: widget.language === "ar" ? "right" : "left",
            }}
            disabled={!isConnected}
            maxLength={maxLength}
            rows={1}
            aria-label={t("accessibility.messageInput")}
          />

          <div className="input-actions">
            {widget.features.voiceMessages && (
              <button
                className={`input-action-button voice-button ${isRecording ? "recording" : ""}`}
                onClick={handleVoiceRecord}
                disabled={!isConnected}
                aria-label={isRecording ? t("widget.stopRecording") : t("widget.recordVoice")}
                title={isRecording ? t("widget.stopRecording") : t("widget.recordVoice")}
              >
                {isRecording ? <MicOff size={18} /> : <Mic size={18} />}
              </button>
            )}

            <button
              className="send-button"
              onClick={handleSendMessage}
              disabled={!canSendMessage}
              aria-label={t("widget.sendMessage")}
              title={t("widget.sendMessage")}
            >
              <Send size={18} />
            </button>
          </div>
        </div>

        {message.length > 0 && (
          <div className="character-count">
            <span className={message.length > maxLength * 0.9 ? "warning" : ""}>
              {message.length}/{maxLength}
            </span>
          </div>
        )}
      </div>

      <input
        ref={fileInputRef}
        type="file"
        onChange={handleFileUpload}
        accept={widget.behavior.allowedFileTypes.join(",")}
        style={{ display: "none" }}
      />
    </div>
  );
};
