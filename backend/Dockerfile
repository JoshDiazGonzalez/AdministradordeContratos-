# syntax=docker/dockerfile:1
#
# API de contratos (.NET 10). Contexto de construccion: carpeta backend/.
#   docker build -t contratos-api ./backend

# ---- Compilacion ------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Primero solo los proyectos: la restauracion de paquetes queda en una capa
# propia y no se repite mientras no cambien las dependencias.
COPY Directory.Build.props ./
COPY src/Contratos.Domain/Contratos.Domain.csproj src/Contratos.Domain/
COPY src/Contratos.Application/Contratos.Application.csproj src/Contratos.Application/
COPY src/Contratos.Infrastructure/Contratos.Infrastructure.csproj src/Contratos.Infrastructure/
COPY src/Contratos.Api/Contratos.Api.csproj src/Contratos.Api/
RUN dotnet restore src/Contratos.Api/Contratos.Api.csproj

COPY src/ src/
RUN dotnet publish src/Contratos.Api/Contratos.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

# ---- Ejecucion --------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS=8080 \
    Storage__LocalPath=/app/storage

# Carpeta de documentos propiedad del usuario sin privilegios. Un volumen con
# nombre montado aqui hereda este propietario la primera vez.
RUN mkdir -p /app/storage && chown "$APP_UID" /app/storage

COPY --from=build /app/publish .

# La imagen de ASP.NET trae un usuario sin privilegios: la API no corre como root.
USER $APP_UID

EXPOSE 8080

# La imagen no incluye curl ni wget: se consulta /health con bash y /dev/tcp.
HEALTHCHECK --interval=15s --timeout=5s --start-period=30s --retries=5 \
    CMD ["bash", "-c", "exec 3<>/dev/tcp/127.0.0.1/8080 && printf 'GET /health HTTP/1.1\\r\\nHost: localhost\\r\\nConnection: close\\r\\n\\r\\n' >&3 && head -n 1 <&3 | grep -q ' 200 '"]

ENTRYPOINT ["dotnet", "Contratos.Api.dll"]
