using Shared.Domain.Entities;
using SubscriptionService.Models;

namespace SubscriptionService.Services;

public interface IBillingService
{
    Task<Invoice> CreateInvoiceAsync(Guid subscriptionId, DateTime periodStart, DateTime periodEnd);
    Task<Invoice?> GetInvoiceAsync(Guid invoiceId, Guid tenantId);
    Task<List<Invoice>> GetInvoicesAsync(Guid tenantId, int page = 1, int pageSize = 20);
    Task<bool> PayInvoiceAsync(Guid invoiceId, Guid tenantId);
    Task<bool> VoidInvoiceAsync(Guid invoiceId, Guid tenantId);
    Task<byte[]> GenerateInvoicePdfAsync(Guid invoiceId, Guid tenantId);
    Task ProcessOverdueInvoicesAsync();
    Task<BillingAddress?> GetBillingAddressAsync(Guid tenantId);
    Task<bool> UpdateBillingAddressAsync(BillingAddress address, Guid tenantId);
}
