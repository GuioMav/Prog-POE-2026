/* =====================================================================
   RaceDay database - SQL Server script (Part 1, updated for Part 2)

   Creates the RaceDay schema and seeds sample data. The schema is
   identical to the EF Core Code-First model in RaceDay.API (migration
   20261019124903_InitialCreate): same tables, columns, keys, defaults,
   unique and check constraints.

   Changes since the Part 1 submission (explained in the README):
     - USE RaceDay added so tables are not created in master
     - Users: PasswordHash (hashed, never plain text) and CreatedAt
     - Organiser: OrganisationName; Participant: DateOfBirth
     - Events: DistanceKm, EventType (Run/Walk/Cycle), EntryFee
     - Category: CategoryType (Age/Distance), unique name per event
     - Enrollment: CategoryId - entries now record the chosen category
     - Result: one position per event, Position > 0
     - All primary keys are IDENTITY columns

   Every seeded account's password is: Password123!
   ===================================================================== */

CREATE DATABASE RaceDay;
GO

USE RaceDay;
GO

/* ---------- Users: login account shared by both roles ---------- */
CREATE TABLE Users
(
    UsersId      INT IDENTITY(1,1) NOT NULL,
    Email        NVARCHAR(100)     NOT NULL,
    UserName     NVARCHAR(50)      NOT NULL,
    PasswordHash NVARCHAR(255)     NOT NULL,
    Role         NVARCHAR(20)      NOT NULL,   -- Organiser | Participant
    AccountType  NVARCHAR(20)      NOT NULL,   -- Standard | Premium | Admin
    CreatedAt    DATETIME2         NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT (GETUTCDATE()),

    CONSTRAINT PK_Users PRIMARY KEY (UsersId)
);
CREATE UNIQUE INDEX IX_Users_Email ON Users (Email);
CREATE UNIQUE INDEX IX_Users_UserName ON Users (UserName);
GO

/* ---------- Organiser: 1-to-1 with Users ---------- */
CREATE TABLE Organiser
(
    OrganiserId      INT IDENTITY(1,1) NOT NULL,
    OrganiserNumber  INT               NOT NULL,
    OrganisationName NVARCHAR(100)     NOT NULL,
    UsersId          INT               NOT NULL,

    CONSTRAINT PK_Organiser PRIMARY KEY (OrganiserId),
    CONSTRAINT FK_Organiser_Users_UsersId
        FOREIGN KEY (UsersId) REFERENCES Users (UsersId) ON DELETE CASCADE
);
CREATE UNIQUE INDEX IX_Organiser_OrganiserNumber ON Organiser (OrganiserNumber);
CREATE UNIQUE INDEX IX_Organiser_UsersId ON Organiser (UsersId);
GO

/* ---------- Participant: 1-to-1 with Users ---------- */
CREATE TABLE Participant
(
    ParticipantId   INT IDENTITY(1,1) NOT NULL,
    ParticipantName NVARCHAR(100)     NOT NULL,
    DateOfBirth     DATE              NULL,
    UsersId         INT               NOT NULL,

    CONSTRAINT PK_Participant PRIMARY KEY (ParticipantId),
    CONSTRAINT FK_Participant_Users_UsersId
        FOREIGN KEY (UsersId) REFERENCES Users (UsersId) ON DELETE CASCADE
);
CREATE UNIQUE INDEX IX_Participant_UsersId ON Participant (UsersId);
GO

/* ---------- Events: created by an organiser ---------- */
CREATE TABLE Events
(
    EventId     INT IDENTITY(1,1) NOT NULL,
    EventName   NVARCHAR(100)     NOT NULL,
    EventDate   DATE              NOT NULL,
    Description NVARCHAR(500)     NULL,
    Location    NVARCHAR(100)     NOT NULL,
    DistanceKm  DECIMAL(6,2)      NOT NULL,
    EventType   NVARCHAR(10)      NOT NULL,    -- Run | Walk | Cycle
    EntryFee    DECIMAL(10,2)     NOT NULL CONSTRAINT DF_Events_EntryFee DEFAULT (0.0),
    OrganiserId INT               NOT NULL,

    CONSTRAINT PK_Events PRIMARY KEY (EventId),
    CONSTRAINT CK_Events_DistanceKm CHECK (DistanceKm > 0),
    CONSTRAINT CK_Events_EntryFee CHECK (EntryFee >= 0),
    CONSTRAINT FK_Events_Organiser_OrganiserId
        FOREIGN KEY (OrganiserId) REFERENCES Organiser (OrganiserId) ON DELETE NO ACTION
);
CREATE INDEX IX_Events_OrganiserId ON Events (OrganiserId);
GO

/* ---------- Category: age or distance category within an event ---------- */
CREATE TABLE Category
(
    CategoryId   INT IDENTITY(1,1) NOT NULL,
    CategoryName NVARCHAR(50)      NOT NULL,
    CategoryType NVARCHAR(10)      NOT NULL,   -- Age | Distance
    EventId      INT               NOT NULL,

    CONSTRAINT PK_Category PRIMARY KEY (CategoryId),
    CONSTRAINT FK_Category_Events_EventId
        FOREIGN KEY (EventId) REFERENCES Events (EventId) ON DELETE CASCADE
);
CREATE UNIQUE INDEX IX_Category_EventId_CategoryName ON Category (EventId, CategoryName);
GO

/* ---------- Enrollment: participant + event + chosen category ----------
   NO ACTION on all FKs: SQL Server does not allow two cascade paths
   (Events -> Category -> Enrollment and Events -> Enrollment). */
CREATE TABLE Enrollment
(
    EnrollmentId   INT IDENTITY(1,1) NOT NULL,
    ParticipantId  INT               NOT NULL,
    EventId        INT               NOT NULL,
    CategoryId     INT               NOT NULL,
    EnrollmentDate DATE              NOT NULL,
    Status         NVARCHAR(20)      NOT NULL, -- Pending | Confirmed | Cancelled
    Price          DECIMAL(10,2)     NOT NULL,

    CONSTRAINT PK_Enrollment PRIMARY KEY (EnrollmentId),
    CONSTRAINT CK_Enrollment_Price CHECK (Price >= 0),
    CONSTRAINT FK_Enrollment_Participant_ParticipantId
        FOREIGN KEY (ParticipantId) REFERENCES Participant (ParticipantId) ON DELETE NO ACTION,
    CONSTRAINT FK_Enrollment_Events_EventId
        FOREIGN KEY (EventId) REFERENCES Events (EventId) ON DELETE NO ACTION,
    CONSTRAINT FK_Enrollment_Category_CategoryId
        FOREIGN KEY (CategoryId) REFERENCES Category (CategoryId) ON DELETE NO ACTION
);
CREATE UNIQUE INDEX IX_Enrollment_ParticipantId_EventId ON Enrollment (ParticipantId, EventId);
CREATE INDEX IX_Enrollment_EventId ON Enrollment (EventId);
CREATE INDEX IX_Enrollment_CategoryId ON Enrollment (CategoryId);
GO

/* ---------- Result: finish time and position after an event ---------- */
CREATE TABLE Result
(
    ResultId      INT IDENTITY(1,1) NOT NULL,
    ParticipantId INT               NOT NULL,
    EventId       INT               NOT NULL,
    FinishingTime TIME              NOT NULL,
    Position      INT               NOT NULL,

    CONSTRAINT PK_Result PRIMARY KEY (ResultId),
    CONSTRAINT CK_Result_Position CHECK (Position > 0),
    CONSTRAINT FK_Result_Participant_ParticipantId
        FOREIGN KEY (ParticipantId) REFERENCES Participant (ParticipantId) ON DELETE NO ACTION,
    CONSTRAINT FK_Result_Events_EventId
        FOREIGN KEY (EventId) REFERENCES Events (EventId) ON DELETE NO ACTION
);
CREATE UNIQUE INDEX IX_Result_ParticipantId_EventId ON Result (ParticipantId, EventId);
CREATE UNIQUE INDEX IX_Result_EventId_Position ON Result (EventId, Position);
GO

/* ---------- EF Core migration history ----------
   Marks the schema as created so the API (which applies EF migrations on
   startup) can run against a database built by this script. */
CREATE TABLE __EFMigrationsHistory
(
    MigrationId    NVARCHAR(150) NOT NULL,
    ProductVersion NVARCHAR(32)  NOT NULL,
    CONSTRAINT PK___EFMigrationsHistory PRIMARY KEY (MigrationId)
);
INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
VALUES (N'20261019124903_InitialCreate', N'10.0.13');
GO

/* =====================================================================
   Sample data
   ===================================================================== */

-- PBKDF2 hash (ASP.NET Core PasswordHasher) of "Password123!"
DECLARE @Hash NVARCHAR(255) = N'AQAAAAIAAYagAAAAEGTFFgOm3OrYm4TFn1ExT24YIxiWfapH2d43ychdUuU7JxSuIESX/g8XpBVOZO57Ow==';

INSERT INTO Users (Email, UserName, PasswordHash, Role, AccountType)
VALUES
(N'alice@example.com',  N'AliceRunner',  @Hash, N'Participant', N'Standard'),  -- 1
(N'bob@example.com',    N'BobRunner',    @Hash, N'Participant', N'Standard'),  -- 2
(N'carol@example.com',  N'CarolRunner',  @Hash, N'Participant', N'Premium'),   -- 3
(N'david@example.com',  N'DavidEvents',  @Hash, N'Organiser',   N'Admin'),     -- 4
(N'emma@example.com',   N'EmmaEvents',   @Hash, N'Organiser',   N'Admin'),     -- 5
(N'frank@example.com',  N'FrankRunner',  @Hash, N'Participant', N'Standard'),  -- 6
(N'grace@example.com',  N'GraceRunner',  @Hash, N'Participant', N'Premium'),   -- 7
(N'henry@example.com',  N'HenryEvents',  @Hash, N'Organiser',   N'Admin'),     -- 8
(N'isabel@example.com', N'IsabelRunner', @Hash, N'Participant', N'Standard'),  -- 9
(N'james@example.com',  N'JamesRunner',  @Hash, N'Participant', N'Standard');  -- 10
GO

INSERT INTO Organiser (OrganiserNumber, OrganisationName, UsersId)
VALUES
(1001, N'Spring City Athletics', 4),   -- OrganiserId 1
(1002, N'Cape Trail Runners',    5),   -- 2
(1003, N'Winter Charity Trust',  8);   -- 3
GO

INSERT INTO Participant (ParticipantName, DateOfBirth, UsersId)
VALUES
(N'Alice Johnson',  '1995-04-12', 1),   -- ParticipantId 1
(N'Bob Smith',      '1988-09-03', 2),   -- 2
(N'Carol Williams', '2001-01-27', 3),   -- 3
(N'Frank Brown',    '1979-06-15', 6),   -- 4
(N'Grace Wilson',   '1999-11-08', 7),   -- 5
(N'Isabel Davis',   '2008-02-19', 9),   -- 6
(N'James Miller',   '1992-07-30', 10);  -- 7
GO

INSERT INTO Events (EventName, EventDate, Description, Location, DistanceKm, EventType, EntryFee, OrganiserId)
VALUES
(N'Spring City Run',           '2026-09-20', N'A community road running event for all fitness levels.',               N'Central Park',   21.10, N'Run',   150.00, 1),
(N'Summer Beach Race',         '2026-10-11', N'A beach running event featuring multiple race distances.',             N'Brighton Beach', 21.10, N'Run',   250.00, 1),
(N'Autumn Mountain Challenge', '2026-10-25', N'A challenging trail running event through the Highlands.',             N'Highlands',      15.00, N'Run',   300.00, 2),
(N'Winter Charity Walk',       '2026-12-06', N'A charity walk supporting local community projects.',                  N'City Stadium',   21.10, N'Walk',  100.00, 3),
(N'Cape Fitness Ride',         '2026-11-08', N'A fitness-focused cycle race for recreational and competitive riders.', N'Green Point',   50.00, N'Cycle', 275.00, 2),
(N'New Year Road Race',        '2027-01-10', N'A new year road race welcoming runners of different abilities.',       N'Main Street',    21.10, N'Run',   120.00, 1);
GO

INSERT INTO Category (CategoryName, CategoryType, EventId)
VALUES
-- Spring City Run (1)
(N'5K Fun Run',             N'Distance', 1),  -- 1
(N'10K Race',               N'Distance', 1),  -- 2
(N'21K Half Marathon',      N'Distance', 1),  -- 3
-- Summer Beach Race (2)
(N'5K Beach Run',           N'Distance', 2),  -- 4
(N'10K Beach Race',         N'Distance', 2),  -- 5
(N'Half Marathon',          N'Distance', 2),  -- 6
-- Autumn Mountain Challenge (3)
(N'5K Mountain Trail',      N'Distance', 3),  -- 7
(N'10K Mountain Trail',     N'Distance', 3),  -- 8
(N'15K Mountain Challenge', N'Distance', 3),  -- 9
-- Winter Charity Walk (4)
(N'5K Charity Walk',        N'Distance', 4),  -- 10
(N'10K Charity Walk',       N'Distance', 4),  -- 11
(N'21K Charity Walk',       N'Distance', 4),  -- 12
(N'Under 20',               N'Age',      4),  -- 13
(N'Senior',                 N'Age',      4),  -- 14
-- Cape Fitness Ride (5)
(N'25K Fitness Ride',       N'Distance', 5),  -- 15
(N'50K Fitness Ride',       N'Distance', 5),  -- 16
(N'Veterans 50+',           N'Age',      5),  -- 17
-- New Year Road Race (6)
(N'5K Road Race',           N'Distance', 6),  -- 18
(N'10K Road Race',          N'Distance', 6),  -- 19
(N'21K New Year Race',      N'Distance', 6);  -- 20
GO

INSERT INTO Enrollment (ParticipantId, EventId, CategoryId, EnrollmentDate, Status, Price)
VALUES
(1, 1,  3, '2026-08-25', N'Confirmed', 150.00),
(2, 1,  3, '2026-08-26', N'Confirmed', 150.00),
(3, 1,  2, '2026-08-27', N'Confirmed', 150.00),
(4, 1,  1, '2026-08-28', N'Confirmed', 150.00),

(1, 2,  6, '2026-08-25', N'Confirmed', 250.00),
(2, 2,  6, '2026-08-26', N'Confirmed', 250.00),
(3, 2,  6, '2026-08-27', N'Confirmed', 250.00),
(5, 2,  5, '2026-08-28', N'Confirmed', 250.00),

(2, 3,  9, '2026-08-27', N'Confirmed', 300.00),
(4, 3,  8, '2026-08-28', N'Confirmed', 300.00),
(5, 3,  9, '2026-08-29', N'Confirmed', 300.00),

(1, 4, 10, '2026-08-25', N'Confirmed', 100.00),
(5, 4, 10, '2026-08-28', N'Confirmed', 100.00),
(6, 4, 13, '2026-08-29', N'Confirmed', 100.00),

(3, 5, 16, '2026-08-27', N'Confirmed', 275.00),
(4, 5, 15, '2026-08-28', N'Confirmed', 275.00),
(7, 5, 16, '2026-08-29', N'Confirmed', 275.00),

(1, 6, 19, '2026-08-25', N'Confirmed', 120.00),
(6, 6, 18, '2026-08-29', N'Confirmed', 120.00),
(7, 6, 19, '2026-08-30', N'Confirmed', 120.00);
GO

-- Results for the two events that have already taken place
INSERT INTO Result (ParticipantId, EventId, FinishingTime, Position)
VALUES
(1, 1, '01:38:35', 1),
(2, 1, '01:51:42', 2),
(3, 1, '00:54:18', 3),

(1, 2, '01:52:20', 1),
(2, 2, '02:01:45', 2),
(3, 2, '02:08:30', 3);
GO

SELECT * FROM Users;
SELECT * FROM Organiser;
SELECT * FROM Participant;
SELECT * FROM Events;
SELECT * FROM Category;
SELECT * FROM Enrollment;
SELECT * FROM Result;
