# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files
COPY src/HelpMotivateMe.Api/HelpMotivateMe.Api.csproj src/HelpMotivateMe.Api/
COPY src/HelpMotivateMe.Core/HelpMotivateMe.Core.csproj src/HelpMotivateMe.Core/
COPY src/HelpMotivateMe.Infrastructure/HelpMotivateMe.Infrastructure.csproj src/HelpMotivateMe.Infrastructure/

# Restore dependencies for the API project (which includes all dependencies)
RUN dotnet restore src/HelpMotivateMe.Api/HelpMotivateMe.Api.csproj

# Copy everything else and build
COPY src/ src/
WORKDIR /src/src/HelpMotivateMe.Api
RUN dotnet build -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

EXPOSE 5001
ENV ASPNETCORE_URLS=http://+:5001

ENTRYPOINT ["dotnet", "HelpMotivateMe.Api.dll"]
