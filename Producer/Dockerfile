FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files
COPY ["Producer/Producer.csproj", "Producer/"]
COPY ["Contracts/Contracts.csproj", "Contracts/"]

# Restore dependencies
RUN dotnet restore "Producer/Producer.csproj"

# Copy source code
COPY Producer/ Producer/
COPY Contracts/ Contracts/

# Build
WORKDIR /src/Producer
RUN dotnet build "Producer.csproj" -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish "Producer.csproj" -c Release -o /app/publish

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Producer.dll"]
