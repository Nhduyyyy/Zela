#!/bin/bash

echo "🔍 Test OAuth flow để kiểm tra correlation cookie..."
echo ""

# Test 1: GET trang login
echo "1. Test GET /Account/Login:"
curl -v -c /tmp/cookies_step1.txt http://zelahahaha.site/Account/Login 2>&1 | grep -i "set-cookie" | head -10

echo ""
echo "2. Cookies sau bước 1:"
cat /tmp/cookies_step1.txt 2>/dev/null | grep -v "^#" | head -10

echo ""
echo "3. Test POST /Account/GoogleLogin (bắt đầu OAuth):"
curl -v -b /tmp/cookies_step1.txt -c /tmp/cookies_step2.txt \
  -X POST \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "returnUrl=/" \
  http://zelahahaha.site/Account/GoogleLogin 2>&1 | grep -E "Location:|set-cookie" | head -10

echo ""
echo "4. Cookies sau bước 2 (khi redirect đến Google):"
cat /tmp/cookies_step2.txt 2>/dev/null | grep -v "^#" | head -10

echo ""
echo "💡 Kiểm tra xem có correlation cookie không:"
cat /tmp/cookies_step2.txt 2>/dev/null | grep -i "correlation" || echo "❌ Không tìm thấy correlation cookie!"

