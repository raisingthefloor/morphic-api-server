# Copyright 2020 Raising the Floor - International
#
# Licensed under the New BSD license. You may not use this file except in
# compliance with this License.
#
# You may obtain a copy of the License at
# https://github.com/GPII/universal/blob/master/LICENSE.txt
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

ARG VERSION=3.1
FROM mcr.microsoft.com/dotnet/core/sdk:${VERSION} AS build-env
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY ./MorphicServer/*.csproj ./MorphicServer/
COPY ./MorphicServer.Tests/*.csproj ./MorphicServer.Tests/
COPY ./Morphic.Json/*.csproj ./Morphic.Json/
COPY ./Morphic.Json.Tests/*.csproj ./Morphic.Json.Tests/
COPY ./MorphicServer.sln .
RUN dotnet restore

COPY ./MorphicServer/ ./MorphicServer/
COPY ./MorphicServer.Tests/ ./MorphicServer.Tests/
RUN dotnet publish -c Release -o MorphicServer

# Build runtime image
FROM mcr.microsoft.com/dotnet/core/aspnet:${VERSION} as runtime
WORKDIR /app
COPY --from=build-env /app/MorphicServer/ ./
COPY MorphicServer/appsettings.* ./
ENTRYPOINT ["dotnet", "MorphicServer.dll"]
