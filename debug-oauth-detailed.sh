#!/bin/bash

echo "🔍 Debug OAuth flow chi tiết..."
echo ""

echo "1. Xem logs khi đăng nhập (đợi bạn click đăng nhập):"
echo "   Chạy lệnh này trong terminal khác:"
echo "   docker logs -f zela-app | grep -i 'correlation\|cookie\|google\|oauth'"
echo ""

echo "2. Kiểm tra cookies hiện tại trong container:"
docker exec zela-app cat /root/.aspnet/DataProtection-Keys/*.xml 2>/dev/null | head -5

echo ""
echo "3. Test xem correlation cookie có được tạo không:"
echo "   - Mở browser Developer Tools > Network tab"
echo "   - Click 'Đăng nhập bằng Google'"
echo "   - Xem request POST /Account/GoogleLogin"
echo "   - Kiểm tra response headers có Set-Cookie với correlation không"
echo ""

echo "4. Kiểm tra environment:"
docker exec zela-app printenv | grep -E "ASPNETCORE|AppSettings" | head -5

echo ""
echo "💡 Nếu vẫn lỗi, có thể thử:"
echo "   1. Xóa tất cả cookies và thử lại"
echo "   2. Dùng chế độ ẩn danh"
echo "   3. Kiểm tra redirect URI trong Google Console có đúng không"

