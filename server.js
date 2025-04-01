// server.js - Node.js server with Express and Socket.io
const express = require('express');
const http = require('http');
const socketIo = require('socket.io');
const path = require('path');

const app = express();
const server = http.createServer(app);
const io = socketIo(server);

// Static files (HTML, CSS, JavaScript)
app.use(express.static(path.join(__dirname, 'public')));

// Application data
const rooms = new Map(); // Map for storing sessions (rooms)
const heartbeats = new Map(); // Map for tracking user activity

// Socket.io logic
io.on('connection', (socket) => {
  console.log('User connected', socket.id);
  let currentRoom = null;
  let userName = null;

  // User joining a room
  socket.on('joinRoom', ({ room, name }) => {
    // Create room if it doesn't exist
    if (!rooms.has(room)) {
      rooms.set(room, {
        users: [],
        votingEnded: false
      });
    }

    currentRoom = room;
    
    // Check if a user with this name already exists
    const roomData = rooms.get(room);
    let uniqueName = name;
    let nameExists = true;
    let counter = 2;
    
    // Ensure name is unique in the room
    while (nameExists) {
      // Check if this name already exists
      const existingUser = roomData.users.find(u => u.name === uniqueName);
      
      if (existingUser) {
        // If name exists, add a number
        uniqueName = `${name} (${counter})`;
        counter++;
      } else {
        // Name is unique, we can break the loop
        nameExists = false;
      }
    }
    
    // Now we have a unique name - use it
    userName = uniqueName;
    
    // Adding user to the room
    const existingUser = roomData.users.find(u => u.name === userName);
    
    if (!existingUser) {
      roomData.users.push({ id: socket.id, name: userName, vote: null, active: true });
    } else {
      // Update socket connection ID for the existing user
      existingUser.id = socket.id;
      existingUser.active = true;
    }

    // Setting heartbeat for user
    heartbeats.set(`${room}:${userName}`, Date.now());

    // Join socket to room
    socket.join(room);

    // Send current state to all users in the room
    io.to(room).emit('roomUpdate', {
      users: roomData.users,
      votingEnded: roomData.votingEnded
    });
    
    // Send login confirmation to specific user
    socket.emit('joinConfirmation', { name: userName });

    console.log(`User ${userName} joined room ${room}`);
  });

  // User voting
  socket.on('vote', (vote) => {
    if (!currentRoom || !userName) return;

    const roomData = rooms.get(currentRoom);
    const user = roomData.users.find(u => u.name === userName);
    
    if (user) {
      user.vote = vote;
      io.to(currentRoom).emit('roomUpdate', {
        users: roomData.users,
        votingEnded: roomData.votingEnded
      });
      console.log(`User ${userName} voted: ${vote}`);
    }
  });

  // End voting
  socket.on('endVoting', () => {
    if (!currentRoom) return;

    const roomData = rooms.get(currentRoom);
    
    // If voting is already ended, ignore the request
    if (roomData.votingEnded) return;
    
    roomData.votingEnded = true;

    io.to(currentRoom).emit('roomUpdate', {
      users: roomData.users,
      votingEnded: true
    });
    console.log(`Voting in room ${currentRoom} has ended`);
  });

  // Reset voting
  socket.on('resetVoting', () => {
    if (!currentRoom) return;

    const roomData = rooms.get(currentRoom);
    roomData.votingEnded = false;
    roomData.users.forEach(user => {
      user.vote = null;
    });

    io.to(currentRoom).emit('resetVoting');  // New event for all clients
    io.to(currentRoom).emit('roomUpdate', {
      users: roomData.users,
      votingEnded: false
    });
    console.log(`Voting in room ${currentRoom} has been reset`);
  });

  // Heartbeat - check that user is still active
  socket.on('heartbeat', () => {
    if (currentRoom && userName) {
      heartbeats.set(`${currentRoom}:${userName}`, Date.now());
    }
  });

  // Interval for checking inactive users (every 5 seconds)
  const heartbeatInterval = setInterval(() => {
    if (currentRoom && userName) {
      const roomData = rooms.get(currentRoom);
      if (roomData) {
        const now = Date.now();
        let roomUpdated = false;

        roomData.users.forEach(user => {
          const heartbeatKey = `${currentRoom}:${user.name}`;
          const lastHeartbeat = heartbeats.get(heartbeatKey);
          
          // If last heartbeat is older than 10 seconds, mark user as inactive
          if (lastHeartbeat && now - lastHeartbeat > 10000) {
            if (user.active) {
              user.active = false;
              roomUpdated = true;
              console.log(`User ${user.name} in room ${currentRoom} is inactive`);
            }
          }
        });

        // If activity changed, update room state
        if (roomUpdated) {
          // Filter out inactive users
          roomData.users = roomData.users.filter(user => user.active);
          
          io.to(currentRoom).emit('roomUpdate', {
            users: roomData.users,
            votingEnded: roomData.votingEnded
          });
        }
      }
    }
  }, 5000);

  // User disconnection
  socket.on('disconnect', () => {
    if (currentRoom && userName) {
      // Stop heartbeat interval
      clearInterval(heartbeatInterval);

      // Set user as inactive immediately
      const roomData = rooms.get(currentRoom);
      if (roomData) {
        const user = roomData.users.find(u => u.name === userName);
        if (user) {
          user.active = false;
        }

        // Filter out inactive users
        roomData.users = roomData.users.filter(u => u.active);

        // Update all clients
        io.to(currentRoom).emit('roomUpdate', {
          users: roomData.users,
          votingEnded: roomData.votingEnded
        });
      }
    }
    console.log('User disconnected', socket.id);
  });
});

// Start server
const PORT = process.env.PORT || 3000;
server.listen(PORT, () => {
  console.log(`Server running on port ${PORT}`);
});