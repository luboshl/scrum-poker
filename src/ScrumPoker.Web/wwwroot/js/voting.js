// Function to update voting progress
function updateVotingProgress() {
    // Get count of voters (excluding observers)
    const voters = state.users.filter(user => !user.isObserver);
    const totalVoters = voters.length;
    
    // Get count of users who have already voted
    const votedUsers = voters.filter(user => user.vote !== null);
    const votedCount = votedUsers.length;
    
    // Calculate percentage
    const percentage = totalVoters > 0 ? Math.round((votedCount / totalVoters) * 100) : 0;
    
    // Update progress bar
    const progressBar = document.getElementById('progressBar');
    progressBar.style.width = `${percentage}%`;
    progressBar.textContent = `${percentage}%`;
    
    // Update text
    const progressText = document.getElementById('progressText');
    progressText.textContent = `${votedCount} out of ${totalVoters} users have voted`;
    
    // Change color - only green for 100%, orange for all other values
    if (percentage === 100) {
        progressBar.style.backgroundColor = '#4CAF50'; // green for 100%
    } else {
        progressBar.style.backgroundColor = '#ff9800'; // orange for everything else
    }
}