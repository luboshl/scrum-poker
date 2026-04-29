// Application state
const state = {
    currentUser: null,
    currentRoom: null,
    users: [],
    votingEnded: false,
    connected: false,
    observerMode: false
};

// DOM elements
const statusIndicator = document.getElementById('statusIndicator');
const loginContainer = document.getElementById('loginContainer');
const votingContainer = document.getElementById('votingContainer');
const nameInput = document.getElementById('nameInput');
const loginBtn = document.getElementById('loginBtn');
const loginAsObserverBtn = document.getElementById('loginAsObserverBtn');
const loginError = document.getElementById('loginError');
const roomIdDisplay = document.getElementById('roomIdDisplay');
const copyLinkBtn = document.getElementById('copyLinkBtn');
const currentUserName = document.getElementById('currentUserName');
const observerBadge = document.getElementById('observerBadge');
const votingButtons = document.getElementById('votingButtons');
const voteError = document.getElementById('voteError');
const endVotingBtn = document.getElementById('endVotingBtn');
const resetBtn = document.getElementById('resetBtn');
const usersListContainer = document.getElementById('usersListContainer');
const votingStats = document.getElementById('votingStats');
const consensusMessage = document.getElementById('consensusMessage');
const avgVote = document.getElementById('avgVote');
const minVote = document.getElementById('minVote');
const maxVote = document.getElementById('maxVote');

// Setting heartbeat interval for activity checking
let heartbeatInterval;