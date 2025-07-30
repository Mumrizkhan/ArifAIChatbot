using Shared.Domain.Entities;
using SubscriptionService.Models;

namespace SubscriptionService.Services;

public interface IInvoiceService
{
    Task<Invoice> GenerateInvoiceAsync(Guid subscriptionId, DateTime periodStart, DateTime periodEnd);
    Task<bool> SendInvoiceAsync(Guid invoiceId, Guid tenantId);
    Task<byte[]> GenerateInvoicePdfAsync(Guid invoiceId);
    Task<string> GetInvoiceHtmlAsync(Guid invoiceId);
    Task ProcessDunningAsync();
    Task<List<Invoice>> GetOverdueInvoicesAsync();
}
