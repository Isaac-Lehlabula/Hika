FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props .
COPY Hika.slnx .
COPY src/Hika.Domain/Hika.Domain.csproj src/Hika.Domain/
COPY src/Hika.Application/Hika.Application.csproj src/Hika.Application/
COPY src/Hika.Infrastructure/Hika.Infrastructure.csproj src/Hika.Infrastructure/
COPY src/Hika.Api/Hika.Api.csproj src/Hika.Api/
COPY tests/Hika.UnitTests/Hika.UnitTests.csproj tests/Hika.UnitTests/
COPY tests/Hika.IntegrationTests/Hika.IntegrationTests.csproj tests/Hika.IntegrationTests/
RUN dotnet restore src/Hika.Api/Hika.Api.csproj

COPY . .
RUN dotnet publish src/Hika.Api/Hika.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN useradd --no-create-home --shell /usr/sbin/nologin hika

COPY --from=build /app .
# The app creates/writes LocalFileStorage's uploads dir under /app at startup; WORKDIR and COPY
# both run as root, so without this the non-root user below can't write into its own app folder.
RUN mkdir -p /app/uploads && chown -R hika:hika /app

USER hika

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Hika.Api.dll"]
