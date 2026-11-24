# PeopleHub

A small bilingual (English / Arabic, with RTL) HR management **demo project** built with ASP.NET Core 8 MVC.
It is a sample app with fictional data, made to demonstrate architecture and product thinking. It is not client work and not a production HR system.

## What it does

- **Employees** (HR only): search and filter by department and status, add, edit, terminate. Each employee has an English and an Arabic name.
- **Leave requests**: employees request annual, sick or unpaid leave. HR approves, rejects or cancels.
- **Leave rules** enforced in the domain layer:
  - Friday and Saturday (the Egyptian weekend) are not counted as working days.
  - Annual leave cannot exceed the remaining balance. The balance is deducted on approval and restored if an approved request is cancelled.
  - Overlapping requests are blocked. Only sick leave can start in the past.
  - A request can be decided only once.
- **Dashboard**: headcount, people on leave, pending requests, monthly payroll (HR only) and headcount by department.
- **Auth and roles**: cookie authentication, PBKDF2 password hashing, `HR` and `Employee` roles, antiforgery protection on every POST.
- **Localization**: English and Arabic UI with a language switch and right-to-left layout.

## Architecture

```
src/
  PeopleHub.Domain          entities, business rules, repository interface, password hasher
  PeopleHub.Application     use-case services (employees, leave, dashboard, auth)
  PeopleHub.Infrastructure  EF Core + SQLite, seed data
  PeopleHub.Web             MVC controllers, Razor views, localization, CSS (no frontend build step)
tests/
  PeopleHub.Tests           xUnit tests for the leave rules and password hashing
```

Dependencies point inward: Web -> Infrastructure -> Application -> Domain.

## Run locally

Requires the .NET 8 SDK.

```sh
dotnet test
dotnet run --project src/PeopleHub.Web --urls http://localhost:5000
```

A SQLite database is created and seeded on first start. In Development the HR password is `Admin123!`.

| Role | Email | Password |
| --- | --- | --- |
| HR | hr@peoplehub.local | `Admin123!` (Development only) |
| Employee | employee@peoplehub.local | `Demo123!` |

Outside Development the HR password comes from the `HR_SEED_PASSWORD` environment variable. If it is not set, the HR account stays locked.

## Deploy (Docker, for example on Render)

Create a Web Service from this repository with the Docker runtime and set `HR_SEED_PASSWORD`. The container listens on port 8080.
On a free instance the filesystem is ephemeral, so the demo database reseeds after every restart or redeploy.

## Limits

Demo scope only: no migrations (the schema uses `EnsureCreated`), no payroll calculation, no attendance, no file uploads, no email notifications. Salaries and people are fictional.
