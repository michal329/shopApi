Shop Project API
A robust, scalable REST API built with .NET 9, implementing modern architectural patterns and best practices for high-quality software development.

🏗 Architecture & Design
The project follows a Clean Architecture (3-Layer) approach to ensure separation of concerns and maintainability:

Application Layer: Handles the core business logic.

Services Layer: Orchestrates data flow and integrates external components.

Repositories Layer: Manages data access and persistence.

Key Principles:
Dependency Injection (DI): Used extensively to achieve decoupling between layers.

Asynchronous Programming: All operations (logic and DB access) are implemented asynchronously to free up threads and improve scalability.

Data Transfer Objects (DTOs): Used for data passing between layers to eliminate circular dependencies and ensure data integrity.

Records: DTOs are implemented as records, providing immutable data structures which are ideal for data transfer.

🛠 Tech Stack
Framework: ASP.NET Core Web API (.NET 9)

Language: C#

ORM: Entity Framework Core

Approach: Database First

Mapping: AutoMapper for seamless conversion between Entities and DTOs.

Logging: NLog for detailed diagnostic logging and system monitoring.

🚀 Performance & Security
Configurations: All sensitive data and environment settings are stored securely in appsettings.json, keeping them separate from the application code.

Error Handling: Global exception management implemented via custom ErrorHandlingMiddleware, ensuring consistent and secure error responses.

Validation: Strict adherence to REST principles.

🧪 Testing & Quality Assurance
32 xUnit tests cover the repositories and services:

Unit Testing: repositories and services tested with Moq and Moq.EntityFrameworkCore, without a database.

Integration Testing: repositories tested against a real SQL Server database. Each test class creates its own throwaway database and deletes it afterwards.

Running the tests: `dotnet test TestProject`. The integration tests use SQL Server LocalDB (`(localdb)\MSSQLLocalDB`, installed with Visual Studio) by default. To use another instance, set the `TEST_SQL_SERVER` environment variable, for example `.` or `localhost\SQLEXPRESS`.

Tracing: Comprehensive logging of all transactions in the rating table to track system activity.
