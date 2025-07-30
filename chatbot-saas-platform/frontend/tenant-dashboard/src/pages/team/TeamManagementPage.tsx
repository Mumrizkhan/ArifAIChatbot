import { useEffect, useState } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { useTranslation } from 'react-i18next';
import { AppDispatch, RootState } from '../../store/store';
import { fetchTeamMembers, inviteTeamMember, updateTeamMember, removeTeamMember } from '../../store/slices/teamSlice';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../../components/ui/card';
import { Button } from '../../components/ui/button';
import { Input } from '../../components/ui/input';
import { Label } from '../../components/ui/label';
import { Badge } from '../../components/ui/badge';
import { Avatar, AvatarFallback, AvatarImage } from '../../components/ui/avatar';
import { Skeleton } from '../../components/ui/skeleton';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '../../components/ui/table';
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
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '../../components/ui/select';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '../../components/ui/dropdown-menu';
import {
  Plus,
  Search,
  MoreHorizontal,
  Edit,
  Trash2,
  Users,
  UserCheck,
  Mail,
  Star,
  Calendar,
  Shield,
} from 'lucide-react';

const TeamManagementPage = () => {
  const { t } = useTranslation();
  const dispatch = useDispatch<AppDispatch>();
  const { members, invitations, isLoading } = useSelector((state: RootState) => state.team);

  const [searchTerm, setSearchTerm] = useState('');
  const [isInviteDialogOpen, setIsInviteDialogOpen] = useState(false);
  const [editingMember, setEditingMember] = useState<any>(null);
  const [newInvitation, setNewInvitation] = useState({
    email: '',
    role: 'agent',
  });

  useEffect(() => {
    dispatch(fetchTeamMembers());
  }, [dispatch]);

  const handleInviteMember = async () => {
    try {
      await dispatch(inviteTeamMember(newInvitation)).unwrap();
      setIsInviteDialogOpen(false);
      setNewInvitation({ email: '', role: 'agent' });
    } catch (error) {
      console.error('Failed to invite member:', error);
    }
  };

  const handleUpdateMember = async () => {
    if (!editingMember) return;
    try {
      await dispatch(updateTeamMember({ id: editingMember.id, updates: editingMember })).unwrap();
      setEditingMember(null);
    } catch (error) {
      console.error('Failed to update member:', error);
    }
  };

  const handleRemoveMember = async (id: string) => {
    if (window.confirm('Are you sure you want to remove this team member?')) {
      try {
        await dispatch(removeTeamMember(id)).unwrap();
      } catch (error) {
        console.error('Failed to remove member:', error);
      }
    }
  };

  const getStatusBadge = (status: string) => {
    const variants: Record<string, 'default' | 'secondary' | 'destructive'> = {
      online: 'default',
      away: 'secondary',
      busy: 'destructive',
      offline: 'secondary',
    };
    return (
      <Badge variant={variants[status] || 'secondary'}>
        {t(`team.status.${status}`)}
      </Badge>
    );
  };

  const filteredMembers = members?.filter(member =>
    member.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
    member.email.toLowerCase().includes(searchTerm.toLowerCase())
  ) || [];

  if (isLoading && !members) {
    return (
      <div className="space-y-6">
        <div className="flex items-center justify-between">
          <Skeleton className="h-8 w-[200px]" />
          <Skeleton className="h-10 w-[120px]" />
        </div>
        <Card>
          <CardHeader>
            <Skeleton className="h-6 w-[150px]" />
            <Skeleton className="h-4 w-[300px]" />
          </CardHeader>
          <CardContent>
            <div className="space-y-3">
              {[...Array(5)].map((_, i) => (
                <Skeleton key={i} className="h-12 w-full" />
              ))}
            </div>
          </CardContent>
        </Card>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">{t('team.title')}</h1>
          <p className="text-muted-foreground">
            {t('team.subtitle')}
          </p>
        </div>
        <Dialog open={isInviteDialogOpen} onOpenChange={setIsInviteDialogOpen}>
          <DialogTrigger asChild>
            <Button>
              <Plus className="mr-2 h-4 w-4" />
              {t('team.inviteMember')}
            </Button>
          </DialogTrigger>
          <DialogContent>
            <DialogHeader>
              <DialogTitle>{t('team.inviteMember')}</DialogTitle>
              <DialogDescription>
                {t('team.inviteMemberDesc')}
              </DialogDescription>
            </DialogHeader>
            <div className="grid gap-4 py-4">
              <div className="grid grid-cols-4 items-center gap-4">
                <Label htmlFor="email" className="text-right">
                  {t('team.email')}
                </Label>
                <Input
                  id="email"
                  type="email"
                  value={newInvitation.email}
                  onChange={(e) => setNewInvitation({ ...newInvitation, email: e.target.value })}
                  className="col-span-3"
                  placeholder={t('team.emailPlaceholder')}
                />
              </div>
              <div className="grid grid-cols-4 items-center gap-4">
                <Label htmlFor="role" className="text-right">
                  {t('team.role')}
                </Label>
                <Select
                  value={newInvitation.role}
                  onValueChange={(value) => setNewInvitation({ ...newInvitation, role: value })}
                >
                  <SelectTrigger className="col-span-3">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="admin">{t('team.role.admin')}</SelectItem>
                    <SelectItem value="manager">{t('team.role.manager')}</SelectItem>
                    <SelectItem value="agent">{t('team.role.agent')}</SelectItem>
                  </SelectContent>
                </Select>
              </div>
            </div>
            <DialogFooter>
              <Button onClick={handleInviteMember}>{t('team.sendInvitation')}</Button>
            </DialogFooter>
          </DialogContent>
        </Dialog>
      </div>

      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">
              {t('team.totalMembers')}
            </CardTitle>
            <Users className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{members?.length || 0}</div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">
              {t('team.onlineMembers')}
            </CardTitle>
            <UserCheck className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">
              {members?.filter(m => m.status === 'active').length || 0}
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">
              {t('team.pendingInvitations')}
            </CardTitle>
            <Mail className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{invitations?.length || 0}</div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">
              {t('team.avgRating')}
            </CardTitle>
            <Star className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">
              {members?.length ? (members.reduce((acc) => acc + (4.5), 0) / members.length).toFixed(1) : '0.0'}
            </div>
          </CardContent>
        </Card>
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center">
            <Users className="mr-2 h-5 w-5" />
            {t('team.teamMembers')}
          </CardTitle>
          <CardDescription>
            {t('team.teamMembersDesc')}
          </CardDescription>
        </CardHeader>
        <CardContent>
          <div className="flex items-center space-x-2 mb-4">
            <div className="relative flex-1">
              <Search className="absolute left-2 top-2.5 h-4 w-4 text-muted-foreground" />
              <Input
                placeholder={t('team.searchMembers')}
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                className="pl-8"
              />
            </div>
          </div>

          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>{t('team.member')}</TableHead>
                <TableHead>{t('team.role')}</TableHead>
                <TableHead>{t('team.status')}</TableHead>
                <TableHead>{t('team.conversations')}</TableHead>
                <TableHead>{t('team.rating')}</TableHead>
                <TableHead>{t('team.lastActive')}</TableHead>
                <TableHead>{t('team.actions')}</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {filteredMembers.map((member) => (
                <TableRow key={member.id}>
                  <TableCell>
                    <div className="flex items-center space-x-3">
                      <Avatar className="h-8 w-8">
                        <AvatarImage src={member.avatar} />
                        <AvatarFallback>
                          {member.name.split(' ').map(n => n[0]).join('')}
                        </AvatarFallback>
                      </Avatar>
                      <div>
                        <div className="font-medium">{member.name}</div>
                        <div className="text-sm text-muted-foreground">{member.email}</div>
                      </div>
                    </div>
                  </TableCell>
                  <TableCell>
                    <Badge variant="outline">{t(`team.role.${member.role}`)}</Badge>
                  </TableCell>
                  <TableCell>{getStatusBadge(member.status)}</TableCell>
                  <TableCell>{member.maxConversations || 0}</TableCell>
                  <TableCell>
                    <div className="flex items-center">
                      <Star className="mr-1 h-4 w-4 text-yellow-400" />
                      {(4.5).toFixed(1)}
                    </div>
                  </TableCell>
                  <TableCell>
                    {member.id ? (
                      <div className="flex items-center">
                        <Calendar className="mr-1 h-4 w-4" />
                        {new Date().toLocaleDateString()}
                      </div>
                    ) : (
                      <span className="text-muted-foreground">Never</span>
                    )}
                  </TableCell>
                  <TableCell>
                    <DropdownMenu>
                      <DropdownMenuTrigger asChild>
                        <Button variant="ghost" className="h-8 w-8 p-0">
                          <MoreHorizontal className="h-4 w-4" />
                        </Button>
                      </DropdownMenuTrigger>
                      <DropdownMenuContent align="end">
                        <DropdownMenuLabel>{t('team.actions')}</DropdownMenuLabel>
                        <DropdownMenuSeparator />
                        <DropdownMenuItem onClick={() => setEditingMember(member)}>
                          <Edit className="mr-2 h-4 w-4" />
                          {t('common.edit')}
                        </DropdownMenuItem>
                        <DropdownMenuItem>
                          <Shield className="mr-2 h-4 w-4" />
                          {t('team.changeRole')}
                        </DropdownMenuItem>
                        <DropdownMenuItem
                          onClick={() => handleRemoveMember(member.id)}
                          className="text-red-600"
                        >
                          <Trash2 className="mr-2 h-4 w-4" />
                          {t('common.remove')}
                        </DropdownMenuItem>
                      </DropdownMenuContent>
                    </DropdownMenu>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>

          {filteredMembers.length === 0 && !isLoading && (
            <div className="text-center py-8">
              <Users className="mx-auto h-12 w-12 text-muted-foreground" />
              <h3 className="mt-2 text-sm font-semibold">No team members found</h3>
              <p className="mt-1 text-sm text-muted-foreground">
                Get started by inviting your first team member.
              </p>
            </div>
          )}
        </CardContent>
      </Card>

      {/* Edit Member Dialog */}
      <Dialog open={!!editingMember} onOpenChange={() => setEditingMember(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{t('team.editMember')}</DialogTitle>
            <DialogDescription>
              Update team member information and settings.
            </DialogDescription>
          </DialogHeader>
          {editingMember && (
            <div className="grid gap-4 py-4">
              <div className="grid grid-cols-4 items-center gap-4">
                <Label htmlFor="edit-name" className="text-right">
                  {t('team.name')}
                </Label>
                <Input
                  id="edit-name"
                  value={editingMember.name}
                  onChange={(e) => setEditingMember({ ...editingMember, name: e.target.value })}
                  className="col-span-3"
                />
              </div>
              <div className="grid grid-cols-4 items-center gap-4">
                <Label htmlFor="edit-role" className="text-right">
                  {t('team.role')}
                </Label>
                <Select
                  value={editingMember.role}
                  onValueChange={(value) => setEditingMember({ ...editingMember, role: value })}
                >
                  <SelectTrigger className="col-span-3">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="admin">{t('team.role.admin')}</SelectItem>
                    <SelectItem value="manager">{t('team.role.manager')}</SelectItem>
                    <SelectItem value="agent">{t('team.role.agent')}</SelectItem>
                  </SelectContent>
                </Select>
              </div>
            </div>
          )}
          <DialogFooter>
            <Button onClick={handleUpdateMember}>{t('common.save')}</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
};

export default TeamManagementPage;
