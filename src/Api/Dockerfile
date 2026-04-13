FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY global.json Directory.Build.props ./
COPY src/Api/Aerarium.Api.csproj src/Api/
COPY src/Application/Aerarium.Application.csproj src/Application/
COPY src/Domain/Aerarium.Domain.csproj src/Domain/
COPY src/Infrastructure/Aerarium.Infrastructure.csproj src/Infrastructure/
RUN dotnet restore src/Api/Aerarium.Api.csproj

COPY src/Api/ src/Api/
COPY src/Application/ src/Application/
COPY src/Domain/ src/Domain/
COPY src/Infrastructure/ src/Infrastructure/
RUN dotnet publish src/Api/Aerarium.Api.csproj -c Release -o /app /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app ./

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true

EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "Aerarium.Api.dll"]
