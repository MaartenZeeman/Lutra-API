# Lutra API

REST API for Verspakketten, built with .NET 10 and PostgreSQL.

## Prerequisites

- [Docker](https://docs.docker.com/get-docker/) installed and running
- A PostgreSQL database reachable from the container

## Running with Docker

### 1. Build the image

Run this command from the **root of the repository** (where `Lutra.sln` lives), because the
Dockerfile copies the full solution source tree during the build:

```bash
docker build -f Lutra.API/Dockerfile -t lutra-api .
```

### 2. Run the container

Supply the PostgreSQL connection string via the `ConnectionStrings__LutraDb` environment variable:

```bash
docker run -d \
  --name lutra-api \
  -p 8080:8080 \
  -e ConnectionStrings__LutraDb="Host=<host>;Username=<user>;Password=<password>;Database=Lutra" \
  lutra-api
```

> **How it works:** ASP.NET Core maps environment variables to configuration keys by replacing
> `__` with `:`, so `ConnectionStrings__LutraDb` is equivalent to `ConnectionStrings:LutraDb`
> in `appsettings.json`. No code changes are needed.

The API will be available at `http://localhost:8080`.

### 3. (Optional) docker-compose

Create a `docker-compose.yml` alongside this README and adjust the values to your environment:

```yaml
services:
  api:
	image: lutra-api
	build:
	  context: .
	  dockerfile: Lutra.API/Dockerfile
	ports:
	  - "8080:8080"
	environment:
	  - ConnectionStrings__LutraDb=Host=db;Username=user;Password=secret;Database=Lutra
	depends_on:
	  - db

  db:
	image: postgres:17
	environment:
	  POSTGRES_USER: user
	  POSTGRES_PASSWORD: secret
	  POSTGRES_DB: Lutra
	volumes:
	  - pgdata:/var/lib/postgresql/data

volumes:
  pgdata:
```

Then run:

```bash
docker compose up -d
```

## Configuration reference

All settings can be overridden with environment variables using the `__` separator.

| Environment variable | Default | Description |
|---|---|---|
| `ConnectionStrings__LutraDb` | *(empty)* | Full Npgsql connection string to the PostgreSQL database |
| `OpenRouter__ApiKey` | *(empty)* | OpenRouter API key used by the verspakket import endpoint |
| `OpenRouter__Model` | `nvidia/nemotron-3-super-120b-a12b:free` | OpenRouter model used for product-page extraction |
| `OpenRouter__BaseUrl` | `https://openrouter.ai/api/v1` | OpenRouter API base URL |
| `OpenRouter__AllowedHosts__0` | `ah.nl` | Retailer host allowed for import (list, add more with increasing index) |
| `ASPNETCORE_ENVIRONMENT` | `Production` | Set to `Development` to enable Scalar API docs at `/scalar/v1` |

## Importing verspakketten

`POST /api/verspakketten/import` accepts `{ "url": "https://www.ah.nl/product/..." }`. The API
fetches the product page, extracts the details with the configured OpenRouter model, downloads the
product photos, and stores the verspakket. It returns `201 Created` with the new ID, or `200 OK`
with the existing ID when the same normalized source URL was already imported. Configure the API
key with `OpenRouter__ApiKey` (never commit it).

> **Security note:** The endpoint fetches user-supplied URLs. Product hosts are restricted to
> `OpenRouter:AllowedHosts`, image hosts to `OpenRouter:AllowedImageHosts`, and all connections are
> blocked from resolving to non-public addresses. Authentication is not yet implemented, so expose
> this endpoint only in trusted environments.


## Development

See the individual project READMEs or the `.github/copilot-instructions.md` for architecture
and contribution guidelines.
