FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY backend/CloneEbay.sln ./backend/
COPY backend/CloneEbay.Api ./backend/CloneEbay.Api
COPY backend/CloneEbay.Application ./backend/CloneEbay.Application
COPY backend/CloneEbay.Contracts ./backend/CloneEbay.Contracts
COPY backend/CloneEbay.Domain ./backend/CloneEbay.Domain
COPY backend/CloneEbay.Infrastructure ./backend/CloneEbay.Infrastructure

WORKDIR /src/backend
RUN dotnet restore CloneEbay.sln
RUN dotnet publish CloneEbay.Api/CloneEbay.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "CloneEbay.Api.dll", "--urls", "http://0.0.0.0:8080"]