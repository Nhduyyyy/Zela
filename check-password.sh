#!/bin/bash

echo "🔍 Kiểm tra password trong container SQL Server..."
echo ""

# Xem password environment variable
echo "SA_PASSWORD trong container:"
docker exec zela-sqlserver printenv SA_PASSWORD

echo ""
echo "💡 Nếu password khác với 'Str0ng_Pa$$w0rd!', cần sửa docker-compose.yml"

