using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Zela.Services.Interface;
using Zela.ViewModels;

namespace Zela.Controllers;

/// <summary>
/// Controller for handling PayOS payment operations
/// </summary>
public class PaymentController : Controller
{
    private readonly IPayOSService _payOSService;
    private readonly ILogger<PaymentController> _logger;
    private readonly IConfiguration _configuration;

    public PaymentController(IPayOSService payOSService, ILogger<PaymentController> logger, IConfiguration configuration)
    {
        _payOSService = payOSService;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Display premium plans page
    /// </summary>
    public IActionResult Plans()
    {
        var plans = new List<PremiumPlanViewModel>
        {
            new()
            {
                Id = "monthly",
                Name = "Premium Tháng",
                Price = 99000,
                Duration = 30,
                Features = new List<string>
                {
                    "Video call không giới hạn",
                    "Ghi âm/ghi hình HD",
                    "Chia sẻ màn hình",
                    "Whiteboard cộng tác",
                    "Quiz không giới hạn",
                    "Hỗ trợ ưu tiên"
                }
            },
            new()
            {
                Id = "yearly",
                Name = "Premium Năm",
                Price = 990000,
                Duration = 365,
                Popular = true,
                Features = new List<string>
                {
                    "Tất cả tính năng Premium Tháng",
                    "Tiết kiệm 17% so với gói tháng",
                    "Ưu tiên hỗ trợ 24/7",
                    "Tính năng beta sớm nhất"
                }
            }
        };

        return View(plans);
    }

    /// <summary>
    /// Create payment order
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        _logger.LogInformation("Creating payment order for plan {PlanId}", request.PlanId);
        
        try
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                _logger.LogWarning("User not authenticated");
                return Json(new { success = false, message = "Vui lòng đăng nhập" });
            }

            var planInfo = GetPlanInfo(request.PlanId);
            if (planInfo == null)
            {
                _logger.LogWarning("Invalid plan ID: {PlanId}", request.PlanId);
                return Json(new { success = false, message = "Gói không hợp lệ" });
            }

            var description = $"Thanh toán gói {planInfo.Name} - Zela Premium";
            var result = await _payOSService.CreatePaymentOrderAsync(userId.Value, planInfo.Name, planInfo.Price, description);

            if (result.Success)
            {
                _logger.LogInformation("Payment order created successfully: {OrderCode}", result.OrderCode);
                return Json(new
                {
                    success = true,
                    orderCode = result.OrderCode,
                    paymentUrl = result.PaymentUrl,
                    qrCode = result.QRCode
                });
            }

            _logger.LogWarning("Failed to create payment order: {ErrorMessage}", result.ErrorMessage);
            return Json(new { success = false, message = result.ErrorMessage });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment order");
            return Json(new { success = false, message = "Lỗi hệ thống" });
        }
    }

    /// <summary>
    /// Handle PayOS callback
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Callback()
    {
        try
        {
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();
            _logger.LogInformation("PayOS callback received: {Body}", body);

            // Parse callback data
            var callbackData = JsonSerializer.Deserialize<PayOSCallbackData>(body);
            if (callbackData == null)
            {
                _logger.LogWarning("Invalid callback data format");
                return BadRequest("Invalid callback data");
            }

            _logger.LogInformation("CallbackData: OrderCode={OrderCode}, TransactionId={TransactionId}, Amount={Amount}, Status={Status}, Signature={Signature}",
                callbackData.OrderCode, callbackData.TransactionId, callbackData.Amount, callbackData.Status, callbackData.Signature);

            // Verify signature
            var isValid = await _payOSService.VerifyCallbackAsync(
                callbackData.OrderCode, 
                callbackData.TransactionId, 
                callbackData.Amount, 
                callbackData.Signature);

            if (!isValid)
            {
                _logger.LogWarning("Invalid callback signature for order {OrderCode}", callbackData.OrderCode);
                return BadRequest("Invalid signature");
            }

            // Update payment status
            var updateResult = await _payOSService.UpdatePaymentStatusAsync(
                callbackData.OrderCode,
                callbackData.TransactionId,
                callbackData.Status);
            _logger.LogInformation("UpdatePaymentStatusAsync result: {Result}", updateResult);

            // Activate premium if payment successful
            if (callbackData.Status == "PAID")
            {
                var planInfo = GetPlanInfoByOrderCode(callbackData.OrderCode);
                // Lấy transaction từ DB để lấy userId
                var transaction = await _payOSService.GetPaymentTransactionByOrderCodeAsync(callbackData.OrderCode);
                if (planInfo != null && transaction != null)
                {
                    await _payOSService.ActivatePremiumAsync(
                        transaction.UserId,
                        planInfo.Name,
                        planInfo.Duration,
                        callbackData.Amount,
                        callbackData.OrderCode,
                        callbackData.TransactionId);
                }
                else
                {
                    _logger.LogWarning("Cannot get planInfo or transaction for orderCode: {OrderCode}", callbackData.OrderCode);
                }
            }

            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing PayOS callback");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Payment success page
    /// </summary>
    public async Task<IActionResult> Success(string orderCode = "")
    {
        if (!string.IsNullOrEmpty(orderCode))
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId.HasValue)
            {
                var transactions = await _payOSService.GetPaymentHistoryAsync(userId.Value);
                var currentTransaction = transactions.FirstOrDefault(t => t.PayOSOrderCode == orderCode);
                
                if (currentTransaction != null)
                {
                    ViewBag.OrderCode = orderCode;
                    ViewBag.PaymentStatus = currentTransaction.Status;
                    ViewBag.Amount = currentTransaction.Amount;
                }
            }
        }
        
        return View();
    }

    /// <summary>
    /// Payment cancellation page
    /// </summary>
    public IActionResult Cancel()
    {
        return View();
    }

    /// <summary>
    /// Payment history page
    /// </summary>
    public async Task<IActionResult> History()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue)
            return RedirectToAction("Login", "Account");

        var transactions = await _payOSService.GetPaymentHistoryAsync(userId.Value);
        var currentSubscription = await _payOSService.GetCurrentSubscriptionAsync(userId.Value);

        var viewModel = new PaymentHistoryViewModel
        {
            Transactions = transactions,
            CurrentSubscription = currentSubscription
        };

        return View(viewModel);
    }

    /// <summary>
    /// Check payment status
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> CheckPaymentStatus(string orderCode)
    {
        try
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
                return Json(new { success = false, message = "Unauthorized" });

            var transactions = await _payOSService.GetPaymentHistoryAsync(userId.Value);
            var transaction = transactions.FirstOrDefault(t => t.PayOSOrderCode == orderCode);

            if (transaction == null)
                return Json(new { success = false, message = "Transaction not found" });

            // Nếu đang ở localhost và trạng thái là Pending, tự động chuyển sang PAID và nâng cấp Premium
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            if (transaction.Status == "Pending" && (env == "Development" || env == "Local"))
            {
                await _payOSService.UpdatePaymentStatusAsync(orderCode, transaction.PayOSTransactionId, "PAID");
                // Gọi hàm nâng cấp Premium
                var planInfo = GetPlanInfoByOrderCode(orderCode);
                if (planInfo != null)
                {
                    await _payOSService.ActivatePremiumAsync(
                        transaction.UserId,
                        planInfo.Name,
                        planInfo.Duration,
                        transaction.Amount,
                        orderCode,
                        transaction.PayOSTransactionId
                    );
                }
                // Reload lại transaction
                transactions = await _payOSService.GetPaymentHistoryAsync(userId.Value);
                transaction = transactions.FirstOrDefault(t => t.PayOSOrderCode == orderCode);
            }

            return Json(new
            {
                success = true,
                status = transaction.Status,
                orderCode = transaction.PayOSOrderCode
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking payment status for order {OrderCode}", orderCode);
            return Json(new { success = false, message = "Error checking status" });
        }
    }

    #region Private Methods

    private PremiumPlanInfo? GetPlanInfo(string planId)
    {
        return planId switch
        {
            "monthly" => new PremiumPlanInfo { Name = "Premium Tháng", Price = 99000, Duration = 30 },
            "yearly" => new PremiumPlanInfo { Name = "Premium Năm", Price = 990000, Duration = 365 },
            _ => null
        };
    }

    private PremiumPlanInfo? GetPlanInfoByOrderCode(string orderCode)
    {
        // OLD IMPLEMENTATION ONLY WORKS FOR SPECIAL PREFIX, REPLACED BELOW
        // This method now tries to locate the corresponding transaction in DB and infer plan by amount.
        var transaction = _payOSService.GetPaymentTransactionByOrderCodeAsync(orderCode).GetAwaiter().GetResult();
        if (transaction != null)
        {
            return GetPlanInfoByAmount(transaction.Amount);
        }
        // Fallback: original naive check (kept for backward compatibility)
        if (orderCode.Contains("ZELA_"))
            return new PremiumPlanInfo { Name = "Premium Tháng", Price = 99000, Duration = 30 };
        return null;
    }

    // ---------- NEW UTILITY METHOD ----------
    private static PremiumPlanInfo? GetPlanInfoByAmount(decimal amount)
    {
        return amount switch
        {
            99000  => new PremiumPlanInfo { Name = "Premium Tháng", Price = 99000, Duration = 30 },
            990000 => new PremiumPlanInfo { Name = "Premium Năm",  Price = 990000, Duration = 365 },
            _      => null
        };
    }

    private int? ExtractUserIdFromOrderCode(string orderCode)
    {
        try
        {
            // Order code format: ZELA_20241201120000_123
            var parts = orderCode.Split('_');
            if (parts.Length >= 3 && int.TryParse(parts[2], out var userId))
            {
                return userId;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting user ID from order code {OrderCode}", orderCode);
        }
        return null;
    }

    #endregion
} 