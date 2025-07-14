namespace Zela.ViewModels;

/// <summary>
/// ViewModel for premium plan display
/// </summary>
public class PremiumPlanViewModel
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public int Duration { get; set; }
    public List<string> Features { get; set; } = new();
    public bool Popular { get; set; }
}

/// <summary>
/// Request model for creating payment order
/// </summary>
public class CreateOrderRequest
{
    public string PlanId { get; set; } = "";
}

/// <summary>
/// PayOS callback data model
/// </summary>
public class PayOSCallbackData
{
    public string OrderCode { get; set; } = "";
    public string TransactionId { get; set; } = "";
    public decimal Amount { get; set; }
    public string Status { get; set; } = "";
    public string Signature { get; set; } = "";
}

/// <summary>
/// Premium plan information
/// </summary>
public class PremiumPlanInfo
{
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public int Duration { get; set; }
}

/// <summary>
/// Payment history view model
/// </summary>
public class PaymentHistoryViewModel
{
    public List<Zela.Models.PaymentTransaction> Transactions { get; set; } = new();
    public Zela.Models.Subscription? CurrentSubscription { get; set; }
} 