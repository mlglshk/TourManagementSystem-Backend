FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Копируем csproj
COPY TourManagementSystem/*.csproj TourManagementSystem/
RUN dotnet restore TourManagementSystem/TourManagementSystem.csproj

# Копируем ВСЁ
COPY . .

# Публикуем
WORKDIR /src/TourManagementSystem
RUN dotnet publish -c Release -o /app/publish

# Финальный образ
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Копируем результат сборки
COPY --from=build /app/publish .

# Копируем appsettings.json (ВАЖНО!)
COPY TourManagementSystem/appsettings.json .
COPY TourManagementSystem/appsettings.Development.json .

# Открываем порт
EXPOSE 8080

# Запускаем
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
ENTRYPOINT ["dotnet", "TourManagementSystem.dll"]
