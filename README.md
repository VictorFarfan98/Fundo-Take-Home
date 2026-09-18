Fundo loan application

A Next.js and .NET loan application demo with eligibility rules, transactional SQLite persistence, and background delivery to an external mock service.

Run locally

Open three terminals at the repository root. The API applies the SQLite migrations automatically when it starts.

Backend — terminal 1, repository root

# Requires .NET SDK 10.
dotnet restore Fundo.sln

# Initializes SQLite migrations and starts the API at http://localhost:5000.
dotnet run --project backend/src/Fundo.Api --urls http://localhost:5000

External mock — terminal 2, repository root

# Requires Node.js 20 or later and npm.
cd external-service/mock-service
npm ci
npm start

Frontend — terminal 3, repository root

# Requires Node.js 20 or later and npm.
cd frontend/fundo-web
npm ci
npm run dev

Tests — repository root

# Requires .NET SDK 10 and restored backend packages.
dotnet test Fundo.sln --no-restore

Test data

The configured blacklisted SSN is `111-22-3333` (formatting is optional). Use these values with otherwise valid form fields:

| Result | State | SSN | Notes |
| --- | --- | --- | --- |
| Approved | CA | `123-45-6789` | Any positive requested amount. |
| Denied | NY | `234-56-7890` | NY is not supported. |
| Denied | CA | `111-22-3333` | Blacklisted SSN. |
| Returning customer | CA | `123456789` | Submit after the approved example, changing address, company, or amount. |
