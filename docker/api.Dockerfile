# Imagen de la API BEP. Multi-stage para imagen final mínima (RNF-PORT-001).
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore con caché: primero los manifiestos de proyecto y de paquetes.
COPY Bep.slnx Directory.Build.props Directory.Packages.props nuget.config* ./
COPY src/ src/
COPY tests/ tests/
RUN dotnet restore Bep.slnx

RUN dotnet publish src/Bep.Api/Bep.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
# Ejecutar como usuario no root.
USER $APP_UID
COPY --from=build /app .
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080
ENTRYPOINT ["dotnet", "Bep.Api.dll"]
