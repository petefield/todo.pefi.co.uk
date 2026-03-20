FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY zscaler_root.crt /usr/local/share/ca-certificates/
RUN update-ca-certificates
COPY TodoApp.csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish TodoApp.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /src/zscaler_root.crt /usr/local/share/ca-certificates/
RUN update-ca-certificates
COPY --from=build /app .
EXPOSE 8080
ENTRYPOINT ["dotnet", "TodoApp.dll"]
