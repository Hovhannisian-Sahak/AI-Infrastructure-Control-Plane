FROM mcr.microsoft.com/dotnet/sdk:8.0
WORKDIR /src

COPY BeeCloud.sln ./
COPY src/BeeCloud.Api/ src/BeeCloud.Api/
COPY src/BeeCloud.Application/ src/BeeCloud.Application/
COPY src/BeeCloud.Domain/ src/BeeCloud.Domain/
COPY src/BeeCloud.Infrastructure/ src/BeeCloud.Infrastructure/

RUN dotnet tool install --global dotnet-ef --version 8.0.19
ENV PATH="/root/.dotnet/tools:${PATH}"
RUN dotnet restore src/BeeCloud.Api/BeeCloud.Api.csproj
RUN dotnet build src/BeeCloud.Api/BeeCloud.Api.csproj --configuration Release --no-restore
ENTRYPOINT ["dotnet", "ef"]
