# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files
COPY ["src/Services/PetService/PetAdoption.PetService.API/PetAdoption.PetService.API.csproj", "Services/PetService/PetAdoption.PetService.API/"]
COPY ["src/Services/PetService/PetAdoption.PetService.Application/PetAdoption.PetService.Application.csproj", "Services/PetService/PetAdoption.PetService.Application/"]
COPY ["src/Services/PetService/PetAdoption.PetService.Domain/PetAdoption.PetService.Domain.csproj", "Services/PetService/PetAdoption.PetService.Domain/"]
COPY ["src/Services/PetService/PetAdoption.PetService.Infrastructure/PetAdoption.PetService.Infrastructure.csproj", "Services/PetService/PetAdoption.PetService.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "Services/PetService/PetAdoption.PetService.API/PetAdoption.PetService.API.csproj"

# Copy all source files
COPY src/Services/PetService/ Services/PetService/

# Build
WORKDIR "/src/Services/PetService/PetAdoption.PetService.API"
RUN dotnet build "PetAdoption.PetService.API.csproj" -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish "PetAdoption.PetService.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PetAdoption.PetService.API.dll"]
