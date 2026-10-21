# Runs this SDK, built from source, under the Flagsmith SDK behavioural spec.
# docker build -f sdk-spec/Dockerfile --build-context harness=<sdk-spec>/harness/dotnet .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /sdk
COPY LICENSE ./
COPY Flagsmith.Engine ./Flagsmith.Engine
COPY Flagsmith.FlagsmithClient ./Flagsmith.FlagsmithClient
COPY sdk-spec/steps.cs ./sdk-spec/steps.cs
COPY --from=harness . /harness
RUN dotnet publish /harness/Harness.csproj --configuration Release --output /app \
    -p:StepsFile=/sdk/sdk-spec/steps.cs \
    -p:SdkProject=/sdk/Flagsmith.FlagsmithClient/Flagsmith.FlagsmithClient.csproj

FROM mcr.microsoft.com/dotnet/runtime:10.0

WORKDIR /sdk
COPY --from=build /app /harness
COPY sdk-spec/steps.cs ./sdk-spec/steps.cs

ENTRYPOINT ["dotnet", "/harness/Harness.dll", "sdk-spec/steps.cs"]
