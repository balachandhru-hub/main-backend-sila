#!/bin/bash
set -e

# Internal APIs bind loopback on the ports ocelot.json routes to:
#   /api/v1/{everything}        -> 8001  (Identity)
#   /api/v1/unspsc/{everything} -> 8002  (MasterData)
# Buyer/Supplier have no gateway routes yet; parked on the next ports
# so adding routes later needs no image change:
#   8003 (Buyer)  8004 (Supplier)
# Each process runs from its own directory so appsettings/nlog resolve.
(cd identity   && ASPNETCORE_URLS=http://127.0.0.1:8001 exec dotnet Identity.API.dll)   &
(cd masterdata && ASPNETCORE_URLS=http://127.0.0.1:8002 exec dotnet MasterData.API.dll) &
(cd buyer      && ASPNETCORE_URLS=http://127.0.0.1:8003 exec dotnet Buyer.API.dll)      &
(cd supplier   && ASPNETCORE_URLS=http://127.0.0.1:8004 exec dotnet Supplier.API.dll)   &

# Gateway is the only public listener; container dies with it so docker
# restart policies and CI health checks see failures.
cd gateway && ASPNETCORE_URLS=http://0.0.0.0:8000 exec dotnet OcelotGateway.dll
