// Function to update connection status
function updateConnectionStatus() {
    if (state.connected) {
        statusIndicator.textContent = 'Connected to server';
        statusIndicator.classList.remove('disconnected');
        statusIndicator.classList.add('connected');
        loginBtn.disabled = false;
        loginAsObserverBtn.disabled = false;
    } else {
        statusIndicator.textContent = 'Disconnected from server';
        statusIndicator.classList.remove('connected');
        statusIndicator.classList.add('disconnected');
        loginBtn.disabled = true;
        loginAsObserverBtn.disabled = true;
    }
}

// Function to update end voting button state
function updateEndVotingButton() {
    // If voting is ended, disable the button
    if (state.votingEnded) {
        endVotingBtn.disabled = true;
        endVotingBtn.title = "Voting has already ended";
    } else {
        endVotingBtn.disabled = false;
        endVotingBtn.title = "";
    }
}

// Function to disable voting buttons for observers
function disableVotingButtons() {
    document.querySelectorAll('.vote-btn').forEach(btn => {
        btn.disabled = true;
        btn.title = "Observers cannot vote";
    });
}

// Function to copy link
function copyRoomLink() {
    const url = window.location.href;
    navigator.clipboard.writeText(url).then(() => {
        const originalText = copyLinkBtn.textContent;
        copyLinkBtn.textContent = 'Copied!';
        setTimeout(() => {
            copyLinkBtn.textContent = originalText;
        }, 2000);
    });
}

// Function to display confetti
function triggerConfetti() {
    confetti({
        particleCount: 200,
        spread: 70,
        origin: { y: 0.6 },
        disableForReducedMotion: true
    });
    
    // For greater effect, launch multiple confetti with delay
    setTimeout(() => {
        confetti({
            particleCount: 100,
            angle: 60,
            spread: 55,
            origin: { x: 0 },
            colors: ['#ff0000', '#00ff00', '#0000ff']
        });
    }, 500);
    
    setTimeout(() => {
        confetti({
            particleCount: 100,
            angle: 120,
            spread: 55,
            origin: { x: 1 },
            colors: ['#ff9800', '#9c27b0', '#ffeb3b']
        });
    }, 1000);
}