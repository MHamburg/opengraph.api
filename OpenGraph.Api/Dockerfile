FROM microsoft/aspnetcore:2.0.3 AS base
WORKDIR /app
EXPOSE 80

FROM microsoft/aspnetcore-build:2.0.3 AS build
WORKDIR /src
COPY *.sln ./
COPY OpenGraph.Api/OpenGraph.Api.csproj OpenGraph.Api/
COPY OpenGraph/OpenGraph.csproj OpenGraph/
RUN dotnet restore
COPY . .
WORKDIR /src/OpenGraph.Api
RUN dotnet build -c Release -o /app

FROM build AS publish
RUN dotnet publish -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "OpenGraph.Api.dll"]
