# Use the official .NET 8.0 runtime as a parent image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Use the SDK image to build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY ["UmiHealthPOS.csproj", "."]
RUN dotnet restore "./UmiHealthPOS.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src"
RUN dotnet build "UmiHealthPOS.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "UmiHealthPOS.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Build the final runtime image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "UmiHealthPOS.dll"]
