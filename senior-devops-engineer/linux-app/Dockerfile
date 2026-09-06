FROM mcr.microsoft.com/dotnet/sdk:8.0.424-azurelinux3.0-amd64@sha256:c93d41f8a1bd79fcce318eea19fc2328c06bee1efa865603c3eb41fecf1c431a AS restore
WORKDIR /src

COPY shared/JtlDemo.Abstractions/JtlDemo.Abstractions.csproj shared/JtlDemo.Abstractions/
COPY linux-app/src/JtlDemo.Rest.Server/JtlDemo.Rest.Server.csproj linux-app/src/JtlDemo.Rest.Server/
COPY linux-app/src/JtlDemo.Rest.Linux.Host/JtlDemo.Rest.Linux.Host.csproj linux-app/src/JtlDemo.Rest.Linux.Host/
COPY linux-app/src/JtlDemo.Container.Healthcheck/JtlDemo.Container.Healthcheck.csproj linux-app/src/JtlDemo.Container.Healthcheck/
COPY linux-app/tests/JtlDemo.Linux.CompositionTests/JtlDemo.Linux.CompositionTests.csproj linux-app/tests/JtlDemo.Linux.CompositionTests/
COPY linux-app/JtlDemo.Linux.sln linux-app/

RUN dotnet restore linux-app/JtlDemo.Linux.sln

FROM restore AS test
COPY shared/ shared/
COPY linux-app/ linux-app/
RUN dotnet test linux-app/JtlDemo.Linux.sln --configuration Release --no-restore

FROM test AS publish
RUN dotnet publish linux-app/src/JtlDemo.Rest.Linux.Host/JtlDemo.Rest.Linux.Host.csproj \
        --configuration Release \
        --no-restore \
        --output /out/host /p:UseAppHost=false
RUN dotnet publish linux-app/src/JtlDemo.Container.Healthcheck/JtlDemo.Container.Healthcheck.csproj \
        --configuration Release \
        --no-restore \
        --output /out/healthcheck /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0.30-azurelinux3.0-distroless-extra-amd64@sha256:f0343cdccc97027052f8e6ddd4895b87b0e0433299945b84d16e4d64f2cd8e31 AS final
WORKDIR /app

COPY --from=publish /out/host ./
COPY --from=publish /out/healthcheck /healthcheck/

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID

HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 CMD ["dotnet", "/healthcheck/JtlDemo.Container.Healthcheck.dll"]

ENTRYPOINT ["dotnet", "JtlDemo.Rest.Linux.Host.dll"]
