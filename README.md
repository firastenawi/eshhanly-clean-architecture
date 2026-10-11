# اشحنلي — Eshhanly

منصة لمشاركة شواحن ومواقف السيارات الكهربائية بين الأفراد (P2P EV charging & parking sharing).

**Stack:** .NET 9 · Blazor Web App (MudBlazor, RTL) · Minimal APIs · Clean Architecture + MediatR · EF Core + PostgreSQL/PostGIS

```
src/
  Eshhanly.Domain          entities & business rules (no framework dependencies)
  Eshhanly.Application     use cases (commands/queries) — Stations/, Bookings/, Vehicles/
  Eshhanly.Infrastructure  EF Core, Identity, migrations, background worker
  Eshhanly.Web             Blazor UI + REST API (/api/v1) — composition root
tests/                     Domain, Application, Architecture tests
```

## Run locally

Needs PostgreSQL with PostGIS (or a free [Neon](https://neon.tech) database). Put the connection string in
`src/Eshhanly.Web/appsettings.Development.json`, then:

```bash
dotnet tool restore
dotnet ef database update --project src/Eshhanly.Infrastructure --startup-project src/Eshhanly.Web
dotnet run --project src/Eshhanly.Web
```

- Web: http://localhost:5279
- API docs (try requests): http://localhost:5279/scalar/v1
- Health: http://localhost:5279/health

## API quick tour

| Method | Route | Who |
|---|---|---|
| POST | `/api/v1/auth/register` · `/auth/login` · `/auth/refresh` | anyone |
| GET | `/api/v1/stations/search?lat=33.51&lng=36.27&radiusKm=10` | anyone |
| POST | `/api/v1/stations` | signed in (becomes host) |
| POST | `/api/v1/stations/{id}/review` | admin |
| POST | `/api/v1/vehicles` | signed in |
| POST | `/api/v1/bookings/quote` · `/api/v1/bookings` | signed in |
| GET | `/api/v1/bookings/mine` | signed in |
| POST | `/api/v1/bookings/{id}/cancel` · `/{id}/respond` | guest / host |

Login returns `accessToken` (1 h) + `refreshToken`; send `Authorization: Bearer <accessToken>`.
Add `?useCookies=true` to login for browser (Blazor) sessions.

## Deploy (free): GitHub → Render + Neon

1. **Neon**: create project → database `eshhanly` → in the SQL editor run `CREATE EXTENSION postgis;`.
   Convert the connection string to Npgsql format:
   `Host=ep-xxx.neon.tech;Database=eshhanly;Username=...;Password=...;SSL Mode=Require`
2. **GitHub**: push this repo (branch `main`). The workflow builds, tests and builds the Docker image on every push/PR.
3. **Render**: New → **Blueprint** → select the repo (uses `render.yaml`). Fill `ConnectionStrings__Database`.
   Migrations run automatically on startup.
4. **Auto-deploy after green tests**: Render → service → Settings → *Deploy Hook* → copy URL →
   GitHub → Settings → Secrets → Actions → `RENDER_DEPLOY_HOOK_URL`.
5. **First admin**: register via `/api/v1/auth/register`, set `Admin__Email` on Render to that email, redeploy.

The app is at `https://eshhanly.onrender.com` (free plan sleeps after 15 min idle; first request takes ~1 min).
