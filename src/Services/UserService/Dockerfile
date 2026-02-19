# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["PetAdoption.UserService.API/PetAdoption.UserService.API.csproj", "PetAdoption.UserService.API/"]
COPY ["PetAdoption.UserService.Application/PetAdoption.UserService.Application.csproj", "PetAdoption.UserService.Application/"]
COPY ["PetAdoption.UserService.Domain/PetAdoption.UserService.Domain.csproj", "PetAdoption.UserService.Domain/"]
COPY ["PetAdoption.UserService.Infrastructure/PetAdoption.UserService.Infrastructure.csproj", "PetAdoption.UserService.Infrastructure/"]

RUN dotnet restore "PetAdoption.UserService.API/PetAdoption.UserService.API.csproj"

# Copy all files and build
COPY . .
WORKDIR "/src/PetAdoption.UserService.API"
RUN dotnet build "PetAdoption.UserService.API.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "PetAdoption.UserService.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PetAdoption.UserService.API.dll"]
