# RaceDay ERD (Part 2)

This ERD reflects the schema built by the EF Core Code-First model in `RaceDay.API` and by
[`raceday.sql`](raceday.sql). A PNG copy is in [`ERD Part 2.png`](ERD%20Part%202.png); the
original Part 1 diagram is kept as [`ERD Image.png`](ERD%20Image.png) for comparison.

```mermaid
erDiagram
    Users ||--o| Organiser : "is a"
    Users ||--o| Participant : "is a"
    Organiser ||--o{ Events : creates
    Events ||--o{ Category : has
    Participant ||--o{ Enrollment : makes
    Events ||--o{ Enrollment : receives
    Category ||--o{ Enrollment : "selected in"
    Participant ||--o{ Result : achieves
    Events ||--o{ Result : records

    Users {
        int UsersId PK
        nvarchar Email UK
        nvarchar UserName UK
        nvarchar PasswordHash
        nvarchar Role "Organiser | Participant"
        nvarchar AccountType "Standard | Premium | Admin"
        datetime2 CreatedAt
    }
    Organiser {
        int OrganiserId PK
        int OrganiserNumber UK
        nvarchar OrganisationName
        int UsersId FK,UK
    }
    Participant {
        int ParticipantId PK
        nvarchar ParticipantName
        date DateOfBirth "nullable"
        int UsersId FK,UK
    }
    Events {
        int EventId PK
        nvarchar EventName
        date EventDate
        nvarchar Description "nullable"
        nvarchar Location
        decimal DistanceKm
        nvarchar EventType "Run | Walk | Cycle"
        decimal EntryFee
        int OrganiserId FK
    }
    Category {
        int CategoryId PK
        nvarchar CategoryName "unique per event"
        nvarchar CategoryType "Age | Distance"
        int EventId FK
    }
    Enrollment {
        int EnrollmentId PK
        int ParticipantId FK "unique with EventId"
        int EventId FK
        int CategoryId FK
        date EnrollmentDate
        nvarchar Status "Pending | Confirmed | Cancelled"
        decimal Price
    }
    Result {
        int ResultId PK
        int ParticipantId FK "unique with EventId"
        int EventId FK "unique with Position"
        time FinishingTime
        int Position
    }
```

## Relationships

| Relationship | Cardinality | Notes |
|---|---|---|
| Users – Organiser | 1 to 0..1 | Each organiser profile belongs to exactly one user (unique `UsersId`). |
| Users – Participant | 1 to 0..1 | Each participant profile belongs to exactly one user (unique `UsersId`). |
| Organiser – Events | 1 to many | An organiser creates many events. |
| Events – Category | 1 to many | Categories are deleted with their event. |
| Participant – Enrollment | 1 to many | A participant can enter many events, once per event. |
| Events – Enrollment | 1 to many | |
| Category – Enrollment | 1 to many | The category the participant chose when entering. |
| Participant – Result | 1 to many | One result per participant per event. |
| Events – Result | 1 to many | Each finishing position is used once per event. |

Participant and Events form a **many-to-many** relationship through `Enrollment` (entries) and
through `Result` (finishers).
