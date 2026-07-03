# -------------------------------------------------
# VOSOX Backend – ProcurementSuite (.NET 8 microservices)
# One image, five processes: Ocelot gateway (public) +
# Identity / MasterData / Buyer / Supplier APIs (internal localhost).
# -------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY . .

RUN dotnet publish src/ApiGateWay/OcelotGateway/OcelotGateway.csproj              -c Release -o /app/gateway    /p:UseAppHost=false && \
    dotnet publish src/Platform/Identity/Identity.API/Identity.API.csproj         -c Release -o /app/identity   /p:UseAppHost=false && \
    dotnet publish src/Platform/MasterData/MasterData.API/MasterData.API.csproj   -c Release -o /app/masterdata /p:UseAppHost=false && \
    dotnet publish src/Modules/Buyer/Buyer.API/Buyer.API.csproj                   -c Release -o /app/buyer      /p:UseAppHost=false && \
    dotnet publish src/Modules/Supplier/Supplier.API/Supplier.API.csproj          -c Release -o /app/supplier   /p:UseAppHost=false

# Ocelot loads both files with optional:false — make certain they sit next
# to the gateway dll regardless of SDK content-item defaults.
RUN cp -f src/ApiGateWay/OcelotGateway/ocelot.json \
          src/ApiGateWay/OcelotGateway/ocelot.SwaggerEndPoints.json /app/gateway/

# -------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/gateway    ./gateway
COPY --from=build /app/identity   ./identity
COPY --from=build /app/masterdata ./masterdata
COPY --from=build /app/buyer      ./buyer
COPY --from=build /app/supplier   ./supplier
COPY start.sh .
RUN chmod +x start.sh

# UAT: enables downstream swagger (consumed by SwaggerForOcelot) and picks
# the appsettings.UAT.json connection strings that point at the shared
# SQL Server instead of the broken localhost/Production values.
ENV ASPNETCORE_ENVIRONMENT=UAT

EXPOSE 8000

CMD ["./start.sh"]
