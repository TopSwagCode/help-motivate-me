#!/bin/bash
set -e

# Parse connection string from environment variable
# Expected format: Host=xxx;Database=xxx;Username=xxx;Password=xxx
CONNECTION_STRING="${ConnectionStrings__DefaultConnection}"

if [ -z "$CONNECTION_STRING" ]; then
    echo "Error: ConnectionStrings__DefaultConnection environment variable is not set"
    exit 1
fi

# Extract components from connection string
extract_value() {
    echo "$CONNECTION_STRING" | grep -oP "(?i)$1=\K[^;]+" | head -1
}

DB_HOST=$(extract_value "Host")
DB_NAME=$(extract_value "Database")
DB_USER=$(extract_value "Username")
DB_PASSWORD=$(extract_value "Password")
DB_PORT=$(extract_value "Port")

# Default port if not specified
DB_PORT=${DB_PORT:-5432}

echo "Checking if database '$DB_NAME' exists on host '$DB_HOST'..."

# Set password for psql
export PGPASSWORD="$DB_PASSWORD"

# Check if database exists, create if not
if ! psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -lqt | cut -d \| -f 1 | grep -qw "$DB_NAME"; then
    echo "Database '$DB_NAME' does not exist. Creating..."
    psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d postgres -c "CREATE DATABASE \"$DB_NAME\";"
    echo "Database '$DB_NAME' created successfully."
else
    echo "Database '$DB_NAME' already exists."
fi

# Run EF Core migrations
#echo "Running EF Core migrations..."
#/root/.dotnet/tools/dotnet-ef database update \
#    --project ../HelpMotivateMe.Infrastructure \
#    --no-build \
#    --configuration Release

#echo "Migrations completed successfully."
