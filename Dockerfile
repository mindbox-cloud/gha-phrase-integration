FROM mcr.microsoft.com/dotnet/sdk:6.0 as build

WORKDIR /app
COPY ["nuget.config", "./"]
COPY ["LocalizationServiceIntegration/LocalizationServiceIntegration.csproj", "./"]
RUN dotnet restore

COPY . ./

RUN dotnet build -c Release

RUN dotnet test

RUN dotnet publish ./LocalizationServiceIntegration/LocalizationServiceIntegration.csproj -c Release --no-build -o ./out


FROM mcr.microsoft.com/dotnet/runtime:6.0
WORKDIR /app
COPY --from=build /app/out .
ENTRYPOINT ["dotnet", "/app/LocalizationServiceIntegration.dll"]