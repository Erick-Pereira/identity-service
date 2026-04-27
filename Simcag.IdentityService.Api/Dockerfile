# Build context: raiz do repositório
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["shared/shared.csproj", "shared/"]
COPY ["identity-service/Simcag.IdentityService.Api/Simcag.IdentityService.Api.csproj", "identity-service/Simcag.IdentityService.Api/"]
COPY ["identity-service/Simcag.IdentityService.Application/Simcag.IdentityService.Application.csproj", "identity-service/Simcag.IdentityService.Application/"]
COPY ["identity-service/Simcag.IdentityService.Domain/Simcag.IdentityService.Domain.csproj", "identity-service/Simcag.IdentityService.Domain/"]
COPY ["identity-service/Simcag.IdentityService.Infrastructure/Simcag.IdentityService.Infrastructure.csproj", "identity-service/Simcag.IdentityService.Infrastructure/"]
RUN dotnet restore "identity-service/Simcag.IdentityService.Api/Simcag.IdentityService.Api.csproj"
COPY shared/ shared/
COPY identity-service/ identity-service/
RUN dotnet publish "identity-service/Simcag.IdentityService.Api/Simcag.IdentityService.Api.csproj" -c ${BUILD_CONFIGURATION} -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Simcag.IdentityService.Api.dll"]
