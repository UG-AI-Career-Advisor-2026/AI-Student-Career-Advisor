FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY . .
RUN dotnet restore CareerAdvisor.sln
RUN dotnet publish src/CareerAdvisor.Web/CareerAdvisor.Web.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .
COPY --from=build /src/data /data

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_FORWARDEDHEADERS_ENABLED=true
EXPOSE 10000

CMD ["sh", "-c", "exec dotnet CareerAdvisor.Web.dll --urls http://0.0.0.0:${PORT:-10000}"]
