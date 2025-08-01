# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
COPY /keystore/aa_root.crt /usr/local/share/ca-certificates/
COPY /keystore/aa_intermediate.crt /usr/local/share/ca-certificates/
RUN chmod 644 /usr/local/share/ca-certificates/aa_root.crt
RUN chmod 644 /usr/local/share/ca-certificates/aa_intermediate.crt
RUN update-ca-certificates

USER $APP_UID
WORKDIR /app


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["IBMXMSConsumer.csproj", "."]
RUN dotnet restore "./IBMXMSConsumer.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "./IBMXMSConsumer.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./IBMXMSConsumer.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "IBMXMSConsumer.dll"]