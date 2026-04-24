# todo.pefi.co.uk

A personal todo app built with Blazor Server and MongoDB, deployed to [todo.pefi.co.uk](https://todo.pefi.co.uk).

## Features

- Add, reorder, and track todos with statuses: **Pending**, **In Progress**, **Done**, and **Cancelled**
- Drag-and-drop reordering
- Filter and search todos
- Daily Web Push notifications reminding you of outstanding todos
- Responsive design for both mobile and desktop

## Tech Stack

- **Framework:** ASP.NET Core / Blazor Server (.NET 9)
- **Database:** MongoDB via the [pefi.persistence](https://www.nuget.org/packages/pefi.persistence) library
- **Push Notifications:** Web Push (VAPID) via the [WebPush](https://www.nuget.org/packages/WebPush) library
- **Hosting:** Azure App Service (Linux), deployed via GitHub Actions

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Docker](https://docs.docker.com/get-docker/) and [Docker Compose](https://docs.docker.com/compose/) (for local development)

## Local Development

The easiest way to run the app locally is with Docker Compose, which starts both the app and a local MongoDB instance:

```bash
docker compose up --build
```

The app will be available at `http://localhost:5080`.

## Configuration

Configuration is managed via `appsettings.json` and environment variables. The following settings are required:

| Setting | Description |
|---|---|
| `MongoDb:ConnectionString` | MongoDB connection string |
| `MongoDb:DatabaseName` | Database name (default: `TodoApp`) |
| `MongoDb:TodosCollection` | Collection name for todos (default: `todos`) |
| `MongoDb:SubscriptionsCollection` | Collection name for push subscriptions (default: `pushSubscriptions`) |
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