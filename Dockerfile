# FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build-env
# WORKDIR /app

# # Copy csproj and restore as distinct layers
# COPY *.csproj ./
# RUN dotnet restore

# # Copy everything else and build
# COPY . ./
# RUN dotnet publish -c Release -o out

# # Build
# WORKDIR /app
# RUN dotnet restore
# RUN dotnet publish -c Release -o out

# # Build runtime image
# FROM mcr.microsoft.com/dotnet/aspnet:3.1
# WORKDIR /app
# COPY --from=build-env /app/out .
# ENTRYPOINT ["dotnet", "Shipping.dll"]

FROM mcr.microsoft.com/dotnet/aspnet:3.1 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:3.1 AS build
WORKDIR /src

COPY ["Shipping/Shipping.csproj", "Shipping/"]
COPY ["Common/Common.csproj", "Common/"]
RUN dotnet restore "Shipping/Shipping.csproj"
COPY . .
WORKDIR "/src/Shipping"
RUN dotnet build "Shipping.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Shipping.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Shipping.dll"]
