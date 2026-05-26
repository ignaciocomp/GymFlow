# Stage 1 — Build frontend (Vite)
FROM node:20-alpine AS frontend-build
WORKDIR /app
COPY frontend/package*.json ./
RUN npm ci
COPY frontend/ ./
RUN npm run build

# Stage 2 — Build backend (.NET 8)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS backend-build
WORKDIR /src
COPY backend/GymFlow.sln .
COPY backend/src/ ./src/
COPY backend/tests/ ./tests/
RUN dotnet restore
RUN dotnet publish src/GymFlow.API/GymFlow.API.csproj -c Release -o /app/publish --no-restore

# Stage 3 — Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=backend-build /app/publish .
COPY --from=frontend-build /app/dist ./wwwroot
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "GymFlow.API.dll"]
