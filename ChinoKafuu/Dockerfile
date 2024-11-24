FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["ChinoKafuu.csproj", "./"]
RUN dotnet restore

COPY . .

RUN dotnet build "ChinoKafuu.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ChinoKafuu.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ChinoKafuu.dll"]