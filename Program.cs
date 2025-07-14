/*
 * File:    Program.cs
 * Author:  A–DUY
 * Date:    2025-05-30
 * Desc:    Cấu hình dịch vụ.
 */

using Microsoft.AspNetCore.Authentication.Cookies; // Thư viện để cấu hình Cookie Authentication
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore; // Thư viện Entity Framework Core
using Zela.DbContext;
using Zela.Hubs;
using Zela.Services; // Namespace chứa ApplicationDbContext của bạn
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.IIS;
using Zela.Services.Interface;

var builder = WebApplication.CreateBuilder(args);


// ---------------------------------------------
// 1) Đăng ký IHttpContextAccessor
//    - Cho phép inject IHttpContextAccessor để truy cập HttpContext từ bất cứ nơi nào.
//    - Thường dùng trong các service để lấy thông tin user, session, header, v.v.
// ---------------------------------------------
builder.Services.AddHttpContextAccessor();

// ---------------------------------------------
// 2) Đăng ký MVC (Controllers + Views)
//    - Cho phép ứng dụng sử dụng pattern MVC, dùng Controllers và Views để render HTML.
// ---------------------------------------------
builder.Services.AddControllersWithViews();

// ---------------------------------------------
// 3) Đăng ký Session
//    - Cho phép sử dụng HttpContext.Session để lưu trữ dữ liệu tạm thời (key-value).
//    - Phải gọi app.UseSession() trong pipeline để kích hoạt middleware.
// ---------------------------------------------
builder.Services.AddSession();

// ---------------------------------------------
// 4) Cấu hình Authentication: Cookie + Google OAuth
//
//    Mục tiêu:
//    - Sử dụng Cookie để lưu phiên đăng nhập (authentication ticket).
//    - Khi user chưa đăng nhập, chuyển hướng (Challenge) sang Google OAuth để xác thực.
//    - Sau khi Google cấp token, tạo cookie tự động để lưu session.
// ---------------------------------------------
builder.Services.AddAuthentication(options =>
    {
        // 4.1) Chọn scheme mặc định để "ghi nhớ" user đã đăng nhập là Cookie.
        //    - Khi user đăng nhập thành công (qua Google), cookie sẽ được tạo
        //      để lưu phiên làm việc (authentication session).
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        //    CookieAuthenticationDefaults.AuthenticationScheme = "Cookies" (chuỗi mặc định).

        // 4.2) Chọn scheme để "challenge" (thử xác thực) khi user chưa đăng nhập.
        //    - Ở trường hợp này, nếu user bấm "Login" nhưng chưa có cookie,
        //      ứng dụng sẽ dùng Google scheme để chuyển hướng tới trang Google OAuth.
        options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
        //    GoogleDefaults.AuthenticationScheme = "Google" (chuỗi mặc định).
    })
    // 4.3) Thêm Cookie Authentication
    //    - Khi đã thiết lập DefaultScheme = "Cookies", ta cần gọi AddCookie()
    //      để ASP.NET Core biết cách tạo và quản lý cookie cho user.
    .AddCookie()
    // 4.4) Thêm Google OAuth Authentication
    //    - Thiết lập ClientId và ClientSecret lấy từ cấu hình (appsettings.json hoặc environment).
    .AddGoogle(options =>
    {
        // 4.4a) ClientId: ID ứng dụng được cấp khi đăng ký OAuth trên Google Console.
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
        //      Ví dụ appsettings.json có:
        //      "Authentication": {
        //         "Google": {
        //             "ClientId": "1234567890-abcdefghijklmnopqrstuvwxyz.apps.googleusercontent.com",
        //             "ClientSecret": "ABCDefGhIJkLmNoPQRsTUvWX"
        //          }
        //      }

        // 4.4b) ClientSecret: Khóa bí mật tương ứng với ClientId, dùng để Google xác thực ứng dụng của bạn.
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];

        // 4.4c) Scope (phạm vi quyền yêu cầu). Mặc định Google chỉ trả về "openid".
        //      Để lấy thêm thông tin như thông tin cơ bản (profile) và email,
        //      ta phải thêm scope "profile" và "email".
        options.Scope.Add("profile"); // Yêu cầu quyền lấy thông tin hồ sơ (tên, ảnh, v.v.)
        options.Scope.Add("email"); // Yêu cầu quyền lấy địa chỉ email của user
    });

// ---------------------------------------------
// 5) Cấu hình DbContext (Entity Framework Core)
//    - Lấy connection string từ appsettings.json (DefaultConnection).
//    - Đăng ký ApplicationDbContext để DI có thể inject DbContext vào controller/service.
// ---------------------------------------------
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
//    - UseSqlServer: cấu hình EF Core dùng SQL Server với chuỗi kết nối đã lấy.

// 5b) Đăng ký SignalR
builder.Services.AddSignalR();

// Đăng ký Service cho Friendship
builder.Services.AddScoped<IFriendshipService, FriendshipService>();

builder.Services.AddSingleton<IUserIdProvider, SignalRUserIdProvider>();

builder.Services.AddScoped<IMeetingService, MeetingService>();

builder.Services.AddScoped<IChatService, ChatService>();

builder.Services.AddSingleton<IFileUploadService, FileUploadService>();

builder.Services.AddScoped<IProfileService, ProfileService>();

// add Sticker
builder.Services.AddScoped<IStickerService, StickerService>();

// add Recording
builder.Services.AddScoped<IRecordingService, RecordingService>();

// add Whiteboard
builder.Services.AddScoped<IWhiteboardService, WhiteboardService>();

//add Quiz
builder.Services.AddScoped<IQuizService, QuizService>();

//add PayOS Payment
builder.Services.AddScoped<IPayOSService, PayOSService>();

// Configure form options for file uploads
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 50 * 1024 * 1024; // 50MB
    options.ValueLengthLimit = 50 * 1024 * 1024; // 50MB for form values
    options.KeyLengthLimit = 50 * 1024 * 1024; // 50MB for form keys
    options.MemoryBufferThreshold = int.MaxValue;
});

// Configure Kestrel server options for large file uploads
builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = 50 * 1024 * 1024; // 50MB
});

// Configure Kestrel server limits
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 50 * 1024 * 1024; // 50MB
});

// ---------------------------------------------
// 6) Build WebApplication
//    - Sau khi đăng ký hết services, gọi Build() để tạo đối tượng app.
// ---------------------------------------------
var app = builder.Build();

// ---------------------------------------------
// 7) Cấu hình middleware pipeline
//
//    Lưu ý thứ tự Middleware:
//    1) UseExceptionHandler / UseHsts
//    2) UseHttpsRedirection
//    3) UseStaticFiles
//    4) UseRouting
//    5) UseSession
//    6) UseAuthentication
//    7) UseAuthorization
//    8) MapControllerRoute
// ---------------------------------------------

// 7.1) Xử lý lỗi và HSTS nếu không phải môi trường Development
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// 7.2) Tự động redirect HTTP sang HTTPS
app.UseHttpsRedirection();

// 7.3) Phục vụ file tĩnh từ wwwroot (CSS, JS, hình ảnh,…)
app.UseStaticFiles();

// 7.4) Bật routing (xác định route cho controller/action)
app.UseRouting();

// 7.5) Bật Session middleware
//      - Cho phép dùng HttpContext.Session trong controller/service.
//      - Session lưu trên server hoặc lưu file tạm, key-value, được xác định bởi cookie session.
app.UseSession();

// 7.6) Bật Authentication middleware
//      - Đọc cookie (nếu có) để khôi phục ClaimsPrincipal vào HttpContext.User.
//      - Xử lý Challenge/Forbid (ví dụ [Authorize], HttpContext.ChallengeAsync).
app.UseAuthentication();

// 7.7) Bật Authorization middleware
//      - Kiểm tra HttpContext.User có đủ quyền (policies, roles) để truy cập tài nguyên bảo vệ.
app.UseAuthorization();

// 7.8) Định nghĩa route mặc định cho MVC
//      - Nếu không có controller/action xác định trong URL, sẽ dùng HomeController.Index()
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.UseStatusCodePagesWithRedirects("/Account/Login?error=403");

app.MapHub<ChatHub>("/chathub");
app.MapHub<MeetingHub>("/meetingHub");
app.MapHub<WhiteboardHub>("/whiteboardHub");

// 7.10) Chạy ứng dụng, lắng nghe request trên port đã cấu hình
app.Run();