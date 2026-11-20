# RaceDay — PROG6212 POE Part 3

A two-project ASP.NET Core solution that satisfies Part 3 of the PROG6212 Portfolio of Evidence.

| Project | Type | Responsibility |
|---|---|---|
| `src/RaceDay.Api` | ASP.NET Core Web API | All data access, business rules, authentication and persistence (extended from Part 2). |
| `src/RaceDay.Mvc` | ASP.NET Core MVC | The Part 3 front-end. **Consumes the API for every data operation.** Contains no database context, no EF Core reference and no replicated business logic. |

---

## 1. How Part 3 requirements are met

### MVC application setup
- The MVC project talks to the Part 2 API exclusively through a single typed `HttpClient` (`Services/ApiClient.cs`). There is **no `DbContext`, no connection string and no EF Core package** in the MVC project.
- Registration and login are performed by calling `POST /api/auth/register` and `POST /api/auth/login`.
- Authenticated state and the user's role are stored in **session** (`AddSession`, `Services/SessionKeys.cs`) and survive navigation across every page.
- `Filters/SessionAuthAttribute.cs` protects every non-public controller/action. Unauthenticated users are redirected to `/Account/Login?returnUrl=…`; users with the wrong role are sent to `/Account/AccessDenied`.
- Each role gets a distinct interface and navigation:
  - **Organiser** → Dashboard, My Events (and per-event Categories, Enrolments, Results).
  - **Participant** → Events, My Enrolments, My Results.
  - **Guest** → Events, Log in, Create account.
- Logging out clears the session and redirects to the login page.

### Organiser interface
- **Dashboard** — totals for events, upcoming events, enrolments, unique participants and published results, plus enrolments per event and the next event dates.
- **Events** — create, edit and delete events with name, description, date, location, distance, event type, route info and an uploaded banner image.
- **Categories** — add, edit and delete age or distance categories per event, including distance, capacity and entry fee.
- **Enrolments** — every participant enrolled in a specific event, with their selected category, contact details and enrolment status.
- **Results** — capture finish times and finishing positions per participant, then publish results to participants.

### Participant interface
- **Home** — all upcoming events with search, event-type, location and include-past filters.
- **Event details** — full event information and available categories, with immediate enrolment into a chosen category.
- **My Enrolments** — every entered event with category and enrolment status, plus cancellation.
- **My Results** — personal race history across completed events showing event name, date, category, finish time and finishing position (e.g. *47th out of 312 finishers*).
- **Profile** — view and update personal information and upload a profile picture.

---

## 2. Running the solution

### Prerequisites
- .NET 10 SDK (the solution targets `net10.0`).

### Start everything (one step)
Start the **RaceDay.Mvc** project and the API is launched for you automatically.

- **Visual Studio:** open `RaceDay.sln` and press **F5** (or the green Start button). `RaceDay.Mvc` is the default startup project; it starts the API in the background, waits until it is ready, then opens the site at http://localhost:5090.
- **Command line:** `dotnet run --project src/RaceDay.Mvc` (or run `./run-all.ps1` / `./run-all.sh`).

The API's Swagger UI is available at http://localhost:5088/swagger (https://localhost:7018/swagger when the API is started with its `https` profile). Opening the API root (`/`) redirects to Swagger.

Auto-start only happens in the Development environment and only if nothing is already listening on `ApiBaseUrl`. To start the API yourself instead, set `"AutoStartApi": false` in `src/RaceDay.Mvc/appsettings.json`, then run `dotnet run` inside `src/RaceDay.Api` first.

The MVC app reads the API address from `src/RaceDay.Mvc/appsettings.json`:
```json
{ "ApiBaseUrl": "http://127.0.0.1:5088/" }
```

The SQLite database (`raceday.db`) is created and seeded automatically on first API start.

---

## 3. Demo accounts

| Role | Email | Password |
|---|---|---|
| Organiser | `thandiwe.mokoena@gmail.co.za` | `Password123!` |
| Participant | `lindiwe.dlamini@gmail.com` | `Password123!` |
| Participant | `sipho.nkosi@gmail.com` | `Password123!` |
| Participant | `ayesha.patel@gmail.com` | `Password123!` |
| Participant | `johan.vanwyk@gmail.com` | `Password123!` |

Seed data includes six events (four upcoming, two completed), eleven categories, ten enrolments and five published results, so both interfaces are populated immediately.

---

## 4. Project structure

```
RaceDayPart3/
├── README.md
└── src/
    ├── RaceDay.Api/
    │   ├── Controllers/        Auth, Events, Enrolments, Results, Users
    │   ├── Data/               RaceDayDbContext + seed data
    │   ├── DTOs/               Request/response contracts
    │   ├── Models/             EF Core entities
    │   ├── Migrations/         (schema created with EnsureCreated)
    │   └── Program.cs          JWT auth, CORS, Swagger, SQLite
    └── RaceDay.Mvc/
        ├── Controllers/        Account, Home, Participant, Organiser, Profile
        ├── Filters/            SessionAuthAttribute
        ├── Models/             Client-side DTOs and view models
        ├── Services/           ApiClient, FileUpload, SessionKeys
        ├── Views/              Razor views for all three role experiences
        └── wwwroot/            site.css, site.js, uploads/
```

### Key API endpoints used by the MVC app
```
POST   /api/auth/register            POST   /api/auth/login
GET    /api/events                   GET    /api/events/{id}
GET    /api/events/mine              GET    /api/events/dashboard
POST   /api/events                   PUT    /api/events/{id}
DELETE /api/events/{id}
POST   /api/events/{id}/categories   PUT    /api/events/{id}/categories/{cid}
DELETE /api/events/{id}/categories/{cid}
GET    /api/events/{id}/enrolments   GET    /api/events/{id}/results
POST   /api/events/{id}/publish-results
POST   /api/categories/{cid}/enrolments
GET    /api/users/me/enrolments      DELETE /api/enrolments/{id}
POST   /api/enrolments/{id}/results  PUT    /api/results/{id}
GET    /api/users/me/results         GET/PUT /api/users/me
PUT    /api/users/me/password
```

---

## 5. Design notes

- Custom CSS design system in `wwwroot/css/site.css` (no external CDN), responsive down to mobile and print-friendly.
- Uploaded banners and profile pictures are validated (type + 5 MB limit) and stored under `wwwroot/uploads/{events|profiles}`; the relative URL is persisted through the API.
- JWT bearer tokens are attached to every API call by `ApiClient` from the session; the MVC layer never trusts client-supplied roles.
- Result publishing is explicit: captured results stay hidden from participants until the organiser publishes them.
