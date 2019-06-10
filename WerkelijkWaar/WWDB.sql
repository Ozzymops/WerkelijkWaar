-- ===========================================================================
-- Database script voor 'Werkelijk Waar?'
-- 1. Maak een database aan met de naam 'WWDB'.
-- 2. Plak het onderstaande script in een query en voer deze uit.
-- 3. Start de applicatie.
-- ===========================================================================
-- 1. Administrator - wordt gebruikt door de applicatie om toegang te krijgen tot de database.
-- vergeet niet het veld aan te passen. Vergeet vervolgens niet om het veld te reflecteren in de configuratie!
CREATE LOGIN DBMASTER WITH PASSWORD = 'ADMINPASSWORD'
GO;

-- 2. Geef administrator 100% rechten
IF NOT EXISTS (SELECT * FROM sys.database_principals WHERE name = N'DBMASTER')
BEGIN
	CREATE USER [DBMASTER] FOR LOGIN [DBMASTER]
	EXEC sp_addrolemember N'db_owner', N'DBMASTER'
END;
GO;

-- 3. Verwijder tabellen als deze al bestaan
DROP TABLE [Score];
DROP TABLE [GameType];
DROP TABLE [Story];
DROP TABLE [Configuration];
DROP TABLE [User];
DROP TABLE [Group];
DROP TABLE [School];

-- 4. Maak tabellen aan
CREATE TABLE [dbo].[School] (
[Id] INT IDENTITY (1, 1),
[Name] NVARCHAR(100)
PRIMARY KEY (Id));

CREATE TABLE [dbo].[Group] (
[Id] INT IDENTITY (1, 1),
[SchoolId] INT,
[GroupId] INT,
[GroupName] NVARCHAR(100),
PRIMARY KEY (Id),
FOREIGN KEY (SchoolId) REFERENCES [School](Id));

CREATE TABLE [dbo].[User] (
[Id] INT IDENTITY (1, 1),
[RoleId] INT,
[GroupId] INT,
[Name] NVARCHAR(100),
[Surname] NVARCHAR(100),
[Username] NVARCHAR(100),
[Password] BINARY(64),
[Salt] UNIQUEIDENTIFIER,
[Attempts] INT,
[ImageSource] NVARCHAR(100),
PRIMARY KEY (Id),
FOREIGN KEY (GroupId) REFERENCES [Group](Id));

CREATE TABLE [dbo].[GameType] (
[Id] INT IDENTITY (1, 1),
[Game] NVARCHAR(100)
PRIMARY KEY (Id));

CREATE TABLE [dbo].[Score] (
[Id] INT IDENTITY (1, 1),
[OwnerId] INT NOT NULL,
[GameType] INT NOT NULL,
[FollowerAmount] INT,
[CashAmount] DECIMAL(9, 2),
[AttainedVotes] INT,
[Answers] NVARCHAR(255),
[Date] DATE,
PRIMARY KEY (Id),
FOREIGN KEY (OwnerId) REFERENCES [User](Id),
FOREIGN KEY (GameType) REFERENCES [GameType](Id));

CREATE TABLE [dbo].[Story] (
[Id] INT IDENTITY (1, 1),
[OwnerId] INT NOT NULL,
[IsRoot] INT,
[Title] NVARCHAR(100),
[Description] NVARCHAR(MAX),
[Date] DATE,
[Status] INT,
[Source] INT,
PRIMARY KEY (Id),
FOREIGN KEY (OwnerId) REFERENCES [User](Id));

CREATE TABLE [dbo].[Configuration] (
[Id] INT IDENTITY (1, 1),
[OwnerId] INT NOT NULL,
[MaxWritingTime] INT,
[MaxReadingTime] INT,
[FollowerGain] INT,
[FollowerLoss] INT,
[FollowerPerVote] INT,
[CashPerFollower] DECIMAL(9, 2),
[CashPerVote] DECIMAL,
[MaxPlayers] INT,
[PowerupsAllowed] BIT,
[PowerupsCostMult] DECIMAL(9, 2),
PRIMARY KEY (Id),
FOREIGN KEY (OwnerId) REFERENCES [User](Id));

-- 5. Maak de benodigde procedures aan voor het aanmaken van gebruikers en inloggen.
-- [AddUser]
CREATE PROCEDURE [dbo].[AddUser]
	@pRoleId INT,
	@pGroup INT,
	@pName NVARCHAR(100),
	@pSurname NVARCHAR(100),
	@pUsername NVARCHAR(100),
	@pPassword NVARCHAR(100),
	@responseMessage NVARCHAR(250) OUTPUT
AS
BEGIN
	SET NOCOUNT ON

	DECLARE @Attempts INT = 0
	DECLARE @Salt UNIQUEIDENTIFIER=NEWID()

	BEGIN TRY
		INSERT INTO [dbo].[User] (RoleId, GroupId, Name, Surname, Username, Password, Salt, Attempts)
		VALUES (@pRoleId, @pGroup, @pName, @pSurname, @pUsername, HASHBYTES('SHA2_512', @pPassword+CAST(@Salt AS NVARCHAR(36))), @Salt, @Attempts)
		SET @responseMessage = 'Success!'
	END TRY
	BEGIN CATCH
		SET @responseMessage = ERROR_MESSAGE()
	END CATCH
END;

-- [EditPassword]
CREATE PROCEDURE [dbo].[EditPassword]
	@pId INT,
	@pPassword NVARCHAR(100),
	@responseMessage NVARCHAR(250) OUTPUT
AS
BEGIN
	SET NOCOUNT ON

	BEGIN TRY
		UPDATE [dbo].[User] SET [Password] = HASHBYTES('SHA2_512', @pPassword+CAST([Salt] AS NVARCHAR(36))) WHERE [Id] = @pId
		SET @responseMessage = 'Success!'
	END TRY
	BEGIN CATCH
		SET @responseMessage = ERROR_MESSAGE()
	END CATCH
END;

-- [LogIn]
CREATE PROCEDURE [dbo].[LogIn]
	@pUsername NVARCHAR(100),
	@pPassword NVARCHAR(100),
	@responseMessage NVARCHAR(250) = '' OUTPUT,
	@userId INT = 0 OUTPUT
AS
BEGIN

	SET NOCOUNT ON

	DECLARE @Id INT

	IF EXISTS (SELECT TOP 1 Id FROM [dbo].[User] WHERE Username = @pUsername)
		BEGIN
			SET @Id = (SELECT Id FROM [dbo].[User] WHERE Username = @pUsername AND Password = HASHBYTES('SHA2_512', @pPassword+CAST(Salt AS NVARCHAR(36))))
			IF (@Id IS NULL)
				BEGIN
					SET @responseMessage = 'Incorrect password.'
					SET @userId = 0
				END
			ELSE
				BEGIN
					SET @responseMessage = 'Success!'
					SET @userId = @Id
				END
		END
	ELSE
		BEGIN
			SET @responseMessage = 'Invalid login.'
			SET @userId = 0
		END
END;

-- 6. (optioneel) Vul de tabellen met fillerdata.
INSERT INTO [dbo].[School] SELECT 'geen';
INSERT INTO [dbo].[School] SELECT 'Basissus Schoolus';
INSERT INTO [dbo].[School] SELECT 'Nog een skool';

INSERT INTO [dbo].[Group] SELECT 1, 0, 'geen';
INSERT INTO [dbo].[Group] SELECT 2, 1, '7';
INSERT INTO [dbo].[Group] SELECT 2, 2, '8';
INSERT INTO [dbo].[Group] SELECT 3, 1, '8a';
INSERT INTO [dbo].[Group] SELECT 3, 2, '8b';

DECLARE @responseMessage NVARCHAR(250)

EXEC [dbo].[AddUser] -- systeem
	@pRoleId = 4,
	@pGroup = 1,
	@pName = N'System',
	@pSurname = N'System',
	@pUsername = N'System',
	@pPassword = N'abcdef',
	@responseMessage=@responseMessage OUTPUT

EXEC [dbo].[AddUser] -- überadmin
	@pRoleId = 3,
	@pGroup = 1,
	@pName = N'Uber',
	@pSurname = N'Admin',
	@pUsername = N'UberAdmin',
	@pPassword = N'SuperSterkeUberAdminPassword',
	@responseMessage=@responseMessage OUTPUT

EXEC [dbo].[AddUser] -- admin
	@pRoleId = 2,
	@pGroup = 2,
	@pName = N'Admin',
	@pSurname = N'Admin',
	@pUsername = N'Admin',
	@pPassword = N'SuperSterkeAdminPassword',
	@responseMessage=@responseMessage OUTPUT

EXEC [dbo].[AddUser] -- docent
	@pRoleId = 1,
	@pGroup = 2,
	@pName = N'Justin',
	@pSurname = N'Muris',
	@pUsername = N'JMuris',
	@pPassword = N'1234',
	@responseMessage=@responseMessage OUTPUT

EXEC [dbo].[AddUser] -- student 1
	@pRoleId = 0,
	@pGroup = 2,
	@pName = N'Aarie',
	@pSurname = N'Appel',
	@pUsername = N'AAppel',
	@pPassword = N'4321',
	@responseMessage=@responseMessage OUTPUT

EXEC [dbo].[AddUser] -- student 2
	@pRoleId = 0,
	@pGroup = 2,
	@pName = N'Barrie',
	@pSurname = N'Batsbak',
	@pUsername = N'BBatsbak',
	@pPassword = N'5678',
	@responseMessage=@responseMessage OUTPUT

EXEC [dbo].[AddUser] -- student 3
	@pRoleId = 0,
	@pGroup = 3,
	@pName = N'Carrie',
	@pSurname = N'Cacao',
	@pUsername = N'CCacao',
	@pPassword = N'8765',
	@responseMessage=@responseMessage OUTPUT
	
EXEC [dbo].[AddUser] -- student 4
	@pRoleId = 0,
	@pGroup = 4,
	@pName = N'Derek',
	@pSurname = N'Dik',
	@pUsername = N'DDik',
	@pPassword = N'0000',
	@responseMessage=@responseMessage OUTPUT
		
INSERT INTO [dbo].[GameType] SELECT 'Werkelijk Waar?';
INSERT INTO [dbo].[GameType] SELECT 'Liegen';
INSERT INTO [dbo].[GameType] SELECT 'Overladen';
INSERT INTO [dbo].[GameType] SELECT 'Misleiden';
INSERT INTO [dbo].[GameType] SELECT 'Quizmaster';
INSERT INTO [dbo].[GameType] SELECT 'Verkoper';
INSERT INTO [dbo].[GameType] SELECT 'Detective';
INSERT INTO [dbo].[GameType] SELECT 'Sneller dan het licht';
INSERT INTO [dbo].[GameType] SELECT 'De machine';
	
INSERT INTO [dbo].[Score] SELECT 5, 1, 10, '5.20', 5, '00100111101111', '2019-03-07';
INSERT INTO [dbo].[Score] SELECT 5, 1, 50, '50.20', 50, '111111111111111111111', '2019-03-08';
INSERT INTO [dbo].[Score] SELECT 5, 1, 0, '0.00', 0, '000000000000000', '2019-03-07';
INSERT INTO [dbo].[Score] SELECT 6, 1, 1, '1.00', 1, '000000000000001', '2019-03-08';
INSERT INTO [dbo].[Score] SELECT 6, 1, 25, '25.00', 25, '111111111111101111', '2019-03-07';
INSERT INTO [dbo].[Score] SELECT 6, 1, 90, '900.00', 90, '1111111111101111', '2019-03-08';
INSERT INTO [dbo].[Score] SELECT 7, 2, NULL, NULL, NULL, 'Ja, Nee, Ja, Nee', '2019-03-19';
INSERT INTO [dbo].[Score] SELECT 7, 3, NULL, NULL, NULL, 'AABABBBA', '2019-03-19';
INSERT INTO [dbo].[Score] SELECT 7, 4, NULL, NULL, NULL, '123456', '2019-03-19';
INSERT INTO [dbo].[Score] SELECT 8, 5, NULL, NULL, NULL, 'xyzabc', '2019-03-19';
INSERT INTO [dbo].[Score] SELECT 8, 6, NULL, NULL, NULL, '4hgl3123jgkls', '2019-03-19';

INSERT INTO [dbo].[Story] SELECT 1, 1, 'Een root verhaal', 'Straight outta RSS feed', '2019-05-22', 2, -1;
INSERT INTO [dbo].[Story] SELECT 5, 0, 'Een verhaal', 'Lorem Ipsum bla bla bla', '2019-03-11', 2, 1;
INSERT INTO [dbo].[Story] SELECT 6, 1, 'Nog een verhaal', 'bla bla bla Ipsum Lorem', '2019-03-12', 2, 1;