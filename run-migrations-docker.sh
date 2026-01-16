#!/bin/bash

# Script chạy migrations trên VPS bằng Docker container có SDK

echo "🔄 Đang chạy migrations bằng Docker..."

# Tạo container tạm thời có SDK để chạy migrations
# Sử dụng single quotes để tránh shell expansion của ký tự $
docker run --rm \
  --network zela_zela-network \
  -v "$(pwd):/app" \
  -w /app \
  -e 'ConnectionStrings__DefaultConnection=Server=sqlserver,1433;Database=Zela_FinalV2.0;User ID=SA;Password=Str0ng_Pa$$w0rd!;MultipleActiveResultSets=true;Encrypt=False;TrustServerCertificate=True;' \
  mcr.microsoft.com/dotnet/sdk:8.0 \
  bash -c 'dotnet tool install --global dotnet-ef --version 8.0.0 && export PATH="$PATH:/root/.dotnet/tools" && dotnet ef database update --project Zela.csproj'

if [ $? -eq 0 ]; then
    echo "✅ Migrations đã chạy thành công!"
else
    echo "❌ Có lỗi khi chạy migrations"
    exit 1
fi

