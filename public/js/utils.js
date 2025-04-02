// Function to generate room ID
function generateRoomId() {
    const chars = 'abcdefghijklmnopqrstuvwxyz0123456789';
    let result = '';
    for (let i = 0; i < 10; i++) {
        result += chars.charAt(Math.floor(Math.random() * chars.length));
    }
    return result;
}

// Function to start heartbeat interval
function startHeartbeat() {
    // Stop previous interval if it exists
    if (heartbeatInterval) {
        clearInterval(heartbeatInterval);
    }
    
    // Send heartbeat every 3 seconds
    heartbeatInterval = setInterval(() => {
        if (state.connected && state.currentUser && state.currentRoom) {
            socket.emit('heartbeat');
        }
    }, 3000);
    
    // Set up event listeners for user activity detection
    setupActivityListeners();
}

// Set up event listeners for activity detection
function setupActivityListeners() {
    // Send heartbeat on interaction with the application
    window.addEventListener('click', sendHeartbeat);
    window.addEventListener('keypress', sendHeartbeat);
    window.addEventListener('scroll', sendHeartbeat);
    window.addEventListener('mousemove', debounce(sendHeartbeat, 1000)); // Limit frequency for mouse movement
    
    // Detection when the page is visible/hidden
    document.addEventListener('visibilitychange', function() {
        if (document.visibilityState === 'visible') {
            sendHeartbeat();
        }
    });
}

// Function for immediate heartbeat sending
function sendHeartbeat() {
    if (state.connected && state.currentUser && state.currentRoom) {
        socket.emit('heartbeat');
    }
}

// Helper function to limit the number of calls (debounce)
function debounce(func, delay) {
    let timeout;
    return function() {
        const context = this;
        const args = arguments;
        clearTimeout(timeout);
        timeout = setTimeout(() => func.apply(context, args), delay);
    };
}