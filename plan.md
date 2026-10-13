# CeeSharp Library

A small library system: staff manage the catalog and loans, members browse books and see what they have checked out.

The UI is a React app. The API is ASP.NET Core. PostgreSQL runs in Docker, using the Compose file already in this repo.

Work happens in two phases. Auth has to be finished and usable before any library screens are built.

## What this is

A librarian can add books, record how many physical copies the library owns, check a copy out to a member, and check it back in. A member can sign in, search the catalog, and see their own loans, including what is overdue.

Two roles:

- **Librarian** manages the catalog, copies, members, and loans.
- **Member** uses the catalog and can only see their own account and loans.

The first librarian account is created from configuration when the API starts, so there is a way in before anyone can register. Public registration creates a member.

## Stack

| Piece | Choice | Why |
| --- | --- | --- |
| UI | React, TypeScript, Vite | Replaces the Blazor client. Vite is the app shell. |
| Routing and data | React Router, TanStack Query | Pages and server state stay separate. |
| Forms | React Hook Form, Zod | Shared validation for register, login, and later book forms. |
| API | ASP.NET Core 8 Web API | C# stays on the server. Blazor is gone. |
| Auth | ASP.NET Core Identity, JWT bearer tokens | Accounts, password hashing, and roles. |
| Data | EF Core, Npgsql | Migrations against the Postgres database. |
| Database | PostgreSQL 16 | Already defined in `docker-compose.yml`. |

The API and the React app are separate projects. The React app talks to the API over HTTP. It does not talk to Postgres.

## Tools

Install these before phase 1:

- **Docker** — Postgres only. From the repo root: `docker compose up -d`. It listens on `localhost:5434`. Database, user, and password are all `ceesharp`.
- **.NET 8 SDK** — the API.
- **`dotnet-ef` 8.0.31** — migrations. Install it as a local tool once `server/` exists (`dotnet tool install dotnet-ef --version 8.0.31`). The previous tool manifest was removed with the Blazor solution.
- **Node.js 22 LTS and npm** — the React app.
- **Git** — already in use.

Useful while building, not required to compile:

- `psql` or any Postgres client, pointed at port `5434`.
- A REST client for trying the API before the screens exist.

## Packages

`dotnet new webapi` and `npm create vite` create the two projects. These are the packages added on top. Versions below are the current .NET 8 and npm releases. Keep the three Microsoft packages and `dotnet-ef` on the same `8.0.x` number.

### .NET, in `server/`

```bash
dotnet add package Microsoft.AspNetCore.Identity.EntityFrameworkCore --version 8.0.31
dotnet add package Microsoft.EntityFrameworkCore.Design --version 8.0.31
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL --version 8.0.11
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer --version 8.0.31
```

| Package | What it is for |
| --- | --- |
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore` | Users, password hashing, roles, and the Identity tables. EF Core comes along with it. |
| `Microsoft.EntityFrameworkCore.Design` | `dotnet ef` migrations. Design-time only. |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | Postgres provider. `8.0.11` is the last 8.0 release of this package. |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | Validate the access token on protected routes. |

### React, in `client/`

Vite already installs `react`, `react-dom`, `typescript`, and `vite`. Add these after the app exists:

```bash
npm install react-router-dom@7.18.4 @tanstack/react-query@5.104.2 react-hook-form@7.90.0 zod@4.6.7 @hookform/resolvers@5.9.1
```

| Package | What it is for |
| --- | --- |
| `react-router-dom` | `/login`, `/register`, `/`, and `/librarian`. |
| `@tanstack/react-query` | Login session, catalog, and loans fetched from the API. |
| `react-hook-form` | Register, login, and later the book and checkout forms. |
| `zod` | Field rules shared by those forms. |
| `@hookform/resolvers` | Connects a Zod schema to React Hook Form. |

No UI kit. Phase 1 screens are plain form elements.

## Repo layout

```
cee-sharp/
  docker-compose.yml    Postgres. Leave this as the local database.
  plan.md
  server/               ASP.NET Core Web API
  client/               Vite + React + TypeScript
```

`server/` and `client/` do not exist yet. They get created at the start of phase 1.

## Phase 1 — Auth

Goal: a person can register, sign in, stay signed in across a refresh, and land on a different home screen depending on role. Librarian-only API routes reject members.

### Accounts

- Email and password. Email is the username.
- Fields: first name, last name, email, password.
- Roles: `Librarian` and `Member`. A new registration is a member.
- Passwords are hashed by Identity. The API never returns a password hash.
- One librarian is seeded on startup from environment variables (`Librarian__Email`, `Librarian__Password`, name fields). If that user already exists, startup does nothing.

### API

| Method | Path | Who |
| --- | --- | --- |
| POST | `/api/auth/register` | Anonymous |
| POST | `/api/auth/login` | Anonymous |
| GET | `/api/auth/me` | Signed in |
| POST | `/api/auth/logout` | Signed in |

Login returns a JWT access token and a user payload: id, email, name, role. The React app stores the token in memory and mirrors it to `sessionStorage` so a refresh does not kick the user out. Logout clears both.

`/api/auth/me` is how the app restores a session. An expired or junk token gets `401`, and the app returns to the login page.

Validation errors come back as `400` with field messages. Duplicate email comes back as `409`.

### Screens

- `/login`
- `/register`
- `/` — member home, a placeholder until phase 2
- `/librarian` — librarian home, a placeholder until phase 2

Anonymous visitors hitting `/` or `/librarian` go to `/login`. A member hitting `/librarian` goes to `/`. A librarian hitting `/` goes to `/librarian`.

### Done when

- Register, login, logout, and refresh-while-signed-in all work in the browser.
- A member token cannot call a librarian-only endpoint.
- The seeded librarian can sign in.
- Postgres is the only database. No SQLite fallback.

## Phase 2 — Library

Goal: the librarian can run the shelf, and a member can see the catalog and their loans.

Build this only after phase 1 is done.

### Things we store

- **Book** — title, author, ISBN, genre, year published, short description. ISBN is unique when present.
- **Copy** — one physical item of a book. Status is `Available`, `OnLoan`, or `Lost`. A book with zero copies can exist in the catalog but cannot be checked out.
- **Loan** — which member has which copy, checked out at, due at, returned at. An open loan has no return time. Due date defaults to 14 days after checkout.

Members do not get a separate profile table. The Identity user is the member.

### What a librarian can do

- Add, edit, and remove books.
- Add copies, and mark a copy lost.
- Check an available copy out to a member.
- Check a copy back in.
- See loans that are still out, and which of those are past due.
- Search members by name or email when checking a book out.

Removing a book is refused while any of its copies are on loan.

### What a member can do

- Search the catalog by title, author, or ISBN.
- Open a book and see how many copies are available.
- See their open loans and their past loans.

Members cannot create books, copies, or loans. Checkout is a librarian action in this version.

### API, librarian

| Method | Path | Purpose |
| --- | --- | --- |
| GET | `/api/books` | Catalog search |
| POST | `/api/books` | Add a book |
| PUT | `/api/books/{id}` | Edit a book |
| DELETE | `/api/books/{id}` | Remove a book with no copies on loan |
| POST | `/api/books/{id}/copies` | Add a copy |
| POST | `/api/copies/{id}/lose` | Mark a copy lost |
| GET | `/api/loans` | Open loans, with an overdue filter |
| POST | `/api/loans` | Check out a copy to a member |
| POST | `/api/loans/{id}/return` | Check a copy back in |
| GET | `/api/members` | Search members |

### API, member

| Method | Path | Purpose |
| --- | --- | --- |
| GET | `/api/books` | Same catalog search |
| GET | `/api/books/{id}` | One book plus available copy count |
| GET | `/api/me/loans` | That member's loans only |

### Screens

- Catalog, with search.
- Book detail.
- My loans, for members.
- Librarian: book form, copy list, checkout form, open loans, overdue list.

### Done when

- A librarian can add a book, add a copy, check it out, and check it in, all from the UI.
- The copy status follows the loan. Available, then on loan, then available again.
- A second checkout of the same copy is rejected.
- A member sees that loan on My loans and does not see anyone else's.
- Overdue loans show up when the due date is in the past and the book is still out.

## Order of work

1. Bring Postgres up with `docker compose up -d`.
2. Create `server/` and `client/`.
3. Identity, JWT, seed librarian, auth endpoints.
4. Login, register, and role-based routes in React.
5. Stop. Use the app. Fix auth before touching books.
6. Book, copy, and loan models, then a migration.
7. Librarian catalog and loan screens.
8. Member catalog and my-loans screens.

## Left for later

Holds, fines, email reminders, cover images, barcode scanning, and more than one library branch. None of that is part of phase 1 or phase 2.
