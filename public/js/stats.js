// Function to update voting statistics
function updateVotingStats() {
    // Hide consensus message
    consensusMessage.classList.add('hidden');
    
    // Show statistics only if voting has ended
    if (state.votingEnded) {
        // Disable voting buttons when voting has ended
        document.querySelectorAll('.vote-btn').forEach(btn => {
            btn.disabled = true;
        });
        
        // Filter out observers and get only active voters' votes
        const votes = state.users
            .filter(user => !user.isObserver && user.vote !== null && user.vote !== '?')
            .map(user => typeof user.vote === 'number' ? user.vote : parseFloat(user.vote))
            .filter(vote => !isNaN(vote));
        
        if (votes.length > 0) {
            // Check if everyone voted the same
            const firstVote = votes[0];
            const allSameVote = votes.every(vote => vote === firstVote);
            
            // If there's consensus and more than 1 person voted, show message and confetti
            if (allSameVote && votes.length > 1) {
                consensusMessage.classList.remove('hidden');
                triggerConfetti();
            }
              // Average
            const sum = votes.reduce((acc, vote) => acc + vote, 0);
            const average = sum / votes.length;
            avgVote.textContent = average.toString(); // Don't round
            
            // Min and Max with people
            const minValue = Math.min(...votes);
            const maxValue = Math.max(...votes);
            
            // Get names of users with min and max values (excluding observers)
            const usersWithMin = state.users
                .filter(user => !user.isObserver && parseFloat(user.vote) === minValue)
                .map(user => user.name);
            
            const usersWithMax = state.users
                .filter(user => !user.isObserver && parseFloat(user.vote) === maxValue)
                .map(user => user.name);
            
            // Basic display of min and max values
            minVote.textContent = minValue.toString(); // Don't round
            maxVote.textContent = maxValue.toString(); // Don't round
            
            // Add names if values differ from average
            if (minValue < average && usersWithMin.length > 0) {
                minVote.textContent += ` (${usersWithMin.join(', ')})`;
            }
            
            if (maxValue > average && usersWithMax.length > 0) {
                maxVote.textContent += ` (${usersWithMax.join(', ')})`;
            }
            
            // Show statistics
            votingStats.classList.remove('hidden');
        } else {
            votingStats.classList.add('hidden');
        }
    } else {
        // Re-enable voting buttons when starting a new vote
        if (!state.observerMode) {
            document.querySelectorAll('.vote-btn').forEach(btn => {
                btn.disabled = false;
            });
        }
        
        // Hide statistics if voting is not ended
        votingStats.classList.add('hidden');
    }
}