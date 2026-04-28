// @ts-check
const { expect } = require('@playwright/test');

/**
 * Page object helper that encapsulates interactions with the Scrum Poker application.
 * Wraps Playwright Page actions in role-oriented methods so tests read at the
 * domain level rather than at the CSS-selector level.
 */
class RoomPage {
  /**
   * @param {import('@playwright/test').Page} page
   */
  constructor(page) {
    this.page = page;
  }

  // ---------------------------------------------------------------------------
  // Navigation
  // ---------------------------------------------------------------------------

  /**
   * Navigate to the application home page.
   * Waits for the Socket.io connection to be established before returning so
   * that subsequent actions (typing a name, clicking Join) can rely on the
   * socket being ready.
   * @param {string} [roomId] - Optional room ID to pre-fill via the ?room= query parameter.
   */
  async goto(roomId) {
    const url = roomId ? `/?room=${roomId}` : '/';
    await this.page.goto(url);
    await this.page.waitForLoadState('domcontentloaded');
    // Wait until the socket is connected (status indicator changes from "Disconnected")
    await expect(this.page.locator('#statusIndicator')).toContainText('Connected', { timeout: 15000 });
  }

  // ---------------------------------------------------------------------------
  // Login / Join
  // ---------------------------------------------------------------------------

  /**
   * Type a name into the name input without submitting.
   * @param {string} name
   */
  async typeName(name) {
    await this.page.getByPlaceholder('Enter your name').fill(name);
  }

  /**
   * Join the room as a regular participant.
   * @param {string} name
   * @param {string} [roomId] - When provided, navigate directly to that room URL first.
   */
  async joinAsParticipant(name, roomId) {
    await this.goto(roomId);
    await this.typeName(name);
    await this.page.getByRole('button', { name: 'Join', exact: true }).click();
    await this.waitForVotingContainer();
  }

  /**
   * Join the room as an observer.
   * @param {string} name
   * @param {string} [roomId]
   */
  async joinAsObserver(name, roomId) {
    await this.goto(roomId);
    await this.typeName(name);
    await this.page.getByRole('button', { name: 'Join as Observer' }).click();
    await this.waitForVotingContainer();
  }

  // ---------------------------------------------------------------------------
  // Voting
  // ---------------------------------------------------------------------------

  /**
   * Cast a vote by clicking the vote button with the given label.
   * @param {string} value - The label on the vote button, e.g. '5', '?', 'X'.
   */
  async vote(value) {
    // Escape special regex characters so values like '?' work correctly.
    const escaped = value.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
    await this.page.locator('.vote-btn').filter({ hasText: new RegExp(`^${escaped}$`) }).click();
  }

  /**
   * Click the "Show voting" button to reveal all votes.
   */
  async revealVotes() {
    await this.page.locator('#endVotingBtn').click();
  }

  /**
   * Click the "Clear voting" button to reset the round.
   */
  async resetVoting() {
    await this.page.getByRole('button', { name: 'Clear voting' }).click();
  }

  // ---------------------------------------------------------------------------
  // Observer actions
  // ---------------------------------------------------------------------------

  /**
   * Remove a participant (only available when the current page is logged in as observer).
   * Accepts the browser confirm() dialog automatically.
   * @param {string} name - Display name of the participant to remove.
   */
  async removeParticipant(name) {
    this.page.once('dialog', (dialog) => dialog.accept());
    await this.page.locator('.remove-user').filter({ hasText: '×' }).nth(0).click();
  }

  /**
   * Remove a participant by their specific name.
   * @param {string} name - Display name of the participant to remove.
   */
  async removeParticipantByName(name) {
    this.page.once('dialog', (dialog) => dialog.accept());
    const listItem = this.page.locator('.user-item').filter({ hasText: name });
    await listItem.locator('.remove-user').click();
  }

  // ---------------------------------------------------------------------------
  // Assertions / queries
  // ---------------------------------------------------------------------------

  /**
   * Wait until the voting container is visible (i.e. the user has joined a room).
   * Also waits for the first server-side roomUpdate to arrive so that subsequent
   * socket events (vote, endVoting) are processed in the correct server context.
   */
  async waitForVotingContainer() {
    await expect(this.page.locator('#votingContainer')).toBeVisible({ timeout: 10000 });
    // Wait for the server to acknowledge the join by receiving the first roomUpdate.
    // When at least one non-observer participant is in the room the progress text
    // will be different from the initial "0 out of 0 users have voted".
    await expect(this.page.locator('#progressText')).not.toHaveText(
      '0 out of 0 users have voted',
      { timeout: 10000 }
    );
  }

  /**
   * Wait until the login container is visible (i.e. the user has been returned to the login screen).
   */
  async waitForLoginContainer() {
    await expect(this.page.locator('#loginContainer')).toBeVisible({ timeout: 10000 });
  }

  /**
   * Return the resolved user name shown in the room header.
   * @returns {Promise<string>}
   */
  async getCurrentUserName() {
    return this.page.locator('#currentUserName').innerText();
  }

  /**
   * Return the room ID currently displayed in the room header.
   * @returns {Promise<string>}
   */
  async getRoomId() {
    return this.page.locator('#roomIdDisplay').innerText();
  }

  /**
   * Return the text content of the users list.
   * @returns {Promise<string[]>}
   */
  async getUserListTexts() {
    return this.page.locator('#usersListContainer .user-item span').allInnerTexts();
  }

  /**
   * Return true if the observer badge is visible.
   * @returns {Promise<boolean>}
   */
  async isObserverBadgeVisible() {
    return this.page.locator('#observerBadge').isVisible();
  }

  /**
   * Return true if all vote buttons are disabled (observer or voting ended).
   * @returns {Promise<boolean>}
   */
  async areVoteButtonsDisabled() {
    const buttons = this.page.locator('.vote-btn');
    const count = await buttons.count();
    for (let i = 0; i < count; i++) {
      if (!(await buttons.nth(i).isDisabled())) {
        return false;
      }
    }
    return count > 0;
  }

  /**
   * Return true if the statistics panel is visible.
   * @returns {Promise<boolean>}
   */
  async areStatsVisible() {
    return this.page.locator('#votingStats').isVisible();
  }

  /**
   * Return the average vote text from the statistics panel.
   * @returns {Promise<string>}
   */
  async getAverageVote() {
    return this.page.locator('#avgVote').innerText();
  }

  /**
   * Return the minimum vote text from the statistics panel.
   * @returns {Promise<string>}
   */
  async getMinVote() {
    return this.page.locator('#minVote').innerText();
  }

  /**
   * Return the maximum vote text from the statistics panel.
   * @returns {Promise<string>}
   */
  async getMaxVote() {
    return this.page.locator('#maxVote').innerText();
  }

  /**
   * Return true if the consensus message is visible.
   * @returns {Promise<boolean>}
   */
  async isConsensusVisible() {
    return this.page.locator('#consensusMessage').isVisible();
  }

  /**
   * Return the text of the connection status indicator.
   * @returns {Promise<string>}
   */
  async getConnectionStatus() {
    return this.page.locator('#statusIndicator').innerText();
  }

  /**
   * Return the progress bar text.
   * @returns {Promise<string>}
   */
  async getProgressText() {
    return this.page.locator('#progressText').innerText();
  }

  /**
   * Wait until the progress text matches a specific string.
   * @param {string} text
   */
  async waitForProgressText(text) {
    await expect(this.page.locator('#progressText')).toHaveText(text, { timeout: 10000 });
  }

  /**
   * Wait until the users list contains a participant entry matching the text.
   * @param {string} text
   */
  async waitForUserInList(text) {
    await expect(this.page.locator('#usersListContainer')).toContainText(text, { timeout: 10000 });
  }

  /**
   * Wait until the users list no longer contains text matching the given string.
   * @param {string} text
   */
  async waitForUserRemovedFromList(text) {
    await expect(this.page.locator('#usersListContainer')).not.toContainText(text, { timeout: 10000 });
  }

  /**
   * Wait until voting has ended (stats panel visible or votingEnded UI state).
   */
  async waitForVotingEnded() {
    await expect(this.page.locator('#endVotingBtn')).toBeDisabled({ timeout: 10000 });
  }

  /**
   * Wait until voting has been reset (Show voting button enabled again).
   */
  async waitForVotingReset() {
    await expect(this.page.locator('#endVotingBtn')).toBeEnabled({ timeout: 10000 });
  }

  /**
   * Wait until an alert dialog appears and dismiss it.
   * @returns {Promise<string>} The message shown in the alert.
   */
  async acceptAlert() {
    return new Promise((resolve) => {
      this.page.once('dialog', (dialog) => {
        const msg = dialog.message();
        dialog.accept();
        resolve(msg);
      });
    });
  }
}

module.exports = { RoomPage };
