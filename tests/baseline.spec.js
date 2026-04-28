// @ts-check
/**
 * Baseline Parity Suite
 *
 * These tests lock down the current user-visible behavior of the Scrum Poker
 * application before the backend migration from Node.js + Socket.io to
 * ASP.NET Core + SignalR.
 *
 * Run the suite to validate parity BEFORE and AFTER the backend cutover.
 * See E2E_TESTING.md for full instructions.
 */

const { test, expect } = require('@playwright/test');
const { RoomPage } = require('./helpers/RoomPage');

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

/**
 * Generate a unique room ID for test isolation.
 * @returns {string}
 */
function uniqueRoomId() {
  return `test${Date.now()}${Math.random().toString(36).slice(2, 6)}`;
}

// ---------------------------------------------------------------------------
// Scenario 1: Create a room and join from another browser context
// ---------------------------------------------------------------------------
test('create a room and join it from another browser context', async ({ browser }) => {
  const roomId = uniqueRoomId();

  // First participant creates the room
  const ctx1 = await browser.newContext();
  const page1 = await ctx1.newPage();
  const room1 = new RoomPage(page1);
  await room1.joinAsParticipant('Alice', roomId);

  const displayedRoom = await room1.getRoomId();
  expect(displayedRoom).toBe(roomId);

  // Second participant joins via the shared URL
  const ctx2 = await browser.newContext();
  const page2 = await ctx2.newPage();
  const room2 = new RoomPage(page2);
  await room2.joinAsParticipant('Bob', roomId);

  // Both participants should see each other in the user list
  await room1.waitForUserInList('Bob');
  await room2.waitForUserInList('Alice');

  // Progress should reflect 2 voters who have not yet voted
  await room1.waitForProgressText('0 out of 2 users have voted');
  await room2.waitForProgressText('0 out of 2 users have voted');

  await ctx1.close();
  await ctx2.close();
});

// ---------------------------------------------------------------------------
// Scenario 2: Join as observer and verify voting is disabled
// ---------------------------------------------------------------------------
test('join as observer and verify voting is disabled', async ({ browser }) => {
  const roomId = uniqueRoomId();

  // Regular participant creates the room
  const ctx1 = await browser.newContext();
  const page1 = await ctx1.newPage();
  const room1 = new RoomPage(page1);
  await room1.joinAsParticipant('Alice', roomId);

  // Observer joins the same room
  const ctx2 = await browser.newContext();
  const page2 = await ctx2.newPage();
  const room2 = new RoomPage(page2);
  await room2.joinAsObserver('Observer1', roomId);

  // Observer badge should be visible for the observer's page
  expect(await room2.isObserverBadgeVisible()).toBe(true);

  // All vote buttons should be disabled for the observer
  expect(await room2.areVoteButtonsDisabled()).toBe(true);

  // Observer should NOT appear in the voter list shown on Alice's page
  const aliceList = await room1.getUserListTexts();
  const observerInList = aliceList.some((t) => t.includes('Observer1'));
  expect(observerInList).toBe(false);

  // Observer badge should NOT be visible for Alice
  expect(await room1.isObserverBadgeVisible()).toBe(false);

  await ctx1.close();
  await ctx2.close();
});

// ---------------------------------------------------------------------------
// Scenario 3: Join two users with the same requested name and verify server-side renaming
// ---------------------------------------------------------------------------
test('join two users with the same name and verify server-side renaming', async ({ browser }) => {
  const roomId = uniqueRoomId();

  const ctx1 = await browser.newContext();
  const page1 = await ctx1.newPage();
  const room1 = new RoomPage(page1);
  await room1.joinAsParticipant('Alice', roomId);

  const ctx2 = await browser.newContext();
  const page2 = await ctx2.newPage();
  const room2 = new RoomPage(page2);
  await room2.joinAsParticipant('Alice', roomId);

  // The second Alice should receive a server-assigned unique name
  await expect(page2.locator('#currentUserName')).toHaveText('Alice (2)', { timeout: 5000 });
  const resolvedName = await room2.getCurrentUserName();
  expect(resolvedName).toBe('Alice (2)');

  // The first Alice should still be 'Alice'
  expect(await room1.getCurrentUserName()).toBe('Alice');

  // Both users should appear in the list on each page
  await room1.waitForUserInList('Alice (2)');
  await room2.waitForUserInList('Alice (2)');

  await ctx1.close();
  await ctx2.close();
});

// ---------------------------------------------------------------------------
// Scenario 4: Submit votes and verify hidden-vote behavior before reveal
// ---------------------------------------------------------------------------
test('submit votes and verify hidden-vote behavior before reveal', async ({ browser }) => {
  const roomId = uniqueRoomId();

  const ctx1 = await browser.newContext();
  const page1 = await ctx1.newPage();
  const room1 = new RoomPage(page1);
  await room1.joinAsParticipant('Alice', roomId);

  const ctx2 = await browser.newContext();
  const page2 = await ctx2.newPage();
  const room2 = new RoomPage(page2);
  await room2.joinAsParticipant('Bob', roomId);

  // Alice votes 5
  await room1.vote('5');

  // Alice sees her own vote in her list
  await room1.waitForUserInList('Alice (current user): 5');

  // Bob should see "✅ Voted" for Alice, not the actual value
  await room2.waitForUserInList('✅ Voted');
  const bobList = await room2.getUserListTexts();
  const aliceEntry = bobList.find((t) => t.includes('Alice'));
  expect(aliceEntry).toBeDefined();
  expect(aliceEntry).toContain('✅ Voted');
  expect(aliceEntry).not.toContain(': 5');

  // Bob has not voted yet – Alice should see the waiting indicator
  await room1.waitForUserInList('⌛ Not voted yet');

  await ctx1.close();
  await ctx2.close();
});

// ---------------------------------------------------------------------------
// Scenario 5: Reveal voting and verify all votes become visible
// ---------------------------------------------------------------------------
test('reveal voting and verify all votes become visible', async ({ browser }) => {
  const roomId = uniqueRoomId();

  const ctx1 = await browser.newContext();
  const page1 = await ctx1.newPage();
  const room1 = new RoomPage(page1);
  await room1.joinAsParticipant('Alice', roomId);

  const ctx2 = await browser.newContext();
  const page2 = await ctx2.newPage();
  const room2 = new RoomPage(page2);
  await room2.joinAsParticipant('Bob', roomId);

  await room1.vote('5');
  await room2.vote('8');

  // Reveal votes
  await room1.revealVotes();

  // Both pages should reflect that voting has ended
  await room1.waitForVotingEnded();
  await room2.waitForVotingEnded();

  // Both vote values should now be visible on both pages
  await room1.waitForUserInList(': 5');
  await room1.waitForUserInList(': 8');
  await room2.waitForUserInList(': 5');
  await room2.waitForUserInList(': 8');

  // Show voting button should be disabled on both pages
  expect(await page1.locator('#endVotingBtn').isDisabled()).toBe(true);
  expect(await page2.locator('#endVotingBtn').isDisabled()).toBe(true);

  await ctx1.close();
  await ctx2.close();
});

// ---------------------------------------------------------------------------
// Scenario 6: Statistics appear after reveal and ignore '?'
// ---------------------------------------------------------------------------
test('statistics appear after reveal and ignore question-mark votes', async ({ browser }) => {
  const roomId = uniqueRoomId();

  const ctx1 = await browser.newContext();
  const page1 = await ctx1.newPage();
  const room1 = new RoomPage(page1);
  await room1.joinAsParticipant('Alice', roomId);

  const ctx2 = await browser.newContext();
  const page2 = await ctx2.newPage();
  const room2 = new RoomPage(page2);
  await room2.joinAsParticipant('Bob', roomId);

  const ctx3 = await browser.newContext();
  const page3 = await ctx3.newPage();
  const room3 = new RoomPage(page3);
  await room3.joinAsParticipant('Carol', roomId);

  // Alice = 3, Bob = 5, Carol = ?
  await room1.vote('3');
  await room2.vote('5');
  await room3.vote('?');

  await room1.revealVotes();
  await room1.waitForVotingEnded();
  await room2.waitForVotingEnded();

  // Statistics should be visible
  expect(await room1.areStatsVisible()).toBe(true);

  // Average of 3 and 5 (? excluded) = 4
  expect(await room1.getAverageVote()).toBe('4');

  // Min = 3 (differs from average → names appended), Max = 5 (differs from average → names appended)
  expect(await room1.getMinVote()).toContain('3');
  expect(await room1.getMaxVote()).toContain('5');

  // Stats should also be visible on Bob's page
  expect(await room2.areStatsVisible()).toBe(true);

  await ctx1.close();
  await ctx2.close();
  await ctx3.close();
});

// ---------------------------------------------------------------------------
// Scenario 7: Reset voting and verify votes are cleared for all clients
// ---------------------------------------------------------------------------
test('reset voting and verify votes are cleared for all clients', async ({ browser }) => {
  const roomId = uniqueRoomId();

  const ctx1 = await browser.newContext();
  const page1 = await ctx1.newPage();
  const room1 = new RoomPage(page1);
  await room1.joinAsParticipant('Alice', roomId);

  const ctx2 = await browser.newContext();
  const page2 = await ctx2.newPage();
  const room2 = new RoomPage(page2);
  await room2.joinAsParticipant('Bob', roomId);

  await room1.vote('5');
  await room2.vote('8');
  await room1.revealVotes();
  await room1.waitForVotingEnded();

  // Reset voting
  await room1.resetVoting();

  // Both pages should return to the pre-voting state
  await room1.waitForVotingReset();
  await room2.waitForVotingReset();

  // Statistics panel should be hidden on both pages
  await expect(page1.locator('#votingStats')).toBeHidden({ timeout: 5000 });
  await expect(page2.locator('#votingStats')).toBeHidden({ timeout: 5000 });

  // Consensus message should be hidden
  await expect(page1.locator('#consensusMessage')).toBeHidden({ timeout: 5000 });
  await expect(page2.locator('#consensusMessage')).toBeHidden({ timeout: 5000 });

  // All participants should show "Not voted yet"
  await room1.waitForUserInList('⌛ Not voted yet');
  await room2.waitForUserInList('⌛ Not voted yet');

  // Voting buttons should be re-enabled
  await expect(page1.locator('.vote-btn').first()).toBeEnabled({ timeout: 5000 });
  await expect(page2.locator('.vote-btn').first()).toBeEnabled({ timeout: 5000 });

  await ctx1.close();
  await ctx2.close();
});

// ---------------------------------------------------------------------------
// Scenario 8: Remove a participant as observer and verify forced disconnect
// ---------------------------------------------------------------------------
test('remove a participant as observer and verify forced disconnect', async ({ browser }) => {
  const roomId = uniqueRoomId();

  // Regular participant
  const ctx1 = await browser.newContext();
  const page1 = await ctx1.newPage();
  const room1 = new RoomPage(page1);
  await room1.joinAsParticipant('Alice', roomId);

  // Observer
  const ctx2 = await browser.newContext();
  const page2 = await ctx2.newPage();
  const room2 = new RoomPage(page2);
  await room2.joinAsObserver('Observer1', roomId);

  // Observer should see Alice in the user list with a remove button
  await room2.waitForUserInList('Alice');

  // Set up alert handler for Alice BEFORE the remove action
  const alertPromise = room1.acceptAlert();

  // Observer removes Alice
  await room2.removeParticipantByName('Alice');

  // Alice should receive the forced disconnect alert
  const alertMessage = await alertPromise;
  expect(alertMessage).toMatch(/removed from room/i);

  // Alice's page should return to the login screen
  await room1.waitForLoginContainer();

  // Observer's user list should no longer contain Alice
  await room2.waitForUserRemovedFromList('Alice');

  await ctx1.close();
  await ctx2.close();
});

// ---------------------------------------------------------------------------
// Scenario 9: Consensus message appears only when more than one numeric vote is equal
// ---------------------------------------------------------------------------
test('consensus message appears only when more than one equal numeric vote', async ({ browser }) => {
  const roomId = uniqueRoomId();

  // --- Case A: single voter – no consensus ---
  const ctxA1 = await browser.newContext();
  const pageA1 = await ctxA1.newPage();
  const roomA1 = new RoomPage(pageA1);
  await roomA1.joinAsParticipant('Solo', roomId);
  await roomA1.vote('5');
  await roomA1.revealVotes();
  await roomA1.waitForVotingEnded();

  expect(await roomA1.isConsensusVisible()).toBe(false);

  await ctxA1.close();

  // --- Case B: two voters with different values – no consensus ---
  const roomId2 = uniqueRoomId();
  const ctxB1 = await browser.newContext();
  const pageB1 = await ctxB1.newPage();
  const roomB1 = new RoomPage(pageB1);
  await roomB1.joinAsParticipant('Alice', roomId2);

  const ctxB2 = await browser.newContext();
  const pageB2 = await ctxB2.newPage();
  const roomB2 = new RoomPage(pageB2);
  await roomB2.joinAsParticipant('Bob', roomId2);

  await roomB1.vote('3');
  await roomB2.vote('5');
  await roomB1.revealVotes();
  await roomB1.waitForVotingEnded();

  expect(await roomB1.isConsensusVisible()).toBe(false);

  await ctxB1.close();
  await ctxB2.close();

  // --- Case C: two voters with the SAME value – consensus expected ---
  const roomId3 = uniqueRoomId();
  const ctxC1 = await browser.newContext();
  const pageC1 = await ctxC1.newPage();
  const roomC1 = new RoomPage(pageC1);
  await roomC1.joinAsParticipant('Alice', roomId3);

  const ctxC2 = await browser.newContext();
  const pageC2 = await ctxC2.newPage();
  const roomC2 = new RoomPage(pageC2);
  await roomC2.joinAsParticipant('Bob', roomId3);

  await roomC1.vote('5');
  await roomC2.vote('5');
  await roomC1.revealVotes();
  await roomC1.waitForVotingEnded();
  await roomC2.waitForVotingEnded();

  // Consensus message must appear on both pages
  await expect(pageC1.locator('#consensusMessage')).toBeVisible({ timeout: 10000 });
  await expect(pageC2.locator('#consensusMessage')).toBeVisible({ timeout: 10000 });

  await ctxC1.close();
  await ctxC2.close();
});
