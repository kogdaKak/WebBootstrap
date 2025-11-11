FROM mcr.microsoft.com/dotnet/sdk:8.0-slim AS build
WORKDIR /src

COPY *.csproj ./
RUN dotnet restore

RUN dotnet publish -c Release -o /app/out

FROM mcr.microsoft.com/dotnet/aspnet:8.0-slim AS runtime
WORKDIR /app

RUN apt-get update && apt-get install -y --no-install-recommends \
    brotli \
 && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/out ./

ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_EnableDiagnostics=0

EXPOSE 8080

ENTRYPOINT ["dotnet", "WebMekashron.dll"]
