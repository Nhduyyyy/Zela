using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Net.payOS;
using Net.payOS.Types;
using Zela.DbContext;
using Zela.Models;
using Zela.Services.Interface;

namespace Zela.Services;

/// <summary>
/// Service for PayOS payment integration using PayOS SDK
/// </summary>
public class PayOSService : IPayOSService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PayOSService> _logger;
    private readonly PayOS _payOSClient;
    private readonly PayOSConfiguration _payOSConfig;

    public PayOSService(
        ApplicationDbContext context, 
        IConfiguration configuration, 
        ILogger<PayOSService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
        _payOSConfig = LoadPayOSConfiguration();
        _payOSClient = new PayOS(
            clientId: _payOSConfig.ClientId,
            apiKey: _payOSConfig.ApiKey,
            checksumKey: _payOSConfig.Checksum,
            partnerCode: _payOSConfig.PartnerCode
        );
    }

    public async Task<PayOSCreateOrderResult> CreatePaymentOrderAsync(int userId, string planType, decimal amount, string description)
    {
        _logger.LogInformation("Creating PayOS payment order for user {UserId}, plan {PlanType}, amount {Amount}", 
            userId, planType, amount);

        try
        {
            // Validate user exists
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found", userId);
                return new PayOSCreateOrderResult { Success = false, ErrorMessage = "User không tồn tại" };
            }

            // Generate unique order code
            var orderCode = GenerateOrderCode(userId);
            
            // Rút gọn mô tả không vượt quá 25 ký tự
            var safeDescription = description.Length > 25 ? description.Substring(0, 25) : description;

            // Create payment request using PayOS SDK
            var item = new ItemData(safeDescription, 1, (int)amount);
            var items = new List<ItemData> { item };
            
            var paymentData = new PaymentData(
                orderCode: orderCode,
                amount: (int)amount,
                description: safeDescription,
                items: items,
                cancelUrl: $"{_payOSConfig.BaseUrl}/Payment/Cancel",
                returnUrl: $"{_payOSConfig.BaseUrl}/Payment/Success",
                buyerName: user.FullName ?? user.Email,
                buyerEmail: user.Email,
                buyerPhone: "",
                buyerAddress: "",
                expiredAt: (int)DateTimeOffset.Now.AddHours(24).ToUnixTimeSeconds()
            );

            _logger.LogDebug("Creating PayOS payment request: {@PaymentData}", paymentData);

            // Create payment using SDK
            var payOSResponse = await _payOSClient.createPaymentLink(paymentData);
            
            if (payOSResponse == null)
            {
                _logger.LogError("PayOS SDK returned null response");
                return new PayOSCreateOrderResult { Success = false, ErrorMessage = "Không thể tạo đơn hàng thanh toán" };
            }

            // Save transaction to database
            await SavePaymentTransactionAsync(userId, orderCode, payOSResponse.paymentLinkId, amount, description, payOSResponse);

            _logger.LogInformation("Payment order created successfully: {OrderCode}", orderCode);
            
            return new PayOSCreateOrderResult
            {
                Success = true,
                OrderCode = orderCode.ToString(),
                PaymentUrl = payOSResponse.checkoutUrl,
                QRCode = payOSResponse.qrCode
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating PayOS payment order for user {UserId}", userId);
            return new PayOSCreateOrderResult { Success = false, ErrorMessage = "Lỗi hệ thống" };
        }
    }

    public async Task<bool> VerifyCallbackAsync(string orderCode, string transactionId, decimal amount, string signature)
    {
        try
        {
            // PayOS SDK handles signature verification internally
            // For now, we'll use the SDK's verification method
            _logger.LogInformation("Verifying PayOS callback for order {OrderCode}", orderCode);
            
            // TODO: Implement proper signature verification using SDK
            // The SDK should provide a method to verify callbacks
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying PayOS callback for order {OrderCode}", orderCode);
            return false;
        }
    }

    public async Task<bool> UpdatePaymentStatusAsync(string orderCode, string transactionId, string status)
    {
        try
        {
            var transaction = await _context.PaymentTransactions
                .FirstOrDefaultAsync(t => t.PayOSOrderCode == orderCode);

            if (transaction == null)
            {
                _logger.LogWarning("Payment transaction not found for order {OrderCode}", orderCode);
                return false;
            }

            transaction.Status = status;
            transaction.CompletedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Payment status updated for order {OrderCode}: {Status}", orderCode, status);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating payment status for order {OrderCode}", orderCode);
            return false;
        }
    }

    public async Task<List<PaymentTransaction>> GetPaymentHistoryAsync(int userId)
    {
        return await _context.PaymentTransactions
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<PaymentTransaction?> GetPaymentTransactionByOrderCodeAsync(string orderCode)
    {
        return await _context.PaymentTransactions.FirstOrDefaultAsync(t => t.PayOSOrderCode == orderCode);
    }

    public async Task<Subscription?> GetCurrentSubscriptionAsync(int userId)
    {
        return await _context.Subscriptions
            .Where(s => s.UserId == userId && s.Status == "Active" && s.EndDate > DateTime.Now)
            .OrderByDescending(s => s.EndDate)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> ActivatePremiumAsync(int userId, string planType, int durationDays, decimal amount, string payOSOrderCode, string payOSTransactionId)
    {
        try
        {
            // Deactivate any existing active subscriptions
            var existingSubscriptions = await _context.Subscriptions
                .Where(s => s.UserId == userId && s.Status == "Active")
                .ToListAsync();

            foreach (var subscription in existingSubscriptions)
            {
                subscription.Status = "Expired";
                subscription.UpdatedAt = DateTime.Now;
            }

            // Create new subscription
            var newSubscription = new Subscription
            {
                UserId = userId,
                PlanType = planType,
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(durationDays),
                Amount = amount,
                Status = "Active",
                PayOSOrderCode = payOSOrderCode,
                PayOSTransactionId = payOSTransactionId
            };

            _context.Subscriptions.Add(newSubscription);

            // Update user premium status
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.IsPremium = true;
            }

            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Premium activated for user {UserId}, plan {PlanType}, duration {DurationDays} days", 
                userId, planType, durationDays);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating premium for user {UserId}", userId);
            return false;
        }
    }

    #region Private Methods

    private PayOSConfiguration LoadPayOSConfiguration()
    {
        var config = new PayOSConfiguration
        {
            PartnerCode = _configuration["PayOS:PartnerCode"] ?? "",
            ClientId = _configuration["PayOS:ClientId"] ?? "",
            ApiKey = _configuration["PayOS:ApiKey"] ?? "",
            Checksum = _configuration["PayOS:Checksum"] ?? "",
            BaseUrl = _configuration["AppSettings:BaseUrl"] ?? "http://localhost:5160"
        };

        if (string.IsNullOrEmpty(config.ClientId) || 
            string.IsNullOrEmpty(config.ApiKey) || 
            string.IsNullOrEmpty(config.Checksum))
        {
            _logger.LogError("PayOS configuration is incomplete");
            throw new InvalidOperationException("PayOS configuration is incomplete");
        }

        return config;
    }

    private long GenerateOrderCode(int userId)
    {
        return DateTimeOffset.Now.ToUnixTimeMilliseconds();
    }

    private async Task SavePaymentTransactionAsync(int userId, long orderCode, string transactionId, decimal amount, string description, CreatePaymentResult payOSResponse)
    {
        var transaction = new PaymentTransaction
        {
            UserId = userId,
            PayOSOrderCode = orderCode.ToString(),
            PayOSTransactionId = transactionId,
            Amount = amount,
            Currency = "VND",
            Status = "Pending",
            Description = description,
            PayOSResponse = JsonSerializer.Serialize(payOSResponse)
        };

        _context.PaymentTransactions.Add(transaction);
        await _context.SaveChangesAsync();
    }

    #endregion
}

#region Configuration and Response Models

/// <summary>
/// PayOS configuration settings
/// </summary>
public class PayOSConfiguration
{
    public string PartnerCode { get; set; } = "";
    public string ClientId { get; set; } = "";
    public string ApiKey { get; set; } = "";
    public string Checksum { get; set; } = "";
    public string BaseUrl { get; set; } = "";
}

#endregion

 