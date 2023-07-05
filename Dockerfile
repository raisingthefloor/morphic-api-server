# Copyright 2022 Raising the Floor - US, Inc.
#
# Licensed under the New BSD license. You may not use this file except in
# compliance with this License.
#
# You may obtain a copy of the License at
# https://github.com/raisingthefloor/morphic-api-server/blob/master/LICENSE.md
#
# The R&D leading to these results received funding from the:
# * Rehabilitation Services Administration, US Dept. of Education under
#   grant H421A150006 (APCP)
# * National Institute on Disability, Independent Living, and
#   Rehabilitation Research (NIDILRR)
# * Administration for Independent Living & Dept. of Education under grants
#   H133E080022 (RERC-IT) and H133E130028/90RE5003-01-00 (UIITA-RERC)
# * European Union's Seventh Framework Programme (FP7/2007-2013) grant
#   agreement nos. 289016 (Cloud4all) and 610510 (Prosperity4All)
# * William and Flora Hewlett Foundation
# * Ontario Ministry of Research and Innovation
# * Canadian Foundation for Innovation
# * Adobe Foundation
# * Consumer Electronics Association Foundation

ARG VERSION=3.1-alpine
FROM mcr.microsoft.com/dotnet/core/sdk:${VERSION} AS build-env
WORKDIR /app

# copy and build
COPY ./MorphicServer.sln .
COPY ./Morphic.Server.Settings/ ./Morphic.Server.Settings/
COPY ./Morphic.Server/ ./Morphic.Server/
COPY ./Morphic.Server.Tests/ ./Morphic.Server.Tests/
COPY ./Morphic.Security/ ./Morphic.Security/
COPY ./Morphic.Security.Tests/ ./Morphic.Security.Tests/
COPY ./Morphic.Json/ ./Morphic.Json/
COPY ./Morphic.Json.Tests/ ./Morphic.Json.Tests/
RUN dotnet publish -c Release -o Morphic.Server

# Build runtime image
FROM mcr.microsoft.com/dotnet/core/aspnet:${VERSION} as runtime
RUN apk update && apk upgrade 
WORKDIR /app
COPY --from=build-env /app/Morphic.Server/ ./
COPY Morphic.Server/appsettings.* ./
ENTRYPOINT ["dotnet", "Morphic.Server.dll"]
