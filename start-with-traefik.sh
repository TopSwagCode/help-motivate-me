#!/bin/bash
set -e

echo "🚀 Starting Help Motivate Me with Traefik..."

# Start docker services
echo "📦 Starting Docker services..."
docker-compose up -d --build

echo "⏳ Waiting for services to be healthy..."
sleep 5

# Check if services are running
if docker ps | grep -q helpmotivateme-traefik; then
    echo "✅ Traefik is running"
else
    echo "❌ Traefik failed to start"
    exit 1
fi

if docker ps | grep -q helpmotivateme-backend; then
    echo "✅ Backend is running"
else
    echo "❌ Backend failed to start"
    exit 1
fi

if docker ps | grep -q helpmotivateme-frontend; then
    echo "✅ Frontend is running"
else
    echo "❌ Frontend failed to start"
    exit 1
fi

echo ""
echo "🎉 All services are running!"
echo ""
echo "📍 Local Access:"
echo "   Frontend:  http://localhost/"
echo "   API:       http://localhost/api"
echo "   Mailpit:   http://localhost/mail"
echo "   Traefik:   http://localhost:8080 (dashboard)"
echo ""
echo "🌐 To share via ngrok:"
echo "   1. Run: ngrok http 80"
echo "   2. Copy the ngrok URL (e.g., https://abc123.ngrok.io)"
echo "   3. Share the URL - all routes work through it!"
echo ""
echo "📊 View logs:"
echo "   docker-compose logs -f"
echo ""
