#!/bin/bash

echo "🔧 Force tạo lại database và chạy migrations..."
echo ""

# Lấy network name
NETWORK=$(docker network ls | grep zela | awk '{print $2}' | head -1)

if [ -z "$NETWORK" ]; then
    NETWORK="zela_zela-network"
fi

echo "📡 Sử dụng network: $NETWORK"
echo ""

# Xóa database hoàn toàn (force)
echo "🗑️  Xóa database cũ (force)..."
docker run --rm \
  --network "$NETWORK" \
  mcr.microsoft.com/mssql-tools:latest \
  /opt/mssql-tools/bin/sqlcmd -S sqlserver,1433 -U SA -P 'Str0ng_Pa$w0rd!' \
  -Q "IF EXISTS (SELECT * FROM sys.databases WHERE name = 'Zela_FinalV2.0') BEGIN ALTER DATABASE [Zela_FinalV2.0] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [Zela_FinalV2.0]; END" 2>&1

echo ""
echo "⏳ Đợi 5 giây..."
sleep 5

# Chạy migrations với --force
echo "🔄 Chạy migrations (force)..."
docker run --rm \
  --network "$NETWORK" \
  -v "$(pwd):/app" \
  -w /app \
  -e 'ConnectionStrings__DefaultConnection=Server=sqlserver,1433;Database=Zela_FinalV2.0;User ID=SA;Password=Str0ng_Pa$w0rd!;MultipleActiveResultSets=true;Encrypt=False;TrustServerCertificate=True;' \
  mcr.microsoft.com/dotnet/sdk:8.0 \
  bash -c '
    dotnet tool install --global dotnet-ef --version 8.0.0 > /dev/null 2>&1
    export PATH="$PATH:/root/.dotnet/tools"
    echo "🔄 Đang tạo database và chạy migrations..."
    dotnet ef database update --project Zela.csproj
    echo ""
    echo "🔍 Kiểm tra migrations đã chạy:"
    dotnet ef migrations list --project Zela.csproj
  '

if [ $? -eq 0 ]; then
    echo ""
    echo "✅ Migrations đã chạy!"
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

