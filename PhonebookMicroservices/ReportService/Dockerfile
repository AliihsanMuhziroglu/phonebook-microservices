FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Proje dosyalarını kopyala
COPY . .

# Restore ve publish
RUN dotnet restore
RUN dotnet publish -c Release -o /app

# Runtime imajı
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .

# 8082 portunu aç
EXPOSE 8082

ENTRYPOINT ["dotnet", "ReportService.dll"]
