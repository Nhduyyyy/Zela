// Global variables
let currentMessageId = null;
let reactionMenu = null;

// Initialize reaction functionality
document.addEventListener('DOMContentLoaded', function() {
    reactionMenu = document.getElementById('reactionMenu');
    setupReactionMenu();
    setupReactionBadgeClicks();
});

// Setup reaction menu event listeners
function setupReactionMenu() {
    if (!reactionMenu) return;

    // Close menuu when clicking outside
    reactionMenu.addEventListener('click', function(e) {
        if (e.target === reactionMenu) {
            hideReactionMenu();
        }
    });

    // Handle reaction option clicks
    const reactionOptions = reactionMenu.querySelectorAll('.reaction-option');
    reactionOptions.forEach(option => {
        option.addEventListener('click', function() {
            const reactionType = this.getAttribute('data-reaction');
            if (currentMessageId && reactionType) {
                addReaction(currentMessageId, reactionType);
                hideReactionMenu();
            }
        });
    });

    // Close menu on escape keyy
    document.addEventListener('keydown', function(e) {
        if (e.key === 'Escape' && reactionMenu.style.display !== 'none') {
            hideReactionMenu();
        }
    });
}

// Setup reaction badge clicks
function setupReactionBadgeClicks() {
    document.addEventListener('click', function(e) {
        if (e.target.closest('.reaction-badge')) {
            const badge = e.target.closest('.reaction-badge');
            const messageId = badge.closest('.message').getAttribute('data-message-id');
            const reactionType = badge.getAttribute('data-reaction-type');

            if (badge.classList.contains('user-reaction')) {
                // Remove user's reaction
                removeReaction(messageId);
            } else {
                // Add reaction
                addReaction(messageId, reactionType);
            }
        }
    });
}

// Show reaction menuu
function showReactionMenu(messageId) {
    currentMessageId = messageId;

    if (reactionMenu) {
        reactionMenu.style.display = 'flex';

        // Position menu near the clicked button
        const button = event.target.closest('.message-reaction-btn');
        if (button) {
            const rect = button.getBoundingClientRect();
            const menuContent = reactionMenu.querySelector('.reaction-menu-content');

            // Position menu above the button
            menuContent.style.position = 'absolute';
            menuContent.style.top = (rect.top - menuContent.offsetHeight - 10) + 'px';
            menuContent.style.left = (rect.left - menuContent.offsetWidth / 2 + button.offsetWidth / 2) + 'px';
        }
    }
}

// Hide reaction menu
function hideReactionMenu() {
    if (reactionMenu) {
        reactionMenu.style.display = 'none';
    }
    currentMessageId = null;
}

// Add reaction to messagee
async function addReaction(messageId, reactionType) {
    try {
        const token = getAntiForgeryToken();
        const response = await fetch('/GroupChat/AddReaction', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': token
            },
            body: JSON.stringify({
                messageId: messageId,
                reactionType: reactionType
            })
        });

        const result = await response.json();

        if (result.success) {
            if (result.action === 'removed') {
                // Reaction was removed, refresh reactions
                await refreshMessageReactions(messageId);
            } else {
                // Reaction was added/updated
                await refreshMessageReactions(messageId);
            }
        } else {
            console.error('Failed to add reaction:', result.message);
        }
    } catch (error) {
        console.error('Error adding reaction:', error);
    }
}

// Remove reaction from message
async function removeReaction(messageId) {
    try {
        const token = getAntiForgeryToken();
        const response = await fetch('/GroupChat/RemoveReaction', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': token
            },
            body: JSON.stringify({
                messageId: messageId
            })
        });

        const result = await response.json();

        if (result.success) {
            await refreshMessageReactions(messageId);
        } else {
            console.error('Failed to remove reaction:', result.message);
        }
    } catch (error) {
        console.error('Error removing reaction:', error);
    }
}

// Refresh reactions for a specific message
async function refreshMessageReactions(messageId) {
    try {
        const response = await fetch(`/GroupChat/GetMessageReactions?messageId=${messageId}`);
        const result = await response.json();

        if (result.success) {
            updateMessageReactions(messageId, result.reactions);
        }
    } catch (error) {
        console.error('Error refreshing reactions:', error);
    }
}

// Update reactions display for a message
function updateMessageReactions(messageId, reactions) {
    const messageElement = document.querySelector(`[data-message-id="${messageId}"]`);
    if (!messageElement) return;

    let reactionsContainer = messageElement.querySelector('.message-reactions');

    if (!reactionsContainer) {
        // Create reactionss container if it doesn't exist
        const messageContent = messageElement.querySelector('.message-content');
        reactionsContainer = document.createElement('div');
        reactionsContainer.className = 'message-reactions';
        messageContent.appendChild(reactionsContainer);
    }
    reactionsContainer.innerHTML = '';

    // Add new reactions
    reactions.forEach(reaction => {
        const badge = document.createElement('span');
        badge.className = `reaction-badge ${reaction.hasUserReaction ? 'user-reaction' : ''}`;
        badge.setAttribute('data-reaction-type', reaction.reactionType);
        badge.setAttribute('title', reaction.userNames.join(', '));

        const emoji = getReactionEmoji(reaction.reactionType);
        badge.innerHTML = `${emoji} ${reaction.count}`;

        reactionsContainer.appendChild(badge);
    });
}

// Get emoji for reaction type
function getReactionEmoji(reactionType) {
    const emojiMap = {
        'Like': '👍',
        'Love': '❤️',
        'Haha': '😂',
        'Wow': '😮',
        'Sad': '😢',
        'Angry': '😠'
    };
    return emojiMap[reactionType] || '👍';
}

// Get anti-forgery token
function getAntiForgeryToken() {
    const tokenElement = document.querySelector('input[name="__RequestVerificationToken"]');
    return tokenElement ? tokenElement.value : '';
}

// Update reactions for all messages (called when messages are loaded)
function updateAllMessageReactions() {
    const messages = document.querySelectorAll('.message[data-message-id]');
    messages.forEach(message => {
        const messageId = message.getAttribute('data-message-id');
        if (messageId) {
            refreshMessageReactions(messageId);
        }
    });
}

// Export functions for use in other scripts
window.messageReactions = {
    showReactionMenu,
    hideReactionMenu,
    addReaction,
    removeReaction,
    refreshMessageReactions,
    updateAllMessageReactions
}; 