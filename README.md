# Fundo take home assignment

Demo video: https://www.loom.com/share/eb31f8cf759743908c554f4161f18625

## Project Description

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

## Running Tests

From the repository root, with backend packages restored:

```sh
dotnet build Fundo.sln --no-restore
dotnet test Fundo.sln --no-restore
```

## Test Data

The configured blacklisted SSN is `111-22-3333` (formatting is optional). Use these values with otherwise valid form fields:

| Result | State | SSN | Notes |
| --- | --- | --- | --- |
| Approved | CA | `123-45-6789` | Any positive requested amount. |
| Denied | NY | `234-56-7890` | NY is not supported. |
| Denied | CA | `111-22-3333` | Blacklisted SSN. |
| Returning customer | CA | `123456789` | Submit after the approved example, changing address, company, or amount. |
