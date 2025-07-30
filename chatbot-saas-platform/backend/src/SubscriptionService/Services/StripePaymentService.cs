using Microsoft.EntityFrameworkCore;
using Shared.Application.Common.Interfaces;
using SubscriptionService.Services;
using SubscriptionService.Models;
using Stripe;
using Shared.Domain.Entities;
using PaymentMethod = Shared.Domain.Entities.PaymentMethod;

namespace SubscriptionService.Services;

public class StripePaymentService : IPaymentService
{
    private readonly IApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<StripePaymentService> _logger;

    public StripePaymentService(
        IApplicationDbContext context,
        IConfiguration configuration,
        ILogger<StripePaymentService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<Shared.Domain.Entities.PaymentMethod> CreatePaymentMethodAsync(CreatePaymentMethodRequest request, Guid tenantId)
    {
        try
        {
            if (string.IsNullOrEmpty(request.StripePaymentMethodId))
                throw new ArgumentException("Stripe payment method ID is required");

            var stripeService = new Stripe.PaymentMethodService();
            var stripePaymentMethod = await stripeService.GetAsync(request.StripePaymentMethodId);

            var paymentMethod = new Shared.Domain.Entities.PaymentMethod
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Type = request.Type,
                StripePaymentMethodId = request.StripePaymentMethodId,
                Last4 = stripePaymentMethod.Card?.Last4 ?? "",
                Brand = stripePaymentMethod.Card?.Brand ?? "",
                ExpiryMonth = (int)(stripePaymentMethod.Card?.ExpMonth ?? 0),
                ExpiryYear = (int)(stripePaymentMethod.Card?.ExpYear ?? 0),
                IsDefault = request.SetAsDefault,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            if (request.SetAsDefault)
            {
                var existingPaymentMethods = await _context.Set<Shared.Domain.Entities.PaymentMethod>()
                    .Where(pm => pm.TenantId == tenantId && pm.IsDefault)
                    .ToListAsync();

                foreach (var existing in existingPaymentMethods)
                {
                    existing.IsDefault = false;
                    existing.UpdatedAt = DateTime.UtcNow;
                }
            }

            _context.Set<Shared.Domain.Entities.PaymentMethod>().Add(paymentMethod);
            await _context.SaveChangesAsync();

            return paymentMethod;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment method for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<List<Shared.Domain.Entities.PaymentMethod>> GetPaymentMethodsAsync(Guid tenantId)
    {
        try
        {
            return await _context.Set<Shared.Domain.Entities.PaymentMethod>()
                .Where(pm => pm.TenantId == tenantId && pm.IsActive)
                .OrderByDescending(pm => pm.IsDefault)
                .ThenByDescending(pm => pm.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment methods for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<PaymentMethod?> GetDefaultPaymentMethodAsync(Guid tenantId)
    {
        try
        {
            return await _context.Set<Shared.Domain.Entities.PaymentMethod>()
                .FirstOrDefaultAsync(pm => pm.TenantId == tenantId && pm.IsDefault && pm.IsActive);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting default payment method for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<bool> SetDefaultPaymentMethodAsync(Guid paymentMethodId, Guid tenantId)
    {
        try
        {
            var paymentMethod = await _context.Set<Shared.Domain.Entities.PaymentMethod>()
                .FirstOrDefaultAsync(pm => pm.Id == paymentMethodId && pm.TenantId == tenantId);

            if (paymentMethod == null)
                return false;

            var existingDefaults = await _context.Set<Shared.Domain.Entities.PaymentMethod>()
                .Where(pm => pm.TenantId == tenantId && pm.IsDefault)
                .ToListAsync();

            foreach (var existing in existingDefaults)
            {
                existing.IsDefault = false;
                existing.UpdatedAt = DateTime.UtcNow;
            }

            paymentMethod.IsDefault = true;
            paymentMethod.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting default payment method {PaymentMethodId}", paymentMethodId);
            return false;
        }
    }

    public async Task<bool> DeletePaymentMethodAsync(Guid paymentMethodId, Guid tenantId)
    {
        try
        {
            var paymentMethod = await _context.Set<PaymentMethod>()
                .FirstOrDefaultAsync(pm => pm.Id == paymentMethodId && pm.TenantId == tenantId);

            if (paymentMethod == null)
                return false;

            if (!string.IsNullOrEmpty(paymentMethod.StripePaymentMethodId))
            {
                var stripeService = new Stripe.PaymentMethodService();
                await stripeService.DetachAsync(paymentMethod.StripePaymentMethodId);
            }

            paymentMethod.IsActive = false;
            paymentMethod.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting payment method {PaymentMethodId}", paymentMethodId);
            return false;
        }
    }

    public async Task<string> CreatePaymentIntentAsync(decimal amount, string currency, Guid tenantId)
    {
        try
        {
            var paymentIntentService = new PaymentIntentService();
            var paymentIntent = await paymentIntentService.CreateAsync(new PaymentIntentCreateOptions
            {
                Amount = (long)(amount * 100), // Convert to cents
                Currency = currency.ToLower(),
                Metadata = new Dictionary<string, string>
                {
                    ["tenant_id"] = tenantId.ToString()
                }
            });

            return paymentIntent.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment intent for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<bool> ProcessPaymentAsync(string paymentIntentId, Guid tenantId)
    {
        try
        {
            var paymentIntentService = new PaymentIntentService();
            var paymentIntent = await paymentIntentService.GetAsync(paymentIntentId);

            return paymentIntent.Status == "succeeded";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment {PaymentIntentId} for tenant {TenantId}", paymentIntentId, tenantId);
            return false;
        }
    }

    public async Task HandleWebhookAsync(string payload, string signature)
    {
        try
        {
            var webhookSecret = _configuration["Stripe:WebhookSecret"];
            var stripeEvent = EventUtility.ConstructEvent(payload, signature, webhookSecret);

            _logger.LogInformation("Received Stripe webhook: {EventType}", stripeEvent.Type);

            switch (stripeEvent.Type)
            {
                case EventTypes.InvoicePaymentSucceeded:
                    await HandleInvoicePaymentSucceeded(stripeEvent);
                    break;
                case EventTypes.InvoicePaymentFailed:
                    await HandleInvoicePaymentFailed(stripeEvent);
                    break;
                case EventTypes.CustomerSubscriptionUpdated:
                    await HandleSubscriptionUpdated(stripeEvent);
                    break;
                case EventTypes.CustomerSubscriptionDeleted:
                    await HandleSubscriptionDeleted(stripeEvent);
                    break;
                default:
                    _logger.LogInformation("Unhandled webhook event type: {EventType}", stripeEvent.Type);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling Stripe webhook");
            throw;
        }
    }

    public async Task<string> CreateCustomerAsync(Guid tenantId, string email, string name)
    {
        try
        {
            var customerService = new CustomerService();
            var customer = await customerService.CreateAsync(new CustomerCreateOptions
            {
                Email = email,
                Name = name,
                Metadata = new Dictionary<string, string>
                {
                    ["tenant_id"] = tenantId.ToString()
                }
            });

            return customer.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Stripe customer for tenant {TenantId}", tenantId);
            throw;
        }
    }

    private async Task HandleInvoicePaymentSucceeded(Event stripeEvent)
    {
        var invoice = stripeEvent.Data.Object as Stripe.Invoice;

        string subscriptionId = GetSubscriptionId(invoice);
        if (subscriptionId == null) return;

        var subscription = await _context.Set<Shared.Domain.Entities.Subscription>()
            .FirstOrDefaultAsync(s => s.StripeSubscriptionId == subscriptionId);

        if (subscription != null)
        {
            var localInvoice = await _context.Set<Shared.Domain.Entities.Invoice>()
                .FirstOrDefaultAsync(i => i.StripeInvoiceId == invoice.Id);

            if (localInvoice != null)
            {
                localInvoice.Status = InvoiceStatus.Paid;
                localInvoice.PaidDate = DateTime.UtcNow;
                localInvoice.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }
    }

    private string GetSubscriptionId(Stripe.Invoice? invoice)
    {
        string subscriptionId = null;

        foreach (var lineItem in invoice.Lines.Data)
        {
            if (lineItem.Subscription != null)
            {
                subscriptionId = lineItem.Subscription.Id;
                break;
            }
        }

        return subscriptionId;
    }

    private async Task HandleInvoicePaymentFailed(Event stripeEvent)
    {
        var invoice = stripeEvent.Data.Object as Stripe.Invoice;
        string subscriptionId = GetSubscriptionId(invoice);
        if (subscriptionId == null) return;

        var subscription = await _context.Set<Shared.Domain.Entities.Subscription>()
            .FirstOrDefaultAsync(s => s.StripeSubscriptionId == subscriptionId);

        if (subscription != null)
        {
            subscription.Status = SubscriptionStatus.PastDue;
            subscription.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    private async Task HandleSubscriptionUpdated(Event stripeEvent)
    {
        var stripeSubscription = stripeEvent.Data.Object as Stripe.Subscription;
        if (stripeSubscription == null) return;

        var subscription = await _context.Set<Shared.Domain.Entities.Subscription>()
            .FirstOrDefaultAsync(s => s.StripeSubscriptionId == stripeSubscription.Id);

        if (subscription != null)
        {
            subscription.Status = MapStripeStatus(stripeSubscription.Status);
            subscription.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    private async Task HandleSubscriptionDeleted(Event stripeEvent)
    {
        var stripeSubscription = stripeEvent.Data.Object as Stripe.Subscription;
        if (stripeSubscription == null) return;

        var subscription = await _context.Set<Shared.Domain.Entities.Subscription>()
            .FirstOrDefaultAsync(s => s.StripeSubscriptionId == stripeSubscription.Id);

        if (subscription != null)
        {
            subscription.Status = SubscriptionStatus.Cancelled;
            subscription.EndDate = DateTime.UtcNow;
            subscription.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    private SubscriptionStatus MapStripeStatus(string stripeStatus)
    {
        return stripeStatus switch
        {
            "active" => SubscriptionStatus.Active,
            "canceled" => SubscriptionStatus.Cancelled,
            "incomplete" => SubscriptionStatus.Incomplete,
            "incomplete_expired" => SubscriptionStatus.IncompleteExpired,
            "past_due" => SubscriptionStatus.PastDue,
            "trialing" => SubscriptionStatus.Trialing,
            "unpaid" => SubscriptionStatus.Unpaid,
            _ => SubscriptionStatus.Inactive
        };
    }
}
