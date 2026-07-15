# 🌳 TreeDrive — Secure File Sharing Platform

**TreeDrive** is a secure, full-stack file sharing platform that enables users to upload, download, and share files with specific users. This project is a complete architectural refactoring of the [original monolithic version (v1.0.0)](https://github.com/li-cs-developer/TreeDrive-FileSharingSystem/tree/v1.0.0), redesigned as a suite of independent, containerized microservices to overcome the limitations of a tightly-coupled codebase. Built using a decoupled, testable architecture with an API Gateway pattern, it addresses multi-tenant file sharing challenges by enforcing strict data isolation and granular cryptographic security at the API level, while delivering key advantages in scalability, resilience, and development agility.

<p align="center">
  <video src="https://github.com/user-attachments/assets/afb61a9f-8ebb-4915-907e-dd13e3fb7a45" width="100%" controls autoplay loop muted></video>
</p>

---

## 🚀 Tech Stack
| Layer | Technology |
|-------|------------|
| **Frontend** | React 18, TypeScript, Tailwind CSS, Vite |
| **API Gateway** | .NET 10, ASP.NET Core Web API |
| **Auth Service** | .NET 10, ASP.NET Core Web API, JWT, BCrypt |
| **File Service** | .NET 10, ASP.NET Core Web API |
| **Database** | MongoDB (NoSQL) |
| **Authentication** | JWT (JSON Web Tokens) + BCrypt Password Hashing |
| **Infrastructure** | Docker, Docker Compose |
| **Testing** | xUnit, Moq, FluentAssertions, Testcontainers |

---

## 🏗️ Architecture

### Why Microservices?

The solution follows a **Microservices Architecture** with an API Gateway pattern for the following reasons:

| Reason | Benefit |
|--------|---------|
| **Independent Scaling** | Auth and File services scale separately based on load. Auth handles login spikes, File handles upload/download spikes. |
| **Fault Isolation** | A failure in one service (e.g., File Service) doesn't affect others (e.g., Auth Service). |
| **Independent Deployment** | Deploy updates to Auth Service without redeploying File Service. |
| **Technology Flexibility** | Each service can use different technology stacks. |
| **Develpoment Autonomy** | Different services can be developed and maintained by different teams independently. |

### Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                    Frontend (React + TypeScript)                │
│                        http://localhost:5173                   │
└─────────────────────────────────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────┐
│                      API Gateway (Port 5000)                    │
│                    `backend/services/api-gateway/`              │
│                   Single Entry Point for All Requests           │
└─────────────────────────────────────────────────────────────────┘
                    │                           │
                    ▼                           ▼
┌──────────────────────────────────┐ ┌─────────────────────────────────┐
│    Auth Service (Port 5001)      │ │    File Service (Port 5002)     │
│   `backend/services/auth-service`| | `backend/services/file-service/`│
│     JWT Authentication & Users   │ │   File Upload, Download, Share  │
└──────────────────────────────────┘ └─────────────────────────────────┘
                    │                           │
                    └───────────────┬───────────┘
                                    │
                                    ▼
                    ┌─────────────────────────────────┐
                    │          MongoDB (27017)        │
                    │    `backend/infrastructure/`    │
                    │    Users, Files, Shares Data    │
                    └─────────────────────────────────┘
```

### Service Components

| Service | Port | Description |
|---------|------|-------------|
| **API Gateway** | 5000 | Single entry point, request routing, CORS |
| **Auth Service** | 5001 | User authentication, JWT generation, password hashing |
| **File Service** | 5002 | File CRUD operations, sharing, search |
| **Frontend** | 5173 | React + TypeScript UI |
| **MongoDB** | 27017 | NoSQL database for persistent storage |

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
- ✅ Index on `username` for fast user lookup
  
---

## 📦 Installation & Running Locally

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)

### Option 1: Run with Docker (Recommended)

1. Clone the repository:

```bash
git clone https://github.com/li-cs-developer/TreeDrive-FileSharingSystem.git
cd treedrive
```

2. Start all microservices:

```bash
docker-compose up -d
```

3. Verify all services are running:

```bash
docker-compose ps
```

4. Access the application:

- **Frontend:** http://localhost:5173
- **API Gateway:** http://localhost:5000
- **Auth Service:** http://localhost:5001
- **File Service:** http://localhost:5002


### Option 2: Run Without Docker

**Terminal 1 — Auth Service:**

```bash
# Restore dependencies and run the API
dotnet restore
dotnet run --project TreeDrive.API --urls "http://localhost:5000"
```

**Terminal 1 — Auth Service:**

```bash
cd backend/services/auth-service
dotnet restore
dotnet run --urls "http://localhost:5001"
```

**Terminal 2 — File Service:**

```bash
cd backend/services/file-service
dotnet restore
dotnet run --urls "http://localhost:5002"
```

**Terminal 3 — API Gateway:**

```bash
cd backend/services/api-gateway
dotnet restore
dotnet run --urls "http://localhost:5000"
```

**Terminal 4 — Frontend:**

```bash
cd frontend/TreeDrive.Frontend
npm install
npm run dev
```

**Terminal 3 — MongoDB:**

```bash
docker run -d -p 27017:27017 --name mongodb mongo:latest
```

Then access:
- **Frontend:** http://localhost:5173
- **API Gateway:** http://localhost:5000
- **Auth Service Health:** http://localhost:5001/health
- **File Service Health:** http://localhost:5002/health
---

## 🔧 Running Tests

```bash
# Navigate to backend folder
cd backend

# Run all tests
dotnet test TreeDrive.Backend.slnx

# Run unit tests only
dotnet test tests/unit/TreeDrive.Infrastructure.Tests/TreeDrive.Infrastructure.Tests.csproj

# Run integration tests (requires Docker running)
dotnet test tests/integration/TreeDrive.Integration.Tests/TreeDrive.Integration.Tests.csproj

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"
```

---

## 📋 API Endpoints

| Method | Endpoint | Description | Service | Auth |
|--------|----------|-------------|---------|:----:|
| **POST** | `/api/auth/register` | Register a new user | Auth Service | ❌ |
| **POST** | `/api/auth/login` | Login and receive JWT token | Auth Service | ❌ |
| **GET** | `/api/files/list` | List user's own files + shared files | File Service | ✅ |
| **POST** | `/api/files/upload` | Upload a file (max 100MB) | File Service | ✅ |
| **GET** | `/api/files/download/{id}` | Download a file | File Service | ✅ |
| **DELETE** | `/api/files/{id}` | Delete a file (owner only) | File Service | ✅ |
| **POST** | `/api/files/share/{id}` | Share file with another user | File Service | ✅ |
| **GET** | `/api/files/search/users?query={q}` | Search for users | File Service | ✅ |
| **GET** | `/health` | Health check | All Services | ❌ |

### Service-Specific Health Checks

```bash
# API Gateway Health
curl http://localhost:5000/health

# Auth Service Health
curl http://localhost:5001/health

# File Service Health
curl http://localhost:5002/health
```

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


---

## 🗂️ Project Structure

```
TreeDrive/
├── backend/                                    # Backend Microservices
│   ├── services/
│   │   ├── api-gateway/                       # API Gateway (ASP.NET Core 10)
│   │   │   ├── Controllers/                   # GatewayController
│   │   │   ├── Program.cs                     # Entry point
│   │   │   └── appsettings.json
│   │   ├── auth-service/                      # Auth Microservice
│   │   │   ├── Controllers/                   # AuthController (JWT, Users)
│   │   │   ├── Program.cs
│   │   │   └── appsettings.json
│   │   └── file-service/                      # File Microservice
│   │       ├── Controllers/                   # FilesController
│   │       ├── Program.cs
│   │       └── appsettings.json
│   ├── infrastructure/
│   │   ├── TreeDrive.Infrastructure/          # Data Layer
│   │   │   ├── Data/                          # MongoDbContext
│   │   │   ├── Repositories/                  # File, User, Share Repositories
│   │   │   └── Helpers/                       # PasswordHelper (BCrypt)
│   │   └── TreeDrive.FileStorage/             # File Storage Service
│   │       └── Services/                      # LocalFileStorageService
│   ├── shared/
│   │   ├── TreeDrive.Core/                    # Domain Models
│   │   │   └── Models/                        # FileMetadata, User, FileShareRecord
│   │   └── TreeDrive.Shared/                  # Shared DTOs
│   │       └── DTOs/                          # AuthDtos, FileDtos
│   └── tests/
│       ├── unit/                              # Unit Tests (xUnit + Moq)
│       │   ├── TreeDrive.API.Tests/
│       │   └── TreeDrive.Infrastructure.Tests/
│       └── integration/                       # Integration Tests (Testcontainers)
│           └── TreeDrive.Integration.Tests/
│
├── frontend/                                   # Frontend (React + TypeScript)
│   └── TreeDrive.Frontend/
│       ├── src/
│       │   ├── components/                    # Auth, Files, Common components
│       │   ├── context/                       # AuthContext (JWT management)
│       │   ├── api/                           # Axios client with interceptors
│       │   └── types/                         # TypeScript interfaces
│       └── package.json
│
├── deployment/                                 # Deployment Configurations
│   ├── docker/
│   ├── kubernetes/
│   └── terraform/
│
├── docker-compose.yml                          # Docker orchestration
└── README.md
```

---

## 🐛 Troubleshooting

### Common Issues

**Issue: MongoDB connection refused**

```bash
# Start MongoDB with Docker
docker run -d -p 27017:27017 --name mongodb mongo:latest

# Or check if MongoDB is running
docker ps | findstr mongodb
```

**Issue: Port already in use**

```bash
# Change port for a specific service
# Edit docker-compose.yml or run with different port
dotnet run --project backend/services/auth-service --urls "http://localhost:5003"
```
**Issue: Microservice communication failure**

```bash
# Check if all services are running
docker-compose ps

# View logs for a specific service
docker logs treedrive-auth --tail=50
docker logs treedrive-file --tail=50
docker logs treedrive-gateway --tail=50

# Check if services can communicate
curl http://localhost:5001/health
curl http://localhost:5002/health
```

**Issue: Frontend can't connect to backend**

```bash
# Update the API URL in frontend/.env
VITE_API_URL=http://localhost:5000

# Rebuild the frontend
cd frontend/TreeDrive.Frontend
npm install
npm run dev
```

**Issue: The API Gateway can't reach Auth/File services**

```bash
# Check environment variables in docker-compose.yml
# Ensure services are using the correct internal hostnames:
# Auth Service: http://auth-service:80
# File Service: http://file-service:80

# Rebuild and restart
docker-compose down
docker-compose up -d --build
```

**Issue: Tests failing due to Docker not running**

```bash
# Start Docker Desktop
# For Windows: Open Docker Desktop from Start Menu

# Verify Docker is running
docker --version
docker ps

# Run tests with Docker running
dotnet test backend/TreeDrive.Backend.slnx
```

### Quick Service Status Check

```bash
# Check all service health endpoints
curl http://localhost:5000/health
curl http://localhost:5001/health
curl http://localhost:5002/health

# Check running containers
docker-compose ps

# View all logs
docker-compose logs --tail=20
---

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

