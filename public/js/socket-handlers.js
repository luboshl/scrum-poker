// Connection to Socket.io server
const socket = io();

// Socket.io event handlers
socket.on('connect', () => {
    state.connected = true;
    updateConnectionStatus();
    
    // Restore heartbeat after reconnection
    if (state.currentUser && state.currentRoom) {
        sendHeartbeat();
    }
});

socket.on('disconnect', () => {
    state.connected = false;
    updateConnectionStatus();
});

socket.on('roomUpdate', (data) => {
    state.users = data.users;
    state.votingEnded = data.votingEnded;
    updateUsersList();
    updateVotingStats();
    updateEndVotingButton();
    updateVotingProgress();
});

// Login confirmation with possibly modified name
socket.on('joinConfirmation', (data) => {
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
socket.on('resetVoting', () => {
    // Reset selected button for all users
    document.querySelectorAll('.vote-btn').forEach(btn => {
        btn.classList.remove('selected');
    });
    updateVotingProgress();
});

// User removed event
socket.on('userRemoved', (data) => {
    if (data.status === 'success') {
        console.log(`User ${data.userName} was removed from the room`);
    } else {
        console.error(`Failed to remove user: ${data.message}`);
    }
});

// Forced disconnect event (when user is removed by another user)
socket.on('forcedDisconnect', (data) => {
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