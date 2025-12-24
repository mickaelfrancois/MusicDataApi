FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["MusicDataApi/MusicData.Api.csproj", "MusicDataApi/"]
COPY ["MusicData.Application/MusicData.Application.csproj", "MusicData.Application/"]
COPY ["MusicData.Domain/MusicData.Domain.csproj", "MusicData.Domain/"]
COPY ["MusicData.Shared/MusicData.Shared.csproj", "MusicData.Shared/"]
COPY ["MusicData.Infrastructure/MusicData.Infrastructure.csproj", "MusicData.Infrastructure/"]
RUN dotnet restore "./MusicDataApi/MusicData.Api.csproj"
COPY . .
WORKDIR "/src/MusicDataApi"
RUN dotnet build "./MusicData.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./MusicData.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MusicData.Api.dll"]