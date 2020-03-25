ARG VERSION=3.1
FROM mcr.microsoft.com/dotnet/core/sdk:${VERSION} AS build-env
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY ./MorphicServer/*.csproj ./
RUN dotnet restore

COPY ./MorphicServer/ ./
RUN dotnet publish -c Release -o MorphicServer

# Build runtime image
FROM mcr.microsoft.com/dotnet/core/aspnet:${VERSION} as runtime
WORKDIR /app
COPY --from=build-env /app/MorphicServer/ ./
COPY MorphicServer/appsettings.* ./
ENTRYPOINT ["dotnet", "MorphicServer.dll"]
