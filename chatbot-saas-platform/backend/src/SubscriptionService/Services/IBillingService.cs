using Shared.Domain.Entities;
using Stripe;
using SubscriptionService.Models;

namespace SubscriptionService.Services;

public interface IBillingService
{
    Task<Shared.Domain.Entities.Invoice> CreateInvoiceAsync(Guid subscriptionId, DateTime periodStart, DateTime periodEnd);
    Task<Shared.Domain.Entities.Invoice?> GetInvoiceAsync(Guid invoiceId, Guid tenantId);
    Task<List<Shared.Domain.Entities.Invoice>> GetInvoicesAsync(Guid tenantId, int page = 1, int pageSize = 20);
    Task<bool> PayInvoiceAsync(Guid invoiceId, Guid tenantId);
    Task<bool> VoidInvoiceAsync(Guid invoiceId, Guid tenantId);
    Task<byte[]> GenerateInvoicePdfAsync(Guid invoiceId, Guid tenantId);
    Task ProcessOverdueInvoicesAsync();
    Task<BillingAddress?> GetBillingAddressAsync(Guid tenantId);
    Task<bool> UpdateBillingAddressAsync(BillingAddress address, Guid tenantId);
    Task<CouponResult> ApplyCouponAsync(string couponCode, Guid tenantId);
    Task<List<TaxRate>> GetTaxRatesAsync(string? country, string? state);
}
