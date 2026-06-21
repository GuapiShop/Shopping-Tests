# Shopping-Tests

xUnit test project for the **GuapiShop** backend API. Covers authentication, JWT generation/validation, and refresh token rotation using an in-memory Entity Framework Core database.

## Tech Stack

- **xUnit** for testing
- **Entity Framework Core 10.0.9 (InMemory)** for isolated, database-free test runs
- **BCrypt.Net-Next** for password hashing in seeded test data
- Targets **.NET 10**, referencing the GuapiShop backend project

## Project Structure

```
Shopping-Tests/
├── Helpers/
│   ├── InMemoryDb.cs          # In-memory DbContext factory
│   ├── TestConfiguration.cs   # Shared fake IConfiguration (JWT settings)
│   └── TestDataSeeder.cs      # Shared user-seeding helper
├── Controllers/
│   └── AuthControllerTest.cs  # HTTP-level auth behavior
├── Services/
│   └── JwtServiceTest.cs      # JwtService unit tests
└── Shopping.Tests.csproj
```

## Prerequisites

- .NET 10 SDK
- A local clone of the **GuapiShop** backend project, referenced via `ProjectReference` in `Shopping.Tests.csproj`

## Running the Tests

```bash
dotnet restore
dotnet test
```

For detailed output per test:

```bash
dotnet test --logger "console;verbosity=detailed"
```

### Important: Package Version Alignment

All EF Core-family packages in `Shopping-Tests.csproj` (e.g. `Microsoft.EntityFrameworkCore.InMemory`) **must match** the resolved EF Core version used by **GuapiShop** (currently `10.0.9`). Mismatched versions cause a `MissingMethodException` at runtime due to binary incompatibility between assemblies, even if both restore successfully.

To check the resolved EF Core version in GuapiShop, run from its project folder:

```bash
dotnet list package --include-transitive
```

Avoid using Visual Studio's "Update All NuGet Packages" on this test project, since it can silently reintroduce version mismatches against GuapiShop.

## Generating a Test Coverage Report

### 1. Collect coverage data

From the `Shopping-Tests` folder:

```bash
dotnet test --collect:"XPlat Code Coverage"
```

This produces a `coverage.cobertura.xml` file under `TestResults/<guid>/`.

### 2. Install ReportGenerator (one-time setup)

```bash
dotnet tool install -g dotnet-reportgenerator-globaltool
```

### 3. Generate an HTML report

```bash
reportgenerator -reports:"TestResults/**/coverage.cobertura.xml" -targetdir:"CoverageReport" -reporttypes:Html
```

Open `CoverageReport/index.html` in a browser to view line-by-line coverage per class.

### One-line combined command

```bash
dotnet test --collect:"XPlat Code Coverage" && reportgenerator -reports:"TestResults/**/coverage.cobertura.xml" -targetdir:"CoverageReport" -reporttypes:Html
```

> **Note:** `TestResults/` and `CoverageReport/` are generated artifacts and should not be committed. They're already covered in `.gitignore`.

## What These Tests Cover

The tests in this project exercise GuapiShop's authentication flow:

### Login (`POST /api/auth/login`)

Tests verify successful login returns an access token and refresh token, and that empty email, empty password, or invalid credentials throw the correct exceptions (`EmptyEmailException`, `EmptyPasswordException`, `InvalidCredentialsException`).

### Refresh (`POST /api/auth/refresh`)

Tests verify a valid refresh token returns a new access token and new refresh token (rotation), that a reused or expired refresh token throws `InvalidRefreshTokenException`, and that an unknown token is rejected.

### Token Validation

Tests verify `JwtService.ValidateToken` correctly rejects expired tokens, tokens signed with the wrong key, and tampered tokens, while accepting valid ones.
