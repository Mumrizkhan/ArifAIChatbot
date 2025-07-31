using Microsoft.EntityFrameworkCore;
using Shared.Application.Common.Interfaces;
using SubscriptionService.Services;
using SubscriptionService.Models;
using Shared.Domain.Entities;

namespace SubscriptionService.Services;

public class BillingService : IBillingService
{
    private readonly IApplicationDbContext _context;
    private readonly IPaymentService _paymentService;
    private readonly ILogger<BillingService> _logger;

    public BillingService(
        IApplicationDbContext context,
        IPaymentService paymentService,
        ILogger<BillingService> logger)
    {
        _context = context;
        _paymentService = paymentService;
        _logger = logger;
    }

    public async Task<Invoice> CreateInvoiceAsync(Guid subscriptionId, DateTime periodStart, DateTime periodEnd)
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
            _logger.LogError(ex, "Error creating invoice for subscription {SubscriptionId}", subscriptionId);
            throw;
        }
    }

    public async Task<Invoice?> GetInvoiceAsync(Guid invoiceId, Guid tenantId)
    {
        try
        {
            return await _context.Set<Invoice>()
                .Include(i => i.LineItems)
                .Include(i => i.Subscription)
                .ThenInclude(s => s.Plan)
                .FirstOrDefaultAsync(i => i.Id == invoiceId && i.TenantId == tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting invoice {InvoiceId}", invoiceId);
            throw;
        }
    }

    public async Task<List<Invoice>> GetInvoicesAsync(Guid tenantId, int page = 1, int pageSize = 20)
    {
        try
        {
            return await _context.Set<Invoice>()
                .Include(i => i.Subscription)
                .ThenInclude(s => s.Plan)
                .Where(i => i.TenantId == tenantId)
                .OrderByDescending(i => i.IssueDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting invoices for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<bool> PayInvoiceAsync(Guid invoiceId, Guid tenantId)
    {
        try
        {
            var invoice = await GetInvoiceAsync(invoiceId, tenantId);
            if (invoice == null || invoice.Status == InvoiceStatus.Paid)
                return false;

            var paymentIntentId = await _paymentService.CreatePaymentIntentAsync(
                invoice.Total, 
                invoice.Currency, 
                tenantId);

            var paymentSuccessful = await _paymentService.ProcessPaymentAsync(paymentIntentId, tenantId);

            if (paymentSuccessful)
            {
                invoice.Status = InvoiceStatus.Paid;
                invoice.PaidDate = DateTime.UtcNow;
                invoice.PaymentIntentId = paymentIntentId;
                invoice.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error paying invoice {InvoiceId}", invoiceId);
            return false;
        }
    }

    public async Task<bool> VoidInvoiceAsync(Guid invoiceId, Guid tenantId)
    {
        try
        {
            var invoice = await GetInvoiceAsync(invoiceId, tenantId);
            if (invoice == null || invoice.Status == InvoiceStatus.Paid)
                return false;

            invoice.Status = InvoiceStatus.Void;
            invoice.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error voiding invoice {InvoiceId}", invoiceId);
            return false;
        }
    }

    public async Task<byte[]> GenerateInvoicePdfAsync(Guid invoiceId, Guid tenantId)
    {
        try
        {
            var invoice = await GetInvoiceAsync(invoiceId, tenantId);
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

    public async Task ProcessOverdueInvoicesAsync()
    {
        try
        {
            var overdueInvoices = await _context.Set<Invoice>()
                .Where(i => i.Status == InvoiceStatus.Open && i.DueDate < DateTime.UtcNow)
                .ToListAsync();

            foreach (var invoice in overdueInvoices)
            {
                try
                {
                    var subscription = await _context.Set<Subscription>()
                        .FirstOrDefaultAsync(s => s.Id == invoice.SubscriptionId);

                    if (subscription != null)
                    {
                        subscription.Status = SubscriptionStatus.PastDue;
                        subscription.UpdatedAt = DateTime.UtcNow;
                    }

                    
                    _logger.LogInformation("Processed overdue invoice {InvoiceId}", invoice.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing overdue invoice {InvoiceId}", invoice.Id);
                }
            }

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing overdue invoices");
        }
    }

    public async Task<BillingAddress?> GetBillingAddressAsync(Guid tenantId)
    {
        try
        {
            return await _context.Set<BillingAddress>()
                .FirstOrDefaultAsync(ba => ba.TenantId == tenantId && ba.IsDefault);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting billing address for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<bool> UpdateBillingAddressAsync(BillingAddress address, Guid tenantId)
    {
        try
        {
            var existingAddress = await GetBillingAddressAsync(tenantId);
            
            if (existingAddress != null)
            {
                existingAddress.CompanyName = address.CompanyName;
                existingAddress.AddressLine1 = address.AddressLine1;
                existingAddress.AddressLine2 = address.AddressLine2;
                existingAddress.City = address.City;
                existingAddress.State = address.State;
                existingAddress.PostalCode = address.PostalCode;
                existingAddress.Country = address.Country;
                existingAddress.TaxId = address.TaxId;
                existingAddress.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                address.Id = Guid.NewGuid();
                address.TenantId = tenantId;
                address.IsDefault = true;
                address.CreatedAt = DateTime.UtcNow;
                _context.Set<BillingAddress>().Add(address);
            }

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating billing address for tenant {TenantId}", tenantId);
            return false;
        }
    }

    public async Task<CouponResult> ApplyCouponAsync(string couponCode, Guid tenantId)
    {
        // TODO: Implement actual coupon logic
        if (couponCode == "DISCOUNT10")
        {
            return new CouponResult
            {
                IsValid = true,
                DiscountAmount = 10,
                DiscountPercentage = 10,
                ErrorMessage = null
            };
        }
        return new CouponResult
        {
            IsValid = false,
            DiscountAmount = 0,
            DiscountPercentage = 0,
            ErrorMessage = "Invalid coupon code"
        };
    }

    public async Task<List<TaxRate>> GetTaxRatesAsync(string? country, string? state)
    {
        // TODO: Implement actual tax rate lookup
        return new List<TaxRate>
        {
            new TaxRate
            {
                Country = country ?? "US",
                State = state ?? "CA",
                Rate = 0.075m,
                Description = "California Sales Tax"
            }
        };
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

    private async Task<string> GetInvoiceHtmlAsync(Guid invoiceId)
    {
        return $@"
        <html>
        <head><title>Invoice {invoiceId}</title></head>
        <body>
            <h1>Invoice</h1>
            <p>Invoice ID: {invoiceId}</p>
            <p>Generated on: {DateTime.UtcNow:yyyy-MM-dd}</p>
        </body>
        </html>";
    }
}
