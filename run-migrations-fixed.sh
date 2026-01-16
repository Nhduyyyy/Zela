#!/bin/bash

# Script chạy migrations - phiên bản đã sửa password

echo "🔄 Đang chạy migrations bằng Docker..."

# Kiểm tra network name
NETWORK_NAME=$(docker network ls | grep zela | awk '{print $1}' | head -1)
if [ -z "$NETWORK_NAME" ]; then
    NETWORK_NAME="zela_zela-network"
fi

echo "📡 Sử dụng network: $NETWORK_NAME"

# Tạo file appsettings tạm thời với connection string đúng
cat > /tmp/appsettings.migrate.json << EOF
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=sqlserver,1433;Database=Zela_FinalV2.0;User ID=SA;Password=Str0ng_Pa\$\$w0rd!;MultipleActiveResultSets=true;Encrypt=False;TrustServerCertificate=True;"
  }
}
EOF

# Chạy migrations với appsettings file
docker run --rm \
  --network "$NETWORK_NAME" \
  -v "$(pwd):/app" \
  -v "/tmp/appsettings.migrate.json:/app/appsettings.migrate.json:ro" \
  -w /app \
  mcr.microsoft.com/dotnet/sdk:8.0 \
  bash -c 'dotnet tool install --global dotnet-ef --version 8.0.0 > /dev/null 2>&1 && export PATH="$PATH:/root/.dotnet/tools" && dotnet ef database update --project Zela.csproj --configuration Release'

if [ $? -eq 0 ]; then
    echo "✅ Migrations đã chạy thành công!"
    rm -f /tmp/appsettings.migrate.json
else
    echo "❌ Có lỗi khi chạy migrations"
    echo "💡 Thử cách khác: Kiểm tra password SQL Server"
    echo "   docker exec -it zela-sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U SA -P 'Str0ng_Pa\$\$w0rd!' -Q 'SELECT 1'"
    exit 1
fi

