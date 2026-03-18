FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY TodoApp.csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app .
VOLUME /data
ENV DataDirectory=/data
EXPOSE 8080
ENTRYPOINT ["dotnet", "TodoApp.dll"]
