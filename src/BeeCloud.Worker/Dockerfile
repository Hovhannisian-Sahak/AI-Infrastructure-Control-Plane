FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/BeeCloud.Worker/BeeCloud.Worker.csproj", "src/BeeCloud.Worker/"]
COPY ["src/BeeCloud.Application/BeeCloud.Application.csproj", "src/BeeCloud.Application/"]
COPY ["src/BeeCloud.Domain/BeeCloud.Domain.csproj", "src/BeeCloud.Domain/"]
COPY ["src/BeeCloud.Infrastructure/BeeCloud.Infrastructure.csproj", "src/BeeCloud.Infrastructure/"]

RUN dotnet restore "src/BeeCloud.Worker/BeeCloud.Worker.csproj"

COPY . .

WORKDIR "/src/src/BeeCloud.Worker"
RUN dotnet publish "BeeCloud.Worker.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "BeeCloud.Worker.dll"]