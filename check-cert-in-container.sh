#!/bin/bash

echo "🔍 Kiểm tra certificate trong container..."
echo ""

# Kiểm tra certificate trên host
echo "📁 Certificate trên host:"
sudo ls -la /etc/letsencrypt/live/zelahahaha.site/ 2>&1

echo ""
echo "📁 Certificate trong Nginx container:"
docker compose exec nginx ls -la /etc/letsencrypt/live/zelahahaha.site/ 2>&1

echo ""
echo "🔍 Kiểm tra file cụ thể:"
docker compose exec nginx test -f /etc/letsencrypt/live/zelahahaha.site/fullchain.pem && echo "✅ fullchain.pem tồn tại" || echo "❌ fullchain.pem không tồn tại"
docker compose exec nginx test -f /etc/letsencrypt/live/zelahahaha.site/privkey.pem && echo "✅ privkey.pem tồn tại" || echo "❌ privkey.pem không tồn tại"

echo ""
echo "🔄 Nếu không thấy, thử restart Nginx:"
echo "   docker compose restart nginx"

