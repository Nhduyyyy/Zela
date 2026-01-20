#!/bin/bash

echo "🔍 Kiểm tra cấu hình Nginx..."
echo ""

# Kiểm tra containers
echo "📊 Trạng thái containers:"
docker compose ps | grep -E "nginx|zela-app"

echo ""
echo "📝 Kiểm tra cấu hình Nginx:"
docker exec zela-nginx nginx -t 2>&1

echo ""
echo "📋 Logs Nginx (10 dòng cuối):"
docker logs zela-nginx --tail 10

echo ""
echo "🌐 Test kết nối từ Nginx đến ứng dụng:"
docker exec zela-nginx wget -q -O- http://zela-app:80 | head -20 || echo "❌ Không thể kết nối đến zela-app"

echo ""
echo "🔗 Test HTTP response:"
curl -I http://localhost:80 2>&1 | head -10

echo ""
echo "📡 Kiểm tra network:"
docker network inspect zela_zela-network --format '{{range .Containers}}{{.Name}} {{end}}' 2>/dev/null || echo "Network không tồn tại"

echo ""
echo "✅ Hoàn tất kiểm tra!"

