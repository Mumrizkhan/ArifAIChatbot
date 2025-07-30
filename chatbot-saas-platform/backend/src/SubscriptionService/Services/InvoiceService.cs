using Microsoft.EntityFrameworkCore;
using Shared.Application.Common.Interfaces;
using SubscriptionService.Services;
using SubscriptionService.Models;
using Shared.Domain.Entities;

namespace SubscriptionService.Services;

public class InvoiceService : IInvoiceService
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<InvoiceService> _logger;

    public InvoiceService(
        IApplicationDbContext context,
        ILogger<InvoiceService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Invoice> GenerateInvoiceAsync(Guid subscriptionId, DateTime periodStart, DateTime periodEnd)
    {
        try
        {
            var subscription = await _context.Set<Subscription>()
                .Include(s => s.Plan)
                .FirstOrDefaultAsync(s => s.Id == subscriptionId);

            if (subscription == null)
                throw new ArgumentException("Subscription not found");

            var invoice = new Invoice
            {
                Id = Guid.NewGuid(),
                SubscriptionId = subscriptionId,
                TenantId = subscription.TenantId,
                InvoiceNumber = GenerateInvoiceNumber(),
                Status = InvoiceStatus.Draft,
                Subtotal = subscription.Amount,
                TaxAmount = CalculateTax(subscription.Amount, subscription.TenantId),
                DiscountAmount = 0,
                Currency = subscription.Currency,
                IssueDate = DateTime.UtcNow,
                DueDate = DateTime.UtcNow.AddDays(30),
                PeriodStart = periodStart,
                PeriodEnd = periodEnd,
                Subscription = subscription,
                CreatedAt = DateTime.UtcNow
            };

            invoice.Total = invoice.Subtotal + invoice.TaxAmount - invoice.DiscountAmount;

            var lineItem = new InvoiceLineItem
            {
                Id = Guid.NewGuid(),
                InvoiceId = invoice.Id,
                Description = $"{subscription.Plan.Name} - {periodStart:MMM yyyy} to {periodEnd:MMM yyyy}",
                Quantity = 1,
                UnitPrice = subscription.Amount,
                Amount = subscription.Amount,
                CreatedAt = DateTime.UtcNow
            };

            invoice.LineItems.Add(lineItem);

            _context.Set<Invoice>().Add(invoice);
            await _context.SaveChangesAsync();

            return invoice;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating invoice for subscription {SubscriptionId}", subscriptionId);
            throw;
        }
    }

    public async Task<bool> SendInvoiceAsync(Guid invoiceId, Guid tenantId)
    {
        try
        {
            var invoice = await _context.Set<Invoice>()
                .Include(i => i.Subscription)
                .FirstOrDefaultAsync(i => i.Id == invoiceId && i.TenantId == tenantId);

            if (invoice == null)
                return false;

            invoice.Status = InvoiceStatus.Open;
            invoice.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Invoice {InvoiceId} sent to tenant {TenantId}", invoiceId, tenantId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending invoice {InvoiceId}", invoiceId);
            return false;
        }
    }

    public async Task<byte[]> GenerateInvoicePdfAsync(Guid invoiceId)
    {
        try
        {
            var invoice = await _context.Set<Invoice>()
                .Include(i => i.LineItems)
                .Include(i => i.Subscription)
                .ThenInclude(s => s.Plan)
                .FirstOrDefaultAsync(i => i.Id == invoiceId);

            if (invoice == null)
                throw new ArgumentException("Invoice not found");

            var htmlContent = await GetInvoiceHtmlAsync(invoiceId);
            var pdfBytes = System.Text.Encoding.UTF8.GetBytes($"PDF content for invoice {invoice.InvoiceNumber}");
            
            return pdfBytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating PDF for invoice {InvoiceId}", invoiceId);
            throw;
        }
    }

    public async Task<string> GetInvoiceHtmlAsync(Guid invoiceId)
    {
        try
        {
            var invoice = await _context.Set<Invoice>()
                .Include(i => i.LineItems)
                .Include(i => i.Subscription)
                .ThenInclude(s => s.Plan)
                .FirstOrDefaultAsync(i => i.Id == invoiceId);

            if (invoice == null)
                throw new ArgumentException("Invoice not found");

            var html = $@"
<!DOCTYPE html>
<html>
<head>
    <title>Invoice {invoice.InvoiceNumber}</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 40px; }}
        .header {{ text-align: center; margin-bottom: 40px; }}
        .invoice-details {{ margin-bottom: 30px; }}
        .line-items {{ width: 100%; border-collapse: collapse; }}
        .line-items th, .line-items td {{ border: 1px solid #ddd; padding: 12px; text-align: left; }}
        .line-items th {{ background-color: #f2f2f2; }}
        .totals {{ margin-top: 20px; text-align: right; }}
        .total-row {{ font-weight: bold; }}
    </style>
</head>
<body>
    <div class='header'>
        <h1>INVOICE</h1>
        <h2>{invoice.InvoiceNumber}</h2>
    </div>
    
    <div class='invoice-details'>
        <p><strong>Issue Date:</strong> {invoice.IssueDate:yyyy-MM-dd}</p>
        <p><strong>Due Date:</strong> {invoice.DueDate:yyyy-MM-dd}</p>
        <p><strong>Period:</strong> {invoice.PeriodStart:yyyy-MM-dd} to {invoice.PeriodEnd:yyyy-MM-dd}</p>
        <p><strong>Status:</strong> {invoice.Status}</p>
    </div>
    
    <table class='line-items'>
        <thead>
            <tr>
                <th>Description</th>
                <th>Quantity</th>
                <th>Unit Price</th>
                <th>Amount</th>
            </tr>
        </thead>
        <tbody>";

            foreach (var item in invoice.LineItems)
            {
                html += $@"
            <tr>
                <td>{item.Description}</td>
                <td>{item.Quantity}</td>
                <td>{item.UnitPrice:C}</td>
                <td>{item.Amount:C}</td>
            </tr>";
            }

            html += $@"
        </tbody>
    </table>
    
    <div class='totals'>
        <p>Subtotal: {invoice.Subtotal:C}</p>
        <p>Tax: {invoice.TaxAmount:C}</p>
        <p>Discount: -{invoice.DiscountAmount:C}</p>
        <p class='total-row'>Total: {invoice.Total:C}</p>
    </div>
</body>
</html>";

            return html;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting invoice HTML for invoice {InvoiceId}", invoiceId);
            throw;
        }
    }

    public async Task ProcessDunningAsync()
    {
        try
        {
            var overdueInvoices = await GetOverdueInvoicesAsync();

            foreach (var invoice in overdueInvoices)
            {
                var daysPastDue = (DateTime.UtcNow - invoice.DueDate).Days;

                if (daysPastDue == 1 || daysPastDue == 7 || daysPastDue == 14)
                {
                    await SendInvoiceReminderAsync(invoice, daysPastDue);
                }
                else if (daysPastDue >= 30)
                {
                    var subscription = await _context.Set<Subscription>()
                        .FirstOrDefaultAsync(s => s.Id == invoice.SubscriptionId);

                    if (subscription != null)
                    {
                        subscription.Status = SubscriptionStatus.Unpaid;
                        subscription.UpdatedAt = DateTime.UtcNow;
                    }

                    invoice.Status = InvoiceStatus.Uncollectible;
                    invoice.UpdatedAt = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing dunning");
        }
    }

    public async Task<List<Invoice>> GetOverdueInvoicesAsync()
    {
        try
        {
            return await _context.Set<Invoice>()
                .Where(i => i.Status == InvoiceStatus.Open && i.DueDate < DateTime.UtcNow)
                .Include(i => i.Subscription)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting overdue invoices");
            throw;
        }
    }

    private string GenerateInvoiceNumber()
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd");
        var random = new Random().Next(1000, 9999);
        return $"INV-{timestamp}-{random}";
    }

    private decimal CalculateTax(decimal amount, Guid tenantId)
    {
        return amount * 0.10m;
    }

    private async Task SendInvoiceReminderAsync(Invoice invoice, int daysPastDue)
    {
        try
        {
            _logger.LogInformation("Sending invoice reminder for invoice {InvoiceId}, {DaysPastDue} days past due", 
                invoice.Id, daysPastDue);

            await Task.Delay(100);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending invoice reminder for invoice {InvoiceId}", invoice.Id);
        }
    }
}
