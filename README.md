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

- **Backend**: Node.js with Express and Socket.io
- **Frontend**: static HTML, CSS, and vanilla JavaScript
- **Communication**: Socket.io over WebSockets for real-time communication
- **Local orchestration**: .NET Aspire (development only)

## Local Development with .NET Aspire (Recommended)

.NET Aspire provides a single-command startup that orchestrates the full local development stack.

### Prerequisites

- [Node.js](https://nodejs.org/) (v14 or later)
- [.NET SDK](https://dotnet.microsoft.com/download) (v10.0 or later)

### Start via Aspire AppHost

```bash
dotnet run --project src/ScrumPoker.AppHost
```

The AppHost automatically restores npm dependencies before starting the Node.js app, so no manual dependency installation step is required.

The Aspire dashboard URL is printed to the terminal on startup. Open it in your browser to see all running resources and their logs.

The legacy Node.js application is available at `http://localhost:3000`.

> **Migration note**: During the C# backend migration, the AppHost currently orchestrates the legacy Node.js application as a temporary resource. Once the ASP.NET Core backend is scaffolded, the Node.js resource will be replaced with the new backend project.

## Manual Local Development (Legacy Node.js)

You can also start the Node.js application directly without Aspire:

1. Install dependencies:
```
npm install
```

2. Start the server:
```
npm start
```

For development mode with auto-restart:
```
npm run dev
```

3. Open the application in your browser:
```
http://localhost:3000
```

The server listens on `process.env.PORT` and falls back to port `3000`.

## Solution Structure

```
src/
  ScrumPoker.slnx                       # .NET solution file
  ScrumPoker.AppHost/                   # .NET Aspire orchestration entry point
  ScrumPoker.ServiceDefaults/           # Shared Aspire service defaults (telemetry, health checks)
  ScrumPoker.Web/                       # ASP.NET Core + SignalR backend host
  ScrumPoker.Application/               # Application services layer
  ScrumPoker.Domain/                    # Domain model layer
  ScrumPoker.UnitTests/                 # Unit tests (domain and application)
  ScrumPoker.IntegrationTests/          # Integration tests (WebApplicationFactory smoke tests)
server.js                               # Legacy Node.js server (Express + Socket.io)
public/                                 # Static frontend assets
```

## Project Structure

- `server.js` - Node.js server with Express and Socket.io
- `public/` - Static files for the frontend
  - `index.html` - HTML structure of the application
  - `css/styles.css` - Application styles
  - `js/` - Frontend logic split into small modules
  - `images/` - Icons and Open Graph images
- `src/ScrumPoker.AppHost/` - .NET Aspire AppHost project for local orchestration
- `src/ScrumPoker.ServiceDefaults/` - .NET Aspire shared service defaults

## Architecture Notes

- Room state, votes, and heartbeats are stored in memory inside the Node.js process.
- The current implementation is best suited for a single running instance.
- Restarting the process clears all active rooms and votes.
- Static frontend assets are served directly by the Express application.

## How to Use

1. Open the application in your browser
2. Enter your name
3. Create a new room (by default a room ID is generated) or join an existing one (by using a shared URL)
4. Share the link with other team members
5. Enter your estimate and confirm it
6. After all members have voted, end voting to reveal all estimates
7. For a new round, click "New voting"

## Deployment

The application can be deployed to hosting that supports a long-running Node.js process and WebSockets, such as:

- Railway
- Render
- Fly.io
- Azure App Service
- Azure Container Apps

## License

MIT

## End-to-End Testing

A Playwright baseline parity suite is included to lock down the current user-visible behavior before the backend migration.

See [E2E_TESTING.md](E2E_TESTING.md) for setup and usage instructions.