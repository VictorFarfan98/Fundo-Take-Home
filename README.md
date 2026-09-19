# Fundo take home assignment

## Info

A Next.js and .NET loan application demo with eligibility rules, transactional SQLite persistence, and background delivery to an external mock service.

## How To Run Locally

Open three terminals at the repository root. Start the backend first; it applies SQLite migrations automatically.

### Backend

#### Requirements

- .NET SDK 10

#### Commands

```sh
dotnet restore Fundo.sln
dotnet run --project backend/src/Fundo.Api --urls http://localhost:5000
```

### External Service

#### Requirements

- Node.js 20 or later
- npm

#### Commands

```sh
cd external-service/mock-service
npm ci
npm start
```

### Frontend

#### Requirements

- Node.js 20 or later
- npm

#### Commands

```sh
cd frontend/fundo-web
npm ci
npm run dev
```
