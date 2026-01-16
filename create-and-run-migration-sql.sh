#!/bin/bash

echo "🔧 Tạo SQL script từ migration và chạy trực tiếp..."
echo ""

# Lấy network name
NETWORK=$(docker network ls | grep zela | awk '{print $2}' | head -1)

if [ -z "$NETWORK" ]; then
    NETWORK="zela_zela-network"
fi

echo "📡 Sử dụng network: $NETWORK"
echo ""

# Tạo SQL script từ migration
echo "📝 Tạo SQL script từ migration..."
docker run --rm \
  --network "$NETWORK" \
  -v "$(pwd):/app" \
  -w /app \
  -e 'ConnectionStrings__DefaultConnection=Server=sqlserver,1433;Database=Zela_FinalV2.0;User ID=SA;Password=Str0ng_Pa$w0rd!;MultipleActiveResultSets=true;Encrypt=False;TrustServerCertificate=True;' \
  mcr.microsoft.com/dotnet/sdk:8.0 \
  bash -c '
    dotnet tool install --global dotnet-ef --version 8.0.0 > /dev/null 2>&1
    export PATH="$PATH:/root/.dotnet/tools"
    echo "🔄 Đang tạo SQL script..."
    dotnet ef migrations script --project Zela.csproj --idempotent --output /app/migration.sql
    if [ $? -eq 0 ]; then
        echo "✅ SQL script đã được tạo tại migration.sql"
        echo "📄 Số dòng trong file:"
        wc -l /app/migration.sql
    else
        echo "❌ Lỗi khi tạo SQL script"
        exit 1
    fi
  '

if [ ! -f migration.sql ]; then
    echo "❌ Không tìm thấy file migration.sql"
    exit 1
fi

echo ""
echo "🔄 Chạy SQL script trực tiếp..."
docker run --rm \
  -i \
  --network "$NETWORK" \
  -v "$(pwd)/migration.sql:/tmp/migration.sql:ro" \
  mcr.microsoft.com/mssql-tools:latest \
  /opt/mssql-tools/bin/sqlcmd -S sqlserver,1433 -U SA -P 'Str0ng_Pa$w0rd!' \
  -d Zela_FinalV2.0 \
  -i /tmp/migration.sql

if [ $? -eq 0 ]; then
    echo ""
    echo "✅ SQL script đã được chạy!"
    echo ""
    echo "🔍 Kiểm tra bảng Users:"
    docker run --rm \
      --network "$NETWORK" \
      mcr.microsoft.com/mssql-tools:latest \
      /opt/mssql-tools/bin/sqlcmd -S sqlserver,1433 -U SA -P 'Str0ng_Pa$w0rd!' \
      -d Zela_FinalV2.0 \
      -Q "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Users'" 2>&1 | grep -i "Users" && echo "✅ Bảng Users đã tồn tại!" || echo "❌ Bảng Users vẫn chưa được tạo"
    
    echo ""
    echo "🔍 Kiểm tra __EFMigrationsHistory:"
    docker run --rm \
      --network "$NETWORK" \
      mcr.microsoft.com/mssql-tools:latest \
      /opt/mssql-tools/bin/sqlcmd -S sqlserver,1433 -U SA -P 'Str0ng_Pa$w0rd!' \
      -d Zela_FinalV2.0 \
      -Q "SELECT * FROM [__EFMigrationsHistory]" 2>&1 | head -10
else
    echo "❌ Có lỗi khi chạy SQL script"
    exit 1
fi

