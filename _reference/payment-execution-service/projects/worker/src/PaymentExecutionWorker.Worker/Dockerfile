#checkov:skip=CKV_DOCKER_2:Healthchecks are setup via Kubernetes.
# Satisfies XREQ-24

############################################################################################################
# Build stage - discarded
############################################################################################################
FROM docker-mcr.artifactory.xero-support.com/dotnet/sdk:8.0 AS build

WORKDIR /src
COPY . .
RUN dotnet publish "./worker/src/PaymentExecutionWorker.Worker/PaymentExecutionWorker.csproj" \
        -c Release \
        -o /app/publish


############################################################################################################
# Final/runtime stage
# Base Image definition: https://github.com/xero-internal/Accelerators.Docker/tree/main/Dockerfiles/dotnet8/baseImage
############################################################################################################
FROM xop-accelerator-framework-docker-common.artifactory.xero-support.com/accelerators-docker/dotnet8/base-image:newest AS final

LABEL maintainer="Cashtopia"
ARG NewRelicHome=/usr/local/newrelic-dotnet-agent

COPY --from=build /app/publish /app
COPY --from=build /src/worker/src/PaymentExecutionWorker.Worker/newrelic.config $NewRelicHome/newrelic.config

RUN chmod 755 $NewRelicHome/newrelic.config

# Give non-root user permission to access the /app and NewRelic logs folders
# For more info, please refer to https://aka.ms/vscode-docker-dotnet-configure-containers
RUN chown -R app \
        /app \
        $NewRelicHome/logs/

USER app
WORKDIR /app

ENTRYPOINT ["enteraws", "dotnet", "PaymentExecutionWorker.dll"]
