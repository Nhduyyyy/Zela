/*
 * File: AccountController.cs
 * Author: A–DUY
 * Date: 2025-05-30
 * Desc: Quản lý đăng nhập/đăng xuất qua Google OAuth.
 */

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Zela.DbContext;
using Zela.Models;


namespace Zela.Controllers
{
    /// <summary>
    /// Controller xử lý các chức năng liên quan đến tài khoản:
    /// đăng nhập bằng Google và đăng xuất.
    /// </summary>
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _db;

        // ---------------------------------------------
        // Author : A–DUY
        // Date   : 2025-05-30
        // Task   : Tiêm ApplicationDbContext để thao tác dữ liệu Users.
        // ---------------------------------------------
        /// <summary>
        /// Khởi tạo AccountController với DbContext.
        /// </summary>
        /// <param name="db">Instance ApplicationDbContext để truy xuất bảng Users.</param>
        public AccountController(ApplicationDbContext db)
        {
            _db = db; // Lưu DbContext vào biến thành viên
        }

        // ---------------------------------------------
        // Author : A–DUY
        // Date   : 2025-05-30
        // Task   : Hiển thị trang đăng nhập với nút Google.
        // ---------------------------------------------
        /// <summary>
        /// Hiển thị trang Login chứa form Google Login.
        /// </summary>
        /// <returns>View Login.cshtml.</returns>
        [HttpGet]
        public IActionResult Login()
        {
            return View(); // Trả về giao diện đăng nhập
        }

        // ---------------------------------------------
        // Author : A–DUY
        // Date   : 2025-05-30
        // Task   : Khởi tạo quá trình đăng nhập với Google OAuth.
        // ---------------------------------------------
        /// <summary>
        /// Khi người dùng nhấn nút "Đăng nhập bằng Google", hàm này sẽ được gọi.
        /// Nó thực hiện 2 việc chính:
        /// 1) Xác định đường dẫn (URL) mà Google sẽ gọi lại (callback) khi hoàn tất xác thực.
        /// 2) Bắt đầu quá trình xác thực (Challenge) bằng Google.
        /// </summary>
        /// <param name="returnUrl">
        /// Đây là đường dẫn nội bộ của ứng dụng để chuyển hướng tiếp theo 
        /// khi đăng nhập thành công. Mặc định là "/Messenger/Index".
        /// Nếu người dùng muốn tới trang khác, giá trị này sẽ được truyền vào từ bên ngoài.
        /// </param>
        /// <returns>
        /// Một đối tượng ChallengeResult, tức là yêu cầu ASP.NET Core tự động 
        /// điều hướng (redirect) trình duyệt web sang trang xác thực của Google.
        /// </returns>
        [HttpPost]
        public IActionResult GoogleLogin(string returnUrl = "/Chat/Index")
        {
            // ──────────────────────────────────────────────────────────────────
            // BƯỚC 1: Tạo AuthenticationProperties để chứa thông tin callback URL
            // ──────────────────────────────────────────────────────────────────
            // AuthenticationProperties là một lớp (class) dùng để truyền "thông tin bổ sung"
            // cho quá trình xác thực (authentication). Ở đây, ta cần cho ASP.NET Core biết:
            //      → Khi Google xác thực xong, hãy redirect (trả người dùng) về đâu trong ứng dụng.
            //
            // RedirectUri sẽ là URL mà Google sẽ gọi lại sau khi user đăng nhập thành công.
            // ASP.NET Core sẽ gán URL này vào các tham số bắt buộc của Google (như redirect_uri).
            //
            // Ví dụ: Nếu ứng dụng chạy tại https://localhost:5001,
            //      Url.Action("GoogleResponse", new { ReturnUrl = returnUrl })
            // sẽ tạo ra chuỗi:
            //      "https://localhost:5001/Account/GoogleResponse?ReturnUrl=%2FMessenger%2FIndex"
            //
            // Khi Google xử lý xong, họ sẽ gửi lại người dùng tới URL này,
            // kèm theo các thông tin (như mã thông báo) trong query string hoặc header.
            var properties = new AuthenticationProperties
            {
                // Url.Action("GoogleResponse", new { ReturnUrl = returnUrl }) sẽ:
                //   • Tính toán URL đầy đủ tới action GoogleResponse trong AccountController.
                //   • Tự động lấy hostname và port hiện tại (ví dụ https://localhost:5001).
                //   • Thêm tham số ReturnUrl vào query string, để biết ta nên chuyển tiếp tới đâu sau khi đăng nhập.
                RedirectUri = Url.Action("GoogleResponse", new { ReturnUrl = returnUrl })
            };

            // ──────────────────────────────────────────────────────────────────
            // BƯỚC 2: Bắt đầu quá trình xác thực với Google (Challenge)
            // ──────────────────────────────────────────────────────────────────
            // Kịch bản:
            //   1) Client (trình duyệt) gửi POST tới /Account/GoogleLogin.
            //   2) Trong server: ta gọi Challenge(...) với thông tin properties và Scheme = "Google".
            //   3) ASP.NET Core Authentication Middleware sẽ:
            //        a) Xây dựng URL đến trang đăng nhập Google. URL này gồm các tham số:
            //           - client_id (ID của ứng dụng trên Google, lấy từ cấu hình).
            //           - redirect_uri (là giá trị RedirectUri ở trên).
            //           - response_type=code (bảo đảm Google trả về mã code).
            //           - scope (như request lấy email, profile).
            //           - state (mã ngẫu nhiên để chống giả mạo, kèm ReturnUrl để ta biết vị trí quay về).
            //        b) Trả về HTTP 302 Redirect, đưa trình duyệt user sang URL của Google.
            //   4) Trình duyệt sẽ chuyển hướng đến Google, user sẽ đăng nhập (nếu chưa) 
            //      và cho phép ứng dụng lấy thông tin (email, tên, avatar).
            //
            // Trong code, GoogleDefaults.AuthenticationScheme = "Google" (hằng số do Microsoft định nghĩa).
            // Khi Challenge(properties, "Google"), ASP.NET Core biết sử dụng Google handler (đã cấu hình từ Program.cs).
            //
            // Lưu ý: Chúng ta không phải tự viết code tạo URL phức tạp – Google handler sẽ tự lo tất cả.
            // Chỉ cần gọi Challenge với Scheme = "Google" và cho handler biết RedirectUri.
            //
            // Kết quả: Thư viện sẽ trả về một ChallengeResult và ASP.NET Core tự động trả HTTP 302 
            // với header Location = [URL Google OAuth endpoint]?client_id=...&redirect_uri=...&scope=...
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        // ---------------------------------------------
        // Author : A–DUY
        // Date   : 2025-01-XX
        // Task   : Khởi tạo quá trình đăng nhập với Facebook OAuth.
        // ---------------------------------------------
        /// <summary>
        /// Khi người dùng nhấn nút "Đăng nhập bằng Facebook", hàm này sẽ được gọi.
        /// Nó thực hiện 2 việc chính:
        /// 1) Xác định đường dẫn (URL) mà Facebook sẽ gọi lại (callback) khi hoàn tất xác thực.
        /// 2) Bắt đầu quá trình xác thực (Challenge) bằng Facebook.
        /// </summary>
        /// <param name="returnUrl">
        /// Đây là đường dẫn nội bộ của ứng dụng để chuyển hướng tiếp theo 
        /// khi đăng nhập thành công. Mặc định là "/Chat/Index".
        /// </param>
        /// <returns>
        /// Một đối tượng ChallengeResult, tức là yêu cầu ASP.NET Core tự động 
        /// điều hướng (redirect) trình duyệt web sang trang xác thực của Facebook.
        /// </returns>
        [HttpPost]
        public IActionResult FacebookLogin(string returnUrl = "/Chat/Index")
        {
            // ──────────────────────────────────────────────────────────────────
            // BƯỚC 1: Tạo AuthenticationProperties để chứa thông tin callback URL
            // ──────────────────────────────────────────────────────────────────
            var properties = new AuthenticationProperties
            {
                // Facebook sẽ callback về action FacebookResponse
                RedirectUri = Url.Action("FacebookResponse", new { ReturnUrl = returnUrl })
            };

            // ──────────────────────────────────────────────────────────────────
            // BƯỚC 2: Bắt đầu quá trình xác thực với Facebook (Challenge)
            // ──────────────────────────────────────────────────────────────────
            // Facebook OAuth flow tương tự Google OAuth:
            //   1) Client gửi POST tới /Account/FacebookLogin
            //   2) Server gọi Challenge với Facebook scheme
            //   3) ASP.NET Core redirect đến Facebook OAuth endpoint
            //   4) User đăng nhập Facebook và cấp quyền
            //   5) Facebook callback về FacebookResponse action
            return Challenge(properties, FacebookDefaults.AuthenticationScheme);
        }


        // ---------------------------------------------
        // Author : A–DUY
        // Date   : 2025-05-30
        // Task   : Xử lý callback từ Google, lưu user và đăng nhập.
        // ---------------------------------------------
        /// <summary>
        /// Phương thức này sẽ được gọi khi Google gửi người dùng trở lại
        /// sau khi họ đã đăng nhập và cấp quyền cho ứng dụng. Nó thực hiện các bước:
        /// 1) Lấy kết quả xác thực (authentication) đã lưu trong cookie.
        /// 2) Đọc thông tin email, tên, avatar do Google trả về (claims).
        /// 3) Kiểm tra hoặc tạo mới bản ghi user trong database.
        /// 4) Lưu thông tin user vào session để các trang khác có thể sử dụng.
        /// 5) Ghi nhận user đã đăng nhập (SignIn) bằng cách sử dụng cookie authentication.
        /// 6) Cuối cùng, chuyển hướng (redirect) đến trang đích (returnUrl).
        /// </summary>
        /// <param name="returnUrl">
        /// Đường dẫn nội bộ (trong ứng dụng) mà chúng ta muốn đưa user tới
        /// sau khi quá trình đăng nhập thành công. Mặc định là "/Messenger/Index".
        /// </param>
        /// <returns>
        /// Nếu có lỗi (không lấy được xác thực hoặc email), trả về Redirect về Login.
        /// Nếu thành công, trả về Redirect về returnUrl, cho phép user tiếp tục sử dụng hệ thống.
        /// </returns>
        [HttpGet]
        public async Task<IActionResult> GoogleResponse(string returnUrl = "/Chat/Index")
        {
            // -------------------------------------------------------------------
            // BƯỚC 1: Lấy kết quả xác thực (AuthenticateAsync) đã được middleware
            //         lưu trong cookie trước đó. Middleware đã xử lý Google OAuth
            //         và lưu vào cookie thông tin claims về user.
            // -------------------------------------------------------------------
            // HttpContext.AuthenticateAsync("Cookies") sẽ kiểm tra cookie trong
            // request hiện tại, nếu cookie có thông tin hợp lệ, nó sẽ tạo ra một
            // AuthenticationTicket chứa ClaimsPrincipal (đối tượng chứa thông tin
            // user như email, name, avatar, v.v.). Nếu không có cookie hợp lệ,
            // result.Succeeded sẽ false.
            var result = await HttpContext.AuthenticateAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);
            // CookieAuthenticationDefaults.AuthenticationScheme = "Cookies" (hay tên mà bạn cấu hình)

            // -------------------------------------------------------------------
            // BƯỚC 2: Kiểm tra xem việc xác thực (authentication) có thành công hay không.
            //         Nếu Google không cho phép hoặc có lỗi, chúng ta sẽ đưa user về
            //         trang đăng nhập (Login) để họ thử lại.
            // -------------------------------------------------------------------
            if (!result.Succeeded)
                return RedirectToAction("Login");
            // RedirectToAction("Login") sẽ gửi HTTP 302 về client, yêu cầu
            // trình duyệt chuyển đến action Login (trang đăng nhập).

            // -------------------------------------------------------------------
            // BƯỚC 3: Đọc các thông tin "claims" (tuyên bố) do Google gửi về.
            //         Claims là các thông tin về user mà Google xác nhận (ví dụ:
            //         email, full name, avatar).
            // -------------------------------------------------------------------
            // result.Principal là đối tượng ClaimsPrincipal, chứa rất nhiều thông tin
            // về user dưới dạng danh sách các Claim. Mỗi Claim có một loại (Type) và giá trị (Value).
            // FindFirst(ClaimTypes.Email) sẽ tìm Claim có type = "email" và trả về giá trị (địa chỉ email).
            // Tương tự, ClaimTypes.Name là tên đầy đủ, "picture" là link avatar (do Google cung cấp).
            var email = result.Principal.FindFirst(ClaimTypes.Email)?.Value;
            var fullName = result.Principal.FindFirst(ClaimTypes.Name)?.Value;
            var avatarUrl = result.Principal.FindFirst("picture")?.Value;

            // -------------------------------------------------------------------
            // BƯỚC 4: Nếu Google không trả về avatar (chưa có claim "picture"),
            //         chúng ta thiết lập một avatar mặc định để hiển thị.
            // -------------------------------------------------------------------
            // string.IsNullOrEmpty(avatarUrl) kiểm tra avatarUrl có rỗng (null hoặc "")
            // Nếu rỗng, ta gán avatarUrl = "/images/default-avatar.jpeg"
            // => Ảnh này sẽ được sử dụng cho user nếu chưa có avatar từ Google.
            if (string.IsNullOrEmpty(avatarUrl))
                avatarUrl = "/images/default-avatar.jpeg";

            // -------------------------------------------------------------------
            // BƯỚC 5: Nếu Google không trả về tên đầy đủ (claim ClaimTypes.Name),
            //         ta dùng email làm "tên" thay thế để hiển thị (ít nhất còn biết user là ai).
            // -------------------------------------------------------------------
            // fullName có thể null hoặc rỗng nếu user ở chế độ Google không chia sẻ tên.
            if (string.IsNullOrEmpty(fullName))
                fullName = email ?? "";
            // email ?? "" nghĩa là nếu email null cũng gán tên = "", tránh null reference.

            // -------------------------------------------------------------------
            // BƯỚC 6: Email là thông tin quan trọng để định danh user. Nếu không có email,
            //         không thể xác định user, nên chuyển hướng về trang Login.
            // -------------------------------------------------------------------
            if (string.IsNullOrEmpty(email))
                return RedirectToAction("Login");
            // Không có email = Google không cho chúng ta thông tin để login -> quay lại Login.

            // -------------------------------------------------------------------
            // BƯỚC 7: Tìm hoặc tạo mới bản ghi user trong database (sử dụng Entity Framework).
            // -------------------------------------------------------------------
            // Sử dụng _db.Users.FirstOrDefaultAsync(u => u.Email == email) để kiểm tra
            // xem database đã có user với email này chưa.
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

            string finalFullName = !string.IsNullOrWhiteSpace(user?.FullName)
                ? user.FullName
                : (result.Principal.FindFirst(ClaimTypes.Name)?.Value ?? email ?? "");

            string finalAvatarUrl = null;
            if (user != null && !string.IsNullOrEmpty(user.AvatarUrl) && user.AvatarUrl.Contains("res.cloudinary.com"))
            {
                // Ưu tiên avatar Cloudinary nếu có
                finalAvatarUrl = user.AvatarUrl;
            }
            else if (!string.IsNullOrEmpty(avatarUrl))
            {
                // Nếu Google trả về avatar, dùng nó
                finalAvatarUrl = avatarUrl;
            }
            else
            {
                // Nếu không có gì, dùng default
                finalAvatarUrl = "/images/default-avatar.jpeg";
            }
            
            if (user == null)
            {
                // -------------------------------------------------------------------
                // BƯỚC 7a: Nếu user chưa tồn tại trong DB, tạo mới một đối tượng User
                //         và lưu thông tin do Google trả về.
                // -------------------------------------------------------------------
                user = new User
                {
                    Email = email, // Lưu địa chỉ email do Google cung cấp
                    FullName = finalFullName, // Lưu tên đầy đủ (hoặc email thay thế)
                    AvatarUrl = finalAvatarUrl, // Lưu link avatar (hoặc avatar mặc định)
                    CreatedAt = DateTime.Now, // Lưu thời điểm tạo (ngày giờ hiện tại)
                    LastLoginAt = DateTime.Now, // Lưu thời điểm lần đăng nhập cuối (chính là lúc này)
                    IsPremium = false // Mặc định user chưa nâng cấp tài khoản (ví dụ)
                    // Các navigation property khác (nếu có) không set ở đây
                };
                // Thêm đối tượng user mới vào DbContext, nhưng chưa ghi vào DB cho đến khi SaveChangesAsync()
                _db.Users.Add(user);
            }
            else
            {
                // -------------------------------------------------------------------
                // BƯỚC 7b: Nếu user đã tồn tại (đã login trước đó bằng Google hoặc cách khác),
                //         chỉ cần cập nhật LastLoginAt (để biết lần login mới nhất),
                //         và có thể cập nhật avatar, tên nếu phía Google có thay đổi.
                // -------------------------------------------------------------------
                user.LastLoginAt = DateTime.Now;
                // Chỉ cập nhật tên nếu DB chưa có tên (null hoặc rỗng)
                if (string.IsNullOrEmpty(user.FullName) && !string.IsNullOrEmpty(finalFullName))
                    user.FullName = finalFullName;
                // Chỉ cập nhật avatar nếu không phải Cloudinary (giữ avatar upload thủ công)
                if (!(user.AvatarUrl != null && user.AvatarUrl.Contains("res.cloudinary.com")))
                    user.AvatarUrl = finalAvatarUrl;
            }

            // -------------------------------------------------------------------
            // BƯỚC 8: Gọi SaveChangesAsync() để ghi mọi thay đổi (thêm mới hoặc cập nhật) vào database.
            // -------------------------------------------------------------------
            await _db.SaveChangesAsync();
            // Lúc này, nếu user mới, bản ghi User đã được thêm vào bảng Users.
            // Nếu user cũ, các cột LastLoginAt, FullName, AvatarUrl đã được cập nhật.

            // -------------------------------------------------------------------
            // BƯỚC 8b: Đảm bảo mỗi User luôn có ít nhất 1 Role trong bảng Roles.
            //          • Nếu chưa có bản ghi Role nào cho user -> thêm "User" (mặc định).
            //          • Nếu hệ thống CHƯA có bất kỳ Admin nào, phong Admin cho user đầu tiên.
            // -------------------------------------------------------------------
            var hasAnyRole = await _db.Roles.AnyAsync(r => r.UserId == user.UserId);
            if (!hasAnyRole)
            {
                _db.Roles.Add(new Role
                {
                    UserId   = user.UserId,
                    RoleName = "User",
                    CreateAt = DateTime.Now
                });

                // Nếu chưa tồn tại Admin trong hệ thống, gán luôn Admin cho user hiện tại
                var hasAdmin = await _db.Roles.AnyAsync(r => r.RoleName == "Admin");
                if (!hasAdmin)
                {
                    _db.Roles.Add(new Role
                    {
                        UserId   = user.UserId,
                        RoleName = "Admin",
                        CreateAt = DateTime.Now
                    });
                }

                await _db.SaveChangesAsync(); // Lưu các Role mới thêm
            }

            // -------------------------------------------------------------------
            // BƯỚC 9: Lưu thông tin user vào Session cho các trang khác dễ truy xuất.
            //         Session là một vùng lưu dữ liệu tạm thời trên server, gắn liền với cookie của user.
            // -------------------------------------------------------------------
            // HttpContext.Session.SetInt32("UserId", user.UserId);
            //      - Lưu UserId để sau này khi cần biết ai đang đăng nhập, ta gửi UserId.
            // HttpContext.Session.SetString("FullName", user.FullName ?? "");
            //      - Lưu tên đầy đủ để hiển thị ở navbar, header, v.v.
            // HttpContext.Session.SetString("AvatarUrl", user.AvatarUrl ?? "");
            //      - Lưu link avatar để hiển thị ảnh đại diện ở các trang khác.
            HttpContext.Session.SetInt32("UserId", user.UserId);
            HttpContext.Session.SetString("FullName", user.FullName ?? "");
            HttpContext.Session.SetString("AvatarUrl", user.AvatarUrl ?? "");
            HttpContext.Session.SetString("GoogleFullName", fullName ?? "");
            
            var claims = result.Principal.Claims.ToList();
            claims.Add(new Claim("UserId", user.UserId.ToString()));

            // Bổ sung các role của user vào claims để hệ thống Authorization nhận diện
            var userRoles = await _db.Roles
                .Where(r => r.UserId == user.UserId)
                .Select(r => r.RoleName)
                .ToListAsync();

            foreach (var roleName in userRoles)
            {
                claims.Add(new Claim(ClaimTypes.Role, roleName));
            }

            var identity  = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            
            
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal);

            // -------------------------------------------------------------------
            // BƯỚC 11: Chuyển hướng người dùng dựa trên vai trò (Role).
            // -------------------------------------------------------------------
            // Kiểm tra xem người dùng có vai trò "Admin" hay không.
            if (userRoles.Contains("Admin"))
            {
                // Nếu là Admin, luôn chuyển hướng đến trang Admin Dashboard.
                return RedirectToAction("Index", "Admin");
            }
            
            // Nếu không phải Admin, chuyển hướng đến trang đích ban đầu (mặc định là Chat).
            return Redirect(returnUrl);
        }

        // ---------------------------------------------
        // Author : A–DUY
        // Date   : 2025-01-XX
        // Task   : Xử lý callback từ Facebook, lưu user và đăng nhập.
        // ---------------------------------------------
        /// <summary>
        /// Phương thức này sẽ được gọi khi Facebook gửi người dùng trở lại
        /// sau khi họ đã đăng nhập và cấp quyền cho ứng dụng. Nó thực hiện các bước:
        /// 1) Lấy kết quả xác thực (authentication) đã lưu trong cookie.
        /// 2) Đọc thông tin email, tên, avatar do Facebook trả về (claims).
        /// 3) Kiểm tra hoặc tạo mới bản ghi user trong database.
        /// 4) Lưu thông tin user vào session để các trang khác có thể sử dụng.
        /// 5) Ghi nhận user đã đăng nhập (SignIn) bằng cách sử dụng cookie authentication.
        /// 6) Cuối cùng, chuyển hướng (redirect) đến trang đích (returnUrl).
        /// </summary>
        /// <param name="returnUrl">
        /// Đường dẫn nội bộ (trong ứng dụng) mà chúng ta muốn đưa user tới
        /// sau khi quá trình đăng nhập thành công. Mặc định là "/Chat/Index".
        /// </param>
        /// <returns>
        /// Nếu có lỗi (không lấy được xác thực hoặc email), trả về Redirect về Login.
        /// Nếu thành công, trả về Redirect về returnUrl, cho phép user tiếp tục sử dụng hệ thống.
        /// </returns>
        [HttpGet]
        public async Task<IActionResult> FacebookResponse(string returnUrl = "/Chat/Index")
        {
            // -------------------------------------------------------------------
            // BƯỚC 1: Lấy kết quả xác thực (AuthenticateAsync) đã được middleware
            //         lưu trong cookie trước đó. Middleware đã xử lý Facebook OAuth
            //         và lưu vào cookie thông tin claims về user.
            // -------------------------------------------------------------------
            var result = await HttpContext.AuthenticateAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            // -------------------------------------------------------------------
            // BƯỚC 2: Kiểm tra xem việc xác thực (authentication) có thành công hay không.
            // -------------------------------------------------------------------
            if (!result.Succeeded)
                return RedirectToAction("Login");

            // -------------------------------------------------------------------
            // BƯỚC 3: Đọc các thông tin "claims" (tuyên bố) do Facebook gửi về.
            // -------------------------------------------------------------------
            var email = result.Principal.FindFirst(ClaimTypes.Email)?.Value;
            var fullName = result.Principal.FindFirst(ClaimTypes.Name)?.Value;
            var avatarUrl = result.Principal.FindFirst("picture")?.Value;

            // -------------------------------------------------------------------
            // BƯỚC 4: Xử lý avatar mặc định nếu Facebook không trả về
            // -------------------------------------------------------------------
            if (string.IsNullOrEmpty(avatarUrl))
                avatarUrl = "/images/default-avatar.jpeg";

            // -------------------------------------------------------------------
            // BƯỚC 5: Xử lý tên mặc định nếu Facebook không trả về
            // -------------------------------------------------------------------
            if (string.IsNullOrEmpty(fullName))
                fullName = email ?? "";

            // -------------------------------------------------------------------
            // BƯỚC 6: Kiểm tra email - thông tin quan trọng để định danh user
            // -------------------------------------------------------------------
            if (string.IsNullOrEmpty(email))
                return RedirectToAction("Login");

            // -------------------------------------------------------------------
            // BƯỚC 7: Tìm hoặc tạo mới bản ghi user trong database
            // -------------------------------------------------------------------
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                // -------------------------------------------------------------------
                // BƯỚC 7a: Nếu user chưa tồn tại trong DB, tạo mới một đối tượng User
                // -------------------------------------------------------------------
                user = new User
                {
                    Email = email,
                    FullName = fullName,
                    AvatarUrl = avatarUrl,
                    CreatedAt = DateTime.Now,
                    LastLoginAt = DateTime.Now,
                    IsPremium = false
                };
                _db.Users.Add(user);
            }
            else
            {
                // -------------------------------------------------------------------
                // BƯỚC 7b: Nếu user đã tồn tại, cập nhật thông tin
                // -------------------------------------------------------------------
                user.LastLoginAt = DateTime.Now;
                if (!string.IsNullOrEmpty(fullName))
                    user.FullName = fullName;
                if (!string.IsNullOrEmpty(avatarUrl))
                    user.AvatarUrl = avatarUrl;
            }

            // -------------------------------------------------------------------
            // BƯỚC 8: Lưu thay đổi vào database
            // -------------------------------------------------------------------
            await _db.SaveChangesAsync();

            // -------------------------------------------------------------------
            // BƯỚC 9: Đảm bảo mỗi User luôn có ít nhất 1 Role
            // -------------------------------------------------------------------
            var hasAnyRole = await _db.Roles.AnyAsync(r => r.UserId == user.UserId);
            if (!hasAnyRole)
            {
                _db.Roles.Add(new Role
                {
                    UserId = user.UserId,
                    RoleName = "User",
                    CreateAt = DateTime.Now
                });

                // Nếu chưa tồn tại Admin trong hệ thống, gán luôn Admin cho user hiện tại
                var hasAdmin = await _db.Roles.AnyAsync(r => r.RoleName == "Admin");
                if (!hasAdmin)
                {
                    _db.Roles.Add(new Role
                    {
                        UserId = user.UserId,
                        RoleName = "Admin",
                        CreateAt = DateTime.Now
                    });
                }

                await _db.SaveChangesAsync();
            }

            // -------------------------------------------------------------------
            // BƯỚC 10: Lưu thông tin user vào Session
            // -------------------------------------------------------------------
            HttpContext.Session.SetInt32("UserId", user.UserId);
            HttpContext.Session.SetString("FullName", user.FullName ?? "");
            HttpContext.Session.SetString("AvatarUrl", user.AvatarUrl ?? "");

            // -------------------------------------------------------------------
            // BƯỚC 11: Tạo claims identity và sign in
            // -------------------------------------------------------------------
            var claims = result.Principal.Claims.ToList();
            claims.Add(new Claim("UserId", user.UserId.ToString()));

            // Bổ sung các role của user vào claims
            var userRoles = await _db.Roles
                .Where(r => r.UserId == user.UserId)
                .Select(r => r.RoleName)
                .ToListAsync();

            foreach (var roleName in userRoles)
            {
                claims.Add(new Claim(ClaimTypes.Role, roleName));
            }

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal);

            // -------------------------------------------------------------------
            // BƯỚC 12: Chuyển hướng người dùng dựa trên vai trò (Role)
            // -------------------------------------------------------------------
            if (userRoles.Contains("Admin"))
            {
                return RedirectToAction("Index", "Admin");
            }

            return Redirect(returnUrl);
        }

        // ---------------------------------------------
        // Author: A–DUY
        // Date: 2025-05-30
        // Task: Đăng xuất và xóa session.
        // ---------------------------------------------
        /// <summary>
        /// Phương thức này sẽ được gọi khi user bấm nút "Logout".
        /// Nó thực hiện 3 bước chính:
        /// 1) Xóa cookie xác thực (authentication cookie) để user không còn được xem là đã đăng nhập.
        /// 2) Xóa sạch toàn bộ dữ liệu trong Session (như UserId, FullName, AvatarUrl).
        /// 3) Chuyển hướng (redirect) về trang Login để user có thể đăng nhập lại nếu muốn.
        /// </summary>
        /// <returns>
        /// Một lệnh RedirectToAction để điều hướng trình duyệt đến action "Login" trong cùng controller.
        /// </returns>
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            // -------------------------------------------------------------------
            // BƯỚC 1: Xóa authentication cookie
            // - Khi user đăng nhập, hệ thống đã tạo một cookie xác thực (cookie auth).
            // - Cookie này lưu thông tin về user đã xác thực (ClaimsPrincipal).
            // - Khi gọi SignOutAsync("Cookies"), ASP.NET Core sẽ xóa cookie đó.
            // - Kết quả: user không còn được xem là "đã đăng nhập".
            // -------------------------------------------------------------------
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            // CookieAuthenticationDefaults.AuthenticationScheme = "Cookies" (hoặc tên bạn đã cấu hình).
            // SignOutAsync sẽ gửi một HTTP response để bảo browser xóa cookie xác thực.

            // -------------------------------------------------------------------
            // BƯỚC 2: Xóa toàn bộ dữ liệu Session
            // - Trong suốt quá trình user sử dụng, ta có lưu các thông tin vào Session:
            //     + UserId (số nguyên)
            //     + FullName (chuỗi)
            //     + AvatarUrl (chuỗi)
            // - HttpContext.Session.Clear() sẽ xóa hết các key mà ta đã set trước đó.
            // - Kết quả: session hoàn toàn trống, không còn dữ liệu cũ.
            // -------------------------------------------------------------------
            HttpContext.Session.Clear();

            // -------------------------------------------------------------------
            // BƯỚC 3: Chuyển hướng về trang Login
            // - RedirectToAction("Login") sẽ tạo HTTP 302 redirect đến action Login.
            // - Trình duyệt sẽ tự động điều hướng đến URL tương ứng.
            // - Nhờ đó, user sẽ thấy lại trang đăng nhập nếu muốn vào lại hệ thống.
            // -------------------------------------------------------------------
            return RedirectToAction("Login");
        }
    }
}