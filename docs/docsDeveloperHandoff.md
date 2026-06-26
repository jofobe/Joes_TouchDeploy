# Joe's TouchDeploy - Developer Handoff

## Date

2026-06-26

---

## Current Status

Sprint 1 - Authentication

Status:

Project setup complete.

No networking code has been written yet.

---

## Completed

* GitHub repository created.
* Local repository cloned.
* Visual Studio solution created.
* .NET 8 solution.
* Console project created.
* Core project created.
* Project reference configured.
* Repository connected correctly.
* .gitignore created.
* Initial project structure established.
* Models folder created.
* Services folder created.
* PanelConnection model created.
* AuthenticationService placeholder created.
* Solution builds successfully.

Build Status:

SUCCESS

2 projects
0 errors

---

## Current Architecture

Solution

* JoesTouchDeploy.Console
* JoesTouchDeploy.Core

Core

Models

* PanelConnection

Services

* AuthenticationService

---

## Design Decisions

* C#
* .NET 8
* WPF (future UI)
* SQLite (future)
* HTTPS communication
* Direct communication with Crestron panels
* Do not automate Toolbox
* Authentication via POST /userlogin.html
* Upload via POST /Device/DeviceOperations
* Cookie-based session management
* Reliability over speed

---

## Next Milestone

Sprint 1.2

Create CrestronHttpClient

Responsibilities

* Own HttpClient
* Own CookieContainer
* Handle HTTPS certificate validation for managed Crestron devices
* Provide reusable authenticated connection

No upload functionality yet.

---

## Definition of Done

Joe's TouchDeploy can authenticate to a TSW-1070 and establish a valid session.

---

## Notes

The project intentionally progresses in small, testable milestones.

Every milestone must compile and be verified before moving to the next.
