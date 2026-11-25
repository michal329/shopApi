# Shop API

A REST API for an online shop, built with **ASP.NET Core (.NET 9)**, **Entity Framework Core** and **SQL Server**, using a layered Clean Architecture and covered by **32 xUnit tests**.

The Angular front end (catalog, cart and admin pages) that uses this API lives in a separate repository: [michal329/shop](https://github.com/michal329/shop).

## Architecture

The solution is split into projects, one per layer:

| Project | Responsibility |
|---|---|
| `WebApiShop` | Controllers, middleware, dependency injection setup, AutoMapper profiles |
| `Services` | Business logic: order-sum validation, password strength, user rules |
| `Repositories` | Data access with EF Core |
| `Entities` | EF Core models and `ApiShopContext` (database first) |
| `DTOs` | Immutable `record` types passed between the API and the services |
| `TestProject` | Unit and integration tests |

Design choices:

- **Dependency injection** between all layers, so each layer can be tested with mocks.
- **Async all the way down**: controllers, services and EF Core queries are all asynchronous.
- **DTOs as records** keep entities out of the API and avoid circular references.
- **Global error handling** in a custom `ErrorHandlingMiddleware` that returns consistent error responses.
- **Request logging** to a `Rating` table through a middleware, plus diagnostic logging with **NLog**.

## API

| Resource | Endpoints |
|---|---|
| Products | list, filter by category, price and description with paging (`GET /api/products/Filter`), create, update, soft delete |
| Categories | list |
| Orders | list, get, create (the order sum is recalculated and validated on the server), update status |
| Users | register, login, get, update |
| Password | `POST /api/password/PasswordScore`: password strength score with zxcvbn |

Passwords are hashed with **BCrypt**. Swagger UI is available when running in development.

## Tech stack

C# · .NET 9 · ASP.NET Core Web API · EF Core · SQL Server · AutoMapper · NLog · BCrypt · zxcvbn · Swagger · xUnit · Moq

## Getting started

1. Create the database by running `database.sql` on SQL Server.
2. Set `ConnectionStrings:DefaultConnection` in `WebApiShop/appsettings.json` to your server.
3. Run the API:

   ```bash
   dotnet run --project WebApiShop
   ```

## Tests

```bash
dotnet test TestProject
```

- **Unit tests** cover the repositories and services with Moq and Moq.EntityFrameworkCore, without a database.
- **Integration tests** run the repositories against a real SQL Server database. Each test class creates its own throwaway database and deletes it afterwards.

The integration tests use SQL Server LocalDB (`(localdb)\MSSQLLocalDB`, installed with Visual Studio) by default. To use another instance, set the `TEST_SQL_SERVER` environment variable, for example `.` or `localhost\SQLEXPRESS`.
