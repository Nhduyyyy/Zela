#!/bin/bash

echo "🔍 Đang test kết nối SQL Server..."

# Test với các cách escape password khác nhau
PASSWORDS=(
    "Str0ng_Pa\$\$w0rd!"
    "Str0ng_Pa\$\$w0rd!"
    'Str0ng_Pa$$w0rd!'
    "Str0ng_Pa\$\$w0rd!"
)

for PWD in "${PASSWORDS[@]}"; do
    echo "Testing password: $PWD"
    docker exec -it zela-sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U SA -P "$PWD" -Q 'SELECT @@VERSION' 2>&1 | head -3
    if [ $? -eq 0 ]; then
        echo "✅ Password đúng: $PWD"
        break
    fi
done

