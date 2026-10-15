# Payment Processing API

A robust REST API for processing money transfers between accounts. This project is built using **.NET 10** and demonstrates production-ready patterns, including Clean Architecture, the Repository pattern, and Unit of Work.

## 🛠️ Technology Stack
- **Framework**: .NET 10, ASP.NET Core Web API
- **Data Access**: Entity Framework Core
- **Database**: SQLite (In-Memory for zero-setup testing)
- **Architecture**: Clean Architecture

---

## 🚀 How to Run the Application

The project uses an In-Memory SQLite database to ensure the reviewer can run it immediately without setting up external dependencies like PostgreSQL or Docker.

1. Open your terminal and navigate to the API project directory:
   ```bash
   cd PaymentApi/PaymentApi.Web
   ```
2. Run the application:
   ```bash
   dotnet run
   ```
3. Open your browser and navigate to the Swagger UI to interact with the API:
   **http://localhost:5000/swagger**

### Pre-Seeded Test Data
Upon startup, the database is automatically seeded with the following accounts:
- **Sender Account**: `ACC_DEBIT_001` (Balance: 1,000,000, Currency: 860)
- **Receiver Account**: `ACC_CREDIT_001` (Balance: 0, Currency: 860)

---

## 🔄 API Usage Cycle (Testing the Flow)

Here is the typical lifecycle of processing a payment via the API.

### 1. Check Initial Balance
Verify the sender's balance before the transfer.
```bash
curl -X 'GET' 'http://localhost:5000/api/accounts/ACC_DEBIT_001/balance' -H 'accept: */*'
```

### 2. Create a Payment
Initiate a money transfer. The external bank client simulates a 500ms network delay.
```bash
curl -X 'POST' 'http://localhost:5000/api/payments' \
  -H 'Content-Type: application/json' \
  -d '{
  "transactionId": "EXT_100001",
  "senderAccount": "ACC_DEBIT_001",
  "receiverAccount": "ACC_CREDIT_001",
  "amount": 150000,
  "currency": 860
}'
```

*Note on simulating bank responses:* 
- If `transactionId` contains `"TIMEOUT"`, the external service will throw a timeout exception.
- If `transactionId` contains `"REJECT"`, the external service will decline the transfer.

### 3. Check Payment Status
Verify the status of the transaction you just created.
```bash
curl -X 'GET' 'http://localhost:5000/api/payments/EXT_100001' -H 'accept: */*'
```

---

## 🏗️ Architecture & Implementation Details

The project is structured around **Clean Architecture** to separate concerns and ensure high testability.

* **Domain**: Contains the core business entities (`Account`, `PaymentTransaction`) and enums. It has zero external dependencies.
* **Application**: Contains the business logic (`PaymentService`), DTOs, and interfaces (`IRepository`, `IUnitOfWork`, `IExternalBankClient`).
* **Infrastructure**: Implements the interfaces defined in the Application layer. It contains the Entity Framework `AppDbContext`, the Repository implementations, and the mock `ExternalBankClient`.
* **Web (API)**: The entry point. Contains the Controllers and Dependency Injection configurations. It only interacts with the Application layer, never directly with the database.

---

## 🛡️ Core Mechanisms

### 1. Idempotency (Duplicate Request Protection)
The system is protected against clients accidentally submitting the same payment multiple times (e.g., due to a retry on a dropped network connection).
* **Implementation**: The `TransactionId` provided in the HTTP request payload serves as the Idempotency Key. 
* **Process**: Before executing any logic, `PaymentService` queries the database via the Unit of Work. If a transaction with the same `TransactionId` already exists, the service skips processing and simply returns the cached status of the previous attempt.

### 2. Concurrency Control (Race Condition Prevention)
Handling high-volume concurrent requests safely is critical for a financial API. The system utilizes two layers of protection:
* **Optimistic Concurrency (Balance Protection)**: The `Account` entity contains a `Version` property decorated with the `[ConcurrencyCheck]` attribute. If two parallel threads attempt to deduct money from the same account simultaneously, EF Core detects a version mismatch. The first transaction succeeds, while the second fails with a `DbUpdateConcurrencyException`, protecting the database from inconsistent balance calculations.
* **Primary Key Constraints**: The `PaymentTransaction` entity explicitly sets `TransactionId` as its Primary Key. If two parallel requests manage to bypass the application-level idempotency check at the exact same millisecond, the database engine enforces data integrity by rejecting the duplicate insert attempt.

### 3. Handling External Service Failures
The system interacts with an external bank service (`IExternalBankClient`) that may fail or time out.
* **Two-Phase State**: A payment is first saved to the database in a `Pending` state. Only after the database commit is successful does the system call the external bank.
* **Timeouts**: If the external bank times out, the application catches the `TimeoutException`. The transaction remains in the `Pending` state in the database, allowing a background worker (or manual retry mechanism) to resolve the transaction later without losing the initial request intent.
