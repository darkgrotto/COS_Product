# Stage 1: Build
# Use BUILDPLATFORM to run the SDK natively (faster than QEMU emulation).
# TARGETARCH is injected by buildx and used to select the correct .NET RID.
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG TARGETARCH
WORKDIR /source

COPY src/ ./src/

# Map Docker arch names (amd64, arm64) to .NET RID arch names (x64, arm64).
# amd64 -> x64; arm64 stays arm64.
RUN DOTNET_ARCH=$([ "$TARGETARCH" = "amd64" ] && echo "x64" || echo "$TARGETARCH") && \
    dotnet restore src/CountOrSell.sln -r linux-${DOTNET_ARCH} && \
    dotnet publish src/CountOrSell.Api/CountOrSell.Api.csproj \
        --no-restore \
        -c Release \
        --self-contained true \
        -r linux-${DOTNET_ARCH} \
        -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Install curl for health checks, then create non-root user.
# All root operations complete before USER is set.
RUN apt-get update && \
    apt-get install -y --no-install-recommends curl && \
    rm -rf /var/lib/apt/lists/* && \
    addgroup --system --gid 1001 appgroup && \
    adduser --system --uid 1001 --ingroup appgroup --no-create-home appuser

COPY --from=build /app/publish .

USER appuser

EXPOSE 8080

# Self-contained binary - no dotnet runtime invocation needed.
ENTRYPOINT ["./CountOrSell.Api"]
