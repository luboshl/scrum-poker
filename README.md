# Scrum Poker

> **Note**: This entire application was generated using Claude AI. The code may contain errors, inefficiencies, or unnecessary elements. This project serves as an experiment to explore the capabilities of AI-assisted development. Use at your own discretion and feel free to improve upon it.

Real-time web application for Scrum Poker (or Planning Poker) designed for agile teams. It allows team members to estimate task complexity in real time.

## Experimental Project

This project is primarily an experiment to test the capabilities of AI in generating functional web applications. While the application works as described, it may not follow all best practices or optimal implementation patterns.

## Hosting

The application is currently hosted on [Railway](https://railway.com) and is available at https://scrum-poker.up.railway.app/.

## Features

- Create and join sessions using room ID
- Real-time interactive voting
- Hide other users' votes until voting ends
- Display all votes after voting ends
- Voting statistics (average, minimum, maximum)
- Ability to share room link
- Observer mode
- Reset voting for a new round
- Consensus detection with celebratory confetti
- Automatic inactive user removal

## Technologies

- **Backend**: ASP.NET Core with SignalR
- **Frontend**: static HTML, CSS, and vanilla JavaScript
- **Communication**: SignalR over WebSockets for real-time communication
- **Local orchestration**: .NET Aspire (development only)

## Local Development with .NET Aspire (Recommended)

.NET Aspire provides a single-command startup that orchestrates the full local development stack.

### Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download) (v10.0 or later)

### Start via Aspire AppHost

```bash
dotnet run --project src/ScrumPoker.AppHost
```

The Aspire dashboard URL is printed to the terminal on startup. Open it in your browser to see all running resources and their logs.

The application is available at the URL shown for `scrum-poker-web` in the Aspire dashboard (typically `http://localhost:5000`).

## Manual Local Development

You can also start the ASP.NET Core application directly without Aspire:

```bash
dotnet run --project src/ScrumPoker.Web
```

Open the application in your browser:

```
http://localhost:5000
```

## Solution Structure

```
src/
  ScrumPoker.slnx                       # .NET solution file
  ScrumPoker.AppHost/                   # .NET Aspire orchestration entry point
  ScrumPoker.ServiceDefaults/           # Shared Aspire service defaults (telemetry, health checks)
  ScrumPoker.Web/                       # ASP.NET Core + SignalR backend host
    wwwroot/                            # Static frontend assets (HTML, CSS, JS)
  ScrumPoker.Application/               # Application services layer
  ScrumPoker.Domain/                    # Domain model layer
  ScrumPoker.UnitTests/                 # Unit tests (domain and application)
  ScrumPoker.IntegrationTests/          # Integration tests (SignalR hub and host)
```

## Architecture Notes

- Room state, votes, and heartbeats are stored in memory inside the ASP.NET Core process.
- The current implementation is best suited for a single running instance.
- Restarting the process clears all active rooms and votes.
- Static frontend assets are served directly by the ASP.NET Core application from `wwwroot/`.

## How to Use

1. Open the application in your browser
2. Enter your name
3. Create a new room (by default a room ID is generated) or join an existing one (by using a shared URL)
4. Share the link with other team members
5. Enter your estimate and confirm it
6. After all members have voted, end voting to reveal all estimates
7. For a new round, click "New voting"

## Deployment

The application can be deployed to any hosting that supports a long-running ASP.NET Core process and WebSockets, such as:

- Azure App Service
- Azure Container Apps
- Fly.io
- Render

## License

MIT

## End-to-End Testing

A Playwright baseline parity suite is included to validate user-visible behavior.

See [E2E_TESTING.md](E2E_TESTING.md) for setup and usage instructions.