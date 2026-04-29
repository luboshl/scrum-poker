// Connection to SignalR hub
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hub")
    .withAutomaticReconnect()
    .build();

// Server → Client event handlers
connection.on('roomUpdate', (data) => {
    state.users = data.users;
    state.votingEnded = data.votingEnded;
    updateUsersList();
    updateVotingStats();
    updateEndVotingButton();
    updateVotingProgress();
});

// Login confirmation with possibly modified name
connection.on('joinConfirmation', (data) => {
    // Update user name if server changed it due to duplication
    state.currentUser = data.name;
    currentUserName.textContent = data.name;
    
    // Update observer badge if needed
    if (data.isObserver) {
        state.observerMode = true;
        observerBadge.classList.remove('hidden');
        disableVotingButtons();
    } else {
        state.observerMode = false;
        observerBadge.classList.add('hidden');
    }
});

// Listen for resetVoting event from server
connection.on('resetVoting', () => {
    // Reset selected button for all users
    document.querySelectorAll('.vote-btn').forEach(btn => {
        btn.classList.remove('selected');
    });
    updateVotingProgress();
});

// User removed event
connection.on('userRemoved', (data) => {
    if (data.status === 'success') {
        console.log(`User ${data.userName} was removed from the room`);
    } else {
        console.error(`Failed to remove user: ${data.message}`);
    }
});

// Forced disconnect event (when user is removed by another user)
connection.on('forcedDisconnect', (data) => {
    // User was removed from the room by another user
    alert(data.message);
    
    // Reset state
    state.currentUser = null;
    state.currentRoom = null;
    state.users = [];
    state.votingEnded = false;
    
    // Update UI - show login screen again
    loginContainer.classList.remove('hidden');
    votingContainer.classList.add('hidden');
    
    // Clean up any selected buttons
    document.querySelectorAll('.vote-btn').forEach(btn => {
        btn.classList.remove('selected');
    });
});

// Connection lifecycle handlers
connection.onreconnecting(() => {
    state.connected = false;
    updateConnectionStatus();
});

connection.onreconnected(() => {
    state.connected = true;
    updateConnectionStatus();
    // Re-join room after reconnection
    if (state.currentUser && state.currentRoom) {
        connection.invoke('JoinRoom', state.currentRoom, state.currentUser, state.observerMode)
            .catch(err => console.error(err));
        sendHeartbeat();
    }
});

connection.onclose(() => {
    state.connected = false;
    updateConnectionStatus();
});

// Start the connection
connection.start()
    .then(() => {
        state.connected = true;
        updateConnectionStatus();
        // Restore heartbeat after reconnection
        if (state.currentUser && state.currentRoom) {
            sendHeartbeat();
        }
    })
    .catch(err => console.error('SignalR connection error:', err));