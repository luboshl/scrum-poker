// Function for login
function handleLogin(asObserver) {
    const name = nameInput.value.trim();
    if (!name) {
        loginError.classList.remove('hidden');
        return;
    }
    
    // Create or connect to a room
    let room = state.currentRoom;
    if (!room) {
        // If no room ID is specified, generate a random one
        room = generateRoomId();
        state.currentRoom = room;
    }
    
    state.currentUser = name;
    state.observerMode = asObserver;
    
    // Connect to the room
    socket.emit('joinRoom', { room, name, isObserver: asObserver });
    
    // Update URL
    const url = new URL(window.location);
    url.searchParams.set('room', room);
    window.history.pushState({}, '', url);
    
    // Update UI
    roomIdDisplay.textContent = room;
    currentUserName.textContent = name;
    loginContainer.classList.add('hidden');
    votingContainer.classList.remove('hidden');
    
    // Update observer badge
    if (asObserver) {
        observerBadge.classList.remove('hidden');
        disableVotingButtons();
    } else {
        observerBadge.classList.add('hidden');
    }
    
    loginError.classList.add('hidden');
    
    // Start heartbeat interval
    startHeartbeat();
    
    // Update voting progress
    updateVotingProgress();
}

// Function to end voting
function handleEndVoting() {
    socket.emit('endVoting');
}

// Function to reset voting
function handleResetVoting() {
    // Voting reset is now done on the server
    // and all clients will get the resetVoting event
    socket.emit('resetVoting');
    
    // Local reset of highlighting for immediate UI response
    document.querySelectorAll('.vote-btn').forEach(btn => {
        btn.classList.remove('selected');
    });
    
    // Hide consensus message
    consensusMessage.classList.add('hidden');
    
    // Enable "End voting" button
    endVotingBtn.disabled = false;
    endVotingBtn.title = "";
    
    // Update voting progress
    updateVotingProgress();
}

// Check URL parameters for connection to a room
function checkUrlParams() {
    const urlParams = new URLSearchParams(window.location.search);
    const roomId = urlParams.get('room');
    if (roomId) {
        state.currentRoom = roomId;
        // If a room parameter is present, update the heading
        const loginHeading = document.getElementById('loginHeading');
        if (loginHeading) {
            loginHeading.innerHTML = `Join room: <code class="room-code">${roomId}</code>`;
        }
    }
}

// Add event listeners for value buttons
function setupVoteButtons() {
    document.querySelectorAll('.vote-btn').forEach(button => {
        button.addEventListener('click', function() {
            // If in observer mode, don't allow voting
            if (state.observerMode) {
                return;
            }
            
            // Get value from data-value
            let voteValue = this.getAttribute('data-value');
            
            // Remove selected class from all buttons
            document.querySelectorAll('.vote-btn').forEach(btn => {
                btn.classList.remove('selected');
            });
            
            // Handle cancel vote button (X)
            if (voteValue === 'cancel') {
                // Send a special value to indicate vote cancellation
                socket.emit('cancelVote');
                
                // Don't add selected class to cancel button
                voteError.classList.add('hidden');
                return;
            }
            
            // Add selected class to the chosen button
            this.classList.add('selected');
            
            // Send vote
            if (voteValue === '?') {
                // If the value is a question mark, use a special value
                socket.emit('vote', '?');
            } else {
                // Otherwise, preserve the original string value to avoid rounding issues
                // This is important for decimal values like 0.5
                socket.emit('vote', voteValue);
            }
            
            voteError.classList.add('hidden');
        });
    });
}

// Event listeners
function setupEventListeners() {
    // Login button events
    loginBtn.addEventListener('click', () => handleLogin(false));
    loginAsObserverBtn.addEventListener('click', () => handleLogin(true));
    
    // Add keypress listener for Enter key in login
    nameInput.addEventListener('keypress', function(e) {
        // If Enter key was pressed (code 13)
        if (e.key === 'Enter' || e.keyCode === 13) {
            e.preventDefault(); // Prevent default action (form submission)
            handleLogin(false);  // Call login function (not as observer)
        }
    });
    
    // Action buttons
    endVotingBtn.addEventListener('click', handleEndVoting);
    resetBtn.addEventListener('click', handleResetVoting);
    copyLinkBtn.addEventListener('click', copyRoomLink);
    
    // Setup voting buttons
    setupVoteButtons();
}