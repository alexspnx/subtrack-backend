# SubTrack API contract conventions

Status: normative for `/api/v1`. Owner: `subtrack-backend`. Introduced by ST-003 (2026-10-11).

This document is the single source of truth for how SubTrack data looks on the wire and how the server
calculates recurrences, monthly equivalents and forecasts. The web, iOS and Android clients and every backend
endpoint follow it. Product rules come from section 5 of the workspace product brief
(`docs/subtrack-project-plan.md` in the workspace root, "plan §5" below); where the brief is loose, this
document picks one rule and states it. Each topic has exactly one rule and one JSON example.

The canonical test cases for the calculation rules are in [`recurrence-examples.json`](recurrence-examples.json)
(schema in [Recurrence example fixture](#recurrence-example-fixture)). ST-006 runs them in the backend unit
tests; clients may run them against their preview code.

Not covered here: authentication and its error codes (ST-004), concrete endpoints, request bodies and entities
(ST-005), reminder delivery (ST-008). Nothing in this document is implemented yet; JSON options, ProblemDetails
mapping and the currency table are built in ST-005/ST-006 against these rules.

Examples are complete bodies unless marked *(abbreviated)*, which means documented properties were left out
for brevity. Request examples show only the headers relevant to the topic, plus `SubTrack-Client`, which every
request sends.

Contents: [Versioning and compatibility](#versioning-and-compatibility) · [JSON](#json) · [IDs](#ids) ·
[Money](#money) · [Dates, timestamps and time zones](#dates-timestamps-and-time-zones) · [Errors](#errors) ·
[Concurrency](#concurrency) · [Idempotency](#idempotency) · [Paging](#paging) · [Ordering](#ordering) ·
[Date ranges](#date-ranges) · [Ownership](#ownership) · [Provider URLs and notes](#provider-urls-and-notes) ·
[Calculation rules](#calculation-rules) · [Recurrence example fixture](#recurrence-example-fixture)

## Versioning and compatibility

**Rule.** Every route starts with `/api/v1/`. The version lives only in the path. Changes are classified as
additive or breaking:

- **Additive** (always allowed, `~` commit): a new endpoint; a new optional request field; a new response
  field; a new enum value in a response; a new error `code` or field code; a new problem extension member; a
  new accepted value for a request enum; a relaxed validation limit; raising the maximum page size; a newly
  supported currency.
- **Breaking** (`!` commit): removing or renaming an endpoint, field or enum value; changing a field's type,
  format, unit or meaning; making an optional request field required; tightening validation of an existing
  field; changing a status code or error `code` for an existing condition; changing a default (including the
  default page size); changing a sort order; lowering the maximum page size; changing a calculation result for
  the same input (anything in [Calculation rules](#calculation-rules) or an expectation in the fixture);
  changing the meaning of a header.
- **Exception:** refusing a currency for new items because ISO 4217 withdrew it (see [Money](#money)) is not
  a breaking change; SubTrack follows the standard.

Breaking changes follow a phased policy:

1. **Until the first public store release** of the iOS or Android app: a breaking change is allowed in
   `/api/v1` if the affected clients are updated in a coordinated change. It is still a `!` commit and still
   updates this document and the fixture in the same change.
2. **From the first public store release on:** a breaking change ships as `/api/v2` (only the affected
   routes), served alongside `/api/v1` for at least 6 months. Forced upgrades use the minimum-supported-client
   response described below.

**Client identification.** Every request sends `SubTrack-Client: <platform>/<version>`, where `<platform>` is
`web`, `ios` or `android` and `<version>` is a Semantic Versioning 2.0 version (`1.4.0`, `1.5.0-beta.2`). The web
app sends its build version. Clients send the header from their first release, so the server can enforce a
minimum later without a client change. The server may set a minimum version per platform. Versions are
compared by SemVer 2.0 precedence (build metadata ignored). A request below its platform's minimum gets
`400 Bad Request` with code `clientUpgradeRequired` and the extension member `minimumVersion` (that
platform's minimum). Once any platform has a minimum, a request whose header is missing or malformed gets the
same response with `minimumVersion: null`, because the platform is unknown. While no platform has a minimum,
a missing or malformed header is ignored. `OPTIONS` requests (CORS preflight), `/health` and the OpenAPI
document are never checked. (`426 Upgrade Required` is not used: HTTP reserves it for protocol upgrades with
an `Upgrade` header.)

**Rollout order.** Because the server rejects unknown request fields, a server that accepts a new request field
is deployed before any client that sends it. New response fields can ship at any time, because clients ignore
unknown properties.

```http
GET /api/v1/recurring-items HTTP/1.1
SubTrack-Client: ios/1.4.0

HTTP/1.1 400 Bad Request
Content-Type: application/problem+json

{
  "type": "about:blank",
  "title": "Bad Request",
  "status": 400,
  "code": "clientUpgradeRequired",
  "detail": "This version of SubTrack is no longer supported. Update the app to continue.",
  "instance": "/api/v1/recurring-items",
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01",
  "minimumVersion": "1.6.0",
  "errors": []
}
```

## JSON

**Rule.** Request and response bodies are UTF-8 JSON (`application/json`; errors use
`application/problem+json`).

- Property names are camelCase. Enum values are camelCase strings (`everyNDays`, never `3` or `EveryNDays`).
- The server **rejects unknown request properties** with `400 validationError` and a field error with code
  `unknownField`. It rejects an unknown enum value with code `invalidValue`. A property that appears twice in
  the same object is `invalidFormat` on that property's path (never last-wins).
- Clients **ignore unknown response properties** and **tolerate unknown enum values** (map them to an
  "unknown" fallback and keep working), because both are additive changes.
- Responses always include every documented property; a property with no value is `null`, never omitted.
- Read-only response properties (`id`, `createdAt`, `updatedAt`) sent in a request are `unknownField`.
- In a request, an absent optional property and `null` mean the same thing on create. On `PATCH`, an absent
  property leaves the value unchanged, `null` clears an optional value, and `null` for a required property is
  `required`.
- Integer body properties (such as `interval`) are JSON numbers written without a fraction or exponent; a
  non-integer number, `30.0`, `3e1` or a string is `invalidFormat`. Money amounts are strings (see
  [Money](#money)).
- String length limits count UTF-16 code units (C# `string.Length`, JavaScript `length`; Swift clients use
  `utf16.count`, Kotlin `length`).
- A body that is not valid JSON gets `400 validationError` with one field error, `field: ""`, code
  `invalidFormat`.

*(abbreviated)*

```json
{
  "id": "0b6f2c1e-8d3a-4f5b-9c7e-2a1d4e6f8b90",
  "name": "Streaming Plus",
  "status": "active",
  "schedule": { "frequency": "everyNDays", "interval": 30, "anchorDate": "2027-01-15" },
  "price": { "amount": "9.99", "currency": "EUR" },
  "providerUrl": null,
  "version": "7"
}
```

## IDs

**Rule.** Every resource ID is a UUID that the server generates, sent as a lowercase string in the canonical
hyphenated 8-4-4-4-12 form. Clients treat it as opaque: they never parse it, generate it or derive meaning from
it (the `Idempotency-Key` is the only UUID a client generates). On input, IDs are compared case-insensitively,
but only the canonical form is accepted: braces, a `urn:uuid:` prefix or missing hyphens are `invalidFormat` in
a body, query or header. A path ID that is not a canonical UUID gets `404 notFound`, the same response as an ID
that does not exist or belongs to someone else.

```json
{ "id": "0b6f2c1e-8d3a-4f5b-9c7e-2a1d4e6f8b90" }
```

## Money

**Rule.** Money is an object with a decimal-string `amount` and an ISO 4217 `currency`. Floating-point types
are never used for money, on the server or in clients.

- `currency` is an uppercase three-letter code from ISO 4217 List One that is currently active. Fund codes and
  precious metals/special codes are not supported (`XAU`, `XAG`, `XPD`, `XPT`, `XDR`, `XSU`, `XUA`, `XBA`–`XBD`,
  `XTS`, `XXX`, and the fund codes `BOV`, `CHE`, `CHW`, `CLF`, `COU`, `MXV`, `USN`, `UYI`, `UYW`). A malformed
  code (`eur`, `EURO`) is `invalidFormat`; a well-formed code outside the supported set (`ZZZ`, `XAU`, `CHE`) is
  `unsupportedCurrency`. The server keeps the code-to-minor-units table (built in ST-005/006 from List One); for
  example `JPY` has 0 minor units, `EUR` 2, `KWD` 3.
- ISO 4217 updates: a newly published code is added to the supported set (additive). A withdrawn code stays
  valid for items that already use it, in stored data, in responses and in new price versions of those items;
  only new items that use it are refused with `unsupportedCurrency` (not a breaking change, see
  [Versioning](#versioning-and-compatibility)). If ISO changes a currency's minor units, existing amounts keep
  the old minor units until a reviewed data migration moves them; that migration is a breaking change.
- Input `amount` must be a JSON string matching `^-?(0|[1-9][0-9]*)(\.[0-9]+)?$` (`invalidFormat` otherwise,
  including a JSON number). A leading `-` is `outOfRange` whatever the value, including `"-0"` and `"-0.00"`
  (amount ≥ 0); more than 12 integer digits is
  `outOfRange`; more fraction digits than the currency's minor units is `precisionExceeded`. The server
  **rejects** over-precise input; it never rounds it. Digits are counted, not values: `"9.990"` in EUR and
  `"100.0"` in JPY are rejected. Fewer digits are accepted (`"9.9"`, `"10"`). Precision is not checked when the
  currency is malformed or unsupported, because the minor units are unknown.
- Output `amount` always has exactly the currency's minor-unit digits: `"10.00"` EUR, `"1200"` JPY,
  `"3.500"` KWD.
- Totals are always grouped by currency: one total per currency, sorted by currency code. Amounts in
  different currencies are never added. Currency conversion is out of scope.

```json
{
  "price": { "amount": "9.99", "currency": "EUR" },
  "totals": [
    { "amount": "1200", "currency": "JPY" },
    { "amount": "15.49", "currency": "USD" }
  ]
}
```

## Dates, timestamps and time zones

**Rule.**

- **Billing dates** (anchors, occurrences, effective dates, trial dates, range bounds) are date-only strings
  `yyyy-MM-dd`, between `1900-01-01` and `2099-12-31` (`outOfRange` outside). They carry no time and no zone.
  The server never produces an occurrence after `2099-12-31`.
- **Timestamps** (`createdAt`, `updatedAt`, delivery times) are UTC instants in RFC 3339 form ending in an
  uppercase `Z`. The server emits 0 to 7 fractional-second digits; clients accept any count. Timestamp input
  with an offset other than `Z`, or with a lowercase `z` or `t`, is `invalidFormat`.
- **Time zones** are IANA tz database ids (`Europe/Athens`, `Pacific/Auckland`). Link names are accepted and
  stored as the canonical id they link to (`Asia/Calcutta` is stored and returned as `Asia/Kolkata`). Windows
  ids and raw offsets are `invalidValue`. The user's time
  zone is required when the account is created (ST-004), so there is no default zone.
- **"Today" is derived by the server**: the current UTC instant converted to the user's preference time zone,
  taking the local date. Clients never send "today". Responses that depend on it include `today` so clients
  display the same day the server used.

*(abbreviated)*

```json
{
  "today": "2027-06-15",
  "timeZone": "Pacific/Auckland",
  "nextRenewal": "2027-07-14",
  "updatedAt": "2027-06-14T20:00:00.123Z"
}
```

## Errors

**Rule.** Every error response, including errors raised by the framework before a controller runs, is RFC 9457
`application/problem+json` with these members:

| Member | Meaning |
|---|---|
| `type` | Always `"about:blank"`. |
| `title` | The HTTP reason phrase. For humans only. |
| `status` | The HTTP status code. |
| `code` | Stable machine-readable error code (tables below). **Clients branch on `code`**, never on `title`, `detail` or `message`. |
| `detail` | Human-readable explanation in English. Not for display logic. |
| `instance` | The request path. |
| `traceId` | W3C trace id for support and log correlation. |
| `errors` | Array of field errors; always present, empty when the error is not about specific fields. |

The only other member allowed is the extension `minimumVersion` (a SemVer string or `null`, only with code
`clientUpgradeRequired`). A new extension
member is an additive change; clients ignore members they do not know.

A field error is `{ "field": "<path>", "code": "<fieldCode>", "message": "<English text>" }`. `field` is the
camelCase JSON path of the request property (`price.amount`, `prices[1].effectiveDate`), the query parameter
name (`to`, `limit`), the header name (`Idempotency-Key`), or `""` for the whole body. Field codes:

| Field code | When |
|---|---|
| `required` | A required property, query parameter or header is missing or `null`, or a required array is empty. |
| `unknownField` | The request contains a property the endpoint does not define. |
| `invalidFormat` | Wrong JSON type, duplicate property or wrong string format (date, timestamp, UUID, amount, currency code, cursor). |
| `invalidValue` | Well-formed but not an allowed value (unknown enum value, URL scheme, time zone id). |
| `outOfRange` | A number, date, amount or range outside its documented bounds, or a date out of order with a related date. |
| `notAllowed` | The property or value is not allowed in this combination (for example `interval` when `frequency` is not `everyNDays`). |
| `precisionExceeded` | An amount has more fraction digits than its currency's minor units. |
| `unsupportedCurrency` | A well-formed currency code that is not in the supported set. |
| `tooLong` | A string exceeds its maximum length. |

**One error per field.** Each field gets at most one field error. Checks run in this precedence order and the
first failing one is reported: `required` > `invalidFormat` > `invalidValue` / `unsupportedCurrency` >
`outOfRange` / `notAllowed` / `tooLong` > `precisionExceeded`. A cross-field rule (one that compares two
fields) is checked only when every field it compares is present and passed its own checks. `errors` is sorted
by `field`, ordinal.

`ServiceErrorCode` (`SubTrack.Common.Enums`) maps to the response as follows. A new `ServiceErrorCode` value
gets a row here in the same change that adds it.

| `ServiceErrorCode` | Status | `code` |
|---|---|---|
| `ValidationError` | 400 | `validationError` |
| `NotFound` | 404 | `notFound` |
| `Conflict` | 409 | `conflict` |
| `DatabaseError` | 500 | `internalError` |
| `GenericError` | 500 | `internalError` |

Errors produced outside the services:

| Condition | Status | `code` |
|---|---|---|
| Body is not valid JSON, or fails validation | 400 | `validationError` |
| Unknown route | 404 | `notFound` |
| Method not supported by the route | 405 | `methodNotAllowed` |
| `Accept` excludes JSON | 406 | `notAcceptable` |
| Request body over 1 MiB | 413 | `payloadTooLarge` |
| Body sent with a `Content-Type` other than `application/json` | 415 | `unsupportedMediaType` |
| Rate limit exceeded (with a `Retry-After` header, in seconds) | 429 | `tooManyRequests` |
| Client below its platform's minimum version, or `SubTrack-Client` missing or malformed while any minimum is set | 400 | `clientUpgradeRequired` |
| Unhandled exception anywhere | 500 | `internalError` |

Reserved codes, each added as a `ServiceErrorCode` value by the story that first needs it:
`VersionConflict` → 409 `versionConflict` (ST-005, see [Concurrency](#concurrency)),
`IdempotencyKeyReused` → 422 `idempotencyKeyReused` and `IdempotencyRequestInProgress` → 409
`idempotencyRequestInProgress` (see [Idempotency](#idempotency)). `conflict` stays for every other conflict.
401/403 codes come with authentication in
ST-004. A 500 response never contains exception messages, stack traces or SQL. A client that receives an
unknown `code` falls back to generic handling for the status class.

```http
HTTP/1.1 400 Bad Request
Content-Type: application/problem+json

{
  "type": "about:blank",
  "title": "Bad Request",
  "status": 400,
  "code": "validationError",
  "detail": "One or more fields are invalid.",
  "instance": "/api/v1/recurring-items",
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01",
  "errors": [
    { "field": "price.amount", "code": "precisionExceeded", "message": "EUR allows at most 2 decimal places." },
    { "field": "schedule.interval", "code": "notAllowed", "message": "interval is only allowed when frequency is everyNDays." }
  ]
}
```

## Concurrency

**Rule.** Every editable resource has a `version` property: an opaque string that changes on every successful
change. Clients compare it only for equality. A `PATCH`, a `PUT` or a state-changing `POST` on an existing
resource (a transition, a price, a review, the reminder rules) must send the `version` the client last read, in
the body; a `DELETE` sends it as the `version` query parameter. A missing `version` is `400 required`. A stale
`version` is `409 versionConflict` and nothing is changed; the client reloads the resource, shows the user the current
values and lets them reapply their edit. A successful mutation returns the resource with its new `version`.
This covers the plan §5 case "concurrent edits from two devices".

```http
PATCH /api/v1/recurring-items/0b6f2c1e-8d3a-4f5b-9c7e-2a1d4e6f8b90 HTTP/1.1
Content-Type: application/json
SubTrack-Client: android/1.2.0

{ "version": "7", "name": "Streaming Premium" }

HTTP/1.1 409 Conflict
Content-Type: application/problem+json

{
  "type": "about:blank",
  "title": "Conflict",
  "status": 409,
  "code": "versionConflict",
  "detail": "The item was changed by another request. Reload it and try again.",
  "instance": "/api/v1/recurring-items/0b6f2c1e-8d3a-4f5b-9c7e-2a1d4e6f8b90",
  "traceId": "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01",
  "errors": []
}
```

## Idempotency

**Rule.** Every `POST` on an authenticated endpoint that creates something or changes state requires an
`Idempotency-Key` header: creating an item, recording a transition, a price or a review, and requesting an
export. `POST /api/v1/recurrence-preview` (read-only) and `POST /api/v1/devices` (an upsert keyed by the push
token) do not take one. `PATCH`, `PUT` and `DELETE` never take one. Unauthenticated account endpoints
(registration, sign-in, recovery) define their own retry behavior in ST-004.

- The key is a UUID string (canonical form, see [IDs](#ids)) the client generates once per logical operation
  and reuses unchanged for every retry of that operation. A missing key is `400 required`
  (`field: "Idempotency-Key"`); a malformed key is `400 invalidFormat`.
- Keys are scoped by owner, HTTP method and request path. Two users, or two different endpoints, never
  collide.
- **Records.** Before executing, the server commits an in-progress marker, unique on (owner, method, path,
  key), holding a fingerprint of the parsed request (property order and whitespace do not matter). The
  operation's final response replaces the marker inside the operation's own database transaction, so a final
  record exists exactly when the effect happened. If the operation fails before its commit, the marker is
  removed; if the server crashes, the marker expires **60 seconds** after it was created. If two executions
  race after that expiry (the original still running and a retry), the unique final record makes whichever
  commits second roll back and return the winner's stored response. Final records are kept for **24 hours**
  after the first request completes.
- **What is stored.** Only responses produced by the operation's handler: its 2xx, and its `400`, `404`,
  `409` and `422` results. Rejections raised before the idempotency step (authentication, the client-version
  check, rate limiting, body size, media type) are never stored, so a retry after fixing the cause executes
  normally. A 5xx before the commit stores nothing, so a retry executes again; a 5xx raised after the commit
  still leaves the stored 2xx, which a retry replays.
- Same key, same request, within 24 hours: the stored status and body are returned unchanged, with the header
  `Idempotency-Replayed: true`. Nothing executes twice. After 24 hours the key is forgotten and a request with
  it is a new operation.
- Same key, different request: `422 idempotencyKeyReused`. Same key while an unexpired in-progress marker
  exists: `409 idempotencyRequestInProgress`; the client retries later with the same key.
- An operation the client reapplies after a `409 versionConflict` is a new operation with a new key; retrying
  the old key would replay the stored 409.
- Retrying requests without a key: a retried `DELETE` that gets `404 notFound` counts as done; a retried
  `PATCH` or `PUT` that gets `409 versionConflict` reloads the resource first, because the earlier attempt may
  have succeeded.
- A web app served from another origin needs CORS that allows the request headers `Idempotency-Key` and
  `SubTrack-Client` and exposes the response header `Idempotency-Replayed`.

A retry of a create whose first response was lost (response body *(abbreviated)*):

```http
POST /api/v1/recurring-items HTTP/1.1
Content-Type: application/json
SubTrack-Client: web/1.0.3
Idempotency-Key: 5b1e7c3a-0c1f-4f3e-9a52-6c1d2e8f9a10

{ "name": "Streaming Plus", "schedule": { "frequency": "monthly", "anchorDate": "2027-01-31" }, "price": { "amount": "9.99", "currency": "EUR" } }

HTTP/1.1 201 Created
Idempotency-Replayed: true
Content-Type: application/json

{ "id": "0b6f2c1e-8d3a-4f5b-9c7e-2a1d4e6f8b90", "name": "Streaming Plus", "version": "1" }
```

## Paging

**Rule.** Lists of **resources** (recurring items, notifications, and later resource lists) are cursor-paged.
Date-range occurrence lists (forecast, renewals) are not paged; they have their own shape, described in
[Date ranges](#date-ranges). A paged request takes the query parameters `limit` (integer 1–100, default 50)
and `cursor` (the `nextCursor` of the previous page; absent for the first page). A `limit` outside 1–100 or
not an integer is `400 outOfRange` or `400 invalidFormat` on `limit`, never clamped. The response is
`{ "items": [...], "nextCursor": "<opaque>" | null }`; `null` means the last page. Cursors are opaque, only
valid with the same filters and sort, and a cursor that cannot be decoded or does not match them is
`400 invalidFormat` (`field: "cursor"`). No total count is returned.

`GET /api/v1/notifications?limit=2` returns *(items abbreviated)*:

```json
{
  "items": [
    { "id": "1d0c9b8a-7f6e-4d5c-8b4a-3f2e1d0c9b8a", "type": "renewalReminder", "createdAt": "2027-06-11T06:00:00Z" },
    { "id": "2e1d0c9b-8a7f-4e6d-9c5b-4a3f2e1d0c9b", "type": "trialEnding", "createdAt": "2027-06-10T06:00:00Z" }
  ],
  "nextCursor": "eyJjIjoiMjAyNy0wNi0xMFQwNjowMDowMFoiLCJpIjoiMmUxZDBjOWIifQ"
}
```

## Ordering

**Rule.** Every list has a documented, deterministic sort whose last key is `id` (or `itemId`) ascending, so
two requests over the same data always return the same order. Occurrences and renewals sort by `date`
ascending, then `itemId` ascending. Currency totals sort by `currency` ascending. Text sort keys compare
ordinally on the upper-invariant value. Each list endpoint states its default sort in its OpenAPI description.

*(abbreviated: `from`, `to` and `totals` left out)*

```json
{
  "occurrences": [
    { "itemId": "0b6f2c1e-8d3a-4f5b-9c7e-2a1d4e6f8b90", "date": "2027-06-14", "price": { "amount": "10.00", "currency": "EUR" } },
    { "itemId": "7c2d9e4f-1a3b-4c5d-8e6f-9a0b1c2d3e4f", "date": "2027-06-14", "price": { "amount": "4.99", "currency": "USD" } },
    { "itemId": "0b6f2c1e-8d3a-4f5b-9c7e-2a1d4e6f8b90", "date": "2027-07-14", "price": { "amount": "10.00", "currency": "EUR" } }
  ]
}
```

## Date ranges

**Rule.** Endpoints that take a range use the query parameters `from` and `to` (both `yyyy-MM-dd`, both
required). Both bounds are **inclusive**. `to` must be on or after `from`, and the range may cover at most
**366 days** counting both ends (so one leap year fits); violations are `400 outOfRange` on `to`. Endpoints
relative to "today" (next renewal, the next-30-days list) take no range; the server derives today and the
window (the next-30-days window is `[today, today + 29 days]`).

A date-range occurrence list is returned whole, never paged (the 366-day bound keeps it small):
`{ "from": date, "to": date, "occurrences": [{ "itemId", "date", "price": Money }], "totals": Money[] }`.
`from` and `to` echo the requested range, or the window the server derived for a today-relative list, so a
list computed just before local midnight states which days it covers. `totals` are over the whole window, per
[Forecast](#forecast).

`GET /api/v1/renewals?from=2027-06-01&to=2027-06-30` is valid (30 days) and returns:

```json
{
  "from": "2027-06-01",
  "to": "2027-06-30",
  "occurrences": [
    { "itemId": "0b6f2c1e-8d3a-4f5b-9c7e-2a1d4e6f8b90", "date": "2027-06-14", "price": { "amount": "10.00", "currency": "EUR" } },
    { "itemId": "7c2d9e4f-1a3b-4c5d-8e6f-9a0b1c2d3e4f", "date": "2027-06-20", "price": { "amount": "4.99", "currency": "USD" } }
  ],
  "totals": [
    { "amount": "10.00", "currency": "EUR" },
    { "amount": "4.99", "currency": "USD" }
  ]
}
```

`from=2027-01-01&to=2028-01-01` covers 366 days and is valid; `to=2028-01-02` (367 days) gets
`400 validationError` with `{ "field": "to", "code": "outOfRange", "message": "The range may cover at most 366 days." }`.

## Ownership

**Rule.** The owner of every resource is the authenticated user. No request ever carries an owner or user ID:
an `ownerId` or `userId` property is an unknown field (`400 unknownField`). Every query and mutation is scoped
to the authenticated owner, and a resource that belongs to another user returns `404 notFound`, exactly like a
resource that does not exist (no `403`, so IDs leak nothing).

```json
{
  "type": "about:blank",
  "title": "Not Found",
  "status": 404,
  "code": "notFound",
  "detail": "The recurring item was not found.",
  "instance": "/api/v1/recurring-items/7c2d9e4f-1a3b-4c5d-8e6f-9a0b1c2d3e4f",
  "traceId": "00-1f2e3d4c5b6a79881f2e3d4c5b6a7988-1a2b3c4d5e6f7a8b-01",
  "errors": []
}
```

## Provider URLs and notes

**Rule.** `providerUrl` is optional; when present it is an absolute URL with scheme `http` or `https`, at most
2048 characters (`invalidValue` for any other scheme such as `javascript:` or `file:`, `invalidFormat` for a
relative or malformed URL, `tooLong` above the limit). The server never fetches it. `notes` is optional plain
text, at most 2000 characters, stored and returned verbatim; it is never interpreted as HTML or Markdown, and
clients render it as text (escaped).

*(abbreviated)*

```json
{
  "providerUrl": "https://example.com/account/subscription",
  "notes": "Shared with <family>; cancel before the 14th."
}
```

## Calculation rules

The server owns these calculations; clients may preview them but must reconcile with server output. All
billing arithmetic is on calendar dates; time of day and DST never affect an occurrence date.

### Item model

The calculation inputs of one recurring item are (ST-005 maps its request and storage shapes onto these):

| Part | Shape | Notes |
|---|---|---|
| `scheduleVersions` | `ScheduleVersion[]`, at least one | `ScheduleVersion` = `{ "effectiveDate", "frequency", "anchorDate", "interval" }`. A single-version item may send `schedule` (a `Schedule` = the same without `effectiveDate`) instead; it means one version whose `effectiveDate` is its `anchorDate`. |
| `prices` | `PriceVersion[]`, at least one | `PriceVersion` = `{ "effectiveDate", "price": Money }`. |
| `trial` | `{ "trialEndDate", "firstPaidDate" }` or `null` | |
| `lifecycle` | `LifecycleChange[]`, may be empty | `LifecycleChange` = `{ "transition": "pause" \| "cancel", "effectiveDate", "resumeDate" }`. |

The **first anchor** of an item is the `anchorDate` of its first schedule version, which is also its first
billing date.

### Schedules

`frequency` is one of `weekly`, `monthly`, `quarterly`, `yearly`, `everyNDays`. `anchorDate` is the first
billing date of the schedule. `interval` is required for `everyNDays` and must be an integer in **1–366**; for
every other frequency it must be absent or `null`.

Occurrence `k` (k = 0, 1, 2, …) of a schedule with anchor `A`:

- `monthly`: the month `k` months after `A`'s month, on day `min(day(A), daysInMonth)`.
- `quarterly`: the same with `3k` months.
- `yearly`: the year `k` years after `A`'s year, same month, on day `min(day(A), daysInMonth)` (only February 29
  is affected: it becomes February 28 in non-leap years and returns to February 29 in leap years).
- `weekly`: `A + 7k` calendar days.
- `everyNDays`: `A + interval·k` calendar days.

The clamp always starts from the original anchor day, so dates never drift: January 31 → February 28 →
March 31 → April 30 → May 31. Occurrence 0 is the anchor, and **no occurrence precedes the anchor**.
Occurrences in a window `[from, to]` (inclusive) are listed ascending. When the anchor is in the past, the
first `k` inside the window is computed arithmetically (month or day difference); the engine never iterates
from a distant anchor.

### Schedule versions (schedule edits)

Plan §5: schedule edits take effect on an explicit date, keep prior history and replace future projections.
Schedule versions are ordered by `effectiveDate` (strictly ascending). Each version applies from its
`effectiveDate` until the next version's `effectiveDate` (the last one without end), and generates occurrences
from its own `anchorDate`, which is on or after its `effectiveDate`. The first version's `effectiveDate` equals
its `anchorDate`. Occurrences of an earlier version on or after a later version's `effectiveDate` are dropped.
A later version may not start inside a pause or on or after the date of an open pause (see
[Item validation](#item-validation)), so a resume never silently re-anchors a schedule edit. (The beta UI may restrict new versions to today or later; the calculation does not depend on that.)

### Trial

A trial item has `trialEndDate < firstPaidDate`, and `firstPaidDate` equals the first anchor, so paid charges
are forecast only from `firstPaidDate`. The trial is free until `trialEndDate`; no charge is inferred for the
trial itself. The trial reminder target is `trialEndDate` (reminder offsets and delivery are ST-008). To cancel
before the first charge, record a cancellation effective on `firstPaidDate`.

### Lifecycle

Lifecycle changes are strictly ordered by `effectiveDate` and none is before the first anchor.

- **Cancel** effective `C`: occurrences on or after `C` are excluded; earlier ones remain. A cancel is
  terminal: no change may follow it.
- **Pause** effective `P` with `resumeDate` `R` (`R > P`): occurrences with `P ≤ date < R` are excluded. The
  next change, if any, is on or after `R`. Resume re-anchors the schedule on `R`: from `R` on, occurrences are
  generated from anchor `R` with the frequency and interval of the schedule version in effect on `R`, with the
  normal clamp from `R`'s day; the original anchor day is no longer used. This lasts until the next schedule
  version's `effectiveDate` after `R` (or the next resume). If a schedule version starts on `R` itself, that
  version's anchor applies instead.
- **Open pause** (`resumeDate` `null`): every occurrence on or after `P` is excluded. The only change that may
  follow it is a cancel (after `P`). Resuming means setting the pause's `resumeDate`.

Lifecycle state on a date `d`: `cancelled` if a cancel has `C ≤ d`; otherwise `paused` if a pause has
`P ≤ d < R` (or is open with `P ≤ d`); otherwise `trial` if the item has a trial and `d < firstPaidDate`;
otherwise `active`.

### Prices

Each occurrence uses the latest price version whose `effectiveDate` is on or before the occurrence date (a price
effective on the occurrence date applies to it). Price version `effectiveDate`s are unique and strictly
ascending, the first is on or before the first anchor, and all price versions of one item share one currency.

### Item validation

An item that breaks a rule is rejected with `400 validationError`; each violation is one field error, following
the precedence, cross-field and sorting rules in [Errors](#errors). Paths are relative to the item; `*` stands
for `schedule` or `scheduleVersions[i]`.

| Field | Rule (code) |
|---|---|
| `schedule`, `scheduleVersions` | Exactly one is present (`required` on `schedule` if neither; if both, `notAllowed` on `scheduleVersions` and only `schedule`'s children are validated); `scheduleVersions` is not empty (`required`). |
| `*.frequency` | Required; one of the five values (`invalidValue`). |
| `*.anchorDate` | Required; a date (`invalidFormat`) in 1900–2099 (`outOfRange`). For `scheduleVersions[i]`, i > 0: on or after its `effectiveDate` (`outOfRange`). |
| `*.interval` | `everyNDays`: required, an integer (`invalidFormat`) in 1–366 (`outOfRange`). Other frequencies: absent or `null` (`notAllowed`). |
| `scheduleVersions[i].effectiveDate` | Required; a date in 1900–2099. i = 0: equal to its `anchorDate` (`notAllowed`). i > 0: after the previous version's `effectiveDate`, not inside a pause (`P ≤ effectiveDate < R`) and not on or after the date of an open pause (`outOfRange`). |
| `prices` | Required, not empty (`required`). |
| `prices[i].effectiveDate` | Required; a date in 1900–2099. i = 0: on or before the first anchor (`outOfRange`). i > 0: after the previous price's `effectiveDate` (`outOfRange`; this also rejects duplicates). |
| `prices[i].price.amount`, `.currency` | Required; the [Money](#money) rules. i > 0: same currency as `prices[0]` (`notAllowed` on `.currency`). |
| `trial.trialEndDate` | Required; a date in 1900–2099; before `firstPaidDate` (`outOfRange`). |
| `trial.firstPaidDate` | Required; a date in 1900–2099; equal to the first anchor (`notAllowed`). |
| `lifecycle[i].transition` | Required; `pause` or `cancel` (`invalidValue`). Not allowed after a cancel, and only `cancel` after an open pause (`notAllowed`). |
| `lifecycle[i].effectiveDate` | Required; a date in 1900–2099; on or after the first anchor; after the previous change's `effectiveDate` and on or after a previous pause's `resumeDate` (`outOfRange`). |
| `lifecycle[i].resumeDate` | Pause: `null` or a date in 1900–2099 after its `effectiveDate` (`outOfRange`). Cancel: absent or `null` (`notAllowed`). |

### Eligible occurrences

The eligible occurrences of an item in a window `[from, to]` are computed in this order:

1. **Segments.** Each schedule version starts a segment at its `effectiveDate` with its own anchor; each pause
   with a `resumeDate` `R` starts a segment at `R` with anchor `R` (see [Lifecycle](#lifecycle)). A segment
   lasts until the next segment starts. Its occurrences are the schedule occurrences of its anchor that fall
   inside the segment.
2. **Lifecycle.** Drop occurrences inside a pause interval or on/after a cancel.
3. **Bounds.** Keep occurrences inside `[from, to]` and on or before `2099-12-31`.
4. **Price.** Attach the price in effect on each occurrence date.

### Next renewal

`today` is the local date of the current instant in the user's time zone (see
[Dates](#dates-timestamps-and-time-zones)). The next renewal is the first eligible occurrence on or after
`today`; it is `null` when there is none (after a cancellation, during an open pause, or when the next
occurrence would be after `2099-12-31`). Today's occurrence stays the next renewal until the local date
changes, because there is no payment confirmation. The same instant can give different results in different
zones: `2027-06-14T20:00:00Z` is June 15 in `Pacific/Auckland` and June 14 in `America/Los_Angeles`.

### Monthly equivalent

A comparison metric, not a forecast, evaluated on `today` over all of the owner's items, archived included.
For each item:

- **Contributes** only if its lifecycle state on `today` is `trial` or `active` **and** it has a
  [next renewal](#next-renewal); otherwise it contributes 0 (paused, cancelled, or a trial cancelled effective
  its `firstPaidDate`).
- **Frequency**: from the schedule version in effect on `today` (the first version if `today` is before it).
- **Price**: the price in effect on `today`; if none is in effect yet (a trial, or an item whose first price is
  in the future), its first price version.

| Frequency | Monthly contribution |
|---|---|
| `monthly` | `amount` |
| `quarterly` | `amount / 3` |
| `yearly` | `amount / 12` |
| `weekly` | `amount × 52 / 12` |
| `everyNDays` | `amount × 365 / (interval × 12)` |

These factors follow plan §5 exactly, so `weekly` and `everyNDays` with interval 7 give slightly different
results (€10.00: 43.33 vs 43.45); the UI explains the 52-week and 365-day approximation.

**Exact arithmetic.** Per currency (minor units `d`), contributions are summed as exact fractions and the sum
is **rounded once**, half away from zero, to `d` digits:

1. Write each amount as integer minor units `m = amount × 10^d` (exact, because input never has more than `d`
   fraction digits) and its factor as `p / q`: `1/1`, `1/3`, `1/12`, `52/12`, `365/(12·interval)`.
2. Take a common denominator `Q` (the least common multiple of the `q`s, or any common multiple) and sum the
   integer numerator `T = Σ m · p · (Q / q)`.
3. The total in minor units is `floor((2T + Q) / (2Q))` (`T ≥ 0`, so this is half away from zero), formatted
   with `d` digits.

Use arbitrary-precision integers: C# `System.Numerics.BigInteger`, Kotlin/Java `BigInteger`, JavaScript
`BigInt`. Swift has no built-in big integer; an iOS preview uses a small big-integer helper or shows the
server's value. 64-bit integers are not enough in general (`Q` grows with every distinct interval). Plain
per-item division is **not** exact and must not be used: C# `decimal` stops at 28–29 significant digits
(`10.00m / 3` is `3.3333333333333333333333333333`, and three of those sum to `9.9999999999999999999999999999`),
Java/Kotlin `BigDecimal.divide` throws for non-terminating results unless given a `MathContext` (which rounds),
and JavaScript numbers are floating point. Two quarterly €10.00 items give 6.67 (not 3.33 + 3.33 = 6.66); a
yearly €0.30 item gives 0.03 (0.025 rounds up). A currency gets a total only if at least one of its items is
not cancelled on or before `today` (no cancel with `C ≤ today`); such a total can be zero, for example when
all its items are paused. Long-cancelled items therefore leave no zero totals behind. Totals sort by currency.

### Forecast

A forecast lists the eligible occurrences of the owner's items (all of them, archived included) in an
inclusive date window and totals them per currency. It is an estimate, never a payment record.

- Occurrences are the [eligible occurrences](#eligible-occurrences) of every item, sorted by date, then item id,
  each with the price in effect on its date.
- Totals: one total per currency that has at least one item not cancelled on or before the window start
  (no cancel with `C ≤ from`), including such currencies with no occurrence in the window (total zero), sorted
  by currency code. Totals are exact sums of the occurrence
  amounts (no rounding is needed), formatted with the currency's minor-unit digits.

Annual items therefore contribute their whole charge only in their billing month (€120 renewing December 15:
October total 0.00, December total 120.00).

## Recurrence example fixture

[`recurrence-examples.json`](recurrence-examples.json) is the canonical example set for the rules above. It
uses fixed dates only (never "today"). Changing an expectation is a breaking change (see
[Versioning](#versioning-and-compatibility)).

Distribution: ST-006 links the file into the `SubTrack.DomainServices.UnitTests` project as content copied to
the output directory (no copy inside `tests/`), and the tests load it from there. Client repos copy the file
at a pinned `subtrack-backend` commit and record that commit next to the copy; updating the copy is a
deliberate change.

Top level: `{ "schemaVersion": 1, "description": string, "cases": Case[] }`.

Every case has:

| Property | Type | Meaning |
|---|---|---|
| `id` | string | Unique, kebab-case, stable. Tests are named after it. |
| `kind` | string | `occurrences`, `nextRenewal`, `monthlyEquivalent`, `forecast` or `invalid`. |
| `rule` | string | The plan §5 (or §9) rule, and the edge-case table row when there is one, that the case checks. |
| `description` | string | One line for humans. |
| `input` | object | Shape depends on `kind` (below). |
| `expected` | object | Shape depends on `kind` (below). |

Shared shapes are those of the [Item model](#item-model). In the fixture, optional properties may be omitted:
an absent `interval`, `trial` or `resumeDate` means `null`, an absent `lifecycle` means `[]`.
`ItemBody` = `{ "schedule" | "scheduleVersions", "prices", "trial"?, "lifecycle"? }`; `Item` = `ItemBody` plus
`"id"` (a UUID). `Occurrence` = `{ "itemId", "date", "price": Money }`, the wire shape from
[Date ranges](#date-ranges).

| `kind` | `input` | `expected` |
|---|---|---|
| `occurrences` | `{ "schedule": Schedule, "from": date, "to": date }` | `{ "dates": date[] }`, ascending |
| `nextRenewal` | `{ "schedule" \| "scheduleVersions", "trial"?, "lifecycle"?, "now": timestamp, "timeZone": string }` | `{ "today": date, "nextRenewal": date \| null }` |
| `monthlyEquivalent` | `{ "today": date, "items": Item[] }` | `{ "totals": Money[] }`, by currency |
| `forecast` | `{ "from": date, "to": date, "items": Item[] }` | `{ "occurrences": Occurrence[], "totals": Money[], "trialReminderTarget"?: { "itemId", "date" } }` |
| `invalid` | an `ItemBody`, possibly malformed | `{ "code": "validationError", "errors": [{ "field", "code" }] }` |

Notes:

- `monthlyEquivalent` and `forecast` items are the owner's whole item set, unfiltered: the engine applies the
  lifecycle, trial, schedule-version, price and currency-total rules itself.
- `trialReminderTarget` appears only in forecast cases with exactly one trial item and is that item's
  `trialEndDate`.
- For `invalid` cases, `field` paths are relative to `input`, and `errors` is the complete set the server
  returns for that body, sorted by `field`. All inputs of the other kinds are valid items (a `nextRenewal`
  input carries no `prices`, because the next renewal does not depend on them).
- Occurrence windows in the fixture test the engine directly and may span more than the API's 366-day range
  limit (for example the February 29 case runs to 2032).

Coverage of the plan §5 edge-case table:

| Edge case | Cases |
|---|---|
| €120 annual plan renewing on December 15 | `monthly-equivalent-yearly-120`, `forecast-annual-october`, `forecast-annual-december` |
| €10 monthly plan anchored on January 31, 2027 | `occurrences-monthly-jan31-2027` |
| Annual anchor February 29, 2028 | `occurrences-yearly-feb29` |
| Price €10 until May 31, €12 from June 1 | `forecast-price-change-may`, `forecast-price-change-june` |
| Free trial ends May 10, first paid charge May 11 | `forecast-trial-first-paid-date` (with `trialReminderTarget`), `invalid-trial-anchor-mismatch` |
| Cancellation effective June 15 | `forecast-cancellation-boundary` |
| EUR and USD records | `forecast-eur-and-usd`, `monthly-equivalent-grouped-by-currency` |
| Concurrent edits from two devices | Not a calculation; covered by the [Concurrency](#concurrency) example |
| Worker restarts or duplicate execution | Out of scope here; ST-008 |
