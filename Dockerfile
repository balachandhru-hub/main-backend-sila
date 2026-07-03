# -------------------------------------------------
# VOSOX Backend – ProcurementSuite (.NET 8 microservices)
# One image, four processes: Ocelot gateway (public) +
# Identity / Buyer / Supplier APIs (internal localhost).
# -------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY . .

RUN dotnet publish src/ApiGateWay/OcelotGateway/OcelotGateway.csproj      -c Release -o /app/gateway  /p:UseAppHost=false && \
    dotnet publish src/Platform/Identity/Identity.API/Identity.API.csproj -c Release -o /app/identity /p:UseAppHost=false && \
    dotnet publish src/Modules/Buyer/Buyer.API/Buyer.API.csproj           -c Release -o /app/buyer    /p:UseAppHost=false && \
    dotnet publish src/Modules/Supplier/Supplier.API/Supplier.API.csproj  -c Release -o /app/supplier /p:UseAppHost=false

# ocelot.json is not yet AddJsonFile'd in Program.cs, so publish skips it.
# Ship it next to the gateway dll so wiring Ocelot up later needs no image change.
RUN cp src/ApiGateWay/OcelotGateway/ocelot.json /app/gateway/ocelot.json

# -------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/gateway  ./gateway
COPY --from=build /app/identity ./identity
COPY --from=build /app/buyer    ./buyer
COPY --from=build /app/supplier ./supplier
COPY start.sh .
RUN chmod +x start.sh

ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

CMD ["./start.sh"]
