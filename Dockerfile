FROM node:22-alpine AS frontend-build
WORKDIR /src/frontend
COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci
COPY frontend/ ./
ENV VITE_API_URL=""
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS backend-build
WORKDIR /src
COPY backend/src/HelpMotivateMe.Core/HelpMotivateMe.Core.csproj backend/src/HelpMotivateMe.Core/
COPY backend/src/HelpMotivateMe.Infrastructure/HelpMotivateMe.Infrastructure.csproj backend/src/HelpMotivateMe.Infrastructure/
COPY backend/src/HelpMotivateMe.Api/HelpMotivateMe.Api.csproj backend/src/HelpMotivateMe.Api/
RUN dotnet restore backend/src/HelpMotivateMe.Api/HelpMotivateMe.Api.csproj
COPY backend/src/ backend/src/
RUN dotnet publish backend/src/HelpMotivateMe.Api/HelpMotivateMe.Api.csproj -c Release -o /app/publish --no-restore
COPY --from=frontend-build /src/frontend/build /app/publish/wwwroot

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/* \
    && mkdir -p /data/uploads \
    && chown -R app:app /data
WORKDIR /app
COPY --from=backend-build --chown=app:app /app/publish ./
USER app
ENV ASPNETCORE_URLS=http://+:8080 \
    ConnectionStrings__DefaultConnection="Data Source=/data/helpmotivateme.db;Foreign Keys=True;Default Timeout=30" \
    LocalStorage__BasePath=/data/uploads \
    LocalStorage__BaseUrl=/api/files
EXPOSE 8080
VOLUME ["/data"]
ENTRYPOINT ["dotnet", "HelpMotivateMe.Api.dll"]
