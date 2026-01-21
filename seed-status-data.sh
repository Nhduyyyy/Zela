#!/bin/bash

echo "🌱 Chèn dữ liệu cố định vào bảng Statuses..."
echo ""

# Lấy network name
NETWORK=$(docker network ls | grep zela | awk '{print $2}' | head -1)

if [ -z "$NETWORK" ]; then
    NETWORK="zela_zela-network"
fi

echo "📡 Sử dụng network: $NETWORK"
echo ""

# Kiểm tra file SQL có tồn tại không
if [ ! -f "seed-status-data.sql" ]; then
    echo "❌ Không tìm thấy file seed-status-data.sql"
    exit 1
fi

# Password: Trong docker-compose.yml có $$ nhưng khi dùng trong bash script cần chỉ 1 dấu $
# Hoặc có thể dùng docker exec trực tiếp vào container
PASSWORD='Str0ng_Pa$w0rd!'

# Kiểm tra xem có thể dùng docker exec không (nhanh hơn)
if docker ps | grep -q zela-sqlserver; then
    USE_DOCKER_EXEC=true
    echo "✅ Tìm thấy container zela-sqlserver, sẽ dùng docker exec"
else
    USE_DOCKER_EXEC=false
    echo "📡 Sẽ dùng docker run với network"
fi
echo ""

# Test connection trước
echo "🔐 Đang kiểm tra kết nối SQL Server..."
if [ "$USE_DOCKER_EXEC" = true ]; then
    docker exec zela-sqlserver /opt/mssql-tools/bin/sqlcmd \
      -S localhost \
      -U SA \
      -P "$PASSWORD" \
      -d Zela_FinalV2.0 \
      -Q "SELECT 1" \
      > /dev/null 2>&1
else
    docker run --rm \
      --network "$NETWORK" \
      mcr.microsoft.com/mssql-tools:latest \
      /opt/mssql-tools/bin/sqlcmd \
      -S sqlserver,1433 \
      -U SA \
      -P "$PASSWORD" \
      -d Zela_FinalV2.0 \
      -Q "SELECT 1" \
      > /dev/null 2>&1
fi

if [ $? -ne 0 ]; then
    echo "❌ Không thể kết nối với SQL Server!"
    echo "💡 Hãy kiểm tra lại:"
    echo "   1. Container sqlserver đã chạy chưa? (docker ps | grep sqlserver)"
    echo "   2. Password có đúng không? (kiểm tra docker-compose.yml)"
    echo "   3. Network '$NETWORK' có tồn tại không? (docker network ls)"
    echo ""
    echo "💡 Thử chạy lệnh này để test kết nối:"
    echo "   docker exec zela-sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U SA -P '$PASSWORD' -Q 'SELECT 1'"
    exit 1
fi

echo "✅ Kết nối thành công!"

# Chạy SQL script
echo ""
echo "🔄 Đang chèn dữ liệu vào bảng Statuses..."
if [ "$USE_DOCKER_EXEC" = true ]; then
    # Copy file SQL vào container và chạy
    docker cp seed-status-data.sql zela-sqlserver:/tmp/seed.sql
    docker exec zela-sqlserver /opt/mssql-tools/bin/sqlcmd \
      -S localhost \
      -U SA \
      -P "$PASSWORD" \
      -d Zela_FinalV2.0 \
      -i /tmp/seed.sql
    docker exec zela-sqlserver rm -f /tmp/seed.sql
else
    docker run --rm \
      --network "$NETWORK" \
      -v "$(pwd)/seed-status-data.sql:/tmp/seed.sql:ro" \
      mcr.microsoft.com/mssql-tools:latest \
      /opt/mssql-tools/bin/sqlcmd \
      -S sqlserver,1433 \
      -U SA \
      -P "$PASSWORD" \
      -d Zela_FinalV2.0 \
      -i /tmp/seed.sql
fi

if [ $? -eq 0 ]; then
    echo ""
    echo "✅ Đã chèn dữ liệu thành công!"
    echo ""
    echo "🔍 Kiểm tra dữ liệu đã chèn:"
    if [ "$USE_DOCKER_EXEC" = true ]; then
        docker exec zela-sqlserver /opt/mssql-tools/bin/sqlcmd \
          -S localhost \
          -U SA \
          -P "$PASSWORD" \
          -d Zela_FinalV2.0 \
          -Q "SELECT StatusId, StatuName, CreatedAt, Describe FROM Statuses ORDER BY StatusId" \
          -W -h -1
    else
        docker run --rm \
          --network "$NETWORK" \
          mcr.microsoft.com/mssql-tools:latest \
          /opt/mssql-tools/bin/sqlcmd \
          -S sqlserver,1433 \
          -U SA \
          -P "$PASSWORD" \
          -d Zela_FinalV2.0 \
          -Q "SELECT StatusId, StatuName, CreatedAt, Describe FROM Statuses ORDER BY StatusId" \
          -W -h -1
    fi
else
    echo ""
    echo "❌ Có lỗi xảy ra khi chèn dữ liệu!"
    exit 1
fi

