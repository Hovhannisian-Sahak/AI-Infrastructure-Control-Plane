FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/BeeCloud.Api/BeeCloud.Api.csproj", "src/BeeCloud.Api/"]
COPY ["src/BeeCloud.Application/BeeCloud.Application.csproj", "src/BeeCloud.Application/"]
COPY ["src/BeeCloud.Domain/BeeCloud.Domain.csproj", "src/BeeCloud.Domain/"]
COPY ["src/BeeCloud.Infrastructure/BeeCloud.Infrastructure.csproj", "src/BeeCloud.Infrastructure/"]

RUN dotnet restore "src/BeeCloud.Api/BeeCloud.Api.csproj"

COPY . .

WORKDIR "/src/src/BeeCloud.Api"
RUN dotnet publish "BeeCloud.Api.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080

EXPOSE 8080

ENTRYPOINT ["dotnet", "BeeCloud.Api.dll"]