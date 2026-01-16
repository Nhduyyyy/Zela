#!/bin/bash

# Script để chạy migrations từ máy local đến SQL Server trên VPS
# Sử dụng: ./run-migrations-remote.sh <vps-ip>

if [ -z "$1" ]; then
    echo "❌ Thiếu tham số VPS IP"
    echo "Sử dụng: ./run-migrations-remote.sh <vps-ip>"
    echo "Ví dụ: ./run-migrations-remote.sh 123.456.789.0"
    exit 1
fi

VPS_IP=$1
echo "🔄 Đang chạy migrations đến SQL Server tại $VPS_IP:1433..."

# Tạm thời cập nhật connection string
CONNECTION_STRING="Server=$VPS_IP,1433;Database=Zela_FinalV2.0;User ID=SA;Password=Str0ng_Pa\$\$w0rd!;MultipleActiveResultSets=true;Encrypt=True;TrustServerCertificate=True;"

echo "📝 Connection string: Server=$VPS_IP,1433..."

# Chạy migrations với connection string từ command line
dotnet ef database update --connection "$CONNECTION_STRING" --project Zela.csproj

if [ $? -eq 0 ]; then
    echo "✅ Migrations đã chạy thành công!"
else
    echo "❌ Có lỗi khi chạy migrations"
    echo "💡 Đảm bảo:"
    echo "   1. SQL Server trên VPS đã chạy và mở port 1433"
    echo "   2. Firewall cho phép kết nối từ máy local"
    echo "   3. Password trong script khớp với docker-compose.yml"
    exit 1
fi

