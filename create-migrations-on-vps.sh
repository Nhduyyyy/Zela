#!/bin/bash

echo "🔧 Tạo migrations mới trên VPS..."
echo ""

# Lấy network name
NETWORK=$(docker network ls | grep zela | awk '{print $2}' | head -1)

if [ -z "$NETWORK" ]; then
    NETWORK="zela_zela-network"
fi

echo "📡 Sử dụng network: $NETWORK"
echo ""

# Kiểm tra thư mục Migrations
if [ ! -d "Migrations" ]; then
    echo "📁 Tạo thư mục Migrations..."
    mkdir -p Migrations
fi

# Tạo migration mới từ DbContext
echo "🔄 Tạo migration mới từ DbContext..."
docker run --rm \
  --network "$NETWORK" \
  -v "$(pwd):/app" \
  -w /app \
  -e 'ConnectionStrings__DefaultConnection=Server=sqlserver,1433;Database=Zela_FinalV2.0;User ID=SA;Password=Str0ng_Pa$w0rd!;MultipleActiveResultSets=true;Encrypt=False;TrustServerCertificate=True;' \
  mcr.microsoft.com/dotnet/sdk:8.0 \
  bash -c '
    dotnet tool install --global dotnet-ef --version 8.0.0 > /dev/null 2>&1
    export PATH="$PATH:/root/.dotnet/tools"
    echo "🔄 Đang tạo migration mới..."
    dotnet ef migrations add InitialCreate --project Zela.csproj
    if [ $? -eq 0 ]; then
        echo "✅ Migration đã được tạo!"
        echo "📋 Migrations có sẵn:"
        ls -la Migrations/
    else
        echo "❌ Lỗi khi tạo migration"
        exit 1
    fi
  '

if [ $? -eq 0 ]; then
    echo ""
    echo "🔄 Chạy migrations..."
    docker run --rm \
      --network "$NETWORK" \
      -v "$(pwd):/app" \
      -w /app \
      -e 'ConnectionStrings__DefaultConnection=Server=sqlserver,1433;Database=Zela_FinalV2.0;User ID=SA;Password=Str0ng_Pa$w0rd!;MultipleActiveResultSets=true;Encrypt=False;TrustServerCertificate=True;' \
      mcr.microsoft.com/dotnet/sdk:8.0 \
      bash -c 'dotnet tool install --global dotnet-ef --version 8.0.0 > /dev/null 2>&1 && export PATH="$PATH:/root/.dotnet/tools" && dotnet ef database update --project Zela.csproj'
    
    echo ""
    echo "🔍 Kiểm tra bảng Users:"
    docker run --rm \
      --network "$NETWORK" \
      mcr.microsoft.com/mssql-tools:latest \
      /opt/mssql-tools/bin/sqlcmd -S sqlserver,1433 -U SA -P 'Str0ng_Pa$w0rd!' \
      -d Zela_FinalV2.0 \
      -Q "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Users'" 2>&1 | grep -i "Users" && echo "✅ Bảng Users đã tồn tại!" || echo "❌ Bảng Users vẫn chưa được tạo"
else
    echo "❌ Không thể tạo migration"
    exit 1
fi

