import { useState } from 'react';
import { useDispatch } from 'react-redux';
import { useTranslation } from 'react-i18next';
import { AppDispatch } from '../../store/store';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../../components/ui/card';
import { Button } from '../../components/ui/button';
import { Input } from '../../components/ui/input';
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
  DialogHeader,
  DialogTitle,
} from '../../components/ui/dialog';
import {
  Users,
  Search,
  Filter,
  MessageSquare,
  Star,
  Clock,
  TrendingUp,
  Mail,
  Phone,
  MapPin,
  Calendar,
  MoreHorizontal,
  Eye,
  Edit,
  Ban,
} from 'lucide-react';

interface Customer {
  id: string;
  name?: string;
  email: string;
  avatar?: string;
  status: 'active' | 'inactive' | 'blocked';
  totalConversations?: number;
  satisfactionRating?: number;
  lastContact?: string;
  location?: string;
  phone?: string;
  createdAt: string;
}

const CustomersPage = () => {
  const { t } = useTranslation();
  useDispatch<AppDispatch>();
  const [searchTerm, setSearchTerm] = useState('');
  const [statusFilter, setStatusFilter] = useState('all');
  const [selectedCustomer, setSelectedCustomer] = useState<Customer | null>(null);
  const [isDetailsDialogOpen, setIsDetailsDialogOpen] = useState(false);

  const customers: Customer[] = [
    {
      id: '1',
      name: 'John Doe',
      email: 'john.doe@example.com',
      avatar: '',
      status: 'active',
      totalConversations: 15,
      satisfactionRating: 4.8,
      lastContact: '2024-01-20T10:30:00Z',
      location: 'New York, USA',
      phone: '+1-555-0123',
      createdAt: '2024-01-01T00:00:00Z',
    },
    {
      id: '2',
      name: 'Jane Smith',
      email: 'jane.smith@example.com',
      avatar: '',
      status: 'active',
      totalConversations: 8,
      satisfactionRating: 4.5,
      lastContact: '2024-01-19T15:45:00Z',
      location: 'London, UK',
      phone: '+44-20-7946-0958',
      createdAt: '2024-01-05T00:00:00Z',
    },
    {
      id: '3',
      name: 'Ahmed Hassan',
      email: 'ahmed.hassan@example.com',
      avatar: '',
      status: 'inactive',
      totalConversations: 3,
      satisfactionRating: 4.2,
      lastContact: '2024-01-15T09:20:00Z',
      location: 'Dubai, UAE',
      phone: '+971-4-123-4567',
      createdAt: '2024-01-10T00:00:00Z',
    },
  ];

  const isLoading = false;

  const filteredCustomers = customers.filter(customer => {
    const matchesSearch = customer.name?.toLowerCase().includes(searchTerm.toLowerCase()) ||
                         customer.email.toLowerCase().includes(searchTerm.toLowerCase());
    const matchesStatus = statusFilter === 'all' || customer.status === statusFilter;
    return matchesSearch && matchesStatus;
  });

  const getStatusBadge = (status: string) => {
    const variants: Record<string, 'default' | 'secondary' | 'destructive'> = {
      active: 'default',
      inactive: 'secondary',
      blocked: 'destructive',
    };
    return (
      <Badge variant={variants[status] || 'secondary'}>
        {t(`customers.status.${status}`)}
      </Badge>
    );
  };

  const getSatisfactionColor = (rating?: number) => {
    if (!rating) return 'text-gray-400';
    if (rating >= 4.5) return 'text-green-500';
    if (rating >= 4.0) return 'text-yellow-500';
    return 'text-red-500';
  };

  const handleViewCustomer = (customer: Customer) => {
    setSelectedCustomer(customer);
    setIsDetailsDialogOpen(true);
  };

  if (isLoading && !customers) {
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
          <h1 className="text-3xl font-bold">{t('customers.title')}</h1>
          <p className="text-muted-foreground">
            {t('customers.subtitle')}
          </p>
        </div>
        <div className="flex items-center space-x-2">
          <Badge variant="outline">
            {filteredCustomers.length} {t('customers.total')}
          </Badge>
          <Button variant="outline">
            <Filter className="mr-2 h-4 w-4" />
            {t('customers.filters')}
          </Button>
        </div>
      </div>

      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">
              {t('customers.totalCustomers')}
            </CardTitle>
            <Users className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{customers?.length || 0}</div>
            <div className="flex items-center text-xs text-muted-foreground">
              <TrendingUp className="mr-1 h-3 w-3" />
              +12% from last month
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">
              {t('customers.activeCustomers')}
            </CardTitle>
            <Users className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">
              {customers?.filter(c => c.status === 'active').length || 0}
            </div>
            <div className="flex items-center text-xs text-muted-foreground">
              <TrendingUp className="mr-1 h-3 w-3" />
              Currently active
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">
              {t('customers.avgSatisfaction')}
            </CardTitle>
            <Star className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">
              {customers?.length ? 
                (customers.reduce((acc, c) => acc + (c.satisfactionRating || 0), 0) / customers.length).toFixed(1) : 
                '0.0'
              }
            </div>
            <div className="flex items-center text-xs text-muted-foreground">
              <TrendingUp className="mr-1 h-3 w-3" />
              +0.3 from last month
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">
              {t('customers.avgResponseTime')}
            </CardTitle>
            <Clock className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">2.4m</div>
            <div className="flex items-center text-xs text-muted-foreground">
              <TrendingUp className="mr-1 h-3 w-3" />
              -15% from last month
            </div>
          </CardContent>
        </Card>
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center">
            <Users className="mr-2 h-5 w-5" />
            {t('customers.customerList')}
          </CardTitle>
          <CardDescription>
            {t('customers.customerListDesc')}
          </CardDescription>
        </CardHeader>
        <CardContent>
          <div className="flex items-center space-x-2 mb-4">
            <div className="relative flex-1">
              <Search className="absolute left-2 top-2.5 h-4 w-4 text-muted-foreground" />
              <Input
                placeholder={t('customers.searchPlaceholder')}
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
                <SelectItem value="all">{t('customers.allStatus')}</SelectItem>
                <SelectItem value="active">{t('customers.active')}</SelectItem>
                <SelectItem value="inactive">{t('customers.inactive')}</SelectItem>
                <SelectItem value="blocked">{t('customers.blocked')}</SelectItem>
              </SelectContent>
            </Select>
          </div>

          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>{t('customers.customer')}</TableHead>
                <TableHead>{t('customers.status')}</TableHead>
                <TableHead>{t('customers.conversations')}</TableHead>
                <TableHead>{t('customers.satisfaction')}</TableHead>
                <TableHead>{t('customers.lastContact')}</TableHead>
                <TableHead>{t('customers.location')}</TableHead>
                <TableHead>{t('customers.actions')}</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {filteredCustomers.map((customer) => (
                <TableRow key={customer.id}>
                  <TableCell>
                    <div className="flex items-center space-x-3">
                      <Avatar className="h-8 w-8">
                        <AvatarImage src={customer.avatar} />
                        <AvatarFallback>
                          {customer.name?.split(' ').map(n => n[0]).join('') || 'U'}
                        </AvatarFallback>
                      </Avatar>
                      <div>
                        <div className="font-medium">{customer.name || 'Anonymous'}</div>
                        <div className="text-sm text-muted-foreground">{customer.email}</div>
                      </div>
                    </div>
                  </TableCell>
                  <TableCell>{getStatusBadge(customer.status || 'active')}</TableCell>
                  <TableCell>
                    <div className="flex items-center">
                      <MessageSquare className="mr-1 h-4 w-4" />
                      {customer.totalConversations || 0}
                    </div>
                  </TableCell>
                  <TableCell>
                    <div className="flex items-center">
                      <Star className={`mr-1 h-4 w-4 ${getSatisfactionColor(customer.satisfactionRating)}`} />
                      {customer.satisfactionRating?.toFixed(1) || 'N/A'}
                    </div>
                  </TableCell>
                  <TableCell>
                    {customer.lastContact ? (
                      <div className="flex items-center">
                        <Calendar className="mr-1 h-4 w-4" />
                        {new Date(customer.lastContact).toLocaleDateString()}
                      </div>
                    ) : (
                      <span className="text-muted-foreground">Never</span>
                    )}
                  </TableCell>
                  <TableCell>
                    <div className="flex items-center">
                      <MapPin className="mr-1 h-4 w-4" />
                      {customer.location || 'Unknown'}
                    </div>
                  </TableCell>
                  <TableCell>
                    <div className="flex items-center space-x-2">
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => handleViewCustomer(customer)}
                      >
                        <Eye className="h-4 w-4" />
                      </Button>
                      <Button variant="ghost" size="sm">
                        <Edit className="h-4 w-4" />
                      </Button>
                      <Button variant="ghost" size="sm">
                        <MoreHorizontal className="h-4 w-4" />
                      </Button>
                    </div>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>

          {filteredCustomers.length === 0 && !isLoading && (
            <div className="text-center py-8">
              <Users className="mx-auto h-12 w-12 text-muted-foreground" />
              <h3 className="mt-2 text-sm font-semibold">No customers found</h3>
              <p className="mt-1 text-sm text-muted-foreground">
                Try adjusting your search or filters.
              </p>
            </div>
          )}
        </CardContent>
      </Card>

      {/* Customer Details Dialog */}
      <Dialog open={isDetailsDialogOpen} onOpenChange={setIsDetailsDialogOpen}>
        <DialogContent className="max-w-2xl">
          <DialogHeader>
            <DialogTitle>{t('customers.customerDetails')}</DialogTitle>
            <DialogDescription>
              {t('customers.customerDetailsDesc')}
            </DialogDescription>
          </DialogHeader>
          {selectedCustomer && (
            <div className="space-y-6">
              <div className="flex items-center space-x-4">
                <Avatar className="h-16 w-16">
                  <AvatarImage src={selectedCustomer.avatar} />
                  <AvatarFallback>
                    {selectedCustomer.name?.split(' ').map(n => n[0]).join('') || 'U'}
                  </AvatarFallback>
                </Avatar>
                <div>
                  <h3 className="text-lg font-semibold">{selectedCustomer.name || 'Anonymous'}</h3>
                  <p className="text-sm text-muted-foreground">{selectedCustomer.email}</p>
                  {getStatusBadge(selectedCustomer.status)}
                </div>
              </div>

              <div className="grid gap-4 md:grid-cols-2">
                <div className="space-y-3">
                  <h4 className="font-medium">{t('customers.contactInformation')}</h4>
                  <div className="space-y-2">
                    <div className="flex items-center">
                      <Mail className="mr-2 h-4 w-4" />
                      <span className="text-sm">{selectedCustomer.email}</span>
                    </div>
                    {selectedCustomer.phone && (
                      <div className="flex items-center">
                        <Phone className="mr-2 h-4 w-4" />
                        <span className="text-sm">{selectedCustomer.phone}</span>
                      </div>
                    )}
                    {selectedCustomer.location && (
                      <div className="flex items-center">
                        <MapPin className="mr-2 h-4 w-4" />
                        <span className="text-sm">{selectedCustomer.location}</span>
                      </div>
                    )}
                  </div>
                </div>

                <div className="space-y-3">
                  <h4 className="font-medium">{t('customers.statistics')}</h4>
                  <div className="space-y-2">
                    <div className="flex justify-between">
                      <span className="text-sm">{t('customers.totalConversations')}</span>
                      <span className="text-sm font-medium">{selectedCustomer.totalConversations || 0}</span>
                    </div>
                    <div className="flex justify-between">
                      <span className="text-sm">{t('customers.satisfaction')}</span>
                      <div className="flex items-center">
                        <Star className={`mr-1 h-3 w-3 ${getSatisfactionColor(selectedCustomer.satisfactionRating)}`} />
                        <span className="text-sm font-medium">{selectedCustomer.satisfactionRating?.toFixed(1) || 'N/A'}</span>
                      </div>
                    </div>
                    <div className="flex justify-between">
                      <span className="text-sm">{t('customers.memberSince')}</span>
                      <span className="text-sm font-medium">
                        {new Date(selectedCustomer.createdAt).toLocaleDateString()}
                      </span>
                    </div>
                    <div className="flex justify-between">
                      <span className="text-sm">{t('customers.lastContact')}</span>
                      <span className="text-sm font-medium">
                        {selectedCustomer.lastContact ? 
                          new Date(selectedCustomer.lastContact).toLocaleDateString() : 
                          'Never'
                        }
                      </span>
                    </div>
                  </div>
                </div>
              </div>

              <div className="flex space-x-2">
                <Button>
                  <MessageSquare className="mr-2 h-4 w-4" />
                  {t('customers.startConversation')}
                </Button>
                <Button variant="outline">
                  <Edit className="mr-2 h-4 w-4" />
                  {t('customers.editCustomer')}
                </Button>
                <Button variant="outline" className="text-red-600">
                  <Ban className="mr-2 h-4 w-4" />
                  {t('customers.blockCustomer')}
                </Button>
              </div>
            </div>
          )}
        </DialogContent>
      </Dialog>
    </div>
  );
};

export default CustomersPage;
