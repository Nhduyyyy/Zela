#!/bin/bash

echo "🔍 Kiểm tra environment variables trong container:"
echo "=================================================="

echo ""
echo "Google OAuth:"
docker exec zela-app printenv | grep -i "Authentication__Google" || echo "❌ Không tìm thấy Google OAuth env vars"

echo ""
echo "Facebook OAuth:"
docker exec zela-app printenv | grep -i "Authentication__Facebook" || echo "❌ Không tìm thấy Facebook OAuth env vars"

echo ""
echo "PayOS:"
docker exec zela-app printenv | grep -i "PayOS" || echo "❌ Không tìm thấy PayOS env vars"

echo ""
echo "Tất cả env vars:"
docker exec zela-app printenv | sort

