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
- **Frontend**: HTML, CSS, JavaScript (vanilla JavaScript, no framework)
- **Communication**: Socket.io over WebSockets for real-time communication

## Installation

1. Clone the repository:
```
git clone [repository-URL]
cd scrum-poker
```

2. Install dependencies:
```
npm install
```

3. Start the server:
```
npm start
```

For development mode with auto-restart:
```
npm run dev
```

4. Open the application in your browser:
```
http://localhost:3000
```

The server listens on `process.env.PORT` and falls back to port `3000`.

## Project Structure

- `server.js` - Node.js server with Express and Socket.io
- `public/` - Static files for the frontend
  - `index.html` - HTML structure of the application
  - `css/styles.css` - Application styles
  - `js/` - Frontend logic split into small modules
  - `images/` - Icons and Open Graph images

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