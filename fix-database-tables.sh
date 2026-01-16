#!/bin/bash

echo "🔧 Sửa lỗi database - chạy migrations để tạo bảng Users..."
echo ""

# Lấy tên network từ docker-compose.yml
NETWORK_NAME=$(docker compose config --format json 2>/dev/null | grep -o '"networks":{[^}]*}' | head -1 | grep -o '"[^"]*":' | head -1 | tr -d '":')

if [ -z "$NETWORK_NAME" ]; then
    # Thử cách khác
    NETWORK_NAME=$(docker network ls | grep zela | awk '{print $1}' | head -1)
    if [ -z "$NETWORK_NAME" ]; then
        NETWORK_NAME="zela_zela-network"
    fi
fi

echo "📡 Sử dụng network: $NETWORK_NAME"

# Kiểm tra database có tồn tại không
echo "🔍 Kiểm tra database..."
docker run --rm \
  --network "$NETWORK_NAME" \
  mcr.microsoft.com/mssql-tools:latest \
  /opt/mssql-tools/bin/sqlcmd -S sqlserver,1433 -U SA -P 'Str0ng_Pa$w0rd!' \
  -Q "SELECT name FROM sys.databases WHERE name = 'Zela_FinalV2.0'" 2>&1 | grep -i "Zela_FinalV2.0" || echo "Database chưa tồn tại"

echo ""
echo "🔄 Đang chạy migrations..."

# Chạy migrations
docker run --rm \
  --network "$NETWORK_NAME" \
  -v "$(pwd):/app" \
  -w /app \
  -e ConnectionStrings__DefaultConnection="Server=sqlserver,1433;Database=Zela_FinalV2.0;User ID=SA;Password=Str0ng_Pa$w0rd!;MultipleActiveResultSets=true;Encrypt=False;TrustServerCertificate=True;" \
  mcr.microsoft.com/dotnet/sdk:8.0 \
  bash -c "dotnet tool install --global dotnet-ef --version 8.0.0 && export PATH=\"\$PATH:/root/.dotnet/tools\" && dotnet ef database update --project Zela.csproj"

if [ $? -eq 0 ]; then
    echo ""
    echo "✅ Migrations đã chạy thành công!"
    echo ""
    echo "🔍 Kiểm tra bảng Users đã được tạo:"
    docker run --rm \
      --network "$NETWORK_NAME" \
      mcr.microsoft.com/mssql-tools:latest \
      /opt/mssql-tools/bin/sqlcmd -S sqlserver,1433 -U SA -P 'Str0ng_Pa$w0rd!' \
      -d Zela_FinalV2.0 \
      -Q "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Users'" 2>&1 | grep -i "Users" && echo "✅ Bảng Users đã tồn tại!" || echo "❌ Bảng Users chưa được tạo"
else
    echo "❌ Có lỗi khi chạy migrations"
    exit 1
fi

