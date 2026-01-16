#!/bin/bash

echo "🔍 Kiểm tra vấn đề cookie và correlation..."
echo ""

# Kiểm tra xem có vấn đề gì với cookie không
echo "📝 Test request và xem cookies được set như thế nào:"
echo ""

# Test với curl để xem cookies
echo "1. Test GET request đến trang login:"
curl -v -c /tmp/cookies.txt http://zelahahaha.site/Account/Login 2>&1 | grep -i "set-cookie\|cookie" | head -10

echo ""
echo "2. Cookies được lưu:"
cat /tmp/cookies.txt 2>/dev/null | head -10

echo ""
echo "💡 Nếu không thấy cookies, có thể do:"
echo "   - Domain không khớp"
echo "   - Cookie bị block"
echo "   - SameSite policy"

