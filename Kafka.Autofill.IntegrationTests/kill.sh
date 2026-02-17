#!/bin/bash

echo "======================================"
echo "Stopping Kafka."
echo "======================================"

# Get the directory where this script is located
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
cd "$SCRIPT_DIR"

# Check if docker compose is available
if command -v docker-compose &> /dev/null; then
    DOCKER_COMPOSE="docker-compose"
elif docker compose version &> /dev/null; then
    DOCKER_COMPOSE="docker compose"
else
    echo "Error: Neither 'docker-compose' nor 'docker compose' is available."
    exit 1
fi

# Stop all services and remove volumes
echo ""
echo "Stopping containers and removing volumes..."
$DOCKER_COMPOSE down -v

echo ""
echo "======================================"
echo "Kafka Stopped Successfully!"
echo "======================================"
echo ""
echo "All containers have been stopped and removed."
echo "All volumes have been cleaned up."
echo ""
