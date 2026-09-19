1. Project structure

Use Next.js, a .NET API, EF Core with SQLite migrations, and a minimal Node.js/Express external mock.

Location

Responsibility

frontend/fundo-web/

Form, validation, approved/denied pages; local state and HTTP calls

backend/src/Fundo.Domain/

Customer and LoanApplication entities

backend/src/Fundo.Application/

Submission use case, decision rules, IApplicationStore contract

backend/src/Fundo.Infrastructure/

EF Core store, migrations, outbox worker, external HTTP client

backend/src/Fundo.Api/

Thin HTTP controller, configuration, dependency injection

backend/tests/

Unit and SQLite integration tests

external-service/mock-service/

External create/update endpoints and record inspection

Application references Domain; Infrastructure references Application and Domain; Api references Application and Infrastructure. Domain has no project dependencies. Inner layers never depend on infrastructure or HTTP frameworks.

2. Rule engine

The submission service normalizes and validates input, then evaluates synchronous IApplicationRule implementations in registration order. NyStateRule denies NY; BlacklistedSsnRule checks a configured list of normalized SSNs. The engine stops at the first denial and otherwise approves.

To add a rule, implement IApplicationRule, register it explicitly in dependency injection, and add focused tests. Existing rules remain unchanged. Denials cause no persistence; approved submissions call IApplicationStore.

3. Background events and external calls

An OutboxProcessor : BackgroundService polls committed, pending messages using a scoped EF context. It sends each stored customer/application snapshot through an Infrastructure-owned IExternalApplicationClient using HttpClientFactory; it never reloads newer entity state.

Create events call POST /applications; updates call PUT /applications/{applicationId}. Success sets ProcessedAtUtc. Failure increments Attempts, records a sanitized LastError, and leaves the message pending for retry. External calls run independently of the submission request and outside its database transaction.

Delivery is at least once: a crash after HTTP success but before recording completion can cause redelivery. The mock uses the application GUID as its resource key and tolerates duplicate creates and updates. GET /applications exposes received records for demonstration.

4. Transaction and failures

IApplicationStore.SaveApprovedApplicationAsync owns one explicit EF Core/SQLite transaction. It looks up the normalized SSN, creates or updates the customer and their single application, then inserts an outbox event containing the operation, event ID, resource IDs, and complete customer/application snapshot, including the normalized SSN. Returning customers retain both IDs. Database constraints enforce unique SSNs and one application per customer.

Publishing means durably committing that outbox event with the business records. If a database write or event insertion fails, all changes roll back and no approval is returned. Approval follows successful commit. Later external-delivery failures do not undo approved records; the event remains pending for retry.

5. Trade-offs

SQLite avoids database-server setup while retaining transactions. One application store and explicit rules avoid generic persistence abstractions and rule frameworks. The outbox provides durable publication without operating a broker; basic polling defers advanced backoff, dead letters, and multiple-worker coordination.

Specialized concurrent-submission retries are deferred; unique constraints still protect record invariants. The mock keeps data in memory and loses it on restart. Authentication, application history, and persisted denials are outside scope. SSNs are stored directly only for this exercise and must never be logged; production requires encryption or tokenization. Docker, CI, and seed data remain optional because they are not needed to demonstrate the core flow.
