# CTF Platform

A full-stack Capture The Flag (CTF) platform.

The system allows users to register, solve cybersecurity challenges, submit flags, track scores, and compete on a live scoreboard. Administrators can create and manage challenges through a dedicated interface.

---

## Features

### User Features
- User registration and login
- Browse available challenges
- Submit challenge flags
- View challenge hints
- download challenge files
- Live scoreboard and rankings
- Track personal progress

### Administrator Features
- Create new challenges
- Edit existing challenges
- Manage hints
- Manage files
- Manage users
- Monitor platform activity

### Server Features
- Multi-threaded TCP server
- Custom client-server communication protocol
- Session management
- Real-time score updates
- SQLite database integration
- Secure password handling

---

## Architecture

The project is divided into three main components:

### CTF.Client
Windows Forms desktop application used by players and administrators.

### CTF.Server
Backend TCP server responsible for:
- Authentication
- Challenge management
- Flag validation
- Score calculation
- Database communication

### CTF.Common
Shared library containing:
- Data models
- Network packets
- Validation utilities
- Cryptographic helpers

---

## Technologies Used

### Backend
- C#
- .NET 8
- TCP Sockets
- SQLite
- Asynchronous Programming

### Frontend
- Windows Forms (WinForms)

### Database
- SQLite

### Security
- Password hashing
- Input validation
- Session management

---

## Project Structure

```
CTFPlatform/
│
├── CTF.Client/
│   ├── LoginForm
│   ├── MainForm
│   ├── ChallengesForm
│   ├── ScoreboardForm
│   └── Admin Management Forms
│
├── CTF.Server/
│   ├── TcpServer
│   ├── RequestHandler
│   ├── SessionManager
│   └── DatabaseService
│
└── CTF.Common/
    ├── Models
    ├── Packets
    ├── Crypto Services
    └── Validation Services
```

---

## How It Works

1. The server starts and listens for incoming TCP connections.
2. Clients connect and authenticate.
3. Requests are serialized and transmitted through a custom packet protocol.
4. The server processes requests and interacts with the database.
5. Responses are sent back to the client.
6. Scores and challenge data are updated in real time.

---

## Screenshots

Add screenshots of:
- Login Screen
- Challenge Browser
- Scoreboard
- Challenge Creation Panel
- User Management Panel

Example:

```md
![Login Screen](images/login.png)
```

---

## Learning Outcomes

- Object-Oriented Programming (OOP)
- Client-Server Architecture
- Multi-threaded Programming
- Network Programming with TCP
- Database Design and Integration
- Secure Authentication
- Full-Stack Application Development
- Software Architecture and Design Patterns

---

## Future Improvements

- Web-based client
- Docker deployment
- REST API support
- Team competitions
- Dynamic challenge categories
- Real-time notifications
- Leaderboard analytics

---

## Author

**Daniel Magram**

5-Unit Computer Science Final Project

Focused on cybersecurity, software development, networking, and system design.

---
