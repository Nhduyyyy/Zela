#!/bin/bash

echo "🔧 Sửa lỗi migrations - chạy thủ công..."
echo ""

# Lấy network name
NETWORK=$(docker network ls | grep zela | awk '{print $2}' | head -1)

if [ -z "$NETWORK" ]; then
    NETWORK="zela_zela-network"
fi

echo "📡 Sử dụng network: $NETWORK"
echo ""

# Xóa database
echo "🗑️  Xóa database..."
docker run --rm \
  --network "$NETWORK" \
  mcr.microsoft.com/mssql-tools:latest \
  /opt/mssql-tools/bin/sqlcmd -S sqlserver,1433 -U SA -P 'Str0ng_Pa$w0rd!' \
  -Q "IF EXISTS (SELECT * FROM sys.databases WHERE name = 'Zela_FinalV2.0') BEGIN ALTER DATABASE [Zela_FinalV2.0] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [Zela_FinalV2.0]; END" 2>&1

sleep 5

# Tạo database trước
echo "📦 Tạo database..."
docker run --rm \
  --network "$NETWORK" \
  mcr.microsoft.com/mssql-tools:latest \
  /opt/mssql-tools/bin/sqlcmd -S sqlserver,1433 -U SA -P 'Str0ng_Pa$w0rd!' \
  -Q "CREATE DATABASE [Zela_FinalV2.0]" 2>&1

sleep 2

# Xóa bảng __EFMigrationsHistory nếu có (để force chạy lại migrations)
echo "🗑️  Xóa __EFMigrationsHistory nếu có..."
docker run --rm \
  --network "$NETWORK" \
  mcr.microsoft.com/mssql-tools:latest \
  /opt/mssql-tools/bin/sqlcmd -S sqlserver,1433 -U SA -P 'Str0ng_Pa$w0rd!' \
  -d Zela_FinalV2.0 \
  -Q "IF EXISTS (SELECT * FROM sys.tables WHERE name = '__EFMigrationsHistory') DROP TABLE [__EFMigrationsHistory]" 2>&1

# Chạy migrations
echo "🔄 Chạy migrations..."
docker run --rm \
  --network "$NETWORK" \
  -v "$(pwd):/app" \
  -w /app \
  -e 'ConnectionStrings__DefaultConnection=Server=sqlserver,1433;Database=Zela_FinalV2.0;User ID=SA;Password=Str0ng_Pa$w0rd!;MultipleActiveResultSets=true;Encrypt=False;TrustServerCertificate=True;' \
  mcr.microsoft.com/dotnet/sdk:8.0 \
  bash -c 'dotnet tool install --global dotnet-ef --version 8.0.0 > /dev/null 2>&1 && export PATH="$PATH:/root/.dotnet/tools" && dotnet ef database update --project Zela.csproj'

# Kiểm tra
echo ""
echo "🔍 Kiểm tra bảng Users:"
docker run --rm \
  --network "$NETWORK" \
  mcr.microsoft.com/mssql-tools:latest \
  /opt/mssql-tools/bin/sqlcmd -S sqlserver,1433 -U SA -P 'Str0ng_Pa$w0rd!' \
  -d Zela_FinalV2.0 \
  -Q "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Users'" 2>&1 | grep -i "Users" && echo "✅ Bảng Users đã tồn tại!" || echo "❌ Bảng Users vẫn chưa được tạo"

