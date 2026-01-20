#!/bin/bash

# Script để bật lại HTTPS sau khi đã có certificate
# Sử dụng: ./enable-https-after-cert.sh

echo "🔓 Bật lại HTTPS configuration..."

CONFIG_FILE="nginx/conf.d/zela.conf"

# Kiểm tra certificate đã tồn tại chưa
if ! docker compose exec nginx test -f /etc/letsencrypt/live/zelahahaha.site/fullchain.pem; then
    echo "❌ Certificate chưa tồn tại!"
    echo "💡 Chạy lệnh tạo certificate trước:"
    echo "   docker compose run --rm certbot certonly --webroot --webroot-path=/var/www/certbot --email nhatduyyy131104@gmail.com --agree-tos --no-eff-email -d zelahahaha.site -d www.zelahahaha.site"
    exit 1
fi

echo "✅ Certificate đã tồn tại"
echo ""

# Uncomment HTTPS server block
echo "🔄 Đang uncomment HTTPS server block..."
sed -i 's/^# server {/server {/' "$CONFIG_FILE"
sed -i 's/^#     listen 443 ssl http2;/    listen 443 ssl http2;/' "$CONFIG_FILE"
sed -i 's/^#     server_name/    server_name/' "$CONFIG_FILE"
sed -i 's/^#     # SSL Configuration/    # SSL Configuration/' "$CONFIG_FILE"
sed -i 's/^#     ssl_certificate/    ssl_certificate/' "$CONFIG_FILE"
sed -i 's/^#     ssl_certificate_key/    ssl_certificate_key/' "$CONFIG_FILE"
sed -i 's/^#     # SSL Security Settings/    # SSL Security Settings/' "$CONFIG_FILE"
sed -i 's/^#     ssl_protocols/    ssl_protocols/' "$CONFIG_FILE"
sed -i 's/^#     ssl_ciphers/    ssl_ciphers/' "$CONFIG_FILE"
sed -i 's/^#     ssl_prefer_server_ciphers/    ssl_prefer_server_ciphers/' "$CONFIG_FILE"
sed -i 's/^#     ssl_session_cache/    ssl_session_cache/' "$CONFIG_FILE"
sed -i 's/^#     ssl_session_timeout/    ssl_session_timeout/' "$CONFIG_FILE"
sed -i 's/^#     ssl_session_tickets/    ssl_session_tickets/' "$CONFIG_FILE"
sed -i 's/^#     # Security Headers/    # Security Headers/' "$CONFIG_FILE"
sed -i 's/^#     add_header/    add_header/' "$CONFIG_FILE"
sed -i 's/^#     # Logging/    # Logging/' "$CONFIG_FILE"
sed -i 's/^#     access_log/    access_log/' "$CONFIG_FILE"
sed -i 's/^#     error_log/    error_log/' "$CONFIG_FILE"
sed -i 's/^#     # Client body size limit/    # Client body size limit/' "$CONFIG_FILE"
sed -i 's/^#     client_max_body_size/    client_max_body_size/' "$CONFIG_FILE"
sed -i 's/^#     client_body_buffer_size/    client_body_buffer_size/' "$CONFIG_FILE"
sed -i 's/^#     # Timeouts/    # Timeouts/' "$CONFIG_FILE"
sed -i 's/^#     proxy_connect_timeout/    proxy_connect_timeout/' "$CONFIG_FILE"
sed -i 's/^#     proxy_send_timeout/    proxy_send_timeout/' "$CONFIG_FILE"
sed -i 's/^#     proxy_read_timeout/    proxy_read_timeout/' "$CONFIG_FILE"
sed -i 's/^#     send_timeout/    send_timeout/' "$CONFIG_FILE"
sed -i 's/^#     # Headers/    # Headers/' "$CONFIG_FILE"
sed -i 's/^#     proxy_set_header/    proxy_set_header/' "$CONFIG_FILE"
sed -i 's/^#     # WebSocket support/    # WebSocket support/' "$CONFIG_FILE"
sed -i 's/^#     proxy_http_version/    proxy_http_version/' "$CONFIG_FILE"
sed -i 's/^#     proxy_set_header Upgrade/    proxy_set_header Upgrade/' "$CONFIG_FILE"
sed -i 's/^#     proxy_set_header Connection/    proxy_set_header Connection/' "$CONFIG_FILE"
sed -i 's/^#     # Buffering/    # Buffering/' "$CONFIG_FILE"
sed -i 's/^#     proxy_buffering/    proxy_buffering/' "$CONFIG_FILE"
sed -i 's/^#     proxy_request_buffering/    proxy_request_buffering/' "$CONFIG_FILE"
sed -i 's/^#     # Static files/    # Static files/' "$CONFIG_FILE"
sed -i 's/^#     location ~\*/    location ~*/' "$CONFIG_FILE"
sed -i 's/^#         proxy_pass/        proxy_pass/' "$CONFIG_FILE"
sed -i 's/^#         expires/        expires/' "$CONFIG_FILE"
sed -i 's/^#         add_header Cache-Control/        add_header Cache-Control/' "$CONFIG_FILE"
sed -i 's/^#     # SignalR Hubs/    # SignalR Hubs/' "$CONFIG_FILE"
sed -i 's/^#     location ~ ^\//    location ~ ^\//' "$CONFIG_FILE"
sed -i 's/^#         proxy_pass/        proxy_pass/' "$CONFIG_FILE"
sed -i 's/^#         proxy_http_version/        proxy_http_version/' "$CONFIG_FILE"
sed -i 's/^#         proxy_set_header Upgrade/        proxy_set_header Upgrade/' "$CONFIG_FILE"
sed -i 's/^#         proxy_set_header Connection/        proxy_set_header Connection/' "$CONFIG_FILE"
sed -i 's/^#         proxy_set_header Host/        proxy_set_header Host/' "$CONFIG_FILE"
sed -i 's/^#         proxy_set_header X-Real-IP/        proxy_set_header X-Real-IP/' "$CONFIG_FILE"
sed -i 's/^#         proxy_set_header X-Forwarded-For/        proxy_set_header X-Forwarded-For/' "$CONFIG_FILE"
sed -i 's/^#         proxy_set_header X-Forwarded-Proto/        proxy_set_header X-Forwarded-Proto/' "$CONFIG_FILE"
sed -i 's/^#         proxy_read_timeout/        proxy_read_timeout/' "$CONFIG_FILE"
sed -i 's/^#         proxy_send_timeout/        proxy_send_timeout/' "$CONFIG_FILE"
sed -i 's/^#     # Main application/    # Main application/' "$CONFIG_FILE"
sed -i 's/^#     location \/ {/    location \/ {/' "$CONFIG_FILE"
sed -i 's/^#         proxy_pass/        proxy_pass/' "$CONFIG_FILE"
sed -i 's/^#         proxy_set_header Host/        proxy_set_header Host/' "$CONFIG_FILE"
sed -i 's/^#         proxy_set_header X-Real-IP/        proxy_set_header X-Real-IP/' "$CONFIG_FILE"
sed -i 's/^#         proxy_set_header X-Forwarded-For/        proxy_set_header X-Forwarded-For/' "$CONFIG_FILE"
sed -i 's/^#         proxy_set_header X-Forwarded-Proto/        proxy_set_header X-Forwarded-Proto/' "$CONFIG_FILE"
sed -i 's/^#         proxy_set_header X-Forwarded-Host/        proxy_set_header X-Forwarded-Host/' "$CONFIG_FILE"
sed -i 's/^#         proxy_set_header X-Forwarded-Port/        proxy_set_header X-Forwarded-Port/' "$CONFIG_FILE"
sed -i 's/^#     # Health check endpoint/    # Health check endpoint/' "$CONFIG_FILE"
sed -i 's/^#     location \/health {/    location \/health {/' "$CONFIG_FILE"
sed -i 's/^#         proxy_pass/        proxy_pass/' "$CONFIG_FILE"
sed -i 's/^#         access_log off/        access_log off/' "$CONFIG_FILE"
sed -i 's/^# }/}/' "$CONFIG_FILE"

# Sửa HTTP redirect
sed -i 's/^    # Tạm thời không redirect/    # Redirect tất cả HTTP requests sang HTTPS/' "$CONFIG_FILE"
sed -i 's/^    # return 301/    return 301/' "$CONFIG_FILE"
sed -i 's/^    # Tạm thời proxy trực tiếp/    # Tạm thời proxy trực tiếp (comment lại sau khi có HTTPS)/' "$CONFIG_FILE"
sed -i '/^    location \/ {$/,/^    }$/d' "$CONFIG_FILE"
sed -i '/# Tạm thời proxy trực tiếp/a\    location / {\n        return 301 https://$server_name$request_uri;\n    }' "$CONFIG_FILE"

echo "✅ Đã uncomment HTTPS server block"
echo ""

# Test cấu hình
echo "🔍 Đang test cấu hình Nginx..."
docker compose exec nginx nginx -t

if [ $? -eq 0 ]; then
    echo ""
    echo "✅ Cấu hình hợp lệ!"
    echo ""
    echo "🔄 Đang reload Nginx..."
    docker compose exec nginx nginx -s reload
    echo ""
    echo "✅ Hoàn tất! HTTPS đã được bật."
    echo "🌐 Test: https://zelahahaha.site"
else
    echo ""
    echo "❌ Cấu hình có lỗi, vui lòng kiểm tra lại"
    exit 1
fi

