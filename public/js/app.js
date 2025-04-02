// Main application initialization
document.addEventListener('DOMContentLoaded', function() {
    // Set up event listeners
    setupEventListeners();
    
    // On page load
    window.addEventListener('load', function() {
        // Check URL parameters on page load
        checkUrlParams();
        
        // Update connection status
        updateConnectionStatus();
        
        // Update end voting button state
        updateEndVotingButton();
        
        // Update voting progress
        updateVotingProgress();
        
        // Set page title based on room ID
        if (state.currentRoom) {
            document.title = `Scrum Poker (${state.currentRoom})`;
        } else {
            document.title = "Scrum Poker";
        }
        
        // If the page loads with room parameters, send heartbeat
        if (state.currentRoom && state.connected) {
            sendHeartbeat();
        }
        
        // On window/tab close
        window.addEventListener('beforeunload', function() {
            if (heartbeatInterval) {
                clearInterval(heartbeatInterval);
            }
        });
    });
});