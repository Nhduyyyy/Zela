#!/bin/bash

# Script để chạy database migrations trong Docker container

echo "🔄 Đang kiểm tra containers..."

# Kiểm tra container zela-app có đang chạy không
if ! docker ps | grep -q zela-app; then
    echo "❌ Container zela-app chưa chạy. Hãy chạy: docker compose up -d"
    exit 1
fi

# Kiểm tra container sqlserver có đang chạy không
if ! docker ps | grep -q zela-sqlserver; then
    echo "⚠️  Container sqlserver chưa chạy. Đợi SQL Server khởi động..."
    echo "⏳ Đợi 30 giây để SQL Server sẵn sàng..."
    sleep 30
fi

echo "✅ Containers đang chạy"
echo "🔄 Đang cài đặt EF Core tools (nếu chưa có)..."

# Cài đặt EF Core tools trong container
docker exec -it zela-app dotnet tool install --global dotnet-ef --version 8.0.0 || echo "EF tools đã được cài đặt hoặc có lỗi"

echo "🔄 Đang chạy migrations..."

# Chạy migrations
docker exec -it zela-app dotnet ef database update --project /app/Zela.csproj

if [ $? -eq 0 ]; then
    echo "✅ Migrations đã chạy thành công!"
else
    echo "❌ Có lỗi khi chạy migrations"
    echo "💡 Thử cách khác:"
    echo "   docker exec -it zela-app bash"
    echo "   dotnet tool install --global dotnet-ef"
    echo "   export PATH=\"\$PATH:/root/.dotnet/tools\""
    echo "   dotnet ef database update"
    exit 1
fi

