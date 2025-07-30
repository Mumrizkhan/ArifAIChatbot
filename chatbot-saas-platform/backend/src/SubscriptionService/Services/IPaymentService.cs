using Shared.Domain.Entities;
using SubscriptionService.Models;

namespace SubscriptionService.Services;

public interface IPaymentService
{
    Task<PaymentMethod> CreatePaymentMethodAsync(CreatePaymentMethodRequest request, Guid tenantId);
    Task<List<PaymentMethod>> GetPaymentMethodsAsync(Guid tenantId);
    Task<PaymentMethod?> GetDefaultPaymentMethodAsync(Guid tenantId);
    Task<bool> SetDefaultPaymentMethodAsync(Guid paymentMethodId, Guid tenantId);
    Task<bool> DeletePaymentMethodAsync(Guid paymentMethodId, Guid tenantId);
    Task<string> CreatePaymentIntentAsync(decimal amount, string currency, Guid tenantId);
    Task<bool> ProcessPaymentAsync(string paymentIntentId, Guid tenantId);
    Task HandleWebhookAsync(string payload, string signature);
    Task<string> CreateCustomerAsync(Guid tenantId, string email, string name);
}
