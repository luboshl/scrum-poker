# Scrum Pocker

> **Note**: This entire application was generated using Claude AI. The code may contain errors, inefficiencies, or unnecessary elements. This project serves as an experiment to explore the capabilities of AI-assisted development. Use at your own discretion and feel free to improve upon it.

Real-time web application for Scrum Pocker (or Planning Pocker) designed for agile teams. It allows team members to estimate task complexity in real time.

## Experimental Project

This project is primarily an experiment to test the capabilities of AI in generating functional web applications. While the application works as described, it may not follow all best practices or optimal implementation patterns.

## Features

- Create and join sessions using room ID
- Real-time interactive voting
- Hide other users' votes until voting ends
- Display all votes after voting ends
- Voting statistics (average, median, minimum, maximum)
- Ability to share room link
- Reset voting for a new round
- Consensus detection with celebratory confetti
- Automatic inactive user removal

## Technologies

- **Backend**: Node.js with Express and Socket.io
- **Frontend**: HTML, CSS, JavaScript (vanilla JavaScript, no framework)
- **Communication**: WebSockets for real-time communication

## Installation

1. Clone the repository:
```
git clone [repository-URL]
cd scrum-pocker
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

## Project Structure

- `server.js` - Node.js server with Express and Socket.io
- `public/` - Static files for frontend
  - `index.html` - HTML structure of the application
  - (CSS and JavaScript are embedded directly in the HTML file for simplicity)

## How to Use

1. Open the application in your browser
2. Enter your name
3. Create a new room (by default a room ID is generated) or join an existing one (by using a shared URL)
4. Share the link with other team members
5. Enter your estimate and confirm it
6. After all members have voted, end voting to reveal all estimates
7. For a new round, click "New voting"

## Deployment

The application can be deployed to any hosting that supports Node.js, such as:

- Heroku
- DigitalOcean
- Vercel
- Netlify (using serverless functions)
- AWS, Google Cloud, Azure

## License

MIT