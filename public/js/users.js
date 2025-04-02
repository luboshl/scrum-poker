// Function to update user list
function updateUsersList() {
    usersListContainer.innerHTML = '';
    
    // Create a copy of the user list
    let sortedUsers = [...state.users];
    
    // Filter out observers - they should not be in the voting list
    sortedUsers = sortedUsers.filter(user => !user.isObserver);
    
    // Sort users - current user first, others alphabetically
    sortedUsers.sort((a, b) => {
        if (a.name === state.currentUser) return -1;
        if (b.name === state.currentUser) return 1;
        return a.name.localeCompare(b.name);
    });
    
    sortedUsers.forEach(user => {
        const isCurrentUser = user.name === state.currentUser;
        
        // Create a simple li element
        const listItem = document.createElement('li');
        
        // Mark current user
        if (isCurrentUser) {
            listItem.className = 'user-item current-user';
        } else {
            listItem.className = 'user-item';
        }
        
        // Build item text
        let itemText = user.name;
        if (isCurrentUser) {
            itemText += ' (current user)';
        }
        
        // Voting information
        if (user.vote !== null) {
            if (state.votingEnded) {
                // Show vote only when voting has ended
                itemText += `: ${user.vote}`;
            } else if (user.name === state.currentUser) {
                // For current user show their own vote
                itemText += `: ${user.vote}`;
            } else {
                // For other users before voting ends, show only that they voted
                itemText += ': ✅ Voted';
                listItem.classList.add('pending-vote');
            }
        } else {
            // Not voted yet
            itemText += ': ⌛ Not voted yet';
            listItem.classList.add('pending-vote');
        }
        
        // Create a span for the user info
        const userInfoSpan = document.createElement('span');
        userInfoSpan.textContent = itemText;
        
        // Add to list item
        listItem.appendChild(userInfoSpan);
        
        // Add remove button (×) ONLY if current user is an observer AND not trying to remove themselves
        if (!isCurrentUser && state.observerMode) {
            const removeButton = document.createElement('span');
            removeButton.textContent = '×';
            removeButton.className = 'remove-user';
            removeButton.title = `Remove ${user.name}`;
            
            // Add click event listener
            removeButton.addEventListener('click', function(e) {
                e.stopPropagation(); // Prevent event bubbling
                if (confirm(`Are you sure you want to remove ${user.name}?`)) {
                    socket.emit('removeUser', user.name);
                }
            });
            
            listItem.appendChild(removeButton);
        }
        
        // Add to list
        usersListContainer.appendChild(listItem);
    });
}