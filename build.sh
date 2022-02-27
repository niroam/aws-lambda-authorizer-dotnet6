#!/bin/bash
# Build Dotnet Core Lambdas
rm -r ./code/TenantAuthorizer/bin/
dotnet publish --configuration "Release" --framework "net6.0" --runtime linux-x64 ./code/TenantAuthorizer/TenantAuthorizer.csproj
dotnet publish --configuration "Release" --framework "net6.0" --runtime linux-x64 ./code/TenantAuthorizer.Tests/TenantAuthorizer.Tests.csproj
