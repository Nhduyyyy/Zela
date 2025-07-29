using System.ComponentModel.DataAnnotations;

namespace Zela.Models;

/// <summary>
/// Quản lý gói Premium của user
/// </summary>
public class Subscription
{
    [Key]
    public int SubscriptionId { get; set; }
    
    public int UserId { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string PlanType { get; set; } = "Premium"; // Premium, Pro, etc.
    
    [Required]
    public DateTime StartDate { get; set; }
    
    [Required]
    public DateTime EndDate { get; set; }
    
    [Required]
    public decimal Amount { get; set; }
    
    [Required]
    [MaxLength(20)]
    public string Currency { get; set; } = "VND";
    
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Active"; // Active, Expired, Cancelled
    
    [MaxLength(500)]
    public string? PayOSTransactionId { get; set; }
    
    [MaxLength(500)]
    public string? PayOSOrderCode { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation property
    public User User { get; set; }
}

public static class SubscriptionStatus
{
    public const string Active = "Active";
    public const string Expired = "Expired";
    public const string Cancelled = "Cancelled";
} 