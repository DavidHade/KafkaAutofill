#!/bin/bash

echo "======================================"
echo "Starting Kafka."
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
    echo "Please install Docker Compose first."
    exit 1
fi

# Stop any existing containers
echo ""
echo "Stopping any existing containers..."
$DOCKER_COMPOSE down -v

# Start the services
echo ""
echo "Starting Kafka, Zookeeper, and Schema Registry..."
$DOCKER_COMPOSE up -d zookeeper kafka schema-registry

# Wait for Kafka to be healthy
echo ""
echo "Waiting for Kafka to be ready..."
until docker exec kafka-autofill-broker kafka-broker-api-versions --bootstrap-server localhost:9092 &> /dev/null; do
    echo -n "."
    sleep 2
done

echo ""
echo "Kafka is ready!"

# Create the topic
echo ""
echo "Creating 'People' topic..."
$DOCKER_COMPOSE up kafka-init

# Show status
echo ""
echo "======================================"
echo "Kafka Setup Complete!"
echo "======================================"
echo ""
echo "Kafka is running on: localhost:9092"
echo "Zookeeper is running on: localhost:2181"
echo "Schema Registry is running on: http://localhost:8081"
echo ""
echo "Topic 'People' has been created with:"
echo "  - 3 partitions"
echo "  - Replication factor: 1"
echo ""
echo "To view logs: $DOCKER_COMPOSE logs -f"
echo "To stop: ./kill.sh"
echo "======================================"
