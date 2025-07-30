#!/bin/bash

echo "Starting Arif Platform Infrastructure Services..."

docker compose -f docker-compose.infrastructure.yml up -d

echo "Waiting for services to be ready..."
sleep 30

echo "Infrastructure services started successfully!"
echo "SQL Server: localhost:1433"
echo "Redis: localhost:6379"
echo "RabbitMQ: localhost:5672 (Management: localhost:15672)"
echo "Qdrant: localhost:6333"
