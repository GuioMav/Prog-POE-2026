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
