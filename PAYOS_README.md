# 💳 PayOS Payment Integration

## 🎯 Overview

Zela integrates with PayOS payment gateway using the official PayOS SDK to provide premium subscription services. Users can upgrade to premium plans with secure payment processing, real-time status updates, and comprehensive transaction history.

## ✨ Features

- **Secure Payment Processing**: Using official PayOS SDK with built-in security
- **Localhost Development**: SDK supports localhost URLs for development
- **Real-time Status Updates**: Automatic polling and webhook handling
- **Premium Plans**: Monthly (99,000 VNĐ) and Yearly (990,000 VNĐ) options
- **User-friendly Interface**: Modern UI with QR codes and payment links
- **Transaction History**: Complete payment and subscription tracking
- **Error Handling**: Comprehensive error management and logging

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

## 📁 File Structure

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
├── Views/Payment/
│   ├── Plans.cshtml                 # Plan selection
│   ├── Success.cshtml               # Success page
│   ├── Cancel.cshtml                # Cancellation page
│   └── History.cshtml               # Payment history
└── Documentation/
    ├── PAYOS_INTEGRATION_GUIDE.md   # Detailed guide
    └── PAYOS_README.md              # This file
```

## 🚀 Quick Start

### 1. Configuration

Add to `appsettings.json`:
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

### 2. Database Setup

```bash
dotnet ef migrations add AddPaymentAndSubscription
dotnet ef database update
```

### 3. Webhook Configuration

Set webhook URL in PayOS Dashboard:
```
http://localhost:5160/Payment/Callback
```

**Note**: PayOS SDK supports localhost URLs for development, so you don't need ngrok!

## 🔌 API Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/Payment/Plans` | GET | Display premium plans |
| `/Payment/CreateOrder` | POST | Create payment order |
| `/Payment/Callback` | POST | PayOS webhook handler |
| `/Payment/Success` | GET | Payment success page |
| `/Payment/Cancel` | GET | Payment cancellation page |
| `/Payment/History` | GET | Payment history |
| `/Payment/CheckPaymentStatus` | GET | Check payment status |

## 🔒 Security Features

- **SDK Security**: PayOS SDK handles all security internally
- **Signature Verification**: Built-in webhook verification
- **Input Validation**: Comprehensive data validation
- **Authentication**: User authentication required for all endpoints
- **SQL Injection Protection**: Entity Framework parameterized queries
- **XSS Protection**: ASP.NET Core built-in protection

## 🎨 User Interface

### Plan Selection
- Clean, modern Bootstrap design
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

## 📊 Monitoring & Logging

### Structured Logging
- Payment events with correlation IDs
- Error tracking with stack traces
- Performance metrics

### Key Metrics
- Payment success rate
- Average transaction time
- Popular plan analytics
- Revenue tracking

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

## 📈 Premium Benefits

### Monthly Plan (99,000 VNĐ)
- Unlimited video calls
- HD recording
- Screen sharing
- Collaborative whiteboard
- Unlimited quizzes
- Priority support

### Yearly Plan (990,000 VNĐ) - Save 17%
- All monthly features
- 2 months free
- 24/7 priority support
- Early access to beta features

## 📞 Support

### Documentation
- [PayOS API Documentation](https://docs.payos.vn)
- [PayOS SDK Documentation](https://payos.vn/docs/api/)
- [ASP.NET Core Documentation](https://docs.microsoft.com/en-us/aspnet/core/)
- [Detailed Integration Guide](PAYOS_INTEGRATION_GUIDE.md)

### Contact
- PayOS Support: support@payos.vn
- Development Team: dev@zela.com

---

## 🎉 Summary

The PayOS integration using the official SDK provides a complete, production-ready payment solution for Zela's premium features. The implementation follows industry best practices for security, error handling, and user experience.

**Key Achievements:**
- ✅ Secure payment processing with official PayOS SDK
- ✅ Localhost development support (no ngrok needed)
- ✅ Real-time status updates and webhook handling
- ✅ Comprehensive error handling and logging
- ✅ User-friendly interface with modern design
- ✅ Complete transaction and subscription management
- ✅ Production-ready code with proper documentation

The system is ready for deployment and provides a seamless payment experience for users upgrading to premium features! 🚀 