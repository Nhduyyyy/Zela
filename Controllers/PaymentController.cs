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
    public IActionResult Plans() => View(PaymentPlans.All);

    /// <summary>
    /// Create payment order
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        var userId = GetUserId();
        if (!userId.HasValue) return Json(new { success = false, message = "Vui lòng đăng nhập" });
        var result = await _payOSService.CreatePaymentOrderAsync(userId.Value, request.PlanId);
        return Json(result);
    }

    /// <summary>
    /// Handle PayOS callback
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Callback()
    {
        return await _payOSService.HandleCallbackAsync(Request);
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
        var userId = GetUserId();
        if (!userId.HasValue) return RedirectToAction("Login", "Account");
        var vm = await _payOSService.GetPaymentHistoryViewModelAsync(userId.Value);
        return View(vm);
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

    private int? GetUserId() => HttpContext.Session.GetInt32("UserId");

    #endregion
} 