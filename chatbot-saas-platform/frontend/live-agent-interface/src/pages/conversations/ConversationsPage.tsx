import React, { useEffect, useState } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { useTranslation } from 'react-i18next';
import { AppDispatch, RootState } from '../../store/store';
import { fetchConversations, assignConversation, sendMessage, updateConversationStatus } from '../../store/slices/conversationSlice';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../../components/ui/card';
import { Button } from '../../components/ui/button';
import { Input } from '../../components/ui/input';
import { Badge } from '../../components/ui/badge';
import { Avatar, AvatarFallback, AvatarImage } from '../../components/ui/avatar';
import { Skeleton } from '../../components/ui/skeleton';
import { Textarea } from '../../components/ui/textarea';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '../../components/ui/select';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '../../components/ui/dialog';
import {
  MessageSquare,
  Send,
  Phone,
  Video,
  MoreHorizontal,
  Clock,
  User,
  Search,
  Filter,
  CheckCircle,
  AlertCircle,
  Archive,
  Star,
  Paperclip,
  Smile,
} from 'lucide-react';

const ConversationsPage = () => {
  const { t } = useTranslation();
  const dispatch = useDispatch<AppDispatch>();
  const { conversations, selectedConversation, isLoading } = useSelector((state: RootState) => state.conversation);
  
  const [searchTerm, setSearchTerm] = useState('');
  const [statusFilter, setStatusFilter] = useState('all');
  const [messageText, setMessageText] = useState('');
  const [isTransferDialogOpen, setIsTransferDialogOpen] = useState(false);

  useEffect(() => {
    dispatch(fetchConversations());
  }, [dispatch]);

  const filteredConversations = conversations?.filter(conversation => {
    const matchesSearch = conversation.customer?.name?.toLowerCase().includes(searchTerm.toLowerCase()) ||
                         conversation.lastMessage?.content?.toLowerCase().includes(searchTerm.toLowerCase());
    const matchesStatus = statusFilter === 'all' || conversation.status === statusFilter;
    return matchesSearch && matchesStatus;
  }) || [];

  const handleSendMessage = () => {
    if (messageText.trim() && selectedConversation) {
      dispatch(sendMessage({
        conversationId: selectedConversation.id,
        content: messageText,
        type: 'text'
      }));
      setMessageText('');
    }
  };

  const handleStatusChange = (conversationId: string, status: string) => {
    dispatch(updateConversationStatus({ conversationId, status }));
  };

  const getStatusBadge = (status: string) => {
    const variants: Record<string, 'default' | 'secondary' | 'destructive'> = {
      active: 'default',
      waiting: 'secondary',
      resolved: 'default',
      escalated: 'destructive',
    };
    return (
      <Badge variant={variants[status] || 'secondary'}>
        {t(`conversation.status.${status}`)}
      </Badge>
    );
  };

  const getPriorityColor = (priority: string) => {
    const colors: Record<string, string> = {
      high: 'text-red-500',
      medium: 'text-yellow-500',
      low: 'text-green-500',
    };
    return colors[priority] || 'text-gray-500';
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
          <h1 className="text-3xl font-bold">{t('conversations.title')}</h1>
          <p className="text-muted-foreground">
            {t('conversations.subtitle')}
          </p>
        </div>
        <div className="flex items-center space-x-2">
          <Badge variant="outline">
            {filteredConversations.length} {t('conversations.total')}
          </Badge>
          <Button variant="outline">
            <Filter className="mr-2 h-4 w-4" />
            {t('conversations.filters')}
          </Button>
        </div>
      </div>

      <div className="grid gap-4 lg:grid-cols-3">
        {/* Conversations List */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center">
              <MessageSquare className="mr-2 h-5 w-5" />
              {t('conversations.list')}
            </CardTitle>
            <div className="flex space-x-2">
              <div className="relative flex-1">
                <Search className="absolute left-2 top-2.5 h-4 w-4 text-muted-foreground" />
                <Input
                  placeholder={t('conversations.searchPlaceholder')}
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
                  <SelectItem value="all">{t('conversations.allStatus')}</SelectItem>
                  <SelectItem value="active">{t('conversations.active')}</SelectItem>
                  <SelectItem value="waiting">{t('conversations.waiting')}</SelectItem>
                  <SelectItem value="resolved">{t('conversations.resolved')}</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </CardHeader>
          <CardContent className="p-0">
            <div className="max-h-96 overflow-y-auto">
              {filteredConversations.length > 0 ? (
                filteredConversations.map((conversation) => (
                  <div
                    key={conversation.id}
                    className={`p-4 border-b cursor-pointer hover:bg-accent ${
                      selectedConversation?.id === conversation.id ? 'bg-accent' : ''
                    }`}
                    onClick={() => dispatch({ type: 'conversation/setSelectedConversation', payload: conversation })}
                  >
                    <div className="flex items-start justify-between">
                      <div className="flex items-start space-x-3">
                        <Avatar className="h-10 w-10">
                          <AvatarImage src={conversation.customer?.avatar} />
                          <AvatarFallback>
                            {conversation.customer?.name?.charAt(0) || 'U'}
                          </AvatarFallback>
                        </Avatar>
                        <div className="flex-1 min-w-0">
                          <div className="flex items-center space-x-2">
                            <p className="font-medium truncate">
                              {conversation.customer?.name || 'Anonymous'}
                            </p>
                            <div className={`w-2 h-2 rounded-full ${getPriorityColor(conversation.priority || 'low')}`} />
                          </div>
                          <p className="text-sm text-muted-foreground truncate">
                            {conversation.lastMessage?.content || 'No messages yet'}
                          </p>
                          <div className="flex items-center space-x-2 mt-1">
                            {getStatusBadge(conversation.status)}
                            <span className="text-xs text-muted-foreground">
                              {new Date(conversation.updatedAt).toLocaleTimeString()}
                            </span>
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
                  <h3 className="mt-2 text-sm font-semibold">No conversations found</h3>
                  <p className="mt-1 text-sm text-muted-foreground">
                    Try adjusting your search or filters.
                  </p>
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
                      <AvatarFallback>
                        {selectedConversation.customer?.name?.charAt(0) || 'U'}
                      </AvatarFallback>
                    </Avatar>
                    <div>
                      <h3 className="font-semibold">
                        {selectedConversation.customer?.name || 'Anonymous'}
                      </h3>
                      <p className="text-sm text-muted-foreground">
                        {selectedConversation.customer?.email || 'No email'}
                      </p>
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
                      onValueChange={(value) => handleStatusChange(selectedConversation.id, value)}
                    >
                      <SelectTrigger className="w-32">
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                        <SelectItem value="active">{t('conversations.active')}</SelectItem>
                        <SelectItem value="waiting">{t('conversations.waiting')}</SelectItem>
                        <SelectItem value="resolved">{t('conversations.resolved')}</SelectItem>
                        <SelectItem value="escalated">{t('conversations.escalated')}</SelectItem>
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
                          <DialogTitle>{t('conversations.transferConversation')}</DialogTitle>
                          <DialogDescription>
                            {t('conversations.transferConversationDesc')}
                          </DialogDescription>
                        </DialogHeader>
                        <div className="space-y-4">
                          <Select>
                            <SelectTrigger>
                              <SelectValue placeholder={t('conversations.selectAgent')} />
                            </SelectTrigger>
                            <SelectContent>
                              <SelectItem value="agent1">John Doe</SelectItem>
                              <SelectItem value="agent2">Jane Smith</SelectItem>
                              <SelectItem value="agent3">Mike Johnson</SelectItem>
                            </SelectContent>
                          </Select>
                          <Textarea
                            placeholder={t('conversations.transferNote')}
                            rows={3}
                          />
                        </div>
                        <DialogFooter>
                          <Button onClick={() => setIsTransferDialogOpen(false)}>
                            {t('conversations.transfer')}
                          </Button>
                        </DialogFooter>
                      </DialogContent>
                    </Dialog>
                  </div>
                </div>
              </CardHeader>

              <CardContent className="flex flex-col h-96">
                {/* Messages */}
                <div className="flex-1 overflow-y-auto p-4 space-y-4">
                  {selectedConversation.messages?.map((message) => (
                    <div
                      key={message.id}
                      className={`flex ${message.sender === 'agent' ? 'justify-end' : 'justify-start'}`}
                    >
                      <div
                        className={`max-w-xs lg:max-w-md px-4 py-2 rounded-lg ${
                          message.sender === 'agent'
                            ? 'bg-primary text-primary-foreground'
                            : 'bg-muted'
                        }`}
                      >
                        <p className="text-sm">{message.content}</p>
                        <p className="text-xs opacity-70 mt-1">
                          {new Date(message.createdAt).toLocaleTimeString()}
                        </p>
                      </div>
                    </div>
                  )) || (
                    <div className="text-center py-8">
                      <MessageSquare className="mx-auto h-12 w-12 text-muted-foreground" />
                      <h3 className="mt-2 text-sm font-semibold">No messages yet</h3>
                      <p className="mt-1 text-sm text-muted-foreground">
                        Start the conversation by sending a message.
                      </p>
                    </div>
                  )}
                </div>

                {/* Message Input */}
                <div className="border-t p-4">
                  <div className="flex items-end space-x-2">
                    <div className="flex-1">
                      <Textarea
                        placeholder={t('conversations.typeMessage')}
                        value={messageText}
                        onChange={(e) => setMessageText(e.target.value)}
                        rows={2}
                        onKeyPress={(e) => {
                          if (e.key === 'Enter' && !e.shiftKey) {
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
                <h3 className="mt-2 text-lg font-semibold">Select a conversation</h3>
                <p className="mt-1 text-sm text-muted-foreground">
                  Choose a conversation from the list to start chatting.
                </p>
              </div>
            </Card>
          )}
        </div>
      </div>
    </div>
  );
};

export default ConversationsPage;
