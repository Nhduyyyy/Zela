using Zela.Models;
using Zela.ViewModels;

namespace Zela.Services.Interface;

/// <summary>
/// Service interface for PayOS payment integration
/// </summary>
public interface IPayOSService
{
    /// <summary>
    /// Creates a new payment order with PayOS
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="planType">Plan type (e.g., "Premium Tháng", "Premium Năm")</param>
    /// <param name="amount">Payment amount in VND</param>
    /// <param name="description">Payment description</param>
    /// <returns>Payment order creation result</returns>
    Task<PayOSCreateOrderResult> CreatePaymentOrderAsync(int userId, string planType, decimal amount, string description);
    
    /// <summary>
    /// Verifies PayOS callback signature
    /// </summary>
    /// <param name="orderCode">Order code from PayOS</param>
    /// <param name="transactionId">Transaction ID from PayOS</param>
    /// <param name="amount">Payment amount</param>
    /// <param name="signature">Signature from PayOS</param>
    /// <returns>True if signature is valid</returns>
    Task<bool> VerifyCallbackAsync(string orderCode, string transactionId, decimal amount, string signature);
    
    /// <summary>
    /// Updates payment transaction status
    /// </summary>
    /// <param name="orderCode">Order code</param>
    /// <param name="transactionId">Transaction ID</param>
    /// <param name="status">Payment status</param>
    /// <returns>True if update successful</returns>
    Task<bool> UpdatePaymentStatusAsync(string orderCode, string transactionId, string status);
    
    /// <summary>
    /// Gets payment history for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>List of payment transactions</returns>
    Task<List<PaymentTransaction>> GetPaymentHistoryAsync(int userId);
    
    /// <summary>
    /// Gets current active subscription for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>Active subscription or null</returns>
    Task<Subscription?> GetCurrentSubscriptionAsync(int userId);
    
    /// <summary>
    /// Activates premium subscription for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="planType">Plan type</param>
    /// <param name="durationDays">Subscription duration in days</param>
    /// <param name="amount">Payment amount</param>
    /// <param name="payOSOrderCode">PayOS order code</param>
    /// <param name="payOSTransactionId">PayOS transaction ID</param>
    /// <returns>True if activation successful</returns>
    Task<bool> ActivatePremiumAsync(int userId, string planType, int durationDays, decimal amount, string payOSOrderCode, string payOSTransactionId);

    /// <summary>
    /// Gets a payment transaction by order code
    /// </summary>
    /// <param name="orderCode">Order code</param>
    /// <returns>PaymentTransaction or null</returns>
    Task<PaymentTransaction?> GetPaymentTransactionByOrderCodeAsync(string orderCode);
}

/// <summary>
/// Result of PayOS payment order creation
/// </summary>
public class PayOSCreateOrderResult
{
    public bool Success { get; set; }
    public string? OrderCode { get; set; }
    public string? PaymentUrl { get; set; }
    public string? QRCode { get; set; }
    public string? ErrorMessage { get; set; }
} 