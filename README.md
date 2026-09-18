Fundo loan application

A Next.js and .NET loan application demo with eligibility rules, transactional SQLite persistence, and background delivery to an external mock service.

Backend — terminal 1, repository root

# Planned commands; verify once implementation exists.
# Requires the repository's .NET SDK and a committed dotnet-ef tool manifest.
dotnet tool restore
dotnet ef database update --project backend/src/Fundo.Infrastructure --startup-project backend/src/Fundo.Api
dotnet run --project backend/src/Fundo.Api

External mock — terminal 2, repository root

# Planned commands; requires Node.js and npm.
cd external-service/mock-service
npm install
npm start

Frontend — terminal 3, repository root

# Planned commands; requires Node.js and npm.
cd frontend/fundo-web
npm install
npm run dev