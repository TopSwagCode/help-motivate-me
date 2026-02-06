#!/bin/bash
# Generate EF Core migration using Docker (for when local SDK version doesn't match)

MIGRATION_NAME=${1:-InitialCreate}

docker run --rm \
  -v "$(pwd)/src:/src/src" \
  -w /src \
  mcr.microsoft.com/dotnet/sdk:10.0 \
  bash -c "
    dotnet tool install --global dotnet-ef &&
    export PATH=\"\$PATH:/root/.dotnet/tools\" &&
    dotnet restore src/HelpMotivateMe.Api/HelpMotivateMe.Api.csproj &&
    dotnet ef migrations add $MIGRATION_NAME -p src/HelpMotivateMe.Infrastructure -s src/HelpMotivateMe.Api
  "

echo "Migration '$MIGRATION_NAME' generated successfully!"
