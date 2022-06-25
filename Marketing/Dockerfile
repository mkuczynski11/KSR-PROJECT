FROM mcr.microsoft.com/dotnet/core/sdk:2.2 AS build
WORKDIR /usr/src/app
COPY . ./
RUN dotnet restore
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/core/sdk:2.2 AS runtime
WORKDIR /usr/src/app/out
COPY --from=build /usr/src/app/out ./
EXPOSE 91
ENTRYPOINT ["dotnet", "Marketing.dll"]
