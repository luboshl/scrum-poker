# Scrum Poker Application Behavior Specification

## Purpose

This document captures the current behavior of the application before the backend migration from Node.js + Socket.io to ASP.NET Core + SignalR.

The goal is behavior parity, not a redesign. After the migration, the application should be verifiable against this specification so that existing users experience the same workflows, business rules, and realtime behavior unless a later issue explicitly changes them.

This document describes the current implementation as it behaves today.

## Source of Truth

The current behavior described here is based on the implementation in:

- `server.js`
- `public/index.html`
- `public/js/app.js`
- `public/js/event-handlers.js`
- `public/js/socket-handlers.js`
- `public/js/ui-elements.js`
- `public/js/users.js`
- `public/js/stats.js`
- `public/js/utils.js`
- `public/js/voting.js`

## High-Level Overview

The application is a realtime Scrum Poker tool for collaborative estimation.

Current architecture:

- Backend: Node.js, Express, Socket.io
- Frontend: static HTML, CSS, and vanilla JavaScript
- State storage: in-memory only inside the server process
- Deployment shape: single running instance is the safe current model

Key implications:

- A process restart clears all rooms and votes.
- No database is used.
- No authentication or accounts exist.
- The application is intended for short-lived, lightweight collaborative sessions.

## Roles and Core Concepts

### Roles

The application supports two participant modes:

- Regular participant
- Observer

Regular participant:

- can join a room
- can vote
- can cancel their vote
- can see their own vote before voting ends
- can see all votes after voting ends

Observer:

- can join a room
- cannot vote
- cannot cancel a vote
- can remove other users from the room
- is not listed in the voting list shown in the main room UI

### Core Concepts

Room:

- A session identified by a room ID.
- Created on demand when a participant joins a room that does not exist.

Participant:

- Has a display name.
- Has a connection ID tied to the current socket/session.
- Has an `isObserver` flag.
- Has an `active` flag.
- Has a nullable vote value.

Vote:

- Allowed values are the UI card values:
  - `0`
  - `0.5`
  - `1`
  - `2`
  - `3`
  - `5`
  - `8`
  - `13`
  - `20`
  - `40`
  - `100`
  - `?`
- The `X` button is not a vote value; it cancels an existing vote.
- Votes are stored as sent by the client, preserving string values like `0.5` and `?`.

Voting round:

- A room starts with `votingEnded = false`.
- When voting is ended, votes are revealed and statistics become visible.
- When voting is reset, all participant votes are set back to `null` and `votingEnded` becomes `false`.

## Supported User Interface and Controls

## Login Screen

Visible before a user joins a room.

Controls:

- Name input field
- `Join` button
- `Join as Observer` button
- Validation error: `Please enter your name`

Behavior:

- If the name is blank after trimming, login is blocked and the error message is shown.
- Pressing Enter in the name input triggers the same behavior as clicking `Join`.
- Pressing Enter does not join as observer.

## Room Screen

Visible after a join action is initiated.

Controls and regions:

- Room ID display
- `Copy link` button
- Current user display
- Observer badge
- Vote card buttons
- `Show voting` button
- `Clear voting` button
- Progress bar and text
- Consensus message
- Statistics panel
- Users list
- Connection status footer

## Vote Cards

Available cards:

- `0`
- `0.5`
- `1`
- `2`
- `3`
- `5`
- `8`
- `13`
- `20`
- `40`
- `100`
- `?`
- `X` as cancel vote

Behavior:

- Clicking a card sends the vote to the server.
- Clicking `?` sends a literal `?` vote.
- Clicking `X` cancels the current vote.
- Before sending a new vote, all vote buttons have their `selected` visual state cleared.
- A selected vote button gets the `selected` visual state, except for `X`, which never stays selected.

## Current URL and Room Linking

Room sharing behavior:

- When a user joins or creates a room, the browser URL is updated with the `room` query parameter.
- Example shape: `?room=<roomId>`
- `Copy link` copies the full current URL to the clipboard.
- After successful clipboard write, the button text temporarily changes to `Copied!` for about 2 seconds.

Behavior when the page is opened with a room query parameter:

- The login heading changes from `Create new room` to `Join room: <roomId>`.
- `state.currentRoom` is prefilled from the URL.
- On page load, the document title becomes `Scrum Poker (<roomId>)` if a room parameter already exists.

Important note:

- The current implementation updates the document title on page load based on the URL state.
- Creating a room after page load does not have an explicit title refresh in the current code.

## Connection Status UI

Footer status values:

- `Connected to server`
- `Disconnected from server`

Behavior:

- Login buttons are disabled while the client is disconnected.
- Login buttons are enabled when the client is connected.

## Realtime Server Events and Client Reactions

## Server-Initiated Events

### `roomUpdate`

Payload contains:

- current users array
- `votingEnded`

Client reaction:

- replaces local users state
- replaces local `votingEnded` state
- rerenders user list
- recalculates voting statistics
- updates the `Show voting` button state
- updates voting progress

### `joinConfirmation`

Payload contains:

- resolved user name
- observer flag

Client reaction:

- replaces the local current user name with the resolved server-side name
- updates the displayed current user name
- applies observer mode if needed

### `resetVoting`

Client reaction:

- clears selected visual state from vote buttons on all clients
- updates progress UI

### `userRemoved`

This is sent to the client that requested a remove operation.

Payload contains:

- `status`
- `userName`
- optional error `message`

Client reaction:

- logs success or failure to the console
- no visible toast or inline message is currently shown

### `forcedDisconnect`

Sent to a user removed by an observer.

Payload contains:

- `message`

Client reaction:

- shows `alert(message)`
- clears local user, room, and vote state
- returns UI to login screen
- clears selected vote button styling

## Client-Initiated Actions and Business Rules

## Use Case 1: Create a Room as Regular Participant

Steps:

1. User opens the application without a `room` query parameter.
2. User enters a non-empty name.
3. User clicks `Join` or presses Enter.
4. Client generates a random 10-character lowercase alphanumeric room ID.
5. Client sends `joinRoom` with room ID, entered name, and `isObserver = false`.
6. Client updates the URL with `?room=<generatedRoomId>`.
7. Client shows the room screen immediately.

Expected results:

- If the room does not exist, the server creates it.
- The user is added to the room as an active non-observer participant.
- The room receives a `roomUpdate` event.
- The joining client receives `joinConfirmation`.

## Use Case 2: Join Existing Room as Regular Participant

Steps:

1. User opens a shared URL containing `?room=<roomId>`.
2. User enters a non-empty name.
3. User clicks `Join` or presses Enter.

Expected results:

- The join screen heading shows the target room ID before login.
- The user joins the existing room.
- The room UI is shown.
- The room state is broadcast to all connected room members.

## Use Case 3: Join a Room as Observer

Steps:

1. User enters a non-empty name.
2. User clicks `Join as Observer`.

Expected results:

- The user joins the room with `isObserver = true`.
- The observer badge is shown next to the current user name.
- Voting buttons are disabled.
- Disabled vote buttons have the tooltip `Observers cannot vote`.
- The observer is not rendered in the visible voting user list.

## Use Case 4: Duplicate User Name Resolution

Behavior:

- User names must be unique within a room.
- If the requested name already exists, the server appends ` (2)`, then ` (3)`, and so on until the name becomes unique.

Expected examples:

- `Alice`
- `Alice (2)`
- `Alice (3)`

Parity note:

- The final resolved name comes from the server and is applied on `joinConfirmation`.

## Use Case 5: Vote as Regular Participant

Preconditions:

- User is a non-observer participant.
- User is in a room.

Behavior:

- Clicking a vote card sends the selected vote to the server.
- The server stores the vote value without coercing decimal values to integers.
- The server broadcasts `roomUpdate` after each vote.

Visibility rules before voting ends:

- The current user sees their own selected vote value.
- Other participants are shown only as `✅ Voted`.
- Non-voting participants are shown as `⌛ Not voted yet`.

## Use Case 6: Cancel Vote

Behavior:

- Clicking `X` sends `cancelVote`.
- The participant's vote becomes `null`.
- The server broadcasts `roomUpdate`.
- The cancel button does not remain selected.

## Use Case 7: Observer Cannot Vote

Behavior:

- The UI blocks voting in observer mode.
- The server also rejects observer vote and cancel-vote attempts.

Parity expectation:

- Both client-side and server-side protection must remain in place.

## Use Case 8: End Voting

Behavior:

- Clicking `Show voting` sends `endVoting`.
- If voting is already ended, the server ignores repeated requests.
- The server sets `votingEnded = true` and broadcasts `roomUpdate`.

Expected UI after voting ends:

- The `Show voting` button is disabled.
- Its tooltip becomes `Voting has already ended`.
- Votes for all non-observer participants become visible.
- Statistics become visible if at least one numeric vote exists.
- Vote buttons are disabled while voting is ended.

## Use Case 9: Reset Voting

Behavior:

- Clicking `Clear voting` sends `resetVoting`.
- The server sets `votingEnded = false`.
- The server clears all participant votes to `null`.
- The server emits `resetVoting` and then `roomUpdate`.

Expected UI after reset:

- Selected vote button styling is cleared on all clients.
- Consensus message is hidden.
- `Show voting` becomes enabled again.
- Statistics panel is hidden.
- Non-observer voting buttons are enabled again.

## Use Case 10: Remove User as Observer

Preconditions:

- The acting user is an observer.
- The target user is another participant in the room.

Behavior:

- Observer sees a remove button `×` next to every non-current visible participant.
- Clicking remove shows a browser `confirm()` dialog.
- If confirmed, the client sends `removeUser` with the target user's name.

Server rules:

- Only observers may remove users.
- An observer cannot remove themselves.
- If the target user is found, they are removed from the room.
- Their heartbeat entry is deleted.
- The room receives `roomUpdate`.
- The requester receives a success `userRemoved` event.
- If the target still has an active socket, they receive `forcedDisconnect` and are removed from the room channel.

Error cases:

- Non-observer requester gets an error `userRemoved` result.
- Removing self returns an error `userRemoved` result.
- Removing an unknown user returns an error `userRemoved` result.

## Use Case 11: Progress Indicator

Rules:

- Only non-observer participants are counted as voters.
- A participant counts as having voted if their vote is not `null`.
- A `?` vote counts as a submitted vote for progress purposes.

Displayed values:

- Progress bar width as percentage
- Progress bar text such as `2 out of 3 users have voted`

Color rules:

- 100% is green
- Any other percentage is orange

## Use Case 12: Voting Statistics

Visibility:

- Statistics are only shown after voting has ended.
- Statistics are only shown if there is at least one numeric vote.

Rules:

- Observers are excluded.
- `?` votes are excluded from statistics.
- Numeric strings are parsed into numbers.
- Average is not rounded.
- Minimum and maximum are not rounded.

Additional display rule:

- Names are appended to minimum and maximum only when the value differs from the average and at least one participant has that value.

Consensus rules:

- Consensus is detected only from numeric votes.
- Consensus requires more than one numeric vote.
- If all numeric votes are equal and there is more than one numeric vote:
  - the consensus message is shown
  - confetti is triggered

## Use Case 13: User List Rendering Rules

Rules:

- Observers are filtered out of the visible voting list.
- The current user is sorted to the top.
- All other visible participants are sorted alphabetically.
- The current user is marked with `(current user)`.

Before voting ends:

- Current user with vote: actual vote value is shown
- Other user with vote: `✅ Voted`
- User without vote: `⌛ Not voted yet`

After voting ends:

- Actual vote values are shown for all visible non-observer participants

## Use Case 14: Connection and Reconnection Behavior

Behavior:

- On socket connection, the client marks itself as connected.
- On socket disconnect, the client marks itself as disconnected.
- If the client reconnects while local room and user state still exist, it immediately sends a heartbeat.

Note:

- The current implementation does not perform a full automatic rejoin workflow after connection loss.
- Behavior parity should preserve user-visible functionality first; reconnection robustness may be improved later only through an explicit issue.

## Use Case 15: Heartbeat and Inactivity Cleanup

Client heartbeat behavior:

- Heartbeat is sent every 3 seconds after login.
- Additional heartbeat attempts are triggered by:
  - click
  - keypress
  - scroll
  - mousemove with debounce
  - page becoming visible again

Server inactivity behavior:

- The server checks inactivity every 5 seconds.
- If the last heartbeat is older than 10 minutes, the user is marked inactive.
- Inactive users are filtered out of the room.
- The room receives `roomUpdate`.

## Use Case 16: Disconnect Behavior

Behavior:

- On socket disconnect, the per-connection heartbeat interval is cleared.
- If the user belongs to a room, they are marked inactive immediately.
- Inactive users are removed from the room user list.
- The room receives `roomUpdate`.

## Non-Functional and Technical Constraints to Preserve

- Static frontend assets are served directly by the backend application.
- The application should remain usable without authentication.
- The room model remains ephemeral in the initial migration.
- A single instance is the supported runtime model during migration.
- The migration should preserve current user-visible wording unless a later issue changes it explicitly.

## Parity Checklist Summary

The migrated application should preserve the following:

- room creation on first join
- room join via shared URL
- observer mode behavior
- duplicate name resolution using numeric suffixes
- vote and cancel vote workflows
- reveal votes and reset voting workflows
- progress calculation rules
- statistics rules, including exclusion of `?`
- observer-only remove user workflow
- forced disconnect behavior for removed users
- heartbeat-driven inactivity cleanup
- single-instance in-memory state model for the initial migration

## Recommended Baseline E2E Coverage

This behavior can be fixed with end-to-end tests before the migration.

Recommended scenarios:

1. create room and join second participant via shared URL
2. observer joins same room and cannot vote
3. duplicate names are resolved by the server
4. participants vote and only own vote is visible before reveal
5. reveal voting shows all votes and statistics
6. reset voting clears votes for all participants
7. observer removes another participant
8. `?` counts toward progress but is excluded from statistics
9. consensus message appears only for more than one equal numeric vote
10. connection status footer changes on disconnect and reconnect

## Open Notes

- The current implementation is small and intentionally lightweight.
- Some technical behaviors are coupled to the current client and Socket.io protocol.
- The migration should preserve domain behavior even when the transport changes from Socket.io to SignalR.