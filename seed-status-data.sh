#!/bin/bash

echo "🌱 Chèn dữ liệu cố định vào bảng Statuses..."
echo ""

# Lấy network name
NETWORK=$(docker network ls | grep zela | awk '{print $2}' | head -1)

if [ -z "$NETWORK" ]; then
    NETWORK="zela_zela-network"
fi

echo "📡 Sử dụng network: $NETWORK"
echo ""

# Kiểm tra file SQL có tồn tại không
if [ ! -f "seed-status-data.sql" ]; then
    echo "❌ Không tìm thấy file seed-status-data.sql"
    exit 1
fi

# Chạy SQL script
echo "🔄 Đang chèn dữ liệu vào bảng Statuses..."
docker run --rm \
  --network "$NETWORK" \
  -v "$(pwd)/seed-status-data.sql:/tmp/seed.sql:ro" \
  mcr.microsoft.com/mssql-tools:latest \
  /opt/mssql-tools/bin/sqlcmd \
  -S sqlserver,1433 \
  -U SA \
  -P 'Str0ng_Pa$$w0rd!' \
  -d Zela_FinalV2.0 \
  -i /tmp/seed.sql

if [ $? -eq 0 ]; then
    echo ""
    echo "✅ Đã chèn dữ liệu thành công!"
    echo ""
    echo "🔍 Kiểm tra dữ liệu đã chèn:"
    docker run --rm \
      --network "$NETWORK" \
      mcr.microsoft.com/mssql-tools:latest \
      /opt/mssql-tools/bin/sqlcmd \
      -S sqlserver,1433 \
      -U SA \
      -P 'Str0ng_Pa$$w0rd!' \
      -d Zela_FinalV2.0 \
      -Q "SELECT StatusId, StatuName, CreatedAt, Describe FROM Statuses ORDER BY StatusId" \
      -W -h -1
else
    echo ""
    echo "❌ Có lỗi xảy ra khi chèn dữ liệu!"
    exit 1
fi

