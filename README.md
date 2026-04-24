# todo.pefi.co.uk

A personal todo app built with Blazor Server and Azure Cosmos DB, deployed to [todo.pefi.co.uk](https://todo.pefi.co.uk).

## Features

- Add, reorder, and track todos with statuses: **Pending**, **In Progress**, **Done**, and **Cancelled**
- Drag-and-drop reordering
- Filter and search todos
- Daily Web Push notifications reminding you of outstanding todos
- Responsive design for both mobile and desktop

## Tech Stack

- **Framework:** ASP.NET Core / Blazor Server (.NET 9)
- **Database:** Azure Cosmos DB (single-document pattern)
- **Push Notifications:** Web Push (VAPID) via the [WebPush](https://www.nuget.org/packages/WebPush) library
- **Hosting:** Azure App Service (Linux), deployed via GitHub Actions

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Docker](https://docs.docker.com/get-docker/) and [Docker Compose](https://docs.docker.com/compose/) (for local development)

## Local Development

The easiest way to run the app locally is with Docker Compose, which starts both the app and a local Azure Cosmos DB emulator:

```bash
docker compose up --build
```

The app will be available at `http://localhost:5080`.

> **Note:** The Cosmos DB emulator can take a minute to start. The app retries the connection automatically.

## Configuration

Configuration is managed via `appsettings.json` and environment variables. The following settings are required:

| Setting | Description |
|---|---|
| `CosmosDb:ConnectionString` | Azure Cosmos DB connection string |
| `CosmosDb:DatabaseName` | Database name (default: `TodoApp`) |
| `CosmosDb:ContainerName` | Container name (default: `Todos`) |
| `Vapid:PublicKey` | VAPID public key for Web Push |
| `Vapid:PrivateKey` | VAPID private key for Web Push |
| `Vapid:Subject` | VAPID subject (e.g. `mailto:you@example.com`) |
| `Notifications:DailyTime` | Time to send the daily notification (default: `09:00`) |
| `Notifications:TimeZone` | Time zone for the daily notification (default: `UTC`) |

To generate a VAPID key pair you can use the [web-push](https://www.npmjs.com/package/web-push) CLI:

```bash
npx web-push generate-vapid-keys
```

## Deployment

Pushes to the `main` branch automatically build and deploy to the Azure Web App `pefi-todo` via the GitHub Actions workflow in [`.github/workflows/main_pefi-todo.yml`](.github/workflows/main_pefi-todo.yml).

The workflow requires the following repository secrets to be configured:

- `AZUREAPPSERVICE_CLIENTID_*`
- `AZUREAPPSERVICE_TENANTID_*`
- `AZUREAPPSERVICE_SUBSCRIPTIONID_*`

## License

[MIT](LICENSE)