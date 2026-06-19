FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /app

COPY . .

RUN dotnet restore Source/SmartLib.Web/SmartLib.Web.csproj

RUN dotnet publish Source/SmartLib.Web/SmartLib.Web.csproj \
-c Release \
-o /app/out


FROM mcr.microsoft.com/dotnet/aspnet:10.0

WORKDIR /app

COPY --from=build /app/out .

EXPOSE 8080

ENTRYPOINT ["dotnet","SmartLib.Web.dll"]