# Shop API

A REST API for an online shop, built with **ASP.NET Core (.NET 9)**, **Entity Framework Core** and **SQL Server**, using a layered Clean Architecture. It has **Redis caching** with invalidation, **JWT authentication in an HTTP-only cookie** with role-based authorization, **sliding-window rate limiting**, and **45 xUnit tests**.

The Angular front end (catalog, cart and admin pages) that uses this API lives in a separate repository: [michal329/shop](https://github.com/michal329/shop).

## Architecture

The solution is split into projects, one per layer:

| Project | Responsibility |
|---|---|
| `WebApiShop` | Controllers, middleware, authentication and rate-limit setup, AutoMapper profiles |
| `Services` | Business logic, caching, token creation, order-sum validation, password strength |
| `Repositories` | Data access with EF Core |
| `Entities` | EF Core models and `ApiShopContext` (database first) |
| `DTOs` | Immutable `record` types passed between the API and the services |
| `TestProject` | Unit and integration tests |

Design choices:

- **Dependency injection** between all layers, so each layer can be tested with mocks.
- **Async all the way down**: controllers, services and EF Core queries are all asynchronous.
- **DTOs as records** keep entities out of the API and avoid circular references. The user DTO never includes the password hash.
- **Global error handling** in a custom `ErrorHandlingMiddleware` that returns consistent error responses.
- **Request logging** to a `Rating` table through a middleware, plus diagnostic logging with **NLog**.

## Caching (Redis)

Product and category reads use **cache-aside** in the service layer (`Services/Caching`):

- `GET /api/Products`, `GET /api/Products/Filter` (every filter and page combination) and `GET /api/Categories` are cached in Redis with a TTL from `appsettings.json` (`Cache:ProductsTtlSeconds`, `Cache:CategoriesTtlSeconds`).
- **Invalidation**: product keys include a version (`shop:products:v{version}:...`). Adding, updating or deleting a product writes a new version, which makes every cached product list unreachable at once, without searching for and deleting each filter/page key. The old entries expire by their TTL.
- **Redis is an optimization, not a dependency.** If Redis fails, the request is served from the database, and a small circuit breaker skips Redis for 30 seconds so an outage doesn't make every request wait for a Redis timeout.
- Without a `ConnectionStrings:Redis` setting, the app uses an in-memory cache instead.

Every key starts with `shop:`. To look inside the cache:

```bash
docker exec -it shop-redis redis-cli -a shop-dev-redis --no-auth-warning
KEYS shop:*
HGETALL shop:categories:all      # the cached value (stored by ASP.NET Core as a hash)
TTL shop:categories:all          # seconds until it expires
GET shop:products:version        # the current product version
```

## Authentication and authorization (JWT)

- **Login and registration** issue a JWT in the service layer (`AuthService`, `TokenService`) with the claims `sub` (user id), `email`, `name` and `role` (`Admin` or `Customer`).
- The controller puts the token in an **HTTP-only, Secure, SameSite=Strict cookie**: browser JavaScript can't read it (XSS can't steal it), the browser sends it with every request, and other sites can't send it (CSRF). `POST /api/User/Logout` deletes it, and `GET /api/User/Me` tells the client who is logged in.
- The JWT bearer handler reads the token from the cookie, or from an `Authorization: Bearer` header (for Swagger and other tools).
- **Deny by default**: every endpoint requires a signed-in user unless it is marked `[AllowAnonymous]`. Admin endpoints use a custom `[AuthorizeRoles(Roles.Admin)]` attribute. Without a valid token the API returns **401**; with a token for the wrong role, **403**.
- The signing key is not in `appsettings.json`. Locally it comes from `appsettings.Development.json`; anywhere else, set the `Jwt__Key` environment variable (at least 32 bytes). The app refuses to start without it.

| Endpoint | Who can call it |
|---|---|
| `GET /api/Products`, `GET /api/Products/Filter`, `GET /api/Categories` | Anyone |
| `POST /api/User` (register), `POST /api/User/Login`, `POST /api/User/Logout` | Anyone (register and login are rate limited, see below) |
| `POST /api/Password/PasswordScore` | Anyone |
| `GET /api/User/Me` | Signed-in user |
| `GET /api/User/{id}`, `PUT /api/User/{id}` | The user themself, or an admin |
| `POST /api/Orders` | Signed-in user (the order is always created for the signed-in user) |
| `GET /api/Orders/{id}` | The order's owner, or an admin |
| `PUT /api/Orders/{id}/status` | Signed-in user: admins set shipping statuses, the customer confirms delivery. The user comes from the token. |
| `GET /api/User`, `GET /api/Orders` | Admin |
| `POST`, `PUT`, `DELETE /api/Products` | Admin |

Passwords are hashed with **BCrypt** and checked for strength with **zxcvbn**.

## Rate limiting

ASP.NET Core's built-in rate limiter with **sliding windows**, per client IP (settings in `RateLimiting` in `appsettings.json`):

- **Global**: 100 requests per minute for every endpoint.
- **Login and registration**: 5 attempts per minute, against password guessing.

Over the limit, the API returns **429 Too Many Requests**.

## Tech stack

C# · .NET 9 · ASP.NET Core Web API · EF Core · SQL Server · Redis · JWT · AutoMapper · NLog · BCrypt · zxcvbn · Swagger · Docker Compose · xUnit · Moq

## Getting started

1. Create the database by running `database.sql` on SQL Server.
2. Set `ConnectionStrings:DefaultConnection` in `WebApiShop/appsettings.json` to your server.
3. Start Redis (Docker Desktop must be running). The password is in `docker-compose.yml`; to change it, copy `.env.example` to `.env` and update `ConnectionStrings:Redis` in `WebApiShop/appsettings.Development.json` to match.

   ```bash
   docker compose up -d
   ```

4. Run the API, then open Swagger at `/swagger`:

   ```bash
   dotnet run --project WebApiShop
   ```

## Tests

```bash
dotnet test TestProject
```

- **Unit tests** cover the repositories and services with Moq and Moq.EntityFrameworkCore, without a database: caching (hits, invalidation on add/update/delete, Redis failures and the circuit breaker) and JWT creation (claims, roles, expiry, signature).
- **Integration tests** run the repositories against a real SQL Server database. Each test class creates its own throwaway database and deletes it afterwards.

The integration tests use SQL Server LocalDB (`(localdb)\MSSQLLocalDB`, installed with Visual Studio) by default. To use another instance, set the `TEST_SQL_SERVER` environment variable, for example `.` or `localhost\SQLEXPRESS`.
