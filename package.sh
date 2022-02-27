#!/bin/bash
# Package Dotnet Core Lambdas, Ignore Tests
rm -f "./code/TenantAuthorizer/bin/Release/net6.0/linux-x64/publish/TenantAuthorizer.zip"
zip "./code/TenantAuthorizer/bin/Release/net6.0/linux-x64/publish/TenantAuthorizer.zip" ./code/TenantAuthorizer/bin/Release/net6.0/linux-x64/publish/*