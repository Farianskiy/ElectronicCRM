# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS restore

WORKDIR /src

COPY Directory.Build.props ./
COPY Directory.Packages.props ./
COPY .editorconfig ./
COPY .globalconfig ./

COPY ElectronicService/src/ElectronicService.Contracts/ElectronicService.Contracts.csproj ElectronicService/src/ElectronicService.Contracts/
COPY ElectronicService/src/ElectronicService.Core/ElectronicService.Core.csproj ElectronicService/src/ElectronicService.Core/
COPY ElectronicService/src/ElectronicService.Domain/ElectronicService.Domain.csproj ElectronicService/src/ElectronicService.Domain/
COPY ElectronicService/src/ElectronicService.Infrastructure.Postgres/ElectronicService.Infrastructure.Postgres.csproj ElectronicService/src/ElectronicService.Infrastructure.Postgres/
COPY ElectronicService/src/ElectronicService.Web/ElectronicService.Web.csproj ElectronicService/src/ElectronicService.Web/

RUN dotnet restore ElectronicService/src/ElectronicService.Web/ElectronicService.Web.csproj

FROM restore AS publish

COPY ElectronicService/ ElectronicService/

RUN dotnet publish ElectronicService/src/ElectronicService.Web/ElectronicService.Web.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080

EXPOSE 8080

RUN apt-get update \
    && apt-get install -y --no-install-recommends \
    curl \
    libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/* \
    && mkdir -p /home/app/.aspnet/DataProtection-Keys \
    && chown -R app:app /home/app/.aspnet

COPY --from=publish --chown=app:app /app/publish .

USER app

ENTRYPOINT ["dotnet", "ElectronicService.Web.dll"]