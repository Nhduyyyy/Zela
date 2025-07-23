using System.ComponentModel.DataAnnotations;

namespace Zela.Models;

/// <summary>
/// Lưu trữ lịch sử thanh toán PayOS
/// </summary>
public class PaymentTransaction
{
    [Key]
    public int TransactionId { get; set; }
    
    public int UserId { get; set; }
    
    [Required]
    [MaxLength(500)]
    public string PayOSOrderCode { get; set; }
    
    [Required]
    [MaxLength(500)]
    public string PayOSTransactionId { get; set; }
    
    [Required]
    public decimal Amount { get; set; }
    
    [Required]
    [MaxLength(20)]
    public string Currency { get; set; } = "VND";
    
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } // Pending, Success, Failed, Cancelled
    
    [MaxLength(1000)]
    public string? Description { get; set; }
    
    [MaxLength(500)]
    public string? PaymentMethod { get; set; } // Bank transfer, QR code, etc.
    
    [MaxLength(1000)]
    public string? PayOSResponse { get; set; } // JSON response from PayOS
    
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    public DateTime? CompletedAt { get; set; }
    
    // Navigation property
    public User User { get; set; }
} 