#!/bin/bash

echo "🔍 Chẩn đoán vấn đề OAuth Correlation failed..."
echo ""

# 1. Kiểm tra container có rebuild chưa
echo "1. Kiểm tra thời gian build container:"
docker inspect zela-app --format='{{.Created}}' 2>/dev/null || echo "Container không tồn tại"

echo ""
echo "2. Kiểm tra có reverse proxy (nginx/apache) không:"
ps aux | grep -E "nginx|apache" | grep -v grep || echo "Không có reverse proxy"

echo ""
echo "3. Kiểm tra port 80 đang được dùng bởi:"
sudo netstat -tulpn | grep :80 | head -5 || echo "Không tìm thấy"

echo ""
echo "4. Kiểm tra DataProtection keys:"
docker exec zela-app ls -la /root/.aspnet/DataProtection-Keys 2>&1 | head -5

echo ""
echo "5. Kiểm tra environment variables:"
docker exec zela-app printenv | grep -E "ASPNETCORE|AppSettings" | head -10

echo ""
echo "6. Test cookie settings với curl:"
curl -v -c /tmp/test_cookies.txt http://zelahahaha.site/Account/Login 2>&1 | grep -i "set-cookie" | head -5

echo ""
echo "7. Cookies được set:"
cat /tmp/test_cookies.txt 2>/dev/null | head -10

echo ""
echo "💡 Nếu cookies có Secure flag, có thể do:"
echo "   - Reverse proxy đang set Secure flag"
echo "   - Container chưa rebuild với code mới"
echo "   - Cần cấu hình thêm trong Program.cs"

