# Imagen del Worker BEP (trabajos en segundo plano con Hangfire, ADR-005).
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Bep.slnx Directory.Build.props Directory.Packages.props nuget.config* ./
COPY src/ src/
COPY tests/ tests/
RUN dotnet restore Bep.slnx

RUN dotnet publish src/Bep.Worker/Bep.Worker.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
USER $APP_UID
COPY --from=build /app .
ENTRYPOINT ["dotnet", "Bep.Worker.dll"]
