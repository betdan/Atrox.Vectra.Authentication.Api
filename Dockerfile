FROM dotnet/sdk:v8.0.303-alpine3.20-amd64 AS build
COPY . .

RUN dotnet restore "Atrox.Vectra.Runtime.Api/Atrox.Vectra.Runtime.Api.csproj"
RUN dotnet build "Atrox.Vectra.Runtime.Api/Atrox.Vectra.Runtime.Api.csproj" -c Release -o /app/build --no-restore
RUN dotnet publish "Atrox.Vectra.Runtime.Api/Atrox.Vectra.Runtime.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false --no-restore

FROM dotnet/aspnet:8.0.7-alpine3.20-amd64 AS final

WORKDIR /app

ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV ASPNETCORE_HTTP_PORTS=80

EXPOSE 80
EXPOSE 443

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Atrox.Vectra.Runtime.Api.dll"]




