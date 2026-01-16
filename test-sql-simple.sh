#!/bin/bash

# Test kết nối SQL Server bằng cách tạo container tạm thời có sqlcmd

echo "🔍 Đang test kết nối SQL Server..."

# Sử dụng image có sẵn sqlcmd
docker run --rm \
  --network zela_zela-network \
  mcr.microsoft.com/mssql-tools:latest \
  /opt/mssql-tools/bin/sqlcmd \
  -S sqlserver \
  -U SA \
  -P 'Str0ng_Pa$w0rd!' \
  -Q 'SELECT @@VERSION'

if [ $? -eq 0 ]; then
    echo "✅ Kết nối thành công!"
else
    echo "❌ Kết nối thất bại - kiểm tra password"
fi

