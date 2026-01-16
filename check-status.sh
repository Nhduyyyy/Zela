#!/bin/bash

echo "📊 Kiểm tra trạng thái containers..."
docker compose ps

echo ""
echo "📝 Logs SQL Server (10 dòng cuối):"
docker logs zela-sqlserver --tail 10

echo ""
echo "📝 Logs ứng dụng (20 dòng cuối):"
docker logs zela-app --tail 20

echo ""
echo "🔍 Kiểm tra kết nối SQL Server..."
docker exec -it zela-sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U SA -P 'Str0ng_Pa$$w0rd!' -Q 'SELECT @@VERSION' 2>&1 | head -5

echo ""
echo "🌐 Kiểm tra ứng dụng web..."
curl -I http://localhost:80 2>&1 | head -5

