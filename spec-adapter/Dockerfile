# Builds the SDK from source with the adapter for the Flagsmith SDK behavioural spec.
# Build from the repository root: docker build -f spec-adapter/Dockerfile .
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /sdk
COPY LICENSE ./
COPY Flagsmith.Engine ./Flagsmith.Engine
COPY Flagsmith.FlagsmithClient ./Flagsmith.FlagsmithClient
COPY spec-adapter ./spec-adapter
RUN dotnet publish spec-adapter/SpecAdapter.csproj --configuration Release --output /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0

WORKDIR /app
COPY --from=build /app ./

ENV PORT=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "SpecAdapter.dll"]
