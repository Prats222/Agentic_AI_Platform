FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY AgenticPlatform.sln ./
COPY src/AgenticPlatform.Core/AgenticPlatform.Core.csproj src/AgenticPlatform.Core/
COPY src/AgenticPlatform.Infrastructure/AgenticPlatform.Infrastructure.csproj src/AgenticPlatform.Infrastructure/
COPY src/AgenticPlatform.API/AgenticPlatform.API.csproj src/AgenticPlatform.API/
RUN dotnet restore src/AgenticPlatform.API/AgenticPlatform.API.csproj

COPY . .
RUN dotnet publish src/AgenticPlatform.API/AgenticPlatform.API.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends python3 python3-requests ca-certificates \
    && ln -sf /usr/bin/python3 /usr/bin/python \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "AgenticPlatform.API.dll"]
