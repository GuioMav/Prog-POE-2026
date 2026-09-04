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
