# 1) BUILD STAGE
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# csproj’u kopyala ve paketleri restore et
COPY FutbolSitesi.csproj .
RUN dotnet restore

# tüm kaynak kodu kopyala
COPY . .

# projeyi publish et
RUN dotnet publish -c Release -o /app/publish


# 2) RUNTIME STAGE
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# publish edilen dosyaları taşı
COPY --from=build /app/publish .

# futbol.db dosyasının runtime içinde olduğundan emin ol
COPY futbol.db /app/futbol.db

# Render PORT’unu kullan
ENV PORT=5000

# uygulamayı 5000’e bind et
EXPOSE 5000

ENTRYPOINT ["dotnet", "FutbolSitesi.dll"]
