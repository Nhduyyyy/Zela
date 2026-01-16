#!/bin/bash

# Script đơn giản để chạy migrations - sử dụng appsettings.json có sẵn

echo "🔄 Đang chạy migrations..."

# Kiểm tra network
NETWORK=$(docker network ls | grep zela | awk '{print $2}' | head -1)
if [ -z "$NETWORK" ]; then
    echo "❌ Không tìm thấy network zela"
    exit 1
fi

echo "📡 Sử dụng network: $NETWORK"

# Chạy migrations - EF sẽ đọc connection string từ appsettings.json
# Nhưng cần override để trỏ đến sqlserver thay vì localhost
docker run --rm \
  --network "$NETWORK" \
  -v "$(pwd):/app" \
  -w /app \
  -e 'ConnectionStrings__DefaultConnection=Server=sqlserver,1433;Database=Zela_FinalV2.0;User ID=SA;Password=Str0ng_Pa$w0rd!;MultipleActiveResultSets=true;Encrypt=False;TrustServerCertificate=True;' \
  mcr.microsoft.com/dotnet/sdk:8.0 \
  bash -c '
    dotnet tool install --global dotnet-ef --version 8.0.0 > /dev/null 2>&1
    export PATH="$PATH:/root/.dotnet/tools"
    dotnet ef database update --project Zela.csproj
  '

if [ $? -eq 0 ]; then
    echo "✅ Migrations đã chạy thành công!"
else
    echo "❌ Có lỗi khi chạy migrations"
    echo ""
    echo "💡 Thử test kết nối SQL Server trước:"
    echo "   ./test-sql-simple.sh"
    exit 1
fi

