#!/bin/bash

echo "📝 Logs ứng dụng (50 dòng cuối):"
echo "=================================="
docker logs zela-app --tail 50

echo ""
echo "📝 Logs SQL Server (20 dòng cuối):"
echo "=================================="
docker logs zela-sqlserver --tail 20

echo ""
echo "🔍 Kiểm tra trạng thái containers:"
echo "=================================="
docker compose ps

echo ""
echo "🌐 Test kết nối HTTP:"
echo "=================================="
curl -I http://localhost:80 2>&1 | head -10

