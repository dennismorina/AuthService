FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props global.json ./
COPY src/AuthService.Domain/AuthService.Domain.csproj src/AuthService.Domain/
COPY src/AuthService.Application/AuthService.Application.csproj src/AuthService.Application/
COPY src/AuthService.Infrastructure/AuthService.Infrastructure.csproj src/AuthService.Infrastructure/
COPY src/AuthService.Api/AuthService.Api.csproj src/AuthService.Api/
RUN dotnet restore src/AuthService.Api/AuthService.Api.csproj

COPY . .
RUN dotnet publish src/AuthService.Api/AuthService.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "AuthService.Api.dll"]
