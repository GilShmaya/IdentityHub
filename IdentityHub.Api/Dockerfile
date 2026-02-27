FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY IdentityHub.Api/IdentityHub.Api.csproj IdentityHub.Api/
RUN dotnet restore IdentityHub.Api/IdentityHub.Api.csproj
COPY . .
WORKDIR /src/IdentityHub.Api
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "IdentityHub.Api.dll"]
