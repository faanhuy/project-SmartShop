# SmartShop

A full-stack e-commerce platform built with .NET 8 Clean Architecture and React 19, featuring AI-powered product search, recommendations, and description generation.

![CI](https://github.com/YOUR_USERNAME/SmartShop/actions/workflows/ci.yml/badge.svg)
![CD](https://github.com/YOUR_USERNAME/SmartShop/actions/workflows/cd.yml/badge.svg)
![.NET](https://img.shields.io/badge/.NET-8.0-purple)
![React](https://img.shields.io/badge/React-19-blue)

## Features

- **Auth**: JWT Bearer + Refresh Token rotation
- **Products**: CRUD, Redis cache, soft delete, slug-based routing
- **Categories**: Hierarchical product categorization
- **Cart**: Add/update/remove items with stock validation
- **Orders**: Atomic checkout — stock reduction + cart clear in one transaction
- **AI Search**: Semantic product search powered by Voyage AI embeddings
- **AI Recommendations**: Similar product suggestions via Claude
- **AI Description**: Auto-generate product descriptions with Groq (llama-3.3-70b)
- **Admin Panel**: Product management UI with AI description generator

## Tech Stack

| Layer       | Technology                                      |
|-------------|-------------------------------------------------|
| Backend     | .NET 8, ASP.NET Core, Clean Architecture, CQRS  |
| ORM         | EF Core 8, SQL Server 2022                      |
| Cache       | Redis 7                                         |
| Frontend    | React 19, TypeScript, Vite, TailwindCSS         |
| Auth        | JWT Bearer + Refresh Token Rotation             |
| AI          | Groq (llama-3.3-70b), Voyage AI (voyage-3)      |
| Testing     | xUnit, Moq, FluentAssertions, Coverlet          |
| CI/CD       | GitHub Actions, Docker, GHCR                    |

## Quick Start

### Prerequisites

- Docker Desktop
- Git

### Run with Docker Compose

```bash
# Clone
git clone https://github.com/YOUR_USERNAME/SmartShop.git
cd SmartShop

# Create .env file
cp .env.example .env
# Edit .env and fill in your API keys

# Start all services
docker compose up -d

# API:      http://localhost:8080/swagger
# Frontend: http://localhost:3000
```

### Run Locally (without Docker)

**Prerequisites**: .NET 8 SDK, Node.js 22, SQL Server, Redis

```bash
# Backend
cd src/SmartShop.WebAPI
dotnet run

# Frontend
cd smartshop-frontend
npm install
npm run dev
```

## Environment Variables

| Variable       | Description                              |
|----------------|------------------------------------------|
| `JWT_KEY`      | JWT signing key (min 32 characters)      |
| `GROQ_API_KEY` | Groq API key for AI description generation |

For GitHub Actions CD: set `PRODUCTION_API_URL` secret in repository settings.

## Architecture

```
SmartShop/
├── src/
│   ├── SmartShop.Domain/         # Entities, interfaces (no dependencies)
│   ├── SmartShop.Application/    # CQRS handlers, DTOs, exceptions
│   ├── SmartShop.Infrastructure/ # EF Core, Redis, JWT, AI services
│   └── SmartShop.WebAPI/         # Controllers, middleware, Swagger
├── tests/
│   └── SmartShop.Application.Tests/  # Unit tests (xUnit + Moq)
└── smartshop-frontend/           # React 19 + TypeScript + Vite
```

**Patterns**: Clean Architecture, CQRS (MediatR), Repository, Factory (`Entity.Create()`), Soft Delete, Audit Trail

## API Endpoints

| Method | Path                              | Auth     | Description                    |
|--------|-----------------------------------|----------|--------------------------------|
| POST   | `/api/auth/register`              | No       | Register new user              |
| POST   | `/api/auth/login`                 | No       | Login, get JWT + refresh token |
| POST   | `/api/auth/refresh`               | No       | Rotate refresh token           |
| GET    | `/api/products`                   | No       | Paged product list             |
| GET    | `/api/products/{id}`              | No       | Get product by ID              |
| POST   | `/api/products`                   | Admin    | Create product                 |
| PUT    | `/api/products/{id}`              | Admin    | Update product                 |
| DELETE | `/api/products/{id}`              | Admin    | Soft delete product            |
| GET    | `/api/cart`                       | Yes      | Get user cart                  |
| POST   | `/api/cart`                       | Yes      | Add item to cart               |
| POST   | `/api/orders`                     | Yes      | Place order (checkout)         |
| GET    | `/api/orders`                     | Yes      | Order history                  |
| POST   | `/api/ai/search`                  | No       | Semantic product search        |
| GET    | `/api/ai/recommendations/{id}`    | No       | Similar products               |
| POST   | `/api/ai/generate-description`    | Admin    | AI description generator       |

## Running Tests

```bash
dotnet test tests/SmartShop.Application.Tests/ --collect:"XPlat Code Coverage"
```

36 unit tests across Auth, Products, Cart, and Orders handlers.

## CI/CD

- **CI** (`.github/workflows/ci.yml`): Runs on push/PR — dotnet test, frontend build, backend Docker build check
- **CD** (`.github/workflows/cd.yml`): Runs on push to `main` — builds and pushes Docker images to GHCR

## Sprints

| Sprint | Description               | Status      |
|--------|---------------------------|-------------|
| 0      | Setup, Clean Architecture | ✅ Complete |
| 1      | Auth & Domain entities    | ✅ Complete |
| 2      | Product Catalog + Redis   | ✅ Complete |
| 3      | Cart & Orders             | ✅ Complete |
| 4      | AI Features               | ✅ Complete |
| 5      | DevOps & Deploy           | ✅ Complete |
