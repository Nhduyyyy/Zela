#!/bin/bash

echo "🔍 Kiểm tra nội dung file migration.sql..."
echo ""

if [ -f migration.sql ]; then
    echo "📄 Nội dung file migration.sql:"
    echo "=================================="
    cat migration.sql
    echo "=================================="
    echo ""
    echo "📊 Số dòng: $(wc -l < migration.sql)"
    echo "📊 Số ký tự: $(wc -c < migration.sql)"
else
    echo "❌ File migration.sql không tồn tại"
fi

echo ""
echo "💡 Nếu file quá ngắn, có thể migration script không được tạo đúng cách"
echo "   Thử tạo lại với lệnh:"
echo "   docker run --rm --network zela_zela-network -v \"\$(pwd):/app\" -w /app -e 'ConnectionStrings__DefaultConnection=Server=sqlserver,1433;Database=Zela_FinalV2.0;User ID=SA;Password=Str0ng_Pa\$w0rd!;MultipleActiveResultSets=true;Encrypt=False;TrustServerCertificate=True;' mcr.microsoft.com/dotnet/sdk:8.0 bash -c 'export PATH=\"\$PATH:/root/.dotnet/tools\" && dotnet ef migrations script --project Zela.csproj --idempotent'"

