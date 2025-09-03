import React, { useEffect, useState } from "react";
import { useDispatch, useSelector } from "react-redux";
import { useTranslation } from "react-i18next";
import { AppDispatch, RootState } from "../../store/store";
import { fetchTenants, createTenant, updateTenant, deleteTenant } from "../../store/slices/tenantSlice";
import { Button } from "../../components/ui/button";
import { Input } from "../../components/ui/input";
import { Badge } from "../../components/ui/badge";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "../../components/ui/table";
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle, DialogTrigger } from "../../components/ui/dialog";
import { Label } from "../../components/ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "../../components/ui/select";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "../../components/ui/dropdown-menu";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "../../components/ui/card";
import { Skeleton } from "../../components/ui/skeleton";
import { Plus, Search, MoreHorizontal, Edit, Trash2, Building2, Users, MessageSquare, Calendar } from "lucide-react";

const TenantsPage: React.FC = () => {
  const { t, i18n } = useTranslation();
  const dispatch = useDispatch<AppDispatch>();
  const { tenants, isLoading, currentPage, pageSize } = useSelector((state: RootState) => state.tenant);
  const direction = i18n.language === "ar" ? "rtl" : "ltr";
  const [searchTerm, setSearchTerm] = useState("");
  const [isCreateDialogOpen, setIsCreateDialogOpen] = useState(false);
  const [editingTenant, setEditingTenant] = useState<any>(null);
  const [newTenant, setNewTenant] = useState({
    name: "",
    subdomain: "",
    customDomain: "",
    primaryColor: "",
    secondaryColor: "",
    defaultLanguage: "en",
    isRtlEnabled: false,
    status: "Active" as "Active" | "Inactive" | "Suspended",
    subscriptionPlan: "Basic",
  });

  useEffect(() => {
    dispatch(fetchTenants({ page: currentPage, pageSize, search: searchTerm }));
  }, [dispatch, currentPage, pageSize, searchTerm]);

  const handleCreateTenant = async () => {
    try {
      await dispatch(createTenant(newTenant)).unwrap();
      setIsCreateDialogOpen(false);
      setNewTenant({
        name: "",
        subdomain: "",
        customDomain: "",
        primaryColor: "",
        secondaryColor: "",
        defaultLanguage: "en",
        isRtlEnabled: false,
        status: "Active" as "Active" | "Inactive" | "Suspended",
        subscriptionPlan: "Basic",
      });
    } catch (error) {
      console.error("Failed to create tenant:", error);
    }
  };

  const handleUpdateTenant = async () => {
    if (!editingTenant) return;
    try {
      await dispatch(updateTenant({ id: editingTenant.id, data: editingTenant })).unwrap();
      setEditingTenant(null);
    } catch (error) {
      console.error("Failed to update tenant:", error);
    }
  };

  const handleDeleteTenant = async (id: string) => {
    if (window.confirm(t("tenants.deleteConfirmation"))) {
      try {
        await dispatch(deleteTenant(id)).unwrap();
      } catch (error) {
        console.error("Failed to delete tenant:", error);
      }
    }
  };

  const getStatusBadge = (status: string) => {
    const variants: Record<string, "default" | "secondary" | "destructive"> = {
      Active: "default",
      Inactive: "secondary",
      Suspended: "destructive",
    };
    const statusTranslations: Record<string, string> = {
      Active: t("tenants.status.active"),
      Inactive: t("tenants.status.inactive"),
      Suspended: t("tenants.status.suspended"),
    };
    return <Badge variant={variants[status] || "secondary"}>{statusTranslations[status] || status}</Badge>;
  };

  if (isLoading && tenants.length === 0) {
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
    <div className="space-y-6" dir={direction}>
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">{t("tenants.title")}</h1>
          <p className="text-muted-foreground">{t("tenants.description")}</p>
        </div>
        <Dialog open={isCreateDialogOpen} onOpenChange={setIsCreateDialogOpen}>
          <DialogTrigger asChild>
            <Button>
              <Plus className="mr-2 h-4 w-4" />
              {t("tenants.createTenant")}
            </Button>
          </DialogTrigger>
          <DialogContent>
            <DialogHeader>
              <DialogTitle>{t("tenants.createTenant")}</DialogTitle>
              <DialogDescription>{t("tenants.createDescription")}</DialogDescription>
            </DialogHeader>
            <div className="grid gap-4 py-4">
              <div className="grid grid-cols-4 items-center gap-4">
                <Label htmlFor="name" className="text-right">
                  {t("tenants.tenantName")}
                </Label>
                <Input
                  id="name"
                  value={newTenant.name}
                  onChange={(e) => setNewTenant({ ...newTenant, name: e.target.value })}
                  className="col-span-3"
                />
              </div>
              <div className="grid grid-cols-4 items-center gap-4">
                <Label htmlFor="domain" className="text-right">
                  {t("tenants.domain")}
                </Label>
                <Input
                  id="subdomain"
                  value={newTenant.subdomain}
                  onChange={(e) => setNewTenant({ ...newTenant, subdomain: e.target.value })}
                  className="col-span-3"
                />
              </div>
              <div className="grid grid-cols-4 items-center gap-4">
                <Label htmlFor="customDomain" className="text-right">
                  {t("tenants.customDomain")}
                </Label>
                <Input
                  id="customDomain"
                  value={newTenant.customDomain}
                  onChange={(e) => setNewTenant({ ...newTenant, customDomain: e.target.value })}
                  className="col-span-3"
                />
              </div>
              <div className="grid grid-cols-4 items-center gap-4">
                <Label htmlFor="primaryColor" className="text-right">
                  {t("tenants.primaryColor")}
                </Label>
                <Input
                  id="primaryColor"
                  type="color"
                  value={newTenant.primaryColor}
                  onChange={(e) => setNewTenant({ ...newTenant, primaryColor: e.target.value })}
                  className="col-span-3 h-10 w-16 p-0 border-none bg-transparent"
                  style={{ minWidth: "60px" }}
                />
              </div>
              <div className="grid grid-cols-4 items-center gap-4">
                <Label htmlFor="secondaryColor" className="text-right">
                  {t("tenants.secondaryColor")}
                </Label>
                <Input
                  id="secondaryColor"
                  type="color"
                  value={newTenant.secondaryColor}
                  onChange={(e) => setNewTenant({ ...newTenant, secondaryColor: e.target.value })}
                  className="col-span-3 h-10 w-16 p-0 border-none bg-transparent"
                  style={{ minWidth: "60px" }}
                />
              </div>
              <div className="grid grid-cols-4 items-center gap-4">
                <Label htmlFor="defaultLanguage" className="text-right">
                  {t("tenants.defaultLanguage")}
                </Label>
                <Select value={newTenant.defaultLanguage} onValueChange={(value) => setNewTenant({ ...newTenant, defaultLanguage: value })}>
                  <SelectTrigger className="col-span-3">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="en">{t("settings.general.languages.english")}</SelectItem>
                    <SelectItem value="ar">{t("settings.general.languages.arabic")}</SelectItem>
                  </SelectContent>
                </Select>
              </div>
              <div className="grid grid-cols-4 items-center gap-4">
                <Label htmlFor="isRtlEnabled" className="text-right">
                  {t("tenants.isRtlEnabled")}
                </Label>
                <Input
                  type="checkbox"
                  id="isRtlEnabled"
                  checked={newTenant.isRtlEnabled}
                  onChange={(e) => setNewTenant({ ...newTenant, isRtlEnabled: e.target.checked })}
                  className="col-span-3 h-5 w-5 accent-primary"
                />
              </div>
              <div className="grid grid-cols-4 items-center gap-4">
                <Label htmlFor="status" className="text-right">
                  {t("tenants.status.label")}
                </Label>
                <Select
                  value={newTenant.status}
                  onValueChange={(value) => setNewTenant({ ...newTenant, status: value as "Active" | "Inactive" | "Suspended" })}
                >
                  <SelectTrigger className="col-span-3">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="Active">{t("tenants.status.active")}</SelectItem>
                    <SelectItem value="Inactive">{t("tenants.status.inactive")}</SelectItem>
                    <SelectItem value="Suspended">{t("tenants.status.suspended")}</SelectItem>
                  </SelectContent>
                </Select>
              </div>
              <div className="grid grid-cols-4 items-center gap-4">
                <Label htmlFor="plan" className="text-right">
                  {t("tenants.subscriptionPlan")}
                </Label>
                <Select value={newTenant.subscriptionPlan} onValueChange={(value) => setNewTenant({ ...newTenant, subscriptionPlan: value })}>
                  <SelectTrigger className="col-span-3">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="Basic">{t("tenants.plans.basic")}</SelectItem>
                    <SelectItem value="Pro">{t("tenants.plans.pro")}</SelectItem>
                    <SelectItem value="Enterprise">{t("tenants.plans.enterprise")}</SelectItem>
                  </SelectContent>
                </Select>
              </div>
            </div>
            <DialogFooter>
              <Button onClick={handleCreateTenant}>{t("common.create")}</Button>
            </DialogFooter>
          </DialogContent>
        </Dialog>
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center">
            <Building2 className="mr-2 h-5 w-5" />
            {t("tenants.management.title")}
          </CardTitle>
          <CardDescription>{t("tenants.management.description")}</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="flex items-center space-x-2 mb-4">
            <div className="relative flex-1">
              <Search className="absolute left-2 top-2.5 h-4 w-4 text-muted-foreground" />
              <Input
                placeholder={t("tenants.searchPlaceholder")}
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                className="pl-8"
              />
            </div>
          </div>

          <Table>
            <TableHeader>
              <TableRow>
                <TableHead className={direction === "rtl" ? "text-right" : "text-left"}>{t("tenants.tenantName")}</TableHead>
                <TableHead className={direction === "rtl" ? "text-right" : "text-left"}>{t("tenants.domain")}</TableHead>
                <TableHead className={direction === "rtl" ? "text-right" : "text-left"}>{t("tenants.status.label")}</TableHead>
                <TableHead className={direction === "rtl" ? "text-right" : "text-left"}>{t("tenants.subscriptionPlan")}</TableHead>
                <TableHead className={direction === "rtl" ? "text-right" : "text-left"}>{t("tenants.userCount")}</TableHead>
                <TableHead className={direction === "rtl" ? "text-right" : "text-left"}>{t("tenants.conversationCount")}</TableHead>
                <TableHead className={direction === "rtl" ? "text-right" : "text-left"}>{t("tenants.createdAt")}</TableHead>
                <TableHead className={direction === "rtl" ? "text-right" : "text-left"}>{t("tenants.actions")}</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {tenants.map((tenant) => (
                <TableRow key={tenant.id}>
                  <TableCell className="font-medium">{tenant.name}</TableCell>
                  <TableCell>{tenant.subdomain}</TableCell>
                  <TableCell>{getStatusBadge(tenant.status)}</TableCell>
                  {/* <TableCell>{t(`tenants.plans.${tenant.subscriptionPlan?.toLowerCase()}`)}</TableCell> */}
                  <TableCell>{t(`tenants.subscriptionPlan`)}</TableCell>
                  <TableCell>
                    <div className="flex items-center">
                      <Users className="mr-1 h-4 w-4" />
                      {tenant.userCount}
                    </div>
                  </TableCell>
                  <TableCell>
                    <div className="flex items-center">
                      <MessageSquare className="mr-1 h-4 w-4" />
                      {tenant.conversationCount}
                    </div>
                  </TableCell>
                  <TableCell>
                    <div className="flex items-center">
                      <Calendar className="mr-1 h-4 w-4" />
                      {new Date(tenant.createdAt).toLocaleDateString()}
                    </div>
                  </TableCell>
                  <TableCell>
                    <DropdownMenu>
                      <DropdownMenuTrigger asChild>
                        <Button variant="ghost" className="h-8 w-8 p-0">
                          <MoreHorizontal className="h-4 w-4" />
                        </Button>
                      </DropdownMenuTrigger>
                      <DropdownMenuContent align="end">
                        <DropdownMenuLabel>{t("tenants.actions")}</DropdownMenuLabel>
                        <DropdownMenuSeparator />
                        <DropdownMenuItem onClick={() => setEditingTenant(tenant)}>
                          <Edit className="mr-2 h-4 w-4" />
                          {t("common.edit")}
                        </DropdownMenuItem>
                        <DropdownMenuItem onClick={() => handleDeleteTenant(tenant.id)} className="text-red-600">
                          <Trash2 className="mr-2 h-4 w-4" />
                          {t("common.delete")}
                        </DropdownMenuItem>
                      </DropdownMenuContent>
                    </DropdownMenu>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>

          {tenants.length === 0 && !isLoading && (
            <div className="text-center py-8">
              <Building2 className="mx-auto h-12 w-12 text-muted-foreground" />
              <h3 className="mt-2 text-sm font-semibold">{t("tenants.noTenants.title")}</h3>
              <p className="mt-1 text-sm text-muted-foreground">{t("tenants.noTenants.description")}</p>
            </div>
          )}
        </CardContent>
      </Card>

      {/* Edit Dialog */}
      <Dialog open={!!editingTenant} onOpenChange={() => setEditingTenant(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{t("tenants.editTenant")}</DialogTitle>
            <DialogDescription>{t("tenants.editDescription")}</DialogDescription>
          </DialogHeader>
          {editingTenant && (
            <div className="grid gap-4 py-4">
              <div className="grid grid-cols-4 items-center gap-4">
                <Label htmlFor="edit-name" className="text-right">
                  {t("tenants.tenantName")}
                </Label>
                <Input
                  id="edit-name"
                  value={editingTenant.name}
                  onChange={(e) => setEditingTenant({ ...editingTenant, name: e.target.value })}
                  className="col-span-3"
                />
              </div>
              <div className="grid grid-cols-4 items-center gap-4">
                <Label htmlFor="edit-subdomain" className="text-right">
                  {t("tenants.domain")}
                </Label>
                <Input
                  id="edit-subdomain"
                  value={editingTenant.subdomain || editingTenant.domain}
                  onChange={(e) => setEditingTenant({ ...editingTenant, subdomain: e.target.value })}
                  className="col-span-3"
                />
              </div>
              <div className="grid grid-cols-4 items-center gap-4">
                <Label htmlFor="edit-customDomain" className="text-right">
                  {t("tenants.customDomain")}
                </Label>
                <Input
                  id="edit-customDomain"
                  value={editingTenant.customDomain || ""}
                  onChange={(e) => setEditingTenant({ ...editingTenant, customDomain: e.target.value })}
                  className="col-span-3"
                />
              </div>
              <div className="grid grid-cols-4 items-center gap-4">
                <Label htmlFor="edit-primaryColor" className="text-right">
                  {t("tenants.primaryColor")}
                </Label>
                <Input
                  id="edit-primaryColor"
                  type="color"
                  value={editingTenant.primaryColor || "#3B82F6"}
                  onChange={(e) => setEditingTenant({ ...editingTenant, primaryColor: e.target.value })}
                  className="col-span-3 h-10 w-16 p-0 border-none bg-transparent"
                  style={{ minWidth: "60px" }}
                />
              </div>
              <div className="grid grid-cols-4 items-center gap-4">
                <Label htmlFor="edit-secondaryColor" className="text-right">
                  {t("tenants.secondaryColor")}
                </Label>
                <Input
                  id="edit-secondaryColor"
                  type="color"
                  value={editingTenant.secondaryColor || "#64748B"}
                  onChange={(e) => setEditingTenant({ ...editingTenant, secondaryColor: e.target.value })}
                  className="col-span-3 h-10 w-16 p-0 border-none bg-transparent"
                  style={{ minWidth: "60px" }}
                />
              </div>
              <div className="grid grid-cols-4 items-center gap-4">
                <Label htmlFor="edit-defaultLanguage" className="text-right">
                  {t("tenants.defaultLanguage")}
                </Label>
                <Select
                  value={editingTenant.defaultLanguage || "en"}
                  onValueChange={(value) => setEditingTenant({ ...editingTenant, defaultLanguage: value })}
                >
                  <SelectTrigger className="col-span-3">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="en">{t("language.english")}</SelectItem>
                    <SelectItem value="ar">{t("language.arabic")}</SelectItem>
                  </SelectContent>
                </Select>
              </div>
              <div className="grid grid-cols-4 items-center gap-4">
                <Label htmlFor="edit-isRtlEnabled" className="text-right">
                  {t("tenants.isRtlEnabled")}
                </Label>
                <Input
                  type="checkbox"
                  id="edit-isRtlEnabled"
                  checked={editingTenant.isRtlEnabled || false}
                  onChange={(e) => setEditingTenant({ ...editingTenant, isRtlEnabled: e.target.checked })}
                  className="col-span-3 h-5 w-5 accent-primary"
                />
              </div>
              <div className="grid grid-cols-4 items-center gap-4">
                <Label htmlFor="edit-status" className="text-right">
                  {t("tenants.status.label")}
                </Label>
                <Select value={editingTenant.status} onValueChange={(value) => setEditingTenant({ ...editingTenant, status: value })}>
                  <SelectTrigger className="col-span-3">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="Active">{t("tenants.status.active")}</SelectItem>
                    <SelectItem value="Inactive">{t("tenants.status.inactive")}</SelectItem>
                    <SelectItem value="Suspended">{t("tenants.status.suspended")}</SelectItem>
                  </SelectContent>
                </Select>
              </div>
            </div>
          )}
          <DialogFooter>
            <Button onClick={handleUpdateTenant}>{t("common.save")}</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
};

export default TenantsPage;
