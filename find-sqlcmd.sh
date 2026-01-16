#!/bin/bash

echo "🔍 Đang tìm sqlcmd trong container..."

# Tìm sqlcmd
docker exec zela-sqlserver find / -name sqlcmd 2>/dev/null

# Hoặc kiểm tra các đường dẫn phổ biến
echo ""
echo "Kiểm tra các đường dẫn phổ biến:"
docker exec zela-sqlserver ls -la /opt/mssql-tools*/bin/sqlcmd 2>/dev/null || echo "Không tìm thấy ở /opt/mssql-tools"
docker exec zela-sqlserver ls -la /usr/bin/sqlcmd 2>/dev/null || echo "Không tìm thấy ở /usr/bin"
docker exec zela-sqlserver which sqlcmd 2>/dev/null || echo "which không tìm thấy"

# Kiểm tra version SQL Server
echo ""
echo "Version SQL Server:"
docker exec zela-sqlserver cat /opt/mssql/lib/mssql-conf/mssql.conf 2>/dev/null || echo "Không tìm thấy config"

