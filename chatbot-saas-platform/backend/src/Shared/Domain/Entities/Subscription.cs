using Shared.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Domain.Entities
{

    public class Subscription : AuditableEntity
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid PlanId { get; set; }
        public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime NextBillingDate { get; set; }
        public BillingCycle BillingCycle { get; set; } = BillingCycle.Monthly;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";
        public string? StripeSubscriptionId { get; set; }
        public string? StripeCustomerId { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
        public int TrialDays { get; set; }
        public DateTime? TrialEndDate { get; set; }
        public bool IsTrialActive => TrialEndDate.HasValue && TrialEndDate > DateTime.UtcNow;
        public Plan Plan { get; set; } = null!;
        public List<Invoice> Invoices { get; set; } = new();
        public List<UsageRecord> UsageRecords { get; set; } = new();
    }

    public class Plan : AuditableEntity
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal MonthlyPrice { get; set; }
        public decimal YearlyPrice { get; set; }
        public string Currency { get; set; } = "USD";
        public bool IsActive { get; set; } = true;
        public bool IsPublic { get; set; } = true;
        public PlanType Type { get; set; } = PlanType.Standard;
        public Dictionary<string, PlanFeature> Features { get; set; } = new();
        public Dictionary<string, int> Limits { get; set; } = new();
        public string? StripePriceIdMonthly { get; set; }
        public string? StripePriceIdYearly { get; set; }
        public int TrialDays { get; set; } = 14;
        public int SortOrder { get; set; }
        public List<Subscription> Subscriptions { get; set; } = new();
    }

    public class PlanFeature
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }
        public int? Limit { get; set; }
        public Dictionary<string, object> Configuration { get; set; } = new();
    }

    public class Invoice : AuditableEntity
    {
        public Guid Id { get; set; }
        public Guid SubscriptionId { get; set; }
        public Guid TenantId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
        public decimal Subtotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal Total { get; set; }
        public string Currency { get; set; } = "USD";
        public DateTime IssueDate { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? PaidDate { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public string? StripeInvoiceId { get; set; }
        public string? PaymentIntentId { get; set; }
        public List<InvoiceLineItem> LineItems { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
        public Subscription Subscription { get; set; } = null!;
    }

    public class InvoiceLineItem : AuditableEntity
    {
        public Guid Id { get; set; }
        public Guid InvoiceId { get; set; }
        public string Description { get; set; } = string.Empty;
        public int Quantity { get; set; } = 1;
        public decimal UnitPrice { get; set; }
        public decimal Amount { get; set; }
        public string? ProductId { get; set; }
        public string? PriceId { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
        public Invoice Invoice { get; set; } = null!;
    }

    public class UsageRecord : AuditableEntity
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid? SubscriptionId { get; set; }
        public string MetricName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public DateTime RecordedAt { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
        public Subscription? Subscription { get; set; }
    }

    public class PaymentMethod : AuditableEntity
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public PaymentMethodType Type { get; set; }
        public string? StripePaymentMethodId { get; set; }
        public string Last4 { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public int ExpiryMonth { get; set; }
        public int ExpiryYear { get; set; }
        public bool IsDefault { get; set; }
        public bool IsActive { get; set; } = true;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class BillingAddress : AuditableEntity
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string AddressLine1 { get; set; } = string.Empty;
        public string? AddressLine2 { get; set; }
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string? TaxId { get; set; }
        public bool IsDefault { get; set; }
    }

    public enum SubscriptionStatus
    {
        Active,
        Inactive,
        Cancelled,
        PastDue,
        Unpaid,
        Trialing,
        Incomplete,
        IncompleteExpired
    }

    public enum BillingCycle
    {
        Monthly,
        Yearly,
        Quarterly
    }

    public enum PlanType
    {
        Free,
        Basic,
        Standard,
        Premium,
        Enterprise,
        Custom
    }

    public enum InvoiceStatus
    {
        Draft,
        Open,
        Paid,
        Void,
        Uncollectible
    }

    public enum PaymentMethodType
    {
        Card,
        BankAccount,
        PayPal,
        Other
    }
}
