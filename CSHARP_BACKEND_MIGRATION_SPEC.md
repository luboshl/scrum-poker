# C# Backend Migration Specification

## Purpose

This document defines the migration from the current Node.js + Socket.io backend to ASP.NET Core + SignalR.

It serves as the umbrella specification for the migration work. Individual GitHub issues should reference this document and only narrow or refine a specific part of the migration.

This document is intentionally broader than a single issue and narrower than a product roadmap.

## Background

Current backend stack:

- Node.js
- Express
- Socket.io
- In-memory room state

Target backend stack:

- ASP.NET Core
- SignalR
- In-memory room state for the first migrated version
- Static frontend assets served by the ASP.NET Core host

The frontend may remain visually and structurally similar, but its realtime transport layer must be migrated from Socket.io to SignalR.

## Migration Goal

Replace the current backend with a C# implementation while preserving existing application behavior as described in `APPLICATION_BEHAVIOR_SPEC.md`.

The final result should:

- provide equivalent room and voting behavior
- use ASP.NET Core as the runtime host
- use SignalR for realtime communication
- support local development through .NET Aspire
- keep the initial runtime model simple and single-instance

## Final Target Solution

The final migrated solution should have the following characteristics:

- ASP.NET Core application hosts the backend
- SignalR hub exposes the realtime room interactions
- Static frontend assets are served by the ASP.NET Core application
- Core room and voting rules live in testable C# services, not directly inside the SignalR hub
- Runtime room state is stored in memory in the first migrated version
- Local orchestration uses .NET Aspire for a clean developer experience

## Explicit Technical Decisions

### Decision: Use ASP.NET Core + SignalR

Reasoning:

- The application is inherently realtime and bidirectional.
- SignalR maps naturally to join, vote, broadcast, and room updates.
- It is a better fit than Server-Sent Events for this domain because the workflow is not server-push-only.

### Decision: Keep state in memory for the initial migration

Reasoning:

- The current application already uses in-memory runtime state.
- Persisted state and scale-out are not required for behavior parity.
- A database would expand scope without improving the first migration goal.

### Decision: Centralize heartbeat cleanup in a background service

Reasoning:

- The current Node implementation keeps inactivity checks inside each connection lifecycle.
- In ASP.NET Core, a central background service is cleaner, more predictable, and easier to test.

### Decision: Preserve static frontend hosting inside the same application boundary

Reasoning:

- The current application is delivered as one deployable app.
- The desired direction is to keep frontend and backend together.
- This reduces deployment complexity for the first migrated version.

### Decision: Add both automated backend tests and browser E2E tests

Reasoning:

- Unit and integration tests protect the C# domain and SignalR behavior.
- E2E tests protect user-visible parity during transport and backend replacement.

## In Scope

The migration includes:

- creating the ASP.NET Core solution structure for the new backend
- introducing SignalR for realtime communication
- implementing room management in C#
- implementing participant join logic and duplicate name handling in C#
- implementing voting, cancel vote, reveal voting, and reset voting in C#
- implementing observer behavior and observer-only participant removal in C#
- implementing forced-disconnect style behavior for removed users
- implementing inactivity tracking and cleanup in a central background service
- serving the existing frontend assets from ASP.NET Core
- migrating frontend realtime integration from Socket.io client to SignalR client
- setting up .NET Aspire for local orchestration and developer startup flow
- adding unit tests for domain rules
- adding integration tests for backend and SignalR flows
- adding end-to-end tests for behavior parity
- updating documentation and startup instructions after the cutover

## Out of Scope

The migration does not include:

- redesigning the visual UI
- introducing authentication, user accounts, or authorization beyond current observer rules
- adding a database in the first migrated version
- enabling multi-instance scale-out in the first migrated version
- adding distributed caching or a Redis backplane in the first migrated version
- changing the estimation deck values
- redesigning the voting or statistics rules
- changing public application wording unless an issue explicitly calls for it
- adding advanced admin functionality beyond current observer capabilities
- building a separate SPA framework migration such as React, Vue, or Angular
- hosting and deployment redesign

## Behavior Parity Requirement

Behavior parity is mandatory for the first migrated version.

The migrated implementation must preserve, at minimum:

- room creation and join behavior
- `room` query parameter flow
- duplicate display name resolution
- observer limitations
- observer ability to remove participants
- vote reveal and reset flows
- progress bar calculation rules
- statistics rules
- consensus detection rules
- forced disconnect behavior for removed users
- inactivity cleanup behavior

Any intentional deviation must be documented and approved in a dedicated issue.

## Recommended Architecture

## Host Layer

Responsibilities:

- ASP.NET Core startup
- static file hosting
- SignalR registration
- dependency injection setup
- configuration and logging
- background service registration

## Realtime Layer

Responsibilities:

- thin SignalR hub methods
- client connection mapping
- group membership per room
- forwarding calls into domain/application services
- publishing room updates to connected clients

Important constraint:

- The SignalR hub should not contain the core business rules.

## Domain/Application Layer

Responsibilities:

- room lifecycle
- participant join/remove logic
- vote and cancel vote logic
- reveal/reset logic
- duplicate name resolution
- observer rule enforcement
- generation of room state DTOs used by clients

## State Layer

Responsibilities:

- in-memory storage of rooms and participants
- thread-safe access to room state
- lookup by room and participant identity

## Background Processing Layer

Responsibilities:

- heartbeat timeout detection
- inactive participant cleanup
- broadcasting room changes after cleanup

## Local Development and Orchestration

The migration should include .NET Aspire early.

Desired outcome:

- developers can start the local stack from one orchestrated entry point
- frontend and backend startup are easy to run together during migration
- the orchestration remains useful after cutover, even if the final runtime becomes a single ASP.NET Core host

Practical interpretation:

- Aspire should be introduced as a development orchestration tool, not as a production requirement
- During migration, Aspire may orchestrate the new ASP.NET Core backend and the existing frontend/static assets or supporting local processes as needed

## Testing Strategy

## Unit Tests

Unit tests should cover deterministic domain rules such as:

- duplicate name generation
- observer voting restrictions
- reset behavior
- reveal behavior
- participant removal permissions
- accepted vote values and vote transitions

## Integration Tests

Integration tests should cover backend behavior through the real host and SignalR stack such as:

- joining rooms through the real hub
- multiple clients receiving room updates
- removing a participant from a room
- reset and reveal flows through SignalR
- heartbeat cleanup and disconnect handling at the application boundary

## End-to-End Tests

Browser-based E2E tests should protect the existing user-visible behavior.

Recommended tool:

- Playwright

Reasons:

- strong multi-browser and multi-context support
- good fit for multiple concurrent participants in the same test
- reliable automation for modern web apps
- strong ecosystem and CI suitability

E2E tests should be used to lock down:

- join flows
- observer behavior
- voting and reveal behavior
- reset behavior
- remove-user flow
- progress and statistics behavior

## Recommended Work Breakdown

Suggested sequence:

1. lock down existing behavior with documentation and baseline E2E tests
2. introduce .NET Aspire for local orchestration
3. scaffold the ASP.NET Core + SignalR backend host
4. implement testable domain services and in-memory state storage
5. implement the SignalR hub and room broadcast contract
6. implement central heartbeat cleanup and connection lifecycle handling
7. migrate the frontend realtime client from Socket.io to SignalR
8. add backend unit and integration tests
9. cut over to C# backend and retire the Node backend

## Migration Acceptance Criteria

The migration is considered complete when all of the following are true:

- the application no longer requires the Node.js backend to run
- the ASP.NET Core backend provides behavior parity with `APPLICATION_BEHAVIOR_SPEC.md`
- the frontend communicates through SignalR instead of Socket.io
- the application can be started locally through the intended .NET Aspire development flow
- automated unit, integration, and E2E tests exist for the migrated system
- documentation is updated to describe the new runtime and local startup flow

## Risks

Main migration risks:

- accidental behavior drift during transport migration
- hidden assumptions in the current frontend about Socket.io event names or timing
- reconnect and disconnect edge cases
- race conditions in shared in-memory room state if the C# implementation is not designed for concurrent access

Mitigations:

- baseline behavior spec
- baseline E2E tests before cutover
- thin SignalR hub with testable services
- explicit issue breakdown with acceptance criteria

## Relationship to Issues

All migration issues should:

- reference this document
- reference `APPLICATION_BEHAVIOR_SPEC.md` when behavior parity matters
- narrow scope to a single implementable slice
- define acceptance criteria and tests clearly

The issue set created under the repository `issues/` directory is expected to act as the working backlog for this migration.