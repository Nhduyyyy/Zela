#!/bin/bash

echo "🔍 Kiểm tra appsettings.json trong container..."
echo ""

# Kiểm tra file appsettings.json có trong container không
echo "📄 Kiểm tra appsettings.json:"
docker exec zela-app ls -la /app/appsettings.json 2>&1

echo ""
echo "📄 Nội dung appsettings.json (PayOS section):"
docker exec zela-app cat /app/appsettings.json 2>&1 | grep -A 5 "PayOS" || echo "Không tìm thấy PayOS config"

echo ""
echo "🔍 Kiểm tra environment variables:"
docker exec zela-app printenv | grep -i payos || echo "Không có PayOS env vars"

echo ""
echo "💡 Nếu file không tồn tại hoặc thiếu config, cần rebuild container"

