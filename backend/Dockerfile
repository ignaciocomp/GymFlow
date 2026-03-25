FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files first (better layer caching)
COPY GymFlow.sln .
COPY src/GymFlow.Domain/GymFlow.Domain.csproj src/GymFlow.Domain/
COPY src/GymFlow.Application/GymFlow.Application.csproj src/GymFlow.Application/
COPY src/GymFlow.Infrastructure/GymFlow.Infrastructure.csproj src/GymFlow.Infrastructure/
COPY src/GymFlow.API/GymFlow.API.csproj src/GymFlow.API/
COPY tests/GymFlow.Domain.Tests/GymFlow.Domain.Tests.csproj tests/GymFlow.Domain.Tests/
COPY tests/GymFlow.Application.Tests/GymFlow.Application.Tests.csproj tests/GymFlow.Application.Tests/
COPY tests/GymFlow.Infrastructure.Tests/GymFlow.Infrastructure.Tests.csproj tests/GymFlow.Infrastructure.Tests/

RUN dotnet restore

# Copy everything and build
COPY . .
RUN dotnet restore
RUN dotnet publish src/GymFlow.API/GymFlow.API.csproj -c Release -o /app/publish --no-restore

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "GymFlow.API.dll"]
