#!/bin/bash
# Generate OpenAPI spec from running backend server
# Usage: ./generate-openapi.sh [base-url]
# Default base-url: http://localhost:5001

BASE_URL="${1:-http://localhost:5001}"
OUTPUT_DIR="$(dirname "$0")/../openapi"
OUTPUT_FILE="$OUTPUT_DIR/v1.json"

mkdir -p "$OUTPUT_DIR"

echo "Fetching OpenAPI spec from $BASE_URL/openapi/v1.json..."
HTTP_CODE=$(curl -s -w "%{http_code}" -o "$OUTPUT_FILE" "$BASE_URL/openapi/v1.json")

if [ "$HTTP_CODE" != "200" ]; then
    echo "Error: Failed to fetch OpenAPI spec (HTTP $HTTP_CODE)"
    echo "Make sure the backend is running: cd backend && dotnet run --project src/HelpMotivateMe.Api"
    rm -f "$OUTPUT_FILE"
    exit 1
fi

echo "OpenAPI spec saved to $OUTPUT_FILE"
