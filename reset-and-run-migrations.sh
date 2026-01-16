#!/bin/bash

echo "🔄 Reset và chạy lại migrations từ đầu..."
echo ""

# Lấy network name
NETWORK=$(docker network ls | grep zela | awk '{print $2}' | head -1)

if [ -z "$NETWORK" ]; then
    NETWORK="zela_zela-network"
fi

echo "📡 Sử dụng network: $NETWORK"
echo ""

# Xác nhận
read -p "⚠️  Bạn có chắc muốn XÓA database và chạy lại migrations? (y/N): " confirm
if [ "$confirm" != "y" ] && [ "$confirm" != "Y" ]; then
    echo "❌ Đã hủy"
    exit 1
fi

# Xóa database
echo "🗑️  Xóa database cũ..."
docker run --rm \
  --network "$NETWORK" \
  mcr.microsoft.com/mssql-tools:latest \
  /opt/mssql-tools/bin/sqlcmd -S sqlserver,1433 -U SA -P 'Str0ng_Pa$w0rd!' \
  -Q "DROP DATABASE IF EXISTS [Zela_FinalV2.0]" 2>&1 | grep -v "^$" || echo "Database đã được xóa hoặc không tồn tại"

echo ""
echo "⏳ Đợi 3 giây..."
sleep 3

# Chạy migrations từ đầu
echo "🔄 Chạy migrations từ đầu..."
docker run --rm \
  --network "$NETWORK" \
  -v "$(pwd):/app" \
  -w /app \
  -e 'ConnectionStrings__DefaultConnection=Server=sqlserver,1433;Database=Zela_FinalV2.0;User ID=SA;Password=Str0ng_Pa$w0rd!;MultipleActiveResultSets=true;Encrypt=False;TrustServerCertificate=True;' \
  mcr.microsoft.com/dotnet/sdk:8.0 \
  bash -c 'dotnet tool install --global dotnet-ef --version 8.0.0 > /dev/null 2>&1 && export PATH="$PATH:/root/.dotnet/tools" && dotnet ef database update --project Zela.csproj'

if [ $? -eq 0 ]; then
    echo ""
    echo "✅ Migrations đã chạy thành công!"
    echo ""
    echo "🔍 Kiểm tra bảng Users:"
    docker run --rm \
      --network "$NETWORK" \
      mcr.microsoft.com/mssql-tools:latest \
      /opt/mssql-tools/bin/sqlcmd -S sqlserver,1433 -U SA -P 'Str0ng_Pa$w0rd!' \
      -d Zela_FinalV2.0 \
      -Q "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Users'" 2>&1 | grep -i "Users" && echo "✅ Bảng Users đã tồn tại!" || echo "❌ Bảng Users vẫn chưa được tạo"
else
    echo "❌ Có lỗi khi chạy migrations"
    exit 1
fi

