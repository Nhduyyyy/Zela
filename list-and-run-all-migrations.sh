#!/bin/bash

echo "🔧 Liệt kê migrations và tạo SQL script từ tất cả..."
echo ""

# Lấy network name
NETWORK=$(docker network ls | grep zela | awk '{print $2}' | head -1)

if [ -z "$NETWORK" ]; then
    NETWORK="zela_zela-network"
fi

echo "📡 Sử dụng network: $NETWORK"
echo ""

# Liệt kê migrations
echo "📋 Liệt kê migrations có sẵn:"
docker run --rm \
  --network "$NETWORK" \
  -v "$(pwd):/app" \
  -w /app \
  -e 'ConnectionStrings__DefaultConnection=Server=sqlserver,1433;Database=Zela_FinalV2.0;User ID=SA;Password=Str0ng_Pa$w0rd!;MultipleActiveResultSets=true;Encrypt=False;TrustServerCertificate=True;' \
  mcr.microsoft.com/dotnet/sdk:8.0 \
  bash -c '
    dotnet tool install --global dotnet-ef --version 8.0.0 > /dev/null 2>&1
    export PATH="$PATH:/root/.dotnet/tools"
    echo "🔄 Đang liệt kê migrations..."
    dotnet ef migrations list --project Zela.csproj
  '

echo ""
echo "📝 Tạo SQL script từ tất cả migrations (từ đầu đến cuối)..."
docker run --rm \
  --network "$NETWORK" \
  -v "$(pwd):/app" \
  -w /app \
  -e 'ConnectionStrings__DefaultConnection=Server=sqlserver,1433;Database=Zela_FinalV2.0;User ID=SA;Password=Str0ng_Pa$w0rd!;MultipleActiveResultSets=true;Encrypt=False;TrustServerCertificate=True;' \
  mcr.microsoft.com/dotnet/sdk:8.0 \
  bash -c '
    dotnet tool install --global dotnet-ef --version 8.0.0 > /dev/null 2>&1
    export PATH="$PATH:/root/.dotnet/tools"
    echo "🔄 Đang tạo SQL script từ tất cả migrations..."
    # Tạo script từ đầu (0) đến cuối (không chỉ định)
    dotnet ef migrations script --project Zela.csproj --output /app/migration.sql
    if [ $? -eq 0 ]; then
        echo "✅ SQL script đã được tạo"
        LINES=$(wc -l < /app/migration.sql)
        echo "📄 Số dòng: $LINES"
        if [ "$LINES" -lt 100 ]; then
            echo "⚠️  File quá ngắn, xem nội dung:"
            cat /app/migration.sql
        else
            echo "📄 100 dòng đầu:"
            head -100 /app/migration.sql
        fi
    else
        echo "❌ Lỗi khi tạo SQL script"
        exit 1
    fi
  '

if [ ! -f migration.sql ]; then
    echo "❌ Không tìm thấy file migration.sql"
    exit 1
fi

# Kiểm tra file có đủ lớn không
LINES=$(wc -l < migration.sql)
if [ "$LINES" -lt 50 ]; then
    echo ""
    echo "⚠️  File migration.sql quá ngắn ($LINES dòng)."
    echo "📄 Nội dung đầy đủ:"
    cat migration.sql
    echo ""
    echo "💡 Có thể database đã được tạo nhưng migrations không được apply."
    echo "   Thử chạy migration SQL trực tiếp từ file migration C#."
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
  -i /tmp/migration.sql 2>&1 | tail -20

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
else
    echo "❌ Có lỗi khi chạy SQL script"
    exit 1
fi

