# 🚀 PayOS Payment Integration Guide

## 📋 Overview

Zela integrates with PayOS payment gateway using the official PayOS SDK to provide premium subscription services. This guide covers the complete implementation including SDK integration, webhook handling, and user interface.

## 🏗️ Architecture

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Frontend      │    │   Backend       │    │   PayOS SDK     │
│   (Plans UI)    │───▶│   (Controller)  │───▶│   (Payment)     │
└─────────────────┘    └─────────────────┘    └─────────────────┘
                                │                        │
                                ▼                        ▼
                       ┌─────────────────┐    ┌─────────────────┐
                       │   Database      │    │   Webhook       │
                       │   (Transactions)│    │   (Callback)    │
                       └─────────────────┘    └─────────────────┘
```

## 🛠️ Setup

### 1. Install PayOS SDK

```bash
dotnet add package PayOS
```

### 2. Configuration

Add PayOS settings to `appsettings.json`:

```json
{
  "PayOS": {
    "PartnerCode": "YOUR_PARTNER_CODE",
    "ClientId": "YOUR_CLIENT_ID", 
    "ApiKey": "YOUR_API_KEY",
    "Checksum": "YOUR_CHECKSUM"
  },
  "AppSettings": {
    "BaseUrl": "http://localhost:5160"
  }
}
```

### 3. Database Migration

```bash
dotnet ef migrations add AddPaymentAndSubscription
dotnet ef database update
```

### 4. Webhook Configuration

In PayOS Dashboard, configure webhook URL:
```
http://localhost:5160/Payment/Callback
```

**Note**: PayOS SDK supports localhost URLs for development!

## 🎯 Features

### Premium Plans

| Plan | Price | Duration | Savings |
|------|-------|----------|---------|
| Monthly | 99,000 VNĐ | 30 days | - |
| Yearly | 990,000 VNĐ | 365 days | 17% |

### Premium Benefits

- ✅ Unlimited video calls
- ✅ HD recording
- ✅ Screen sharing
- ✅ Collaborative whiteboard
- ✅ Unlimited quizzes
- ✅ Priority support

## 📁 Code Structure

```
Zela/
├── Controllers/
│   └── PaymentController.cs          # Payment endpoints
├── Services/
│   ├── Interface/
│   │   └── IPayOSService.cs         # Service interface
│   └── PayOSService.cs              # PayOS SDK integration
├── Models/
│   ├── PaymentTransaction.cs        # Payment records
│   └── Subscription.cs              # User subscriptions
├── ViewModels/
│   └── PaymentViewModels.cs         # View models
└── Views/Payment/
    ├── Plans.cshtml                 # Plan selection
    ├── Success.cshtml               # Success page
    ├── Cancel.cshtml                # Cancellation page
    └── History.cshtml               # Payment history
```

## 🔌 API Endpoints

### Create Payment Order
```http
POST /Payment/CreateOrder
Content-Type: application/json

{
  "planId": "monthly" | "yearly"
}
```

### Payment Callback
```http
POST /Payment/Callback
Content-Type: application/json

{
  "orderCode": "1234567890",
  "transactionId": "payos_transaction_id",
  "amount": 99000,
  "status": "PAID",
  "signature": "payos_signature"
}
```

### Check Payment Status
```http
GET /Payment/CheckPaymentStatus?orderCode=1234567890
```

## 🔒 Security

### SDK Security
- PayOS SDK handles all security internally
- Built-in signature verification
- Automatic request signing
- Secure communication with PayOS API

### Authorization
- All payment endpoints require user authentication
- Only callback endpoint is public (required by PayOS)

### Data Validation
- Input validation on all user data
- SQL injection protection via Entity Framework
- XSS protection via ASP.NET Core

## 🎨 User Interface

### Plan Selection
- Clean, modern design with Bootstrap
- Feature comparison table
- Popular plan highlighting
- Responsive mobile design

### Payment Flow
1. User selects plan
2. Modal opens with payment options
3. QR code and payment URL displayed
4. Real-time status polling
5. Success/failure feedback

### Payment History
- Transaction list with status
- Current subscription status
- Remaining time display
- Download receipts

## 🧪 Testing

### Local Development
```bash
# Start application
dotnet run

# Test payment flow
curl -X POST http://localhost:5160/Payment/CreateOrder \
  -H "Content-Type: application/json" \
  -d '{"planId": "monthly"}'
```

### Webhook Testing
**No ngrok needed!** PayOS SDK supports localhost URLs directly.

## 📊 Monitoring

### Logging
- Structured logging with Serilog
- Payment events logged with correlation IDs
- Error tracking with stack traces

### Metrics
- Payment success rate
- Average transaction time
- Popular plan analytics
- Revenue tracking

### Alerts
- Failed payment notifications
- Webhook delivery failures
- API rate limit warnings

## 🚨 Troubleshooting

### Common Issues

| Issue | Cause | Solution |
|-------|-------|----------|
| 401 Unauthorized | Invalid API credentials | Check PayOS configuration |
| Invalid Signature | Wrong checksum | Verify PayOS checksum |
| Payment Not Processing | Webhook not configured | Set webhook URL in PayOS dashboard |
| User Not Premium | Callback failed | Check webhook logs |

### Debug Steps
1. Check application logs
2. Verify PayOS configuration
3. Test webhook delivery
4. Validate SDK initialization
5. Check database transactions

## 🔄 Maintenance

### Subscription Management
- Automatic expiry handling
- Grace period for renewals
- Downgrade notifications

### Data Cleanup
- Archive old transactions
- Remove expired subscriptions
- Clean up failed payments

### Performance
- Database indexing
- Caching strategies
- API rate limiting

## 📞 Support

### Documentation
- [PayOS API Documentation](https://docs.payos.vn)
- [PayOS SDK Documentation](https://payos.vn/docs/api/)
- [ASP.NET Core Documentation](https://docs.microsoft.com/en-us/aspnet/core/)

### Contact
- PayOS Support: support@payos.vn
- Development Team: dev@zela.com

---

## 🎉 Conclusion

The PayOS integration using the official SDK provides a complete payment solution for Zela's premium features. The implementation follows best practices for security, error handling, and user experience.

**Key Benefits:**
- ✅ Secure payment processing with official SDK
- ✅ Localhost development support (no ngrok needed)
- ✅ Real-time status updates
- ✅ Comprehensive error handling
- ✅ User-friendly interface
- ✅ Production-ready code

The system is ready for deployment! 🚀 