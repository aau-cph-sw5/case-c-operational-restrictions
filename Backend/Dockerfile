# Stage 1: Dev
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS dev
WORKDIR /app

# Copy csproj and restore as distinct layers for caching
COPY Backend.csproj ./
RUN dotnet restore

# Copy all sources
COPY . ./

ENV ASPNETCORE_ENVIRONMENT=Development
ENV DOTNET_USE_POLLING_FILE_WATCHER=true
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

CMD ["dotnet", "watch", "--project", "/app/Backend.csproj", "run", "--launch-profile", "http", "--non-interactive"]

# Stage 2: Build release
FROM dev AS build
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Backend.dll"]
