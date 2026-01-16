#!/bin/bash

echo "🔧 Chạy migration SQL trực tiếp từ file migration..."
echo ""

# Lấy network name
NETWORK=$(docker network ls | grep zela | awk '{print $2}' | head -1)

if [ -z "$NETWORK" ]; then
    NETWORK="zela_zela-network"
fi

echo "📡 Sử dụng network: $NETWORK"
echo ""

# Tạo script SQL từ migration
echo "📝 Tạo script SQL từ migration..."
docker run --rm \
  --network "$NETWORK" \
  -v "$(pwd):/app" \
  -w /app \
  -e 'ConnectionStrings__DefaultConnection=Server=sqlserver,1433;Database=Zela_FinalV2.0;User ID=SA;Password=Str0ng_Pa$w0rd!;MultipleActiveResultSets=true;Encrypt=False;TrustServerCertificate=True;' \
  mcr.microsoft.com/dotnet/sdk:8.0 \
  bash -c 'dotnet tool install --global dotnet-ef --version 8.0.0 > /dev/null 2>&1 && export PATH="$PATH:/root/.dotnet/tools" && dotnet ef migrations script --project Zela.csproj --idempotent --output /tmp/migration.sql && echo "✅ Script SQL đã được tạo"'

# Xóa __EFMigrationsHistory
echo ""
echo "🗑️  Xóa __EFMigrationsHistory..."
docker run --rm \
  --network "$NETWORK" \
  mcr.microsoft.com/mssql-tools:latest \
  /opt/mssql-tools/bin/sqlcmd -S sqlserver,1433 -U SA -P 'Str0ng_Pa$w0rd!' \
  -d Zela_FinalV2.0 \
  -Q "IF EXISTS (SELECT * FROM sys.tables WHERE name = '__EFMigrationsHistory') DROP TABLE [__EFMigrationsHistory]" 2>&1

# Chạy SQL script trực tiếp
echo ""
echo "🔄 Chạy SQL script trực tiếp..."
docker run --rm \
  --network "$NETWORK" \
  -v "$(pwd):/app" \
  -w /app \
  mcr.microsoft.com/dotnet/sdk:8.0 \
  bash -c 'cat /tmp/migration.sql 2>/dev/null || echo "Không tìm thấy script SQL"' | \
docker run --rm -i \
  --network "$NETWORK" \
  mcr.microsoft.com/mssql-tools:latest \
  /opt/mssql-tools/bin/sqlcmd -S sqlserver,1433 -U SA -P 'Str0ng_Pa$w0rd!' \
  -d Zela_FinalV2.0

# Hoặc cách đơn giản hơn: chạy migration script và lưu vào file, rồi chạy file đó
echo ""
echo "🔄 Cách 2: Tạo và chạy migration script..."
docker run --rm \
  --network "$NETWORK" \
  -v "$(pwd):/app" \
  -w /app \
  -e 'ConnectionStrings__DefaultConnection=Server=sqlserver,1433;Database=Zela_FinalV2.0;User ID=SA;Password=Str0ng_Pa$w0rd!;MultipleActiveResultSets=true;Encrypt=False;TrustServerCertificate=True;' \
  mcr.microsoft.com/dotnet/sdk:8.0 \
  bash -c '
    dotnet tool install --global dotnet-ef --version 8.0.0 > /dev/null 2>&1
    export PATH="$PATH:/root/.dotnet/tools"
    dotnet ef migrations script --project Zela.csproj --idempotent > /app/migration_output.sql
    echo "✅ Script đã được tạo tại migration_output.sql"
  '

# Chạy file SQL
if [ -f migration_output.sql ]; then
    echo ""
    echo "🔄 Chạy file SQL..."
    docker run --rm \
      -i \
      --network "$NETWORK" \
      -v "$(pwd)/migration_output.sql:/tmp/migration.sql:ro" \
      mcr.microsoft.com/mssql-tools:latest \
      /opt/mssql-tools/bin/sqlcmd -S sqlserver,1433 -U SA -P 'Str0ng_Pa$w0rd!' \
      -d Zela_FinalV2.0 \
      -i /tmp/migration.sql
    
    # Kiểm tra
    echo ""
    echo "🔍 Kiểm tra bảng Users:"
    docker run --rm \
      --network "$NETWORK" \
      mcr.microsoft.com/mssql-tools:latest \
      /opt/mssql-tools/bin/sqlcmd -S sqlserver,1433 -U SA -P 'Str0ng_Pa$w0rd!' \
      -d Zela_FinalV2.0 \
      -Q "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Users'" 2>&1 | grep -i "Users" && echo "✅ Bảng Users đã tồn tại!" || echo "❌ Bảng Users vẫn chưa được tạo"
else
    echo "❌ Không tìm thấy file migration_output.sql"
fi

