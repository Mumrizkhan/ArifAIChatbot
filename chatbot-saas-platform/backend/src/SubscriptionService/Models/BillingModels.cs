public class CouponResult
{
    public bool IsValid { get; set; }
    public decimal DiscountAmount { get; set; }
    public double DiscountPercentage { get; set; }
    public string? ErrorMessage { get; set; }
}

public class TaxRate
{
    public string Country { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public string Description { get; set; } = string.Empty;
}