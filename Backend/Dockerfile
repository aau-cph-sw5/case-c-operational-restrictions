# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj and restore as distinct layers for caching
COPY Backend.csproj ./
RUN dotnet restore

# Copy all sources and build release
COPY . ./
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=5000
EXPOSE 5000

ENTRYPOINT ["dotnet", "Backend.dll"]
