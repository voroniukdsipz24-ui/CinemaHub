/* =====================================================================
   CinemaHub_DB - повний скрипт розгортання (структура + дані + представлення
   + процедура + індекси). Можна запускати повторно: таблиці перестворюються.
   Запуск: SSMS -> File -> Open -> цей файл -> Execute (F5)
   ===================================================================== */
IF DB_ID(N'CinemaHub_DB') IS NULL
    CREATE DATABASE CinemaHub_DB;
GO
USE CinemaHub_DB;
GO

-- ---------- 0. Очищення (для повторного запуску) ----------
IF OBJECT_ID(N'dbo.sp_GetSalesMatrixData', N'P') IS NOT NULL DROP PROCEDURE dbo.sp_GetSalesMatrixData;
IF OBJECT_ID(N'dbo.vw_AuditedTickets', N'V') IS NOT NULL DROP VIEW dbo.vw_AuditedTickets;
IF OBJECT_ID(N'dbo.vw_FinancialSummary', N'V') IS NOT NULL DROP VIEW dbo.vw_FinancialSummary;
IF OBJECT_ID(N'dbo.vw_TicketDetails', N'V') IS NOT NULL DROP VIEW dbo.vw_TicketDetails;
IF OBJECT_ID(N'dbo.TicketRefunds', N'U') IS NOT NULL DROP TABLE dbo.TicketRefunds;
IF OBJECT_ID(N'dbo.Tickets', N'U') IS NOT NULL DROP TABLE dbo.Tickets;
IF OBJECT_ID(N'dbo.Sessions', N'U') IS NOT NULL DROP TABLE dbo.Sessions;
IF OBJECT_ID(N'dbo.Movies', N'U') IS NOT NULL DROP TABLE dbo.Movies;
IF OBJECT_ID(N'dbo.Halls', N'U') IS NOT NULL DROP TABLE dbo.Halls;
IF OBJECT_ID(N'dbo.Genres', N'U') IS NOT NULL DROP TABLE dbo.Genres;
GO

-- ---------- 1. Таблиці ----------
CREATE TABLE Genres (
    GenreID     INT IDENTITY(1,1) PRIMARY KEY,
    GenreName   NVARCHAR(50)  NOT NULL,
    Description NVARCHAR(255) NULL
);

CREATE TABLE Halls (
    HallID   INT IDENTITY(1,1) PRIMARY KEY,
    HallName NVARCHAR(50) NOT NULL,
    Capacity INT NOT NULL CHECK (Capacity > 0),
    IsVIP    BIT NOT NULL DEFAULT 0
);

CREATE TABLE Movies (
    MovieID         INT IDENTITY(1,1) PRIMARY KEY,
    Title           NVARCHAR(150) NOT NULL,
    GenreID         INT NOT NULL,
    DurationMinutes INT NOT NULL CHECK (DurationMinutes > 0),
    AgeRating       NVARCHAR(10) NOT NULL DEFAULT '12+',
    BasePrice       DECIMAL(10,2) NOT NULL CHECK (BasePrice >= 0),
    CONSTRAINT FK_Movies_Genres FOREIGN KEY (GenreID) REFERENCES Genres(GenreID) ON DELETE CASCADE
);

CREATE TABLE Sessions (
    SessionID   INT IDENTITY(1,1) PRIMARY KEY,
    MovieID     INT NOT NULL,
    HallID      INT NOT NULL,
    StartTime   DATETIME NOT NULL,
    TicketPrice DECIMAL(10,2) NOT NULL CHECK (TicketPrice >= 0),
    CONSTRAINT FK_Sessions_Movies FOREIGN KEY (MovieID) REFERENCES Movies(MovieID),
    CONSTRAINT FK_Sessions_Halls  FOREIGN KEY (HallID)  REFERENCES Halls(HallID)
);

CREATE TABLE Tickets (
    TicketID      INT IDENTITY(1,1) PRIMARY KEY,
    SessionID     INT NOT NULL,
    CustomerName  NVARCHAR(100) NOT NULL,
    CustomerPhone NVARCHAR(20)  NULL,
    SeatNumber    INT NOT NULL CHECK (SeatNumber > 0),
    PurchaseDate  DATETIME NOT NULL DEFAULT GETDATE(),
    IsPaid        BIT NOT NULL DEFAULT 1,
    CONSTRAINT FK_Tickets_Sessions FOREIGN KEY (SessionID) REFERENCES Sessions(SessionID) ON DELETE CASCADE
);

-- Таблиця повернень (потрібна для підзвіту "Повернення" з Теми 5)
CREATE TABLE TicketRefunds (
    RefundID     INT IDENTITY(1,1) PRIMARY KEY,
    SessionID    INT NOT NULL,
    SeatNumber   INT NOT NULL,
    RefundAmount DECIMAL(10,2) NOT NULL,
    Reason       NVARCHAR(200) NOT NULL,
    RefundDate   DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Refunds_Sessions FOREIGN KEY (SessionID) REFERENCES Sessions(SessionID) ON DELETE CASCADE
);
GO

-- ---------- 2. Тестові дані ----------
INSERT INTO Genres (GenreName, Description) VALUES
(N'Наукова Фантастика', N'Фільми про майбутнє, космос та технології'),
(N'Екшн та Бойовики',   N'Динамічні фільми з бійками та погонями'),
(N'Драма та Біографія', N'Глибокі сюжетні стрічки та реальні історії'),
(N'Анімація та Сімейні',N'Мультфільми для перегляду всією родиною');

INSERT INTO Halls (HallName, Capacity, IsVIP) VALUES
(N'Зал 1 (Red Hall)',   100, 0),
(N'Зал 2 (Blue IMAX)',  150, 0),
(N'Зал 3 (VIP Lounge)',  30, 1);

INSERT INTO Movies (Title, GenreID, DurationMinutes, AgeRating, BasePrice) VALUES
(N'Дюна: Частина друга', 1, 166, N'16+', 200.00),
(N'Інтерстеллар',        1, 169, N'12+', 180.00),
(N'Оппенгеймер',         3, 180, N'18+', 220.00),
(N'Дедпул і Росомаха',   2, 127, N'18+', 210.00),
(N'Думками навиворіт 2', 4,  96, N'0+',  160.00),
(N'Кунг-Фу Панда 4',     4,  94, N'0+',  150.00);

INSERT INTO Sessions (MovieID, HallID, StartTime, TicketPrice) VALUES
(1, 2, '2026-10-01 14:00:00', 200.00),
(1, 2, '2026-10-01 18:00:00', 220.00),
(2, 1, '2026-10-01 15:30:00', 180.00),
(3, 3, '2026-10-01 19:00:00', 350.00),
(4, 2, '2026-10-01 21:30:00', 230.00),
(5, 1, '2026-10-02 12:00:00', 160.00),
(6, 1, '2026-10-02 14:00:00', 150.00),
(1, 3, '2026-10-02 18:30:00', 320.00);

INSERT INTO Tickets (SessionID, CustomerName, CustomerPhone, SeatNumber, PurchaseDate, IsPaid) VALUES
(1, N'Олександр Іваненко', '+380971112233', 12, '2026-09-28 10:15:00', 1),
(1, N'Марія Бондаренко',   '+380502223344', 13, '2026-09-28 10:20:00', 1),
(1, N'Дмитро Шевченко',    '+380633334455', 14, '2026-09-28 11:00:00', 1),
(2, N'Віктор Мороз',       '+380984445566', 45, '2026-09-29 14:30:00', 1),
(2, N'Олена Кравц',        '+380505556677', 46, '2026-09-29 14:35:00', 1),
(3, N'Андрій Ткачук',      '+380676667788', 20, '2026-09-29 16:00:00', 1),
(4, N'Ігор Мельник',       '+380937778899',  5, '2026-09-30 09:12:00', 1),
(4, N'Наталія Савченко',   '+380508889900',  6, '2026-09-30 09:15:00', 1),
(5, N'Сергій Поліщук',     '+380979990011', 88, '2026-09-30 18:40:00', 1),
(6, N'Юлія Лисенко',       '+380630001122', 15, '2026-10-01 08:20:00', 1);

INSERT INTO TicketRefunds (SessionID, SeatNumber, RefundAmount, Reason) VALUES
(1, 20, 200.00, N'Помилковий вибір сеансу'),
(4, 7,  350.00, N'Технічний збій у залі');
GO

-- ---------- 3. Індекси (Тема 3, розділ 3.1) ----------
CREATE NONCLUSTERED INDEX IX_Sessions_Movie_StartTime
    ON Sessions (MovieID, StartTime) INCLUDE (TicketPrice, HallID);
CREATE NONCLUSTERED INDEX IX_Tickets_SessionID_IsPaid
    ON Tickets (SessionID, IsPaid) INCLUDE (CustomerName, SeatNumber);
GO

-- ---------- 4. Представлення для звітів ----------
CREATE VIEW vw_TicketDetails AS
SELECT t.TicketID, t.CustomerName, t.CustomerPhone, t.SeatNumber, t.PurchaseDate, t.IsPaid,
       s.StartTime, s.TicketPrice,
       m.Title AS MovieTitle, m.AgeRating, m.DurationMinutes,
       h.HallName, g.GenreName
FROM Tickets t
INNER JOIN Sessions s ON t.SessionID = s.SessionID
INNER JOIN Movies   m ON s.MovieID   = m.MovieID
INNER JOIN Halls    h ON s.HallID    = h.HallID
INNER JOIN Genres   g ON m.GenreID   = g.GenreID;
GO

CREATE VIEW vw_FinancialSummary AS
SELECT g.GenreName, m.Title,
       COUNT(t.TicketID)  AS TotalTicketsSold,
       SUM(s.TicketPrice) AS TotalRevenue
FROM Genres g
INNER JOIN Movies   m ON g.GenreID = m.GenreID
INNER JOIN Sessions s ON m.MovieID = s.MovieID
INNER JOIN Tickets  t ON s.SessionID = t.SessionID
GROUP BY g.GenreName, m.Title;
GO

-- Маскування телефонів (GDPR, Тема 5 розділ 7)
CREATE VIEW vw_AuditedTickets AS
SELECT TicketID, CustomerName,
       CASE WHEN IS_MEMBER('CinemaManagers') = 1 THEN CustomerPhone
            ELSE 'XXXXXX' + RIGHT(CustomerPhone, 4) END AS SafePhone,
       SeatNumber
FROM Tickets;
GO

-- ---------- 5. Процедура для матричного звіту (Тема 5, 2.1) ----------
CREATE PROCEDURE sp_GetSalesMatrixData
    @StartDate DATETIME,
    @EndDate   DATETIME
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        DATENAME(dw, s.StartTime) AS DayOfWeekName,
        DATEPART(dw, s.StartTime) AS DayOfWeekNumber,
        CASE WHEN DATEPART(hour, s.StartTime) BETWEEN 8  AND 12 THEN '10:00 - 13:00'
             WHEN DATEPART(hour, s.StartTime) BETWEEN 13 AND 16 THEN '13:00 - 17:00'
             WHEN DATEPART(hour, s.StartTime) BETWEEN 17 AND 20 THEN '17:00 - 21:00'
             ELSE '21:00+' END AS TimeSlot,
        CASE WHEN DATEPART(hour, s.StartTime) BETWEEN 8  AND 12 THEN 1
             WHEN DATEPART(hour, s.StartTime) BETWEEN 13 AND 16 THEN 2
             WHEN DATEPART(hour, s.StartTime) BETWEEN 17 AND 20 THEN 3
             ELSE 4 END AS TimeSlotOrder,
        s.TicketPrice
    FROM Tickets t
    INNER JOIN Sessions s ON t.SessionID = s.SessionID
    WHERE t.PurchaseDate >= @StartDate AND t.PurchaseDate <= @EndDate AND t.IsPaid = 1;
END;
GO

-- ---------- 6. Перевірка ----------
SELECT N'Genres' AS [Таблиця], COUNT(*) AS [Рядків] FROM Genres UNION ALL
SELECT N'Halls', COUNT(*) FROM Halls UNION ALL
SELECT N'Movies', COUNT(*) FROM Movies UNION ALL
SELECT N'Sessions', COUNT(*) FROM Sessions UNION ALL
SELECT N'Tickets', COUNT(*) FROM Tickets UNION ALL
SELECT N'TicketRefunds', COUNT(*) FROM TicketRefunds;
GO
