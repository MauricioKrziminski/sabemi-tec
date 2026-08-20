# Estágio de build: restaura com os arquivos de projeto primeiro para aproveitar o cache
# de camadas quando apenas o código muda.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props ./
COPY src/Sabemi.Payments.Api/Sabemi.Payments.Api.csproj src/Sabemi.Payments.Api/
COPY src/Sabemi.Payments.Core/Sabemi.Payments.Core.csproj src/Sabemi.Payments.Core/
COPY src/Sabemi.Payments.Infrastructure/Sabemi.Payments.Infrastructure.csproj src/Sabemi.Payments.Infrastructure/

RUN dotnet restore src/Sabemi.Payments.Api/Sabemi.Payments.Api.csproj

COPY src/ src/
RUN dotnet publish src/Sabemi.Payments.Api/Sabemi.Payments.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

# Usuário sem privilégios já definido na imagem oficial.
USER $APP_UID

ENTRYPOINT ["dotnet", "Sabemi.Payments.Api.dll"]
