import { useEffect, useState } from "react";
import { useDispatch, useSelector } from "react-redux";
import { useTranslation } from "react-i18next";
import { AppDispatch, RootState } from "../../store/store";
import {
  fetchConversations,
  fetchConversation,
  sendMessage,
  updateConversationStatus,
  setConversationSignalRStatus,
  assignConversationRealtime,
  transferConversationRealtime,
  addMessage,
  addNewConversation,
  setTypingUser,
  removeTypingUser,
  markMessageAsRead,
} from "../../store/slices/conversationSlice";
import { setSelectedConversation } from "../../store/slices/selectedConversationSlice";
import { agentSignalRService } from "../../services/signalr";
import { Card, CardContent, CardHeader, CardTitle } from "../../components/ui/card";
import { Button } from "../../components/ui/button";
import { Input } from "../../components/ui/input";
import { Badge } from "../../components/ui/badge";
import { Avatar, AvatarFallback, AvatarImage } from "../../components/ui/avatar";
import { Skeleton } from "../../components/ui/skeleton";
import { Textarea } from "../../components/ui/textarea";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "../../components/ui/select";
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle, DialogTrigger } from "../../components/ui/dialog";
import { MessageSquare, Send, Phone, Video, MoreHorizontal, Search, Filter, Paperclip, Smile } from "lucide-react";
import { TypingIndicator } from "../../components/TypingIndicator";

const ConversationsPage = () => {
  const { t } = useTranslation();
  const dispatch = useDispatch<AppDispatch>();
  const { conversations, isLoading, isSignalRConnected, typingUsers } = useSelector((state: RootState) => state.conversations);
  const { conversationId } = useSelector((state: RootState) => state.selectedConversation);
  const { currentAgent } = useSelector((state: RootState) => state.agent);

  const [searchTerm, setSearchTerm] = useState("");
  const [statusFilter, setStatusFilter] = useState("all");
  const [messageText, setMessageText] = useState("");
  const [isTransferDialogOpen, setIsTransferDialogOpen] = useState(false);
  const [previousConversationId, setPreviousConversationId] = useState<string | null>(null);
  const [typingTimeout, setTypingTimeout] = useState<NodeJS.Timeout | null>(null);

  // Get the actual selected conversation object
  const selectedConversation = conversations?.find((conv) => conv.id === conversationId) || null;

  useEffect(() => {
    dispatch(fetchConversations());

    const signalRConnected = agentSignalRService.getConnectionStatus();
    dispatch(setConversationSignalRStatus(signalRConnected));

    if (signalRConnected) {
      agentSignalRService.setOnConversationAssigned((assignment: any) => {
        console.log("ConversationAssigned event received:", assignment);
        dispatch(assignConversationRealtime(assignment));
        if (assignment.conversationId || assignment.ConversationId) {
          const convId = assignment.conversationId || assignment.ConversationId;
          dispatch(fetchConversation(convId));
          
          // Automatically join the conversation group to receive real-time events
          if (agentSignalRService.getConnectionStatus()) {
            console.log("Auto-joining conversation group after assignment:", convId);
            agentSignalRService.joinConversation(convId);
          }
        }
      });

      agentSignalRService.setOnConversationTransferred((transfer: any) => {
        dispatch(transferConversationRealtime(transfer));
      });

      agentSignalRService.setOnMessageReceived((messageDto: any) => {
        console.log("🔥 Live Agent: MessageReceived event received:", messageDto);
        console.log("🔥 Live Agent: Message sender:", messageDto.sender);
        console.log("🔥 Live Agent: Message conversation ID:", messageDto.conversationId);
        
        // Transform the message format to match the live agent interface expectations
        const transformedMessage = {
          id: messageDto.id,
          conversationId: messageDto.conversationId,
          content: messageDto.content,
          sender: messageDto.sender?.toLowerCase() as "customer" | "agent" | "system", // Convert to lowercase
          timestamp: messageDto.createdAt || new Date().toISOString(), // Use createdAt as timestamp
          type: messageDto.type?.toLowerCase() as "text" | "file" | "image" | "system", // Convert to lowercase
          isRead: false, // New messages are unread by default
        };
        
        console.log("✅ Live Agent: Transformed message:", transformedMessage);
        // Add message to the store - the conversationSlice will handle routing it to the correct conversation
        dispatch(addMessage(transformedMessage));
      });

      // Set up typing indicator handlers
      agentSignalRService.setOnUserStartedTyping((typingInfo) => {
        console.log("🔄 Live Agent: Customer started typing:", typingInfo);
        if (typingInfo.conversationId === conversationId) {
          dispatch(setTypingUser({
            conversationId: typingInfo.conversationId,
            userId: typingInfo.userId,
            userName: typingInfo.userName || "Customer"
          }));
        }
      });

      agentSignalRService.setOnUserStoppedTyping((typingInfo) => {
        console.log("🔄 Live Agent: Customer stopped typing:", typingInfo);
        if (typingInfo.conversationId === conversationId) {
          dispatch(removeTypingUser(typingInfo.conversationId));
        }
      });

      // Set up message read status handler
      agentSignalRService.setOnMessageMarkedAsRead((readInfo) => {
        console.log("🔄 Live Agent: MessageMarkedAsRead event received:", readInfo);
        dispatch(markMessageAsRead({ messageId: readInfo.messageId }));
      });
    }
  }, [dispatch]); // Remove conversationId from dependency array to prevent re-fetching all conversations

  // Effect to fetch conversation details when a conversation is selected
  useEffect(() => {
    if (conversationId) {
      console.log("🔍 Live Agent: Fetching conversation details for selected conversation:", conversationId);
      dispatch(fetchConversation(conversationId));
    }
  }, [conversationId, dispatch]);

  useEffect(() => {
    if (conversationId && conversationId !== previousConversationId) {
      if (previousConversationId && agentSignalRService.getConnectionStatus()) {
        agentSignalRService.leaveConversation(previousConversationId);
      }

      if (agentSignalRService.getConnectionStatus()) {
        agentSignalRService.joinConversation(conversationId);
      }

      setPreviousConversationId(conversationId);
    }
  }, [conversationId, previousConversationId]);

  const filteredConversations = Array.isArray(conversations)
    ? conversations.filter((conversation) => {
        const matchesSearch =
          conversation.customer?.name?.toLowerCase().includes(searchTerm.toLowerCase()) ||
          (conversation.messages?.length > 0 &&
            conversation.messages[conversation.messages.length - 1]?.content?.toLowerCase().includes(searchTerm.toLowerCase()));
        const matchesStatus = statusFilter === "all" || conversation.status === statusFilter;
        return matchesSearch && matchesStatus;
      })
    : [];

  const handleSendMessage = () => {
    if (messageText.trim() && selectedConversation) {
      // Stop typing indicator
      if (typingTimeout) {
        clearTimeout(typingTimeout);
        setTypingTimeout(null);
      }
      if (conversationId) {
        agentSignalRService.stopTyping(conversationId);
      }
      
      dispatch(
        sendMessage({
          conversationId: selectedConversation.id,
          content: messageText,
          type: "text",
        })
      );
      setMessageText("");
    }
  };

  const handleStatusChange = (conversationId: string, status: "waiting" | "active" | "resolved" | "closed") => {
    dispatch(updateConversationStatus({ conversationId, status }));
  };

  const handleAcceptConversation = (conversationId: string) => {
    if (isSignalRConnected && currentAgent?.id) {
      agentSignalRService.acceptConversation(conversationId);
    }
  };

  const handleTransferConversation = (conversationId: string, targetAgentId: string, reason: string) => {
    if (isSignalRConnected) {
      agentSignalRService.transferConversation(conversationId, targetAgentId, reason);
    }
  };

  const handleTyping = (text: string) => {
    setMessageText(text);
    
    if (conversationId) {
      // Send typing indicator when user starts typing
      if (text.length > 0) {
        agentSignalRService.startTyping(conversationId);
      }
      
      // Clear existing timeout
      if (typingTimeout) {
        clearTimeout(typingTimeout);
      }
      
      // Set timeout to stop typing after 3 seconds of inactivity
      const timeout = setTimeout(() => {
        agentSignalRService.stopTyping(conversationId);
        setTypingTimeout(null);
      }, 3000);
      
      setTypingTimeout(timeout);
    }
  };

  const getStatusBadge = (status: string | undefined | null) => {
    // Handle undefined/null status
    if (!status) {
      return <Badge variant="secondary">{t("conversation.status.unknown")}</Badge>;
    }

    // Normalize status to lowercase for consistent matching
    const normalizedStatus = status.toLowerCase();

    // Debug - add this temporarily to see what values you're getting
    console.log("Status value:", status, "Normalized:", normalizedStatus);

    // Define variant mappings - add any variations your API might return
    const variants: Record<string, "default" | "secondary" | "destructive"> = {
      active: "default",
      waiting: "secondary",
      resolved: "default",
      escalated: "destructive",

      // Common variations
      open: "default",
      new: "secondary",
      pending: "secondary",
      closed: "default",
      completed: "default",

      // Add numeric mappings if needed
      "1": "default", // typically active/open
      "2": "secondary", // typically waiting/pending
      "3": "default", // typically resolved/closed
      "4": "destructive", // typically escalated
    };

    // Get variant with fallback
    const variant = variants[normalizedStatus] || "secondary";

    // Return the badge with translation
    return (
      <Badge variant={variant}>
        {t(`conversation.status.${normalizedStatus}`, {
          // Fallback if translation key doesn't exist
          defaultValue: status,
        })}
      </Badge>
    );
  };
  // Update your formatToLocalTime function with this more direct approach
  const formatToLocalTime = (timestamp: string | number | Date) => {
    if (!timestamp) return "";

    try {
      // Log raw input for debugging
      console.log("Raw timestamp input:", timestamp);

      // Create a Date object from the timestamp
      // For ISO strings (2025-08-25T07:20:11) this will be interpreted as UTC
      const date = new Date(timestamp);

      // Explicitly format using the user's local timezone
      const options: Intl.DateTimeFormatOptions = {
        hour: "2-digit",
        minute: "2-digit",
        hour12: true,
        timeZone: Intl.DateTimeFormat().resolvedOptions().timeZone, // Get user's timezone
      };

      // Format the time
      const localTime = new Intl.DateTimeFormat(undefined, options).format(date);

      // Log for comparison
      console.log(`Original time: ${timestamp}`);
      console.log(`Local time: ${localTime}`);
      console.log(`User timezone: ${Intl.DateTimeFormat().resolvedOptions().timeZone}`);

      return localTime;
    } catch (e) {
      console.error("Error formatting time:", e);
      return String(timestamp);
    }
  };

  const getPriorityColor = (priority: string) => {
    const colors: Record<string, string> = {
      high: "text-red-500",
      medium: "text-yellow-500",
      low: "text-green-500",
    };
    return colors[priority] || "text-gray-500";
  };

  if (isLoading && !conversations) {
    return (
      <div className="space-y-6">
        <div className="flex items-center justify-between">
          <Skeleton className="h-8 w-[200px]" />
          <Skeleton className="h-10 w-[120px]" />
        </div>
        <div className="grid gap-4 lg:grid-cols-3">
          <div className="space-y-3">
            {[...Array(5)].map((_, i) => (
              <Skeleton key={i} className="h-20 w-full" />
            ))}
          </div>
          <div className="lg:col-span-2">
            <Skeleton className="h-96 w-full" />
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">{t("conversations.title")}</h1>
          <p className="text-muted-foreground">{t("conversations.subtitle")}</p>
        </div>
        <div className="flex items-center space-x-4">
          <div className="flex items-center space-x-1">
            <div className={`w-2 h-2 rounded-full ${isSignalRConnected ? "bg-green-500" : "bg-red-500"}`} />
            <span className="text-xs text-muted-foreground">
              {isSignalRConnected ? t("conversations.liveUpdates") : t("conversations.staticData")}
            </span>
          </div>
          <div className="flex items-center space-x-2">
            <Badge variant="outline">
              {filteredConversations.length} {t("conversations.total")}
            </Badge>
            <Button variant="outline">
              <Filter className="mr-2 h-4 w-4" />
              {t("conversations.filters")}
            </Button>
          </div>
        </div>
      </div>

      <div className="grid gap-4 lg:grid-cols-3">
        {/* Conversations List */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center">
              <MessageSquare className="mr-2 h-5 w-5" />
              {t("conversations.list")}
            </CardTitle>
            <div className="flex space-x-2">
              <div className="relative flex-1">
                <Search className="absolute left-2 top-2.5 h-4 w-4 text-muted-foreground" />
                <Input
                  placeholder={t("conversations.searchPlaceholder")}
                  value={searchTerm}
                  onChange={(e) => setSearchTerm(e.target.value)}
                  className="pl-8"
                />
              </div>
              <Select value={statusFilter} onValueChange={setStatusFilter}>
                <SelectTrigger className="w-32">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">{t("conversations.allStatus")}</SelectItem>
                  <SelectItem value="active">{t("conversations.active")}</SelectItem>
                  <SelectItem value="waiting">{t("conversations.waiting")}</SelectItem>
                  <SelectItem value="resolved">{t("conversations.resolved")}</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </CardHeader>
          <CardContent className="p-0">
            <div className="max-h-96 overflow-y-auto">
              {filteredConversations.length > 0 ? (
                filteredConversations.map((conversation: any) => (
                  <div
                    key={conversation.id}
                    className={`p-4 border-b cursor-pointer hover:bg-accent ${selectedConversation?.id === conversation.id ? "bg-accent" : ""}`}
                    onClick={() => dispatch(setSelectedConversation(conversation.id))}
                  >
                    <div className="flex items-start justify-between">
                      <div className="flex items-start space-x-3">
                        <Avatar className="h-10 w-10">
                          <AvatarImage src={conversation.customer?.avatar} />
                          <AvatarFallback>{conversation.customer?.name?.charAt(0) || "U"}</AvatarFallback>
                        </Avatar>
                        <div className="flex-1 min-w-0">
                          <div className="flex items-center space-x-2">
                            <p className="font-medium truncate">{conversation.customer?.name || t("conversations.anonymous")}</p>
                            <div className={`w-2 h-2 rounded-full ${getPriorityColor(conversation.priority || "low")}`} />
                          </div>
                          <p className="text-sm text-muted-foreground truncate">
                            {conversation.messages?.length > 0
                              ? conversation.messages[conversation.messages.length - 1]?.content
                              : t("conversations.noMessagesYet")}
                          </p>
                          <p className="text-sm text-muted-foreground truncate">
                            {conversation.lastMessage?.content || t("conversations.noMessagesYet")}
                          </p>
                          <div className="flex items-center space-x-2 mt-1">
                            {getStatusBadge(conversation.status)}
                            {/* <span>{new Date(conversation.updatedAt).toLocaleTimeString()}</span> */}
                            <span className="text-xs text-muted-foreground">{formatToLocalTime(conversation.updatedAt)}</span>
                          </div>
                        </div>
                      </div>
                      {conversation.unreadCount && conversation.unreadCount > 0 && (
                        <Badge variant="destructive" className="text-xs">
                          {conversation.unreadCount}
                        </Badge>
                      )}
                    </div>
                  </div>
                ))
              ) : (
                <div className="text-center py-8">
                  <MessageSquare className="mx-auto h-12 w-12 text-muted-foreground" />
                  <h3 className="mt-2 text-sm font-semibold">{t("conversations.noConversationsFound")}</h3>
                  <p className="mt-1 text-sm text-muted-foreground">{t("conversations.noConversationsFoundDesc")}</p>
                </div>
              )}
            </div>
          </CardContent>
        </Card>

        {/* Chat Interface */}
        <div className="lg:col-span-2">
          {selectedConversation ? (
            <Card className="h-full">
              <CardHeader className="border-b">
                <div className="flex items-center justify-between">
                  <div className="flex items-center space-x-3">
                    <Avatar className="h-10 w-10">
                      <AvatarImage src={selectedConversation.customer?.avatar} />
                      <AvatarFallback>{selectedConversation.customer?.name?.charAt(0) || "U"}</AvatarFallback>
                    </Avatar>
                    <div>
                      <h3 className="font-semibold">{selectedConversation.customer?.name || t("conversations.anonymous")}</h3>
                      <p className="text-sm text-muted-foreground">{selectedConversation.customer?.email || t("conversations.noEmail")}</p>
                    </div>
                    {getStatusBadge(selectedConversation.status)}
                  </div>
                  <div className="flex items-center space-x-2">
                    <Button variant="ghost" size="sm">
                      <Phone className="h-4 w-4" />
                    </Button>
                    <Button variant="ghost" size="sm">
                      <Video className="h-4 w-4" />
                    </Button>
                    <Select
                      value={selectedConversation.status}
                      onValueChange={(value) => handleStatusChange(selectedConversation.id, value as "waiting" | "active" | "resolved" | "closed")}
                    >
                      <SelectTrigger className="w-32">
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                        <SelectItem value="active">{t("conversations.active")}</SelectItem>
                        <SelectItem value="waiting">{t("conversations.waiting")}</SelectItem>
                        <SelectItem value="resolved">{t("conversations.resolved")}</SelectItem>
                        <SelectItem value="escalated">{t("conversations.escalated")}</SelectItem>
                      </SelectContent>
                    </Select>
                    <Dialog open={isTransferDialogOpen} onOpenChange={setIsTransferDialogOpen}>
                      <DialogTrigger asChild>
                        <Button variant="ghost" size="sm">
                          <MoreHorizontal className="h-4 w-4" />
                        </Button>
                      </DialogTrigger>
                      <DialogContent>
                        <DialogHeader>
                          <DialogTitle>{t("conversations.transferConversation")}</DialogTitle>
                          <DialogDescription>{t("conversations.transferConversationDesc")}</DialogDescription>
                        </DialogHeader>
                        <div className="space-y-4">
                          <Select>
                            <SelectTrigger>
                              <SelectValue placeholder={t("conversations.selectAgent")} />
                            </SelectTrigger>
                            <SelectContent>
                              <SelectItem value="agent1">{t("conversations.agentJohnDoe")}</SelectItem>
                              <SelectItem value="agent2">{t("conversations.agentJaneSmith")}</SelectItem>
                              <SelectItem value="agent3">{t("conversations.agentMikeJohnson")}</SelectItem>
                            </SelectContent>
                          </Select>
                          <Textarea placeholder={t("conversations.transferNote")} rows={3} />
                        </div>
                        <DialogFooter>
                          <Button onClick={() => setIsTransferDialogOpen(false)}>{t("conversations.transfer")}</Button>
                        </DialogFooter>
                      </DialogContent>
                    </Dialog>
                  </div>
                </div>
              </CardHeader>

              <CardContent className="flex flex-col h-96">
                {/* Messages */}
                <div className="flex-1 overflow-y-auto p-4 space-y-4">
                  {selectedConversation.messages && selectedConversation.messages.length > 0 ? (
                    <>
                      {console.log("🔍 Live Agent: Displaying messages:", selectedConversation.messages)}
                      {selectedConversation.messages.map((message: any) => (
                        <div key={message.id} className={`flex ${message.sender === "agent" ? "justify-end" : "justify-start"}`}>
                          <div
                            className={`max-w-xs lg:max-w-md px-4 py-2 rounded-lg ${
                              message.sender === "agent" ? "bg-primary text-primary-foreground" : "bg-muted"
                            }`}
                          >
                            <p className="text-sm">{message.content}</p>
                            <p className="text-xs opacity-70 mt-1">{new Date(message.timestamp).toLocaleTimeString()}</p>
                          </div>
                        </div>
                      ))}
                      
                      {/* Typing Indicator */}
                      {conversationId && typingUsers[conversationId] && (
                        <TypingIndicator userName={typingUsers[conversationId].userName} />
                      )}
                    </>
                  ) : (
                    <div className="text-center py-8">
                      <MessageSquare className="mx-auto h-12 w-12 text-muted-foreground" />
                      <h3 className="mt-2 text-sm font-semibold">{t("conversations.noMessagesYet")}</h3>
                      <p className="mt-1 text-sm text-muted-foreground">{t("conversations.startConversation")}</p>
                    </div>
                  )}
                </div>

                {/* Message Input */}
                <div className="border-t p-4">
                  <div className="flex items-end space-x-2">
                    <div className="flex-1">
                      <Textarea
                        placeholder={t("conversations.typeMessage")}
                        value={messageText}
                        onChange={(e) => handleTyping(e.target.value)}
                        rows={2}
                        onKeyPress={(e) => {
                          if (e.key === "Enter" && !e.shiftKey) {
                            e.preventDefault();
                            handleSendMessage();
                          }
                        }}
                      />
                    </div>
                    <div className="flex flex-col space-y-2">
                      <Button variant="ghost" size="sm">
                        <Paperclip className="h-4 w-4" />
                      </Button>
                      <Button variant="ghost" size="sm">
                        <Smile className="h-4 w-4" />
                      </Button>
                      <Button onClick={handleSendMessage} disabled={!messageText.trim()}>
                        <Send className="h-4 w-4" />
                      </Button>
                    </div>
                  </div>
                </div>
              </CardContent>
            </Card>
          ) : (
            <Card className="h-full flex items-center justify-center">
              <div className="text-center">
                <MessageSquare className="mx-auto h-12 w-12 text-muted-foreground" />
                <h3 className="mt-2 text-lg font-semibold">{t("conversations.selectConversation")}</h3>
                <p className="mt-1 text-sm text-muted-foreground">{t("conversations.selectConversationDesc")}</p>
              </div>
            </Card>
          )}
        </div>
      </div>
    </div>
  );
};

export default ConversationsPage;
