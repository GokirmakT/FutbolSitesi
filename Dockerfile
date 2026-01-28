# =========================
# 1️⃣ BUILD STAGE
# =========================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY FutbolSitesi.csproj .
RUN dotnet restore

COPY . .
RUN dotnet publish -c Release -o /app/publish


# =========================
# 2️⃣ RUNTIME STAGE
# =========================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# 🔴 PRODUCTION ENV
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_ENVIRONMENT=Production

# publish edilen dosyalar
COPY --from=build /app/publish .

# 🔴 SQLITE DATA KLASÖRÜ (VOLUME BURAYA)
RUN mkdir -p /app/data

# Render PORT
ENV PORT=5000
EXPOSE 5000

ENTRYPOINT ["dotnet", "FutbolSitesi.dll"]
