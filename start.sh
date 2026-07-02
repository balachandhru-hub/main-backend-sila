#!/bin/bash
set -e

# Internal APIs bind loopback on the ports ocelot.json routes to:
#   /api/person       -> 5289  (Identity)
#   /api/order        -> 5051  (Supplier - matches its launchSettings port)
#   /api/orderdetails -> 5091  (Buyer)
# Each process runs from its own directory so appsettings resolve correctly.
(cd identity && ASPNETCORE_URLS=http://127.0.0.1:5289 exec dotnet Identity.API.dll) &
(cd supplier && ASPNETCORE_URLS=http://127.0.0.1:5051 exec dotnet Supplier.API.dll) &
(cd buyer    && ASPNETCORE_URLS=http://127.0.0.1:5091 exec dotnet Buyer.API.dll) &

# Gateway is the only public listener; container dies with it so docker
# restart policies and CI health checks see failures.
cd gateway && ASPNETCORE_URLS=http://0.0.0.0:8080 exec dotnet OcelotGateway.dll
