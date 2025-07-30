using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Application.Common.Interfaces;
using SubscriptionService.Services;
using SubscriptionService.Models;
using Shared.Domain.Entities;

namespace SubscriptionService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly IBillingService _billingService;
    private readonly ITenantService _tenantService;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
        IPaymentService paymentService,
        IBillingService billingService,
        ITenantService tenantService,
        ILogger<PaymentsController> logger)
    {
        _paymentService = paymentService;
        _billingService = billingService;
        _tenantService = tenantService;
        _logger = logger;
    }

    [HttpGet("methods")]
    public async Task<IActionResult> GetPaymentMethods()
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var paymentMethods = await _paymentService.GetPaymentMethodsAsync(tenantId);

            return Ok(paymentMethods.Select(pm => new
            {
                pm.Id,
                Type = pm.Type.ToString(),
                pm.Last4,
                pm.Brand,
                pm.ExpiryMonth,
                pm.ExpiryYear,
                pm.IsDefault,
                pm.CreatedAt
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment methods");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("methods")]
    public async Task<IActionResult> CreatePaymentMethod([FromBody] CreatePaymentMethodRequest request)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var paymentMethod = await _paymentService.CreatePaymentMethodAsync(request, tenantId);

            return Ok(new
            {
                paymentMethod.Id,
                Type = paymentMethod.Type.ToString(),
                paymentMethod.Last4,
                paymentMethod.Brand,
                paymentMethod.IsDefault,
                Message = "Payment method created successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment method");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPut("methods/{paymentMethodId}/default")]
    public async Task<IActionResult> SetDefaultPaymentMethod(Guid paymentMethodId)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var updated = await _paymentService.SetDefaultPaymentMethodAsync(paymentMethodId, tenantId);

            if (!updated)
            {
                return NotFound(new { message = "Payment method not found" });
            }

            return Ok(new { message = "Default payment method updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting default payment method {PaymentMethodId}", paymentMethodId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpDelete("methods/{paymentMethodId}")]
    public async Task<IActionResult> DeletePaymentMethod(Guid paymentMethodId)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var deleted = await _paymentService.DeletePaymentMethodAsync(paymentMethodId, tenantId);

            if (!deleted)
            {
                return NotFound(new { message = "Payment method not found" });
            }

            return Ok(new { message = "Payment method deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting payment method {PaymentMethodId}", paymentMethodId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("intents")]
    public async Task<IActionResult> CreatePaymentIntent([FromBody] CreatePaymentIntentRequest request)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var paymentIntentId = await _paymentService.CreatePaymentIntentAsync(request.Amount, request.Currency, tenantId);

            return Ok(new
            {
                PaymentIntentId = paymentIntentId,
                Message = "Payment intent created successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment intent");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("invoices")]
    public async Task<IActionResult> GetInvoices([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var invoices = await _billingService.GetInvoicesAsync(tenantId, page, pageSize);

            return Ok(invoices.Select(i => new
            {
                i.Id,
                i.InvoiceNumber,
                Status = i.Status.ToString(),
                i.Subtotal,
                i.TaxAmount,
                i.DiscountAmount,
                i.Total,
                i.Currency,
                i.IssueDate,
                i.DueDate,
                i.PaidDate,
                i.PeriodStart,
                i.PeriodEnd,
                PlanName = i.Subscription.Plan.Name
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting invoices");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("invoices/{invoiceId}")]
    public async Task<IActionResult> GetInvoice(Guid invoiceId)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var invoice = await _billingService.GetInvoiceAsync(invoiceId, tenantId);

            if (invoice == null)
            {
                return NotFound(new { message = "Invoice not found" });
            }

            return Ok(new
            {
                invoice.Id,
                invoice.InvoiceNumber,
                Status = invoice.Status.ToString(),
                invoice.Subtotal,
                invoice.TaxAmount,
                invoice.DiscountAmount,
                invoice.Total,
                invoice.Currency,
                invoice.IssueDate,
                invoice.DueDate,
                invoice.PaidDate,
                invoice.PeriodStart,
                invoice.PeriodEnd,
                PlanName = invoice.Subscription.Plan.Name,
                LineItems = invoice.LineItems.Select(li => new
                {
                    li.Id,
                    li.Description,
                    li.Quantity,
                    li.UnitPrice,
                    li.Amount
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting invoice {InvoiceId}", invoiceId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("invoices/{invoiceId}/pay")]
    public async Task<IActionResult> PayInvoice(Guid invoiceId)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var paid = await _billingService.PayInvoiceAsync(invoiceId, tenantId);

            if (!paid)
            {
                return BadRequest(new { message = "Unable to process payment" });
            }

            return Ok(new { message = "Invoice paid successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error paying invoice {InvoiceId}", invoiceId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("invoices/{invoiceId}/pdf")]
    public async Task<IActionResult> GetInvoicePdf(Guid invoiceId)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var pdfBytes = await _billingService.GenerateInvoicePdfAsync(invoiceId, tenantId);

            return File(pdfBytes, "application/pdf", $"invoice-{invoiceId}.pdf");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating invoice PDF {InvoiceId}", invoiceId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("billing-address")]
    public async Task<IActionResult> GetBillingAddress()
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var address = await _billingService.GetBillingAddressAsync(tenantId);

            if (address == null)
            {
                return NotFound(new { message = "Billing address not found" });
            }

            return Ok(new
            {
                address.Id,
                address.CompanyName,
                address.AddressLine1,
                address.AddressLine2,
                address.City,
                address.State,
                address.PostalCode,
                address.Country,
                address.TaxId,
                address.IsDefault
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting billing address");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPut("billing-address")]
    public async Task<IActionResult> UpdateBillingAddress([FromBody] BillingAddress address)
    {
        try
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var updated = await _billingService.UpdateBillingAddressAsync(address, tenantId);

            if (!updated)
            {
                return BadRequest(new { message = "Unable to update billing address" });
            }

            return Ok(new { message = "Billing address updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating billing address");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}

public class CreatePaymentIntentRequest
{
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
}
