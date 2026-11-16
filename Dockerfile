FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["Data_Access_Layer/Data_Access_Layer.csproj", "Data_Access_Layer/"]
COPY ["BusinessLayer/Business_Layer.csproj", "BusinessLayer/"]
COPY ["QuranCenterManager_API/QuranCenterManager_API.csproj", "QuranCenterManager_API/"]

RUN dotnet restore "QuranCenterManager_API/QuranCenterManager_API.csproj"

COPY . .
WORKDIR /src/QuranCenterManager_API
RUN dotnet publish "QuranCenterManager_API.csproj" \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    -p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .
USER $APP_UID

ENTRYPOINT ["dotnet", "QuranCenterManager_API.dll"]
