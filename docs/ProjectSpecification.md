# Joe's TouchDeploy

## Project Vision

Joe's TouchDeploy is a Windows desktop application for deploying Crestron touchscreen projects to multiple devices quickly, reliably, and with minimal user interaction.

The application is designed to eliminate the repetitive process of opening each panel individually and manually uploading VTZ projects.

The primary design goal is:

> Reliability over speed.

A deployment should only be reported as successful after the panel has accepted the project and returned to an operational state.

---

# Initial Target Hardware

* Crestron TSW-770
* Crestron TSW-1070
* Crestron TSW-760
* Crestron TSW-1060

Future support should be easy to extend.

---

# Technology

Language:

* C#

Framework:

* .NET 8

Desktop UI:

* WPF

Database:

* SQLite

Networking:

* HTTPS

Authentication:

* Session Cookie

---

# Deployment Workflow

1. Authenticate to panel.
2. Receive authentication cookies.
3. Upload VTZ project.
4. Wait for panel UI restart.
5. Verify panel is online.
6. Record deployment result.

---

# Authentication

HTTP POST

/userlogin.html

Content-Type:

application/x-www-form-urlencoded

Fields:

login

passwd

Authentication returns session cookies that must be reused for subsequent requests.

---

# Upload

HTTP POST

/Device/DeviceOperations

Content-Type:

multipart/form-data

Fields:

ProjectType = User

UploadProject = VTZ File

Successful response contains:

Success. Restarting UI..

---

# Device Model

Each device contains:

* Building
* Room
* IP Address
* Username
* Password
* Project Profile
* Variant (1-4)
* Notes
* Enabled
* Last Successful Deployment
* Last Deployment Result

---

# Project Profiles

A Project Profile contains:

* Profile Name
* Variant 1 Folder
* Variant 2 Folder
* Variant 3 Folder
* Variant 4 Folder

Each Variant folder contains exactly one VTZ file.

Joe's TouchDeploy automatically discovers the VTZ in the selected variant folder.

---

# Version 1 Features

* Device database
* Project Profiles
* Multi-device selection
* Parallel deployment (default maximum of five)
* Deployment logging
* Connection validation
* Search
* Filter by building
* Edit devices
* Delete devices

---

# Logging

Each deployment records:

* Timestamp
* Device
* Building
* Room
* IP Address
* VTZ Filename
* Duration
* Result
* Error Message

---

# Reliability Goals

Before deployment:

* Validate device information.
* Validate project profile.
* Verify exactly one VTZ exists.
* Verify panel is reachable.
* Verify authentication.

During deployment:

* Timeout detection.
* Retry support.
* Independent deployments.

After deployment:

* Wait for panel restart.
* Verify panel responds.
* Record success.

---

# Future Features

* Firmware deployment
* Device discovery
* Health monitoring
* Dashboard
* Scheduled deployments
* Toolbox launcher
* Open panel web interface
* Export/Import database
* Bulk credential updates
* Deployment reports

---

# Development Philosophy

* Small milestones.
* Working software at every stage.
* Thorough testing before adding features.
* Maintainable code over clever code.
* User experience focused on simplicity.
