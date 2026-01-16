#!/bin/bash

echo "📝 Logs ứng dụng (50 dòng cuối - tìm lỗi):"
echo "=========================================="
docker logs zela-app --tail 50 2>&1 | grep -i "error\|exception\|fail" | tail -20

echo ""
echo "📝 Toàn bộ logs gần đây:"
echo "=========================================="
docker logs zela-app --tail 30

echo ""
echo "🔍 Test kết nối HTTP:"
echo "=========================================="
curl -v http://localhost:80 2>&1 | head -30

