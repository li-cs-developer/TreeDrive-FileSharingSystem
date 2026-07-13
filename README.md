# 🌳 TreeDrive — Secure File Sharing Platform

**TreeDrive** is a secure, full-stack file sharing platform that enables users to upload, download, and share files with specific users. Built using a decoupled, testable architecture, it addresses multi-tenant file sharing challenges by enforcing strict data isolation and granular cryptographic security at the API level.

<p align="center">
  <video src="https://github.com/user-attachments/assets/afb61a9f-8ebb-4915-907e-dd13e3fb7a45" width="100%" controls autoplay loop muted></video>
</p>

---

## 🚀 Tech Stack

| Layer | Technology |
|-------|------------|
| **Frontend** | React 18, TypeScript, Tailwind CSS, Vite |
| **Backend** | .NET 10, ASP.NET Core Web API, C# |
| **Database** | MongoDB (NoSQL) |
| **Authentication** | JWT (JSON Web Tokens) + BCrypt Password Hashing |
| **Infrastructure** | Docker, Docker Compose |
| **Testing** | xUnit, Moq, FluentAssertions, Testcontainers |

---

## 🏗️ Architecture

The solution follows **Clean Architecture** principles with clear separation of concerns:

- **TreeDrive.API** — Controllers, custom middleware, and API endpoints.
- **TreeDrive.Core** — Core domain models, system exceptions, and business logic interfaces.
- **TreeDrive.Infrastructure** — Data access configurations, MongoDbContext, and repositories.
- **TreeDrive.FileService** — File storage streaming abstractions.
- **TreeDrive.Shared** — Lightweight Data Transfer Objects (DTOs) used for cross-layer communication.

## 🛠️ Key Technical Implementations

- **🔐 Stateless Authentication & Cryptography** — Configured secure, time-restricted JWT Bearer Tokens for stateless session validation, alongside work-factored BCrypt hashing for defense-in-depth password storage.
- **👥 Granular Access Delegation** — Supports multi-user target sharing with explicit viewer permissions, bypassing basic binary (public/private) access limitations.
- **🚧 Multi-Tenant User Isolation** — Enforces strict API-level validation queries ensuring users can exclusively interact with files they explicitly own or have been granted access to.
- **📁 Encapsulated Storage Streams** — Engineered an abstract file handling framework using local system I/O streams, cleanly isolating physical persistence layers from application business logic.
- **📊 Optimized MongoDB Indexing** — Designed compound index matrices on `(owner, filename)` to completely prevent duplicate uploads and optimize query performance by eliminating database collection scans.
- **🧪 Ephemeral Integration Testing** — Leveraged Testcontainers within an xUnit environment to programmatically spin up real, isolated MongoDB Docker instances during the test lifecycle, ensuring zero test data drift.
- **🔄 Continuous Integration Ready** — Pre-configured to hook seamlessly into automated CI pipelines to execute unit and database integration test suites on every pull request.

### Database Design

MongoDB collections structured for high-performance indexing:

```
files:  { owner, filename, size, uploaded_at, download_count, tags }
users:  { username, password_hash, role, created_at }
shares: { file_id, shared_by, shared_with, permission, expires_at }
```

- ✅ Unique compound index on `(owner, filename)` prevents duplicate uploads
- ✅ Index on `owner` for fast file listing
- ✅ Index on `shared_with` for quick share lookups

---

## 📦 Installation & Running Locally

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (optional, for MongoDB)

### Option 1: Run with Docker (Recommended)

```bash
# 1. Clone the repository
git clone https://github.com/li-cs-developer/TreeDrive-FileSharingSystem.git
cd treedrive

# 2. Start all services (MongoDB, Backend, Frontend)
docker-compose up -d

# 3. Access the application
# Frontend: http://localhost
# Backend API: http://localhost:5000
```

### Option 2: Run Without Docker

**Terminal 1 — Backend:**

```bash
# Restore dependencies and run the API
dotnet restore
dotnet run --project TreeDrive.API --urls "http://localhost:5000"
```

**Terminal 2 — Frontend:**

```bash
# Install dependencies and start the dev server
cd TreeDrive.Frontend
npm install
npm run dev
```

**Terminal 3 — MongoDB:**

```bash
# Start MongoDB locally (or use Docker)
docker run -d -p 27017:27017 --name mongodb mongo:latest
```

Then access:

- **Frontend:** http://localhost:5173
- **Backend API:** http://localhost:5000
- **Health Check:** http://localhost:5000/health

---

## 🔧 Running Tests

```bash
# Run all tests
dotnet test

# Run unit tests only
dotnet test TreeDrive.API.Tests/TreeDrive.API.Tests.csproj

# Run integration tests (requires Docker running)
dotnet test TreeDrive.Integration.Tests/TreeDrive.Integration.Tests.csproj

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"
```

---

## 📋 API Endpoints

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| POST | `/api/auth/register` | Register a new user | ❌ |
| POST | `/api/auth/login` | Login and receive JWT token | ❌ |
| GET | `/api/files/list` | List user's own files + shared files | ✅ |
| POST | `/api/files/upload` | Upload a file (max 100MB) | ✅ |
| GET | `/api/files/download/{id}` | Download a file | ✅ |
| DELETE | `/api/files/{id}` | Delete a file (owner only) | ✅ |
| POST | `/api/files/share/{id}` | Share file with another user | ✅ |
| GET | `/api/auth/users/search?query={q}` | Search for users to share with | ✅ |
| GET | `/health` | Health check endpoint | ❌ |

### Authentication Flow

1. Register a new user at `/api/auth/register`
2. Login at `/api/auth/login` to receive a JWT token
3. Include the token in all subsequent requests: `Authorization: Bearer <token>`

---

## 🧪 Sample API Usage

### 1. Register a User

```bash
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"username":"alice","password":"password123"}'
```

### 2. Login

```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"alice","password":"password123"}'
```

Returns:

```json
{ "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." }
```

### 3. Upload a File

```bash
curl -X POST http://localhost:5000/api/files/upload \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -F "file=@document.pdf"
```

### 4. List Your Files

```bash
curl -X GET http://localhost:5000/api/files/list \
  -H "Authorization: Bearer YOUR_TOKEN"
```

### 5. Share a File

```bash
curl -X POST http://localhost:5000/api/files/share/{fileId} \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"users":["bob"],"permission":"read"}'
```

### 6. Download a Shared File

```bash
curl -X GET http://localhost:5000/api/files/download/{fileId} \
  -H "Authorization: Bearer YOUR_TOKEN" \
  --output downloaded_file.pdf
```

---

## 🗂️ Project Structure

```
TreeDrive/
├── TreeDrive.API/                      # API Gateway (ASP.NET Core 10)
│   ├── Controllers/                    # Auth, Files controllers
│   ├── Program.cs                      # Application entry point
│   └── appsettings.json                # Configuration
│
├── TreeDrive.Core/                     # Domain Models
│   └── Models/                         # FileMetadata, User, FileShareRecord
│
├── TreeDrive.Infrastructure/           # Data Layer
│   ├── Data/                           # MongoDbContext
│   ├── Repositories/                   # FileRepository, UserRepository, ShareRepository
│   └── Helpers/                        # PasswordHelper (BCrypt)
│
├── TreeDrive.FileService/              # File Storage Service
│   └── Services/                       # LocalFileStorageService
│
├── TreeDrive.Shared/                   # Shared DTOs
│   └── DTOs/                           # AuthDtos, FileDtos
│
├── TreeDrive.Frontend/                 # React + TypeScript Frontend
│   ├── src/
│   │   ├── components/                 # Auth, Files, Common components
│   │   ├── context/                    # AuthContext (JWT management)
│   │   ├── api/                        # Axios client with interceptors
│   │   └── types/                      # TypeScript interfaces
│   └── package.json
│
├── TreeDrive.API.Tests/                # Unit Tests (xUnit + Moq)
├── TreeDrive.Infrastructure.Tests/     # Unit Tests for data helpers and repositories
├── TreeDrive.Integration.Tests/        # Integration Tests (Testcontainers)
├── docker-compose.yml                  # Docker orchestration
└── README.md
```

---

## 🐛 Troubleshooting

### Common Issues

**Issue: MongoDB connection refused**

```bash
# Start MongoDB with Docker
docker run -d -p 27017:27017 --name mongodb mongo:latest
```

**Issue: Port 5000 already in use**

```bash
# Change the port
dotnet run --project TreeDrive.API --urls "http://localhost:5001"
```

**Issue: Frontend can't connect to backend**

```bash
# Update the API URL in TreeDrive.Frontend/.env
VITE_API_URL=http://localhost:5000
```

---

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

