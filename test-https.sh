#!/bin/bash

echo "🔍 Kiểm tra HTTPS configuration..."
echo ""

# Test cấu hình Nginx
echo "📝 Test cấu hình Nginx:"
docker compose exec nginx nginx -t

if [ $? -eq 0 ]; then
    echo ""
    echo "✅ Cấu hình hợp lệ!"
    echo ""
    echo "🔄 Reload Nginx..."
    docker compose exec nginx nginx -s reload
    
    echo ""
    echo "🌐 Test HTTPS:"
    curl -I https://zelahahaha.site 2>&1 | head -10
    
    echo ""
    echo "🔄 Test HTTP redirect:"
    curl -I http://zelahahaha.site 2>&1 | head -10
    
    echo ""
    echo "✅ Hoàn tất!"
else
    echo ""
    echo "❌ Cấu hình có lỗi, vui lòng kiểm tra lại"
    exit 1
fi

