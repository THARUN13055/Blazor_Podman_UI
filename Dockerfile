FROM  mcr.microsoft.com/dotnet/sdk:10.0 AS build
COPY . /build
WORKDIR /build
RUN dotnet build -c Release -o output

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS runtime
COPY --from=build /build/output .
ENTRYPOINT [ "" ]