FROM mcr.microsoft.com/dotnet/core/sdk:3.1.200 as build

# prepare sources
WORKDIR /src
COPY [".", "."]

# build & test
RUN dotnet restore "LocalizationServiceIntegration.sln"
RUN dotnet build -c Release --no-restore --
RUN dotnet test 

# publish
RUN dotnet publish "./LocalizationServiceIntegration/LocalizationServiceIntegration.csproj" --output "/build/LocalizationServiceIntegration" -c Release --no-restore


# create app cntnr
FROM mcr.microsoft.com/dotnet/core/aspnet:3.1
WORKDIR /app
COPY --from=build /build/LocalizationServiceIntegration .
ENTRYPOINT ["dotnet", "app/LocalizationServiceIntegration.dll"]