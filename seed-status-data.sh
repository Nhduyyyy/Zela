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

# Lấy password từ docker-compose.yml hoặc dùng mặc định
# Lưu ý: Password có thể chứa ký tự đặc biệt như $$, cần xử lý cẩn thận
if [ -f "docker-compose.yml" ]; then
    # Lấy password từ docker-compose.yml, dùng read để tránh expansion
    SA_PASSWORD_LINE=$(grep "SA_PASSWORD=" docker-compose.yml | head -1)
    if [ -n "$SA_PASSWORD_LINE" ]; then
        # Tách password ra, loại bỏ khoảng trắng và quotes
        DOCKER_PASSWORD=$(echo "$SA_PASSWORD_LINE" | sed 's/.*SA_PASSWORD=//' | sed "s/^[[:space:]]*//" | sed "s/[[:space:]]*$//" | sed "s/^['\"]//" | sed "s/['\"]$//")
        if [ -n "$DOCKER_PASSWORD" ]; then
            echo "📋 Đã lấy password từ docker-compose.yml"
            # Gán password, dùng printf để escape đúng cách
            PASSWORD=$(printf '%s' "$DOCKER_PASSWORD")
        else
            PASSWORD='Str0ng_Pa$$w0rd!'
        fi
    else
        PASSWORD='Str0ng_Pa$$w0rd!'
    fi
else
    # Fallback: dùng password mặc định
    PASSWORD='Str0ng_Pa$$w0rd!'
fi

# Debug: hiển thị password (ẩn một phần để debug)
echo "🔑 Sử dụng password: ${PASSWORD:0:5}***"

# Test connection trước
echo "🔐 Đang kiểm tra kết nối SQL Server..."
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

if [ $? -ne 0 ]; then
    echo "❌ Không thể kết nối với SQL Server!"
    echo "💡 Hãy kiểm tra lại:"
    echo "   1. Container sqlserver đã chạy chưa?"
    echo "   2. Password trong docker-compose.yml có đúng không?"
    echo "   3. Network '$NETWORK' có tồn tại không?"
    exit 1
fi

echo "✅ Kết nối thành công!"

# Chạy SQL script
echo ""
echo "🔄 Đang chèn dữ liệu vào bảng Statuses..."
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

if [ $? -eq 0 ]; then
    echo ""
    echo "✅ Đã chèn dữ liệu thành công!"
    echo ""
    echo "🔍 Kiểm tra dữ liệu đã chèn:"
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
else
    echo ""
    echo "❌ Có lỗi xảy ra khi chèn dữ liệu!"
    exit 1
fi

