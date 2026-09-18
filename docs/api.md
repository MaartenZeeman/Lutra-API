# Lutra API Reference

This document is the single source of truth for the HTTP API. It describes every endpoint, every
request and response contract, the enums, validation rules, limits and error handling.

- Base URL: `http://localhost:8080` (Docker default) or wherever the API is deployed.
- All request and response bodies are `application/json` unless stated otherwise. DateTime values
  are ISO 8601 UTC (for example `2026-09-18T09:12:33.5234567Z`).
- There is currently **no authentication**. Content can be soft-deleted in the database, and the API
  only returns records that are not deleted.
- Identifiers are GUIDs (UUIDs).
- Enum values in JSON **bodies and responses are serialized as integers** (no string-enum converter is
  configured). Enum **query-string** parameters accept both the name and the number.
- Interactive OpenAPI documentation (Scalar) is served in Development at `/scalar/v1`; the raw
  OpenAPI document is at `/openapi/v1.json`.

---

## Endpoints overview

| Method | Path                                     | Description                                          |
|--------|------------------------------------------|------------------------------------------------------|
| GET    | `/health`                                | Liveness check                                       |
| GET    | `/api/supermarkten`                      | List supermarkets                                    |
| POST   | `/api/supermarkten`                      | Create a supermarket                                 |
| PUT    | `/api/supermarkten/{id}`                 | Update a supermarket                                 |
| GET    | `/api/verspakketten`                     | List verspakketten (pageable, sortable)              |
| GET    | `/api/verspakketten/{id}`                | Get one verspakket with all details                  |
| POST   | `/api/verspakketten`                    | Create a verspakket                                  |
| PUT    | `/api/verspakketten/{id}`                | Update a verspakket                                  |
| POST   | `/api/verspakketten/import`              | Queue an import from a retailer product page          |
| POST   | `/api/verspakketten/{id}/beoordelingen`  | Add a beoordeling (rating) to a verspakket            |
| GET   | `/api/background-commands/{id}`         | Status of a queued background job                     |

> The endpoints use a REST design. "Verspakket" is a Dutch term for a meal kit / ready-made dish
> package.

---

## Getting started

A minimal workflow:

1. Create a supermarket:
   ```bash
   curl -X POST http://localhost:8080/api/supermarkten \
     -H "Content-Type: application/json" \
     -d '{"naam":"Jumbo"}'
   ```
   Record the returned `"id"`.

2. Create a verspakket that belongs to that supermarket:
   ```bash
   curl -X POST http://localhost:8080/api/verspakketten \
     -H "Content-Type: application/json" \
     -d '{"naam":"Pasta Bolognese","prijsInCenten":899,"aantalPersonen":2,"supermarktId":"SUPERMARKT_ID"}'
   ```
   Replace `SUPERMARKT_ID` in the body with the identifier from step 1.

3. List verspakketten:
   ```bash
   curl "http://localhost:8080/api/verspakketten"
   ```

4. Get the full details of one verspakket:
   ```bash
   curl "http://localhost:8080/api/verspakketten/{id}"
   ```

5. (Optional) Import a product from a supported retail page:
   ```bash
   curl -X POST http://localhost:8080/api/verspakketten/import \
     -H "Content-Type: application/json" \
     -d '{"url":"https://www.ah.nl/product/84100"}'
   ```
   Then poll the returned background-command id until it reaches `Succeeded`:
   ```bash
   curl "http://localhost:8080/api/background-commands/{jobId}"
   ```

---

## Health

### `GET /health`

Liveness check used by orchestration and load balancers.

Responses:

- `200 OK` with an empty body.

---

## Supermarkten

### `GET /api/supermarkten`

Lists supermarkets, ordered by `naam` ascending. Pageable.

Query parameters:

| Parameter | Type    | Default | Description                                           |
|-----------|---------|---------|-------------------------------------------------------|
| `skip`    | integer | `0`     | Number of items to skip. Negative values are clamped to `0`. |
| `take`    | integer | `50`    | Maximum number of items to return. Clamped to the range `[1, 200]`. |

Examples:

```bash
curl "http://localhost:8080/api/supermarkten"
curl "http://localhost:8080/api/supermarkten?skip=20&take=20"
```

`200 OK` — response body:

```json
{
  "supermarkten": [
    { "id": "0a3f9d4e-...", "naam": "Albert Heijn" },
    { "id": "9b1c4d7a-...", "naam": "Jumbo" },
    { "id": "4c2f6e8d-...", "naam": "Lidl" },
    { "id": "7e5a3b9c-...", "naam": "Poiesz" }
  ]
}
```

> The `take` bound of `200` protects the API from clients that ask for the whole table.

### `POST /api/supermarkten`

Creates a supermarket.

Request body (`SupermarktRequest`):

```json
{ "naam": "Jumbo" }
```

Fields:

| Field  | Type   | Required | Validation                          |
|--------|--------|----------|-------------------------------------|
| `naam` | string | yes      | Maximum 50 characters, non-empty    |

`201 Created` — response body:

```json
{ "id": "9b1c4d7a-59d8-4e6a-8f0c-1234567890ab" }
```

- `201 Created` on success.
- `400 Bad Request` when `naam` is empty or longer than 50 characters.

### `PUT /api/supermarkten/{id}`

Updates a supermarket.

Path parameters:

| Parameter | Type | Description                |
|-----------|------|----------------------------|
| `id`      | GUID | The supermarket identifier |

Request body: same `SupermarktRequest` as create.

- `204 No Content` on success (empty body).
- `400 Bad Request` when `naam` is invalid.
- `404 Not Found` when no supermarket with that `id` exists.

---

## Verspakketten

### `GET /api/verspakketten`

Lists verspakketten. Pageable and sortable. Only returns the summary fields for each verspakket, and
the main foto only (see `GET /api/verspakketten/{id}` for the full detail).

Query parameters:

| Parameter       | Type    | Default    | Description                                                                 |
|-----------------|---------|-----------|-------------------------------------------------------------------------------|
| `skip`            | integer | `0`        | Items to skip. Negative values are clamped to `0`.                            |
| `take`            | integer | `50`       | Maximum items to return. Clamped to the range `[1, 200]`.                     |
| `sortField`       | enum    | `Naam`     | `Naam` (0), `PrijsInCenten` (1), `AverageCijferSmaak` (2) or `AverageCijferBereiden` (3). |
| `sortDirection`   | enum    | `Ascending`| `Ascending` (0) or `Descending` (1).                                           |

Examples:

```bash
curl "http://localhost:8080/api/verspakketten"
curl "http://localhost:8080/api/verspakketten?skip=20&take=20&sortField=AverageCijferSmaak&sortDirection=Descending"
curl "http://localhost:8080/api/verspakketten?take=1000&sortField=2&sortDirection=1"
```

`200 OK` — response body:

```json
{
  "verspakketten": [
    {
      "id": "a6d2c4e8-...",
      "naam": "Jumbo Satépannetje Gesneden Verspakket 4 Personen",
      "prijsInCenten": 749,
      "aantalPersonen": 4,
      "averageCijferSmaak": null,
      "averageCijferBereiden": null,
      "foto": null,
      "supermarkt": { "id": "9b1c4d7a-...", "naam": "Jumbo" }
    }
  ]
}
```

Remarks:

- `averageCijferSmaak` and `averageCijferBereiden` are `null` until at least one beoordeling exists.
- `foto` is the **main** image only, returned as base64. It is `null` when the verspakket has no main
  image.
- The `sortField`/`sortDirection` query parameters accept enum **names** (`Naam`, `Descending`, …) as
  well as numbers.
- `take` is clamped to max 200 and at least 1, so `take=0` still returns the first item.

### `GET /api/verspakketten/{id}`

Gets one verspakket with all details.

Path parameters:

| Parameter | Type | Description                |
|-----------|------|----------------------------|
| `id`      | GUID | The verspakket identifier  |

`200 OK` — response body:

```json
{
  "verspakket": {
    "id": "a6d2c4e8-4f1b-4c3d-9a2e-1234567890ab",
    "naam": "Pasta Bolognese",
    "prijsInCenten": 899,
    "aantalPersonen": 2,
    "averageCijferSmaak": 9.0,
    "averageCijferBereiden": 8.0,
    "beoordelingen": [
      { "cijferSmaak": 9, "cijferBereiden": 8, "aanbevolen": true, "tekst": "Heerlijk!" }
    ],
    "fotos": [
      { "id": "c3f4a1b2-...", "base64Data": "iVBORw0KGgoAAAANSUhEUg...", "isMainImage": true }
    ],
    "ingredienten": [
      { "naam": "Tomaten", "hoeveelheid": 400, "eenheid": 0, "inbegrepen": true },
      { "naam": "Gehakt", "hoeveelheid": 300, "eenheid": 0, "inbegrepen": false }
    ],
    "voedingswaarden": [
      { "basis": 0, "energieKj": 520, "energieKcal": 124, "vetten": 4.5, "waarvanVerzadigd": 1.2, "koolhydraten": 14, "waarvanSuikers": 2.1, "vezels": 2.4, "eiwitten": 5.8, "zout": 0.35 }
    ],
    "allergenen": [0, 6, 4],
    "supermarkt": { "id": "9b1c4d7a-59d8-4e6a-8f0c-1234567890ab", "naam": "Jumbo" }
  }
}
```

`200 OK` on success; `404 Not Found` when the verspakket does not (or no longer) exist.

### `POST /api/verspakketten`

Creates a verspakket. This is the main write endpoint and accepts almost all child data.

Request body (`CreateVerspakketRequest`):

```json
{
  "naam": "Pasta Bolognese",
  "prijsInCenten": 899,
  "aantalPersonen": 2,
  "supermarktId": "9b1c4d7a-59d8-4e6a-8f0c-1234567890ab",
  "beoordeling": null,
  "fotos": [],
  "ingredienten": [
    { "naam": "Tomaten", "hoeveelheid": 400, "eenheid": 0, "inbegrepen": true },
    { "naam": "Gehakt", "hoeveelheid": 300, "eenheid": 0, "inbegrepen": false }
  ],
  "voedingswaarden": [
    { "basis": 0, "energieKj": 520, "energieKcal": 124, "vetten": 4.5, "waarvanVerzadigd": 1.2, "koolhydraten": 14, "waarvanSuikers": 2.1, "vezels": 2.4, "eiwitten": 5.8, "zout":0.35 }
  ],
  "allergenen": [0, 6, 4]
}
```

Top-level fields:

| Field              | Type        | Required | Validation / notes                                                            |
|--------------------|-------------|----------|------------------------------------------------------------------------------|
| `naam`            | string      | yes      | Non-empty, maximum 255 characters                                             |
| `prijsInCenten`    | integer     | no       | Cent amount, `>= 0`. Optional on create (null allowed)                        |
| `aantalPersonen`   | integer     | yes      | Between 1 and 10                                                              |
| `supermarktId`      | GUID        | yes      | Must reference an existing supermarket                                        |
| `beoordeling`       | object|null | no       | `Beoordeling` object                                                          |
| `fotos`             | array|null  | no       | `VerspakketFoto` list (see limits below)                                      |
| `ingredienten`     | array|null  | no       | `Ingredient` list                                                             |
| `voedingswaarden`  | array|null  | no       | `Voedingswaarde` list, max 2 entries, max one per `basis`                      |
| `allergenen`       | array|null  | no       | `Allergeen` enum values, unique                                               |

`201 Created` — response body (with `Location` header to `GET /api/verspakketten/{id}`):

```json
{ "id": "a6d2c4e8-4f1b-4c3d-9a2e-1234567890ab" }
```

Error responses:

- `400 Bad Request` when any validation rule fails (invalid foto base64, too many fotos, duplicate
  allergens, `waarvanVerzadigd` > `vetten`, …).
- `404 Not Found` when `supermarktId` does not exist.

### `PUT /api/verspakketten/{id}`

Updates (replaces) a verspakket.

Path parameters:

| Parameter | Type | Description                |
|-----------|------|----------------------------|
| `id`      | GUID | The verspakket identifier  |

Request body (`UpdateVerspakketRequest`):

```json
{
  "naam": "Pasta Bolognese",
  "prijsInCenten": 999,
  "aantalPersonen": 2,
  "supermarktId": "9b1c4d7a-59d8-4e6a-8f0c-1234567890ab",
  "fotos": [],
  "ingredienten": [],
  "voedingswaarden": [],
  "allergenen": []
}
```

Field semantics:

- The top-level scalar fields are required and replace the stored values.
- When `fotos`, `ingredienten`, `voedingswaarden` or `allergenen` are supplied they **replace** the
  entire stored child list. When `null` the existing children are left untouched.

`204 No Content` on success (empty body). `400 Bad Request` on validation failure. `404 Not Found`
when the verspakket or the `supermarktId` does not exist.

### `POST /api/verspakketten/{id}/beoordelingen`

Adds one beoordeling (rating) to a verspakket.

Request body (`AddBeoordelingRequest`):

```json
{
  "cijferSmaak": 9,
  "cijferBereiden": 8,
  "aanbevolen": true,
  "tekst": "Heerlijk!"
}
```

Fields:

| Field           | Type    | Required | Validation             |
|-----------------|---------|----------|------------------------|
| `cijferSmaak`   | integer | yes      | Between 1 and 10     |
| `cijferBereiden`| integer | yes      | Between 1 and 10     |
| `aanbevolen`    | boolean | yes      |                        |
| `tekst`         | string  | no       | Maximum 1024 characters |

`201 Created` — response body:

```json
{ "id": "71f3a9d2-..." }
```

`404 Not Found` when the verspakket does not exist.

### `POST /api/verspakketten/import`

Queues an import of a verspakket from a supported retailer product page. Processing runs in the
background; poll `GET /api/background-commands/{id}` to track it.

Request body (`ImportVerspakketRequest`):

```json
{ "url": "https://www.ah.nl/product/1" }
```

Fields:

| Field | Type   | Required | Validation                                                            |
|-------|--------|----------|----------------------------------------------------------------------|
| `url` | string | yes      | Must be an absolute `https` URL. Stored normalized (see below).        |

Behavior:

- The URL is normalized synchronously: lower-cased host, no default port, no fragment, trailing
  slash stripped and known tracking parameters (`utm_*`, `gclid`, `fbclid`, `msclkid`, `ref`, …)
  removed. Two requests that normalize to the same URL are deduplicated: when an **active** job for
  the same normalized URL already exists, that job is returned with `existing: true`.
- If the number of active jobs is at the `BackgroundCommands:MaxPendingJobs` limit (default 50) the
  request is rejected with `429 Too Many Requests`.
- The endpoint is additionally rate limited per IP: `10` requests per minute per IP (any excess is
  returned with `429` and an empty body).

`202 Accepted` — response body (with `Location` header to `GET /api/background-commands/{id}`):

```json
{
  "id": "c7b4e6a1-8d2f-4a9b-b3c1-0f0e9d8c7b6a",
  "status": 0,
  "attemptCount": 0,
  "nextAttemptAt": "2026-09-18T10:00:00.0000000Z",
  "existing": false
}
```

- `status` is the `BackgroundCommandStatus` enum (`0` = `Queued`).
- `existing` is `true` when an already active job for the same normalized URL is returned.
- `400 Bad Request` for a non-`https`/invalid URL, `429 Too Many Requests` when the queue is full or
  the per-IP rate limit is hit.

> Security note: the endpoint fetches user-supplied URLs. Product hosts are restricted to the
> `OpenRouter:AllowedHosts` allowlist, image hosts to `OpenRouter:AllowedImageHosts`, and all
> connections are blocked from resolving to non-public addresses (SSRF protection). Because the API
> currently has no authentication, deploy this endpoint only in trusted environments.

---

## Background commands

### `GET /api/background-commands/{id}`

Gets the durable status of a background command job (for example an import).

Path parameters:

| Parameter | Type | Description             |
|-----------|------|-------------------------|
| `id`      | GUID | The background job id   |

`200 OK` — response body:

```json
{
  "id": "c7b4e6a1-8d2f-4a9b-b3c1-0f0e9d8c7b6a",
  "type": 1,
  "status": 3,
  "attemptCount": 1,
  "nextAttemptAt": "2026-09-18T10:00:00.0000000Z",
  "startedAt": "2026-09-18T09:59:50.0000000Z",
  "completedAt": "2026-09-18T09:59:55.0000000Z",
  "resultVerspakketId": "a6d2c4e8-4f1b-4c3d-9a2e-1234567890ab",
  "lastError": null
}
```

Fields:

| Field                | Type         | Description                                                       |
|----------------------|--------------|-------------------------------------------------------------------|
| `id`                 | GUID         | Job identifier                                                    |
| `type`               | enum         | `BackgroundCommandType`, `1` = `ImportVerspakket`                 |
| `status`             | enum         | `BackgroundCommandStatus`, see the lifecycle below                |
| `attemptCount`       | integer      | Number of execution attempts so far                                |
| `nextAttemptAt`      | DateTime     | When the job is next eligible to run                              |
| `startedAt`          | DateTime?    | When the first attempt started                                    |
| `completedAt`        | DateTime?    | When the job reached a terminal state                             |
| `resultVerspakketId` | GUID?        | The created/updated verspakket when the import succeeded          |
| `lastError`          | string?      | Last error message for `RetryScheduled`/`Failed` jobs             |

`404 Not Found` when the job does not exist.

### Import lifecycle

A successful import follows `Queued (0) → Processing (1) → Succeeded (3)` and exposes the resulting
verspakket through `resultVerspakketId`. Transient failures (network errors, timeouts, upstream
provider errors) go through `RetryScheduled (2)` and are retried after `BackgroundCommands:RetryDelayMinutes`
(default 5 minutes) up to `BackgroundCommands:MaxAttempts` (default 3) total attempts, then the job
becomes `Failed (4)` with `lastError` filled. Permanent failures (validation, unusable extracted
data, conflicts) fail immediately without retrying.

Jobs are persisted in PostgreSQL, survive API restarts, and are claimed with a lease and an
optimistic concurrency token so multiple replicas or a recovered worker never execute the same job
twice. `nextAttemptAt`, `startedAt`, `completedAt` and `lastError` describe the current lifecycle,
so poll `GET /api/background-commands/{id}` until the status is `3` (`Succeeded`) or `4` (`Failed`).

What happens while importing a specific version of a product:

1. The normalized URL is checked against `Verspakket.BronUrl`. An already-imported product is
   returned as-is (no network call).
2. The retailer product page is fetched (bounded size, redirect limit, host allowlist) and sent to
   the configured OpenRouter model, which extracts the verspakket data (name, price, persons,
   ingredients, nutrition, allergens, photos).
3. The extracted data is validated (a missing/invalid product name, `aantalPersonen` outside
   `[1, 10]` or a negative price fail with an `UnprocessableException`).
4. The data is sanitized to fit the schema: names truncated at a word boundary, ingredient names to
   100 characters, quantities/nutrition rounded to two decimals, `waarvanVerzadigd`/`waarvanSuikers`
   clamped to their totals.
5. The supermarket is resolved from the URL host map (`ah.nl`/`allerhande.nl` → Albert Heijn,
   `jumbo.com` → Jumbo, `poiesz.nl` → Poiesz, `lidl.nl` → Lidl) or the extracted supermarket name.
6. A verspakket with the same name + supermarket but no `bronUrl` (a legacy row) gets its `bronUrl`
   attached; otherwise a new verspakket is created through the normal create flow. A unique index on
   `BronUrl` guards against concurrent duplicate imports.

---

## Enums

Enums are sent and received as integers. For query-string parameters both the name and the number
work; in JSON request and response bodies only the integer is used.

### `Allergeen` (allergens, EU mandatory list)

| Value | Name           |
|-------|----------------|
| 0     | Gluten         |
| 1     | Schaaldieren   |
| 2     | Eieren         |
| 3     | Vis            |
| 4     | Pinda          |
| 5     | Soja           |
| 6     | Melk           |
| 7     | Noten          |
| 8     | Selderij       |
| 9     | Mosterd        |
| 10    | Sesam          |
| 11    | Sulfieten      |
| 12    | Lupine         |
| 13    | Weekdieren     |

### `Eenheid` (ingredient unit)

| Value | Name        |
|-------|-------------|
| 0     | Gram        |
| 1     | Kilogram    |
| 2     | Milliliter  |
| 3     | Liter       |
| 4     | Eetlepel    |
| 5     | Theelepel   |
| 6     | Aantal      |

### `VoedingswaardeBasis` (nutrition basis)

| Value | Name        |
|-------|-------------|
| 0     | Per100Gram  |
| 1     | PerPortie   |

### `VerspakketSortField` (list sort field)

| Value | Name                  |
|-------|-----------------------|
| 0     | Naam                  |
| 1     | PrijsInCenten         |
| 2     | AverageCijferSmaak    |
| 3     | AverageCijferBereiden |

### `SortDirection`

| Value | Name       |
|-------|------------|
| 0     | Ascending  |
| 1     | Descending |

### `BackgroundCommandType`

| Value | Name              |
|-------|-------------------|
| 1     | ImportVerspakket  |

### `BackgroundCommandStatus`

| Value | Name            | Meaning                                          |
|-------|-----------------|--------------------------------------------------|
| 0     | Queued          | Waiting to be picked up                          |
| 1     | Processing      | Claimed and running                             |
| 2     | RetryScheduled  | Failed transiently; will be retried             |
| 3     | Succeeded       | Finished; `resultVerspakketId` is available      |
| 4     | Failed          | Exhausted attempts or permanent failure          |

---

## Data models

All JSON uses camelCase. `required` note: omitted nullable fields serialize as `null`, and DateTime
fields use ISO 8601 UTC (`2026-09-18T09:12:33.5234567Z`).

### Supermarkt

Used as a child of verspakket responses and as items in `GET /api/supermarkten`.

```json
{ "id": "9b1c4d7a-...", "naam": "Jumbo" }
```

### Beoordeling

```json
{ "cijferSmaak": 9, "cijferBereiden": 8, "aanbevolen": true, "tekst": "Heerlijk!" }
```

### VerspakketFoto

Request shape (`fotos` in create/update):

```json
{ "base64Data": "iVBORw0KGgoAAAANSUhEUg...", "isMainImage": true }
```

Response shape (`fotos` in the detail endpoint, includes an id):

```json
{ "id": "c3f4a1b2-...", "base64Data": "iVBORw0KGgoAAAANSUhEUg...", "isMainImage": true }
```

Photo limits (create and update):

- Maximum 10 fotos.
- Each foto decodes to at most 5 MiB.
- Total decoded size across all fotos at most 20 MiB.
- The base64 must be valid; empty/malformed payloads are rejected with `400`.

### Ingredient

```json
{ "naam": "Tomaten", "hoeveelheid": 400, "eenheid": 0, "inbegrepen": true }
```

Validation: `naam` non-empty and ≤ 100 characters, `hoeveelheid` > 0, `eenheid` a valid `Eenheid`.

### Voedingswaarde

```json
{
  "basis": 0,
  "energieKj": 520,
  "energieKcal": 124,
  "vetten": 4.5,
  "waarvanVerzadigd": 1.2,
  "koolhydraten": 14,
  "waarvanSuikers": 2.1,
  "vezels": 2.4,
  "eiwitten": 5.8,
  "zout": 0.35
}
```

All amounts are non-negative decimals (`>= 0`). `basis` is a `VoedingswaardeBasis`. Per verspakket a
maximum of two entries is allowed and each `basis` may occur only once. Subset rules are enforced:
`waarvanVerzadigd ≤ vetten` and `waarvanSuikers ≤ koolhydraten`.

### Request body schemas

| DTO                      | Used in                        |
|--------------------------|--------------------------------|
| `SupermarktRequest`      | create/update supermarket      |
| `CreateVerspakketRequest`| create verspakket             |
| `UpdateVerspakketRequest`| update verspakket             |
| `AddBeoordelingRequest`  | add beoordeling                |
| `ImportVerspakketRequest`| import verspakket             |
| `VerspakketFotoRequest`  | photo inside create/update     |
| `IngredientRequest`      | ingredient inside create/update|
| `VoedingswaardeRequest`  | nutrition inside create/update |

---

## Limits and rate limiting

| Limit                                    | Value                                |
|------------------------------------------|--------------------------------------|
| Maximum request body size                 | 32 MiB (larger bodies → `413`)       |
| Page size (`take`)                        | max 200, min 1 (clamped)              |
| Foto count per verspakket                 | max 10                               |
| Foto size (decoded)                       | max 5 MiB each                       |
| Total foto size (decoded)                 | max 20 MiB                           |
| `Verspakket.Naam`                          | max 255 characters                   |
| `Ingredient.Naam`                          | max 100 characters                   |
| `Voedingswaarde` entries per verspakket   | max 2, one per `basis`               |
| Import queue depth (`MaxPendingJobs`)     | 50 active jobs (beyond → `429`)      |
| Import rate limit                         | 10 requests per minute per IP        |
| Import retry delay (`RetryDelayMinutes`)  | 5 minutes                            |
| Import max attempts (`MaxAttempts`)       | 3                                    |

---

## Errors

Errors are returned as [RFC 7807](https://datatracker.ietf.org/doc/html/rfc7807) problem documents
with content type `application/problem+json`:

```json
{
  "type": null,
  "title": "Not Found",
  "status": 404,
  "detail": "Verspakket with id '5a3b...' was not found."
}
```

`type` may be `null`. `detail` is human-readable and can be in Dutch.

### Status codes

| Status | Meaning                                                                 |
|--------|-------------------------------------------------------------------------|
| 200    | OK — successful `GET`                                                    |
| 201    | Created — successful create; `Location` header points at the resource    |
| 202    | Accepted — import job queued                                             |
| 204    | No Content — successful update                                           |
| 400    | Bad Request — validation and model-binding failures (`ValidationException`) |
| 404    | Not Found — unknown/id reference (`NotFoundException`)                    |
| 409    | Conflict — e.g. import matching multiple legacy verspakketten (`ConflictException`) |
| 413    | Payload Too Large — request body over 32 MiB (Kestrel)                    |
| 422    | Unprocessable Entity — extracted product data unusable (`UnprocessableException`) |
| 429    | Too Many Requests — queue full or per-IP import rate limit (`TooManyRequestsException`; the rate-limit response has an empty body) |
| 500    | Internal Server Error — unexpected failure (logged, not disclosed)        |
| 502    | Bad Gateway — external service failure during import; the detail is fixed (`"De externe dienst gaf een foutmelding."`) and the real error is only logged |

---

## Configuration

The API is configured through `appsettings.json` and the standard `__` environment-variable mapping.
The full reference lives in the repository README; the most important keys:

| Setting                             | Default                 | Purpose                                   |
|-------------------------------------|-------------------------|-------------------------------------------|
| `ConnectionStrings:LutraDb`         | *(empty)*               | PostgreSQL connection string              |
| `OpenRouter:ApiKey`                 | *(never commit it)*     | OpenRouter key; use user-secrets or `OpenRouter__ApiKey` |
| `OpenRouter:Model`                  | `nvidia/nemotron-3-super-120b-a12b:free` | Extraction model |
| `OpenRouter:AllowedHosts`           | `ah.nl`, `allerhande.nl`, `jumbo.com`, `poiesz.nl`, `lidl.nl` | Retail hosts import may fetch |
| `BackgroundCommands:MaxPendingJobs` | `50`                    | Import queue depth cap                   |
| `ASPNETCORE_ENVIRONMENT`            | `Production`            | `Development` enables Scalar at `/scalar/v1` |