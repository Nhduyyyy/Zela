using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Zela.Services.Interface;

namespace Zela.Middleware
{
    public class PremiumStatusMiddleware
    {
        private readonly RequestDelegate _next;

        public PremiumStatusMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IPayOSService payOSService)
        {
            var userId = context.Session.GetInt32("UserId");
            if (userId.HasValue)
            {
                await payOSService.UpdateUserPremiumStatusAsync(userId.Value);
            }
            await _next(context);
        }
    }
} 