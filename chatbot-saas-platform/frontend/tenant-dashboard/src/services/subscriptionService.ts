import { apiClient, ApiResponse } from './api';

// Subscription related types
export interface Subscription {
  id: string;
  tenantId: string;
  planId: string;
  status: 'active' | 'inactive' | 'trial' | 'expired' | 'cancelled' | 'past_due';
  currentPeriodStart: string;
  currentPeriodEnd: string;
  trialStart?: string;
  trialEnd?: string;
  cancelledAt?: string;
  endedAt?: string;
  customerId: string;
  subscriptionId: string;
  metadata: Record<string, any>;
}

export interface Plan {
  id: string;
  name: string;
  description: string;
  price: number;
  currency: string;
  interval: 'month' | 'year';
  intervalCount: number;
  trialDays: number;
  features: PlanFeature[];
  limits: PlanLimits;
  isPopular: boolean;
  isActive: boolean;
  metadata: Record<string, any>;
}

export interface PlanFeature {
  id: string;
  name: string;
  description: string;
  included: boolean;
  limit?: number;
}

export interface PlanLimits {
  users: number;
  conversations: number;
  storage: number; // in GB
  apiCalls: number;
  customIntegrations: number;
}

export interface Invoice {
  id: string;
  subscriptionId: string;
  number: string;
  status: 'draft' | 'open' | 'paid' | 'void' | 'uncollectible';
  total: number;
  subtotal: number;
  tax: number;
  currency: string;
  periodStart: string;
  periodEnd: string;
  dueDate: string;
  paidAt?: string;
  downloadUrl: string;
  lineItems: InvoiceLineItem[];
}

export interface InvoiceLineItem {
  id: string;
  description: string;
  amount: number;
  quantity: number;
  periodStart: string;
  periodEnd: string;
}

export interface PaymentMethod {
  id: string;
  type: 'card' | 'bank_account';
  card?: {
    brand: string;
    last4: string;
    expMonth: number;
    expYear: number;
  };
  bankAccount?: {
    bankName: string;
    last4: string;
    accountType: string;
  };
  isDefault: boolean;
}

export interface UsageMetrics {
  period: {
    start: string;
    end: string;
  };
  usage: {
    users: { current: number; limit: number };
    conversations: { current: number; limit: number };
    storage: { current: number; limit: number }; // in GB
    apiCalls: { current: number; limit: number };
  };
  overages: {
    users: number;
    conversations: number;
    storage: number;
    apiCalls: number;
  };
}

export interface BillingAddress {
  line1: string;
  line2?: string;
  city: string;
  state: string;
  postalCode: string;
  country: string;
}

export interface CreateSubscriptionData {
  planId: string;
  paymentMethodId: string;
  billingAddress: BillingAddress;
  couponCode?: string;
}

export interface UpdateSubscriptionData {
  planId?: string;
  paymentMethodId?: string;
  billingAddress?: BillingAddress;
  metadata?: Record<string, any>;
}

export class SubscriptionService {
  // Get current subscription
  static async getCurrentSubscription(): Promise<ApiResponse<Subscription>> {
    return apiClient.get<Subscription>('/subscription');
  }

  // Create new subscription
  static async createSubscription(data: CreateSubscriptionData): Promise<ApiResponse<Subscription>> {
    return apiClient.post<Subscription>('/subscription', data);
  }

  // Update subscription
  static async updateSubscription(data: UpdateSubscriptionData): Promise<ApiResponse<Subscription>> {
    return apiClient.put<Subscription>('/subscription', data);
  }

  // Cancel subscription
  static async cancelSubscription(cancelAtPeriodEnd: boolean = true): Promise<ApiResponse<Subscription>> {
    return apiClient.post<Subscription>('/subscription/cancel', { cancelAtPeriodEnd });
  }

  // Reactivate subscription
  static async reactivateSubscription(): Promise<ApiResponse<Subscription>> {
    return apiClient.post<Subscription>('/subscription/reactivate');
  }

  // Resume subscription
  static async resumeSubscription(): Promise<ApiResponse<Subscription>> {
    return apiClient.post<Subscription>('/subscription/resume');
  }

  // Plans
  static async getPlans(): Promise<ApiResponse<Plan[]>> {
    return apiClient.get<Plan[]>('/subscription/plans');
  }

  static async getPlan(id: string): Promise<ApiResponse<Plan>> {
    return apiClient.get<Plan>(`/subscription/plans/${id}`);
  }

  // Invoices
  static async getInvoices(params?: {
    page?: number;
    limit?: number;
    status?: string;
  }): Promise<ApiResponse<{ invoices: Invoice[]; total: number }>> {
    return apiClient.get('/subscription/invoices', params);
  }

  static async getInvoice(id: string): Promise<ApiResponse<Invoice>> {
    return apiClient.get<Invoice>(`/subscription/invoices/${id}`);
  }

  static async downloadInvoice(id: string): Promise<ApiResponse<{ downloadUrl: string }>> {
    return apiClient.get<{ downloadUrl: string }>(`/subscription/invoices/${id}/download`);
  }

  static async payInvoice(id: string, paymentMethodId: string): Promise<ApiResponse<Invoice>> {
    return apiClient.post<Invoice>(`/subscription/invoices/${id}/pay`, { paymentMethodId });
  }

  // Payment Methods
  static async getPaymentMethods(): Promise<ApiResponse<PaymentMethod[]>> {
    return apiClient.get<PaymentMethod[]>('/subscription/payment-methods');
  }

  static async addPaymentMethod(token: string): Promise<ApiResponse<PaymentMethod>> {
    return apiClient.post<PaymentMethod>('/subscription/payment-methods', { token });
  }

  static async updatePaymentMethod(id: string, data: {
    billingAddress?: BillingAddress;
    isDefault?: boolean;
  }): Promise<ApiResponse<PaymentMethod>> {
    return apiClient.put<PaymentMethod>(`/subscription/payment-methods/${id}`, data);
  }

  static async deletePaymentMethod(id: string): Promise<ApiResponse<void>> {
    return apiClient.delete<void>(`/subscription/payment-methods/${id}`);
  }

  static async setDefaultPaymentMethod(id: string): Promise<ApiResponse<PaymentMethod>> {
    return apiClient.post<PaymentMethod>(`/subscription/payment-methods/${id}/default`);
  }

  // Usage and Billing
  static async getUsageMetrics(): Promise<ApiResponse<UsageMetrics>> {
    return apiClient.get<UsageMetrics>('/subscription/usage');
  }

  static async getBillingAddress(): Promise<ApiResponse<BillingAddress>> {
    return apiClient.get<BillingAddress>('/subscription/billing-address');
  }

  static async updateBillingAddress(address: BillingAddress): Promise<ApiResponse<BillingAddress>> {
    return apiClient.put<BillingAddress>('/subscription/billing-address', address);
  }

  // Coupons and Discounts
  static async applyCoupon(couponCode: string): Promise<ApiResponse<{
    valid: boolean;
    discount: number;
    description: string;
  }>> {
    return apiClient.post('/subscription/apply-coupon', { couponCode });
  }

  static async removeCoupon(): Promise<ApiResponse<void>> {
    return apiClient.delete<void>('/subscription/coupon');
  }

  // Webhooks and Events
  static async getSubscriptionEvents(params?: {
    page?: number;
    limit?: number;
    type?: string;
  }): Promise<ApiResponse<{
    events: Array<{
      id: string;
      type: string;
      data: Record<string, any>;
      createdAt: string;
    }>;
    total: number;
  }>> {
    return apiClient.get('/subscription/events', params);
  }

  // Tax Information
  static async getTaxRates(): Promise<ApiResponse<Array<{
    id: string;
    country: string;
    state?: string;
    rate: number;
    description: string;
  }>>> {
    return apiClient.get('/subscription/tax-rates');
  }

  static async updateTaxInformation(data: {
    taxId?: string;
    taxExempt?: boolean;
    businessType?: string;
  }): Promise<ApiResponse<void>> {
    return apiClient.put('/subscription/tax-info', data);
  }

  // Preview subscription changes
  static async previewSubscriptionChange(planId: string): Promise<ApiResponse<{
    immediateTotal: number;
    nextInvoiceTotal: number;
    prorationAmount: number;
    effectiveDate: string;
  }>> {
    return apiClient.post('/subscription/preview-change', { planId });
  }
}
