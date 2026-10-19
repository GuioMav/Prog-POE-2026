# RaceDay API – Endpoint Plan

This plan is the specification for the Part 2 API. It started as [`Endpoint.docx`](Endpoint.docx)
in Part 1 and has been updated to match the implemented API exactly – every row below can be
tried in Swagger UI (`/swagger`). Differences from the Part 1 plan are listed at the end.

**Authentication:** `POST /api/auth/login` stores the user's id and role in a **server-side
session**; the client keeps an HttpOnly session cookie. Endpoints marked *Organiser*,
*Participant* or *Any logged in* return **401** without a session and **403** for the wrong role.
Errors are returned as RFC 7807 `ProblemDetails` JSON.

## Authentication

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | `/api/auth/register` | Creates a user account plus its Organiser or Participant profile; the password is hashed before it is stored. | None (public) | `{ email, userName, password, role: "Organiser"\|"Participant", name, dateOfBirth?, accountType? }` | **201 Created** – profile. **400** – invalid fields / role data. **409 Conflict** – email or username already used. |
| POST | `/api/auth/login` | Verifies the email and password hash and starts a session holding the user id and role. | None (public) | `{ email, password }` | **200 OK** – profile + session cookie. **400** – missing fields. **401 Unauthorized** – wrong email or password. |
| POST | `/api/auth/logout` | Clears the server-side session. | None | None | **204 No Content** |
| GET | `/api/auth/me` | Returns the profile of the user in the current session. | Any logged in | None | **200 OK** – profile. **401** – no session. |

## User Profile

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | `/api/users/{id}` | View your own profile (id = your UsersId). | Any logged in | None | **200 OK** – profile. **401**. **403** – another user's id. **404** – not found. |
| PUT | `/api/users/{id}` | Update your own email, username, name (and date of birth for participants). | Any logged in | `{ email, userName, name, dateOfBirth? }` | **200 OK** – updated profile. **400** – invalid. **401**. **403** – another user's id. **409** – email/username in use. |

## Events

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | `/api/events` | List all events, optionally filtered by `type`, `location`, `from`, `to`. | None (public) | None | **200 OK** – events with category and entry counts. |
| GET | `/api/events/upcoming` | List events dated today or later, soonest first. | None (public) | None | **200 OK** – events. |
| GET | `/api/events/{id}` | Full event details including its categories. | None (public) | None | **200 OK** – event + categories. **404** – not found. |
| POST | `/api/events` | Create an event owned by the logged-in organiser. | Organiser | `{ eventName, eventDate, description?, location, distanceKm, eventType: "Run"\|"Walk"\|"Cycle", entryFee }` | **201 Created** – event. **400** – invalid or past date. **401**. **403** – not an organiser. |
| PUT | `/api/events/{id}` | Update one of your own events. | Organiser | Same as POST | **200 OK** – event. **400**. **401**. **403** – not your event. **404**. |
| DELETE | `/api/events/{id}` | Delete one of your events with its categories, entries and results. | Organiser | None | **204 No Content**. **401**. **403** – not your event. **404**. |
| GET | `/api/organisers/{organiserId}/events` | List the events you manage (organiserId = your OrganiserId). | Organiser | None | **200 OK** – events. **401**. **403** – another organiser's id. |

## Categories

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | `/api/events/{eventId}/categories` | List an event's age and distance categories. | None (public) | None | **200 OK** – categories. **404** – event not found. |
| POST | `/api/events/{eventId}/categories` | Add a category (e.g. Under 20, Senior, 10km) to one of your events. | Organiser | `{ categoryName, categoryType: "Age"\|"Distance" }` | **201 Created** – category. **400**. **401**. **403** – not your event. **404**. **409** – name already used in this event. |
| PUT | `/api/categories/{id}` | Rename or retype a category on one of your events. | Organiser | `{ categoryName, categoryType }` | **200 OK** – category. **400**. **401**. **403**. **404**. **409** – duplicate name. |
| DELETE | `/api/categories/{id}` | Remove a category that has no entries. | Organiser | None | **204 No Content**. **401**. **403**. **404**. **409** – participants are entered in it. |

## Event Enrolments

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | `/api/enrollments` | Enter an event by selecting one of its categories; records participant, event and category, price = entry fee. | Participant | `{ eventId, categoryId }` | **201 Created** – enrollment (status Confirmed). **400** – category not in event / event already happened. **401**. **403** – not a participant. **404** – event or category not found. **409** – already entered. |
| GET | `/api/enrollments/{id}` | View one entry. | Any logged in (the entrant or the event's organiser) | None | **200 OK**. **401**. **403** – not your entry / not your event. **404**. |
| GET | `/api/participants/{participantId}/enrollments` | View your own entries and their status. | Participant | None | **200 OK** – entries. **401**. **403** – another participant's id. |
| GET | `/api/events/{eventId}/enrollments` | View everyone entered in one of your events, with category and status. | Organiser | None | **200 OK** – entries. **401**. **403** – not your event. **404**. |
| PUT | `/api/enrollments/{id}` | Change status. Organisers: any status for their events; participants: only `Cancelled` for their own entry. | Participant / Organiser | `{ status: "Pending"\|"Confirmed"\|"Cancelled" }` | **200 OK** – entry. **400**. **401**. **403**. **404**. |
| DELETE | `/api/enrollments/{id}` | Withdraw your entry (not allowed once a result exists). | Participant | None | **204 No Content**. **401**. **403** – not your entry. **404**. **409** – result already recorded. |

## Results

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | `/api/events/{eventId}/results` | Event leaderboard ordered by position. | None (public) | None | **200 OK** – results. **404** – event not found. |
| POST | `/api/results` | Capture a finish time and position for an entered participant after your event. | Organiser | `{ participantId, eventId, finishingTime: "hh:mm:ss", position }` | **201 Created** – result. **400** – invalid, event not yet held, or participant not entered. **401**. **403** – not your event. **404**. **409** – result exists or position taken. |
| PUT | `/api/results/{id}` | Correct a result on your event. | Organiser | `{ finishingTime, position }` | **200 OK** – result. **400**. **401**. **403**. **404**. **409** – position taken. |
| DELETE | `/api/results/{id}` | Delete a result on your event. | Organiser | None | **204 No Content**. **401**. **403**. **404**. |
| GET | `/api/participants/{participantId}/results` | Your race history: event, date, category, time, position and total finishers. | Participant | None | **200 OK** – results. **401**. **403** – another participant's id. |

## Changes from the Part 1 plan

| Part 1 plan | Implemented | Reason |
|---|---|---|
| Register body: `Email, userName, role, AccountType` | Adds `password`, `name`, optional `dateOfBirth`; `accountType` only for participants | Part 2 requires a hashed password and personal details; organisers are always Admin. |
| — | `POST /api/auth/logout`, `GET /api/auth/me` added | Sessions need a way to end, and clients (Swagger, Part 3 MVC) need to know who is logged in. |
| `POST /api/enrollments` body: `ParticipantID, EventID, enrollmentDate, status, price` | Body is `{ eventId, categoryId }` | The participant comes from the session (a user cannot enrol someone else); date, status and price are set by the server; Part 2 requires the chosen category. |
| `PUT /api/users/{id}`, participant/organiser lists by id | Only your own id is allowed (403 otherwise) | Prevents users reading or changing other users' data. |
| `GET /api/events` | Adds optional filters `type`, `location`, `from`, `to` | Supports the Part 3 home page filtering. |
| Role names in plan: "Authenticated User" | "Any logged in" | Same meaning, matches the session filter. |
