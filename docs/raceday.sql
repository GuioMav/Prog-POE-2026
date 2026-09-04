CREATE DATABASE race;
GO

CREATE TABLE Users
(
    usersID INT IDENTITY(1,1),
    Email VARCHAR(50) NOT NULL,
    userName VARCHAR(50) NOT NULL,
    role VARCHAR(50) NOT NULL,
    AccountType VARCHAR(20) NOT NULL,

    CONSTRAINT PK_Users_usersID
        PRIMARY KEY(usersID),

    CONSTRAINT UQ_Users_Email
        UNIQUE(Email),

    CONSTRAINT UQ_Users_userName
        UNIQUE(userName)
);
GO

CREATE TABLE Organiser
(
    organiserID INT,
    organiserNumber INT,
    usersID INT NOT NULL,

    CONSTRAINT PK_Organiser_organiserID
        PRIMARY KEY(organiserID),

    CONSTRAINT UQ_Organiser_usersID
        UNIQUE(usersID),

    CONSTRAINT FK_Organiser_Users
        FOREIGN KEY(usersID)
        REFERENCES Users(usersID)
);
GO

CREATE TABLE Participant
(
    participantID INT,
    participantName VARCHAR(50) NOT NULL,
    usersID INT NOT NULL,

    CONSTRAINT PK_Participant_participantID
        PRIMARY KEY(participantID),

    CONSTRAINT UQ_Participant_usersID
        UNIQUE(usersID),

    CONSTRAINT FK_Participant_Users
        FOREIGN KEY(usersID)
        REFERENCES Users(usersID)
);
GO

CREATE TABLE Events
(
    eventID INT,
    eventName VARCHAR(50) NOT NULL,
    eventDate DATE NOT NULL,
    description VARCHAR(255),
    location VARCHAR(100) NOT NULL,
    organiserID INT NOT NULL,

    CONSTRAINT PK_Events_eventID
        PRIMARY KEY(eventID),

    CONSTRAINT FK_Events_Organiser
        FOREIGN KEY(organiserID)
        REFERENCES Organiser(organiserID)
);
GO

CREATE TABLE Category
(
    categoryID INT,
    categoryName VARCHAR(50) NOT NULL,
    eventID INT NOT NULL,

    CONSTRAINT PK_Category_categoryID
        PRIMARY KEY(categoryID),

    CONSTRAINT FK_Category_Events
        FOREIGN KEY(eventID)
        REFERENCES Events(eventID)
);
GO

CREATE TABLE Enrollment
(
    enrollmentID INT,
    participantID INT NOT NULL,
    eventID INT NOT NULL,
    enrollmentDate DATE NOT NULL,
    status VARCHAR(30) NOT NULL,
    price DECIMAL(10,2) NOT NULL,

    CONSTRAINT PK_Enrollment_enrollmentID
        PRIMARY KEY(enrollmentID),

    CONSTRAINT UQ_Enrollment_Participant_Event
        UNIQUE(participantID, eventID),

    CONSTRAINT FK_Enrollment_Participant
        FOREIGN KEY(participantID)
        REFERENCES Participant(participantID),

    CONSTRAINT FK_Enrollment_Event
        FOREIGN KEY(eventID)
        REFERENCES Events(eventID)
);
GO

CREATE TABLE Result
(
    resultID INT,
    participantID INT NOT NULL,
    eventID INT NOT NULL,
    finishingTime TIME NOT NULL,
    position INT NOT NULL,

    CONSTRAINT PK_Result_resultID
        PRIMARY KEY(resultID),

    CONSTRAINT UQ_Result_Participant_Event
        UNIQUE(participantID, eventID),

    CONSTRAINT FK_Result_Participant
        FOREIGN KEY(participantID)
        REFERENCES Participant(participantID),

    CONSTRAINT FK_Result_Event
        FOREIGN KEY(eventID)
        REFERENCES Events(eventID)
);
GO

INSERT INTO Users
(Email, userName, role, AccountType)
VALUES
('alice@example.com', 'AliceRunner', 'Participant', 'Standard'),
('bob@example.com', 'BobRunner', 'Participant', 'Standard'),
('carol@example.com', 'CarolRunner', 'Participant', 'Premium'),
('david@example.com', 'DavidEvents', 'Organiser', 'Admin'),
('emma@example.com', 'EmmaEvents', 'Organiser', 'Admin'),
('frank@example.com', 'FrankRunner', 'Participant', 'Standard'),
('grace@example.com', 'GraceRunner', 'Participant', 'Premium'),
('henry@example.com', 'HenryEvents', 'Organiser', 'Admin'),
('isabel@example.com', 'IsabelRunner', 'Participant', 'Standard'),
('james@example.com', 'JamesRunner', 'Participant', 'Standard');
GO

INSERT INTO Organiser
(organiserID, organiserNumber, usersID)
VALUES
(1, 1001, 4),
(2, 1002, 5),
(3, 1003, 8);
GO
INSERT INTO Participant
(participantID, participantName, usersID)
VALUES
(1, 'Alice Johnson', 1),
(2, 'Bob Smith', 2),
(3, 'Carol Williams', 3),
(4, 'Frank Brown', 6),
(5, 'Grace Wilson', 7),
(6, 'Isabel Davis', 9),
(7, 'James Miller', 10);
GO
INSERT INTO Events
(eventID, eventName, eventDate, description, location, organiserID)
VALUES
(1, 'Spring City Run', '2026-09-20',
 'A community road running event for all fitness levels.',
 'Central Park', 1),

(2, 'Summer Beach Race', '2026-10-11',
 'A beach running event featuring multiple race distances.',
 'Brighton Beach', 1),

(3, 'Autumn Mountain Challenge', '2026-10-25',
 'A challenging trail running event through the Highlands.',
 'Highlands', 2),

(4, 'Winter Charity Run', '2026-12-06',
 'A charity running event supporting local community projects.',
 'City Stadium', 3),

(5, 'Cape Fitness Race', '2026-11-08',
 'A fitness-focused road race for recreational and competitive runners.',
 'Green Point', 2),

(6, 'New Year Road Race', '2027-01-10',
 'A new year road race welcoming runners of different abilities.',
 'Main Street', 1);
GO
INSERT INTO Category
(categoryID, categoryName, eventID)
VALUES

-- Spring City Run
(1, '5K Fun Run', 1),
(2, '10K Race', 1),
(3, '21K Half Marathon', 1),

-- Summer Beach Race
(4, '5K Beach Run', 2),
(5, '10K Beach Race', 2),
(6, 'Half Marathon', 2),

-- Autumn Mountain Challenge
(7, '5K Mountain Trail', 3),
(8, '10K Mountain Trail', 3),
(9, '15K Mountain Challenge', 3),

-- Winter Charity Run
(10, '5K Charity Run', 4),
(11, '10K Charity Run', 4),
(12, '21K Charity Run', 4),

-- Cape Fitness Race
(13, '5K Fitness Run', 5),
(14, '10K Fitness Race', 5),
(15, '15K Fitness Race', 5),

-- New Year Road Race
(16, '5K Road Race', 6),
(17, '10K Road Race', 6),
(18, '21K New Year Race', 6);
GO
INSERT INTO Enrollment
(enrollmentID, participantID, eventID, enrollmentDate, status, price)
VALUES

(1, 1, 1, '2026-08-25', 'Confirmed', 150.00),
(2, 2, 1, '2026-08-26', 'Confirmed', 150.00),
(3, 3, 1, '2026-08-27', 'Confirmed', 150.00),
(4, 4, 1, '2026-08-28', 'Confirmed', 150.00),

(5, 1, 2, '2026-08-25', 'Confirmed', 250.00),
(6, 2, 2, '2026-08-26', 'Confirmed', 250.00),
(7, 3, 2, '2026-08-27', 'Confirmed', 250.00),
(8, 5, 2, '2026-08-28', 'Confirmed', 250.00),

(9, 2, 3, '2026-08-27', 'Confirmed', 300.00),
(10, 4, 3, '2026-08-28', 'Confirmed', 300.00),
(11, 5, 3, '2026-08-29', 'Confirmed', 300.00),

(12, 1, 4, '2026-08-25', 'Confirmed', 100.00),
(13, 5, 4, '2026-08-28', 'Confirmed', 100.00),
(14, 6, 4, '2026-08-29', 'Confirmed', 100.00),

(15, 3, 5, '2026-08-27', 'Confirmed', 275.00),
(16, 4, 5, '2026-08-28', 'Confirmed', 275.00),
(17, 7, 5, '2026-08-29', 'Confirmed', 275.00),

(18, 1, 6, '2026-08-25', 'Confirmed', 120.00),
(19, 6, 6, '2026-08-29', 'Confirmed', 120.00),
(20, 7, 6, '2026-08-30', 'Confirmed', 120.00);
GO
