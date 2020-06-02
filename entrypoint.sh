#!/bin/sh -l

cd /app

dotnet restore
dotnet build
dotnet run --project LocalizationServiceIntegration $COMMAND