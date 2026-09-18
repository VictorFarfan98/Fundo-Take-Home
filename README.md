Fundo loan application

A Next.js and .NET loan application demo with eligibility rules, transactional SQLite persistence, and background delivery to an external mock service.

Backend — terminal 1, repository root

# Requires .NET SDK 10.
dotnet run --project backend/src/Fundo.Api

External mock — terminal 2, repository root

# Requires Node.js 20 or later and npm.
cd external-service/mock-service
npm install
npm start

Frontend — terminal 3, repository root

# Requires Node.js 20 or later and npm.
cd frontend/fundo-web
npm install
npm run dev
