# Portable container image: runs on Render, Koyeb, Azure App Service (Linux) or any VPS.
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Restore first (cached layer): solution-level props + project files only.
COPY global.json Directory.Build.props Directory.Packages.props .editorconfig ./
COPY src/Eshhanly.Domain/Eshhanly.Domain.csproj src/Eshhanly.Domain/
COPY src/Eshhanly.Application/Eshhanly.Application.csproj src/Eshhanly.Application/
COPY src/Eshhanly.Infrastructure/Eshhanly.Infrastructure.csproj src/Eshhanly.Infrastructure/
COPY src/Eshhanly.Web/Eshhanly.Web.csproj src/Eshhanly.Web/
RUN dotnet restore src/Eshhanly.Web/Eshhanly.Web.csproj

COPY src/ src/
RUN dotnet publish src/Eshhanly.Web/Eshhanly.Web.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "Eshhanly.Web.dll"]
