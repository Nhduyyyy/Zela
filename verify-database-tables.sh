#!/bin/bash

echo "🔍 Kiểm tra các bảng trong database..."
echo ""

# Lấy network name
NETWORK=$(docker network ls | grep zela | awk '{print $2}' | head -1)

if [ -z "$NETWORK" ]; then
    NETWORK="zela_zela-network"
fi

echo "📡 Sử dụng network: $NETWORK"
echo ""

# Kiểm tra bảng Users
echo "1. Kiểm tra bảng Users:"
docker run --rm \
  --network "$NETWORK" \
  mcr.microsoft.com/mssql-tools:latest \
  /opt/mssql-tools/bin/sqlcmd -S sqlserver,1433 -U SA -P 'Str0ng_Pa$w0rd!' \
  -d Zela_FinalV2.0 \
  -Q "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Users'" 2>&1 | grep -i "Users" && echo "✅ Bảng Users đã tồn tại!" || echo "❌ Bảng Users chưa được tạo"

echo ""

# Liệt kê tất cả các bảng
echo "2. Tất cả các bảng trong database:"
docker run --rm \
  --network "$NETWORK" \
  mcr.microsoft.com/mssql-tools:latest \
  /opt/mssql-tools/bin/sqlcmd -S sqlserver,1433 -U SA -P 'Str0ng_Pa$w0rd!' \
  -d Zela_FinalV2.0 \
  -Q "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE' ORDER BY TABLE_NAME" 2>&1 | grep -v "^$" | tail -n +3

echo ""
echo "💡 Nếu bảng Users không tồn tại, có thể cần:"
echo "   1. Kiểm tra migrations có tạo bảng Users không"
echo "   2. Chạy lại migrations từ đầu"
echo "   3. Kiểm tra DbContext có định nghĩa User model không"

