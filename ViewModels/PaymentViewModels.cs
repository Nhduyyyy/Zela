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

public static class PaymentPlans
{
    public static readonly List<PremiumPlanViewModel> All = new()
    {
        new() { Id = "monthly", Name = "Premium Tháng", Price = 99000, Duration = 30, Features = new() { "Video call không giới hạn", "Ghi âm/ghi hình HD", "Chia sẻ màn hình", "Whiteboard cộng tác", "Quiz không giới hạn", "Hỗ trợ ưu tiên" } },
        new() { Id = "yearly", Name = "Premium Năm", Price = 990000, Duration = 365, Popular = true, Features = new() { "Tất cả tính năng Premium Tháng", "Tiết kiệm 17% so với gói tháng", "Ưu tiên hỗ trợ 24/7", "Tính năng beta sớm nhất" } }
    };
    public static PremiumPlanViewModel? Get(string id) => All.FirstOrDefault(p => p.Id == id);
} 