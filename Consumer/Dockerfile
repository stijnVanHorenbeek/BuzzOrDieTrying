FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files
COPY ["Consumer/Consumer.csproj", "Consumer/"]
COPY ["Contracts/Contracts.csproj", "Contracts/"]

# Restore dependencies
RUN dotnet restore "Consumer/Consumer.csproj"

# Copy source code
COPY Consumer/ Consumer/
COPY Contracts/ Contracts/

# Build
WORKDIR /src/Consumer
RUN dotnet build "Consumer.csproj" -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish "Consumer.csproj" -c Release -o /app/publish

EXPOSE 8080
# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Consumer.dll"]
