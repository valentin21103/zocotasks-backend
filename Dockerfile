# Etapa de compilacion
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Los .csproj se copian primero y solos para que Docker pueda cachear el
# restore: si solo cambia el codigo, no vuelve a bajar los paquetes.
COPY ZocoTasks.API/ZocoTasks.API.csproj ZocoTasks.API/
COPY ZocoTasks.Business/ZocoTasks.Business.csproj ZocoTasks.Business/
COPY ZocoTasks.Domain/ZocoTasks.Domain.csproj ZocoTasks.Domain/
COPY ZocoTasks.Infrastructure/ZocoTasks.Infrastructure.csproj ZocoTasks.Infrastructure/

RUN dotnet restore ZocoTasks.API/ZocoTasks.API.csproj

COPY . .
RUN dotnet publish ZocoTasks.API/ZocoTasks.API.csproj -c Release -o /app --no-restore

# Imagen final: solo el runtime, sin el SDK
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .

# La imagen de aspnet ya trae este usuario sin privilegios.
USER $APP_UID

ENTRYPOINT ["dotnet", "ZocoTasks.API.dll"]
