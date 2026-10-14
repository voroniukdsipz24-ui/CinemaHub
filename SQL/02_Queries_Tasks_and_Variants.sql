USE CinemaHub_DB;
GO
/* Усі запити практикуму. Виділяйте потрібний блок і натискайте Execute (F5). */

-- ===== ЗАВДАННЯ 1. Розклад (Multi-JOIN, тільки звичайні зали) =====
SELECT m.Title AS [Назва Фільму], m.AgeRating AS [Віковий Рейтинг], g.GenreName AS [Жанр],
       h.HallName AS [Кінозал], h.Capacity AS [Місткість], s.StartTime AS [Час Початку], s.TicketPrice AS [Ціна]
FROM Genres g
INNER JOIN Movies   m ON g.GenreID = m.GenreID
INNER JOIN Sessions s ON m.MovieID = s.MovieID
INNER JOIN Halls    h ON s.HallID  = h.HallID
WHERE h.IsVIP = 0
ORDER BY s.StartTime;

-- ===== ЗАВДАННЯ 2. Каса: GROUP BY + HAVING (> 300 грн) =====
SELECT m.Title AS [Фільм], COUNT(t.TicketID) AS [Продано Квитків], SUM(s.TicketPrice) AS [Загальна Виручка]
FROM Movies m
INNER JOIN Sessions s ON m.MovieID = s.MovieID
INNER JOIN Tickets  t ON s.SessionID = t.SessionID
GROUP BY m.Title
HAVING SUM(s.TicketPrice) > 300
ORDER BY [Загальна Виручка] DESC;

-- ===== ЗАВДАННЯ 3. Заповненість залів (LEFT JOIN + обчислюване поле) =====
SELECT s.SessionID, s.StartTime AS [Дата та Час], h.HallName AS [Зал], h.Capacity AS [Всього місць],
       COUNT(t.TicketID) AS [Куплено квитків],
       ROUND((CAST(COUNT(t.TicketID) AS FLOAT) / h.Capacity) * 100, 2) AS [Заповненість %]
FROM Halls h
INNER JOIN Sessions s ON h.HallID = s.HallID
LEFT OUTER JOIN Tickets t ON s.SessionID = t.SessionID
GROUP BY s.SessionID, s.StartTime, h.HallName, h.Capacity;

-- ===== ЗАВДАННЯ 4. Параметризований пошук (те, що робить TableAdapter) =====
DECLARE @CustomerName NVARCHAR(100) = N'Олександр',
        @StartDate DATETIME = '2026-09-01', @EndDate DATETIME = '2026-09-30';
SELECT TicketID, SessionID, CustomerName, CustomerPhone, SeatNumber, PurchaseDate, IsPaid
FROM Tickets
WHERE CustomerName LIKE '%' + @CustomerName + '%'
  AND PurchaseDate >= @StartDate AND PurchaseDate <= @EndDate;

-- ===== ЗАВДАННЯ 5. +15% для вечірніх сеансів (УВАГА: змінює дані! повторний запуск множить ціну знову) =====
-- UPDATE Sessions SET TicketPrice = TicketPrice * 1.15 WHERE StartTime >= '2026-10-01 18:00:00';

-- ===================== САМОСТІЙНІ ВАРІАНТИ (10) =====================
-- Варіант 1: VIP-зали - квитки та виручка
SELECT h.HallName, COUNT(t.TicketID) AS Tickets, SUM(s.TicketPrice) AS Revenue
FROM Halls h INNER JOIN Sessions s ON h.HallID = s.HallID INNER JOIN Tickets t ON s.SessionID = t.SessionID
WHERE h.IsVIP = 1 GROUP BY h.HallName;

-- Варіант 2: клієнти, що купили більше 2 квитків (на тестових даних результат порожній)
SELECT CustomerName, COUNT(TicketID) AS Cnt FROM Tickets GROUP BY CustomerName HAVING COUNT(TicketID) > 2;

-- Варіант 3: ТОП-3 фільми за кількістю квитків
SELECT TOP 3 m.Title, COUNT(t.TicketID) AS Tickets
FROM Movies m INNER JOIN Sessions s ON m.MovieID = s.MovieID INNER JOIN Tickets t ON s.SessionID = t.SessionID
GROUP BY m.Title ORDER BY COUNT(t.TicketID) DESC;

-- Варіант 4: видалення неоплачених квитків (DML!)
-- DELETE FROM Tickets WHERE IsPaid = 0;

-- Варіант 5: середня ціна та кількість сеансів за віковим рейтингом
SELECT m.AgeRating, AVG(s.TicketPrice) AS AvgPrice, COUNT(s.SessionID) AS SessionsCount
FROM Movies m INNER JOIN Sessions s ON m.MovieID = s.MovieID GROUP BY m.AgeRating;

-- Варіант 6: сеанси без жодного проданого квитка
SELECT s.SessionID, s.StartTime, s.TicketPrice
FROM Sessions s LEFT JOIN Tickets t ON s.SessionID = t.SessionID WHERE t.TicketID IS NULL;

-- Варіант 7: параметризований пошук сеансів за жанром і ціною
DECLARE @GenreID INT = 1, @MaxPrice DECIMAL(10,2) = 250;
SELECT s.SessionID, m.Title, s.StartTime, s.TicketPrice
FROM Sessions s INNER JOIN Movies m ON s.MovieID = m.MovieID
WHERE m.GenreID = @GenreID AND s.TicketPrice <= @MaxPrice;

-- Варіант 8: знижка 20% на жанр 'Анімація та Сімейні' (DML!)
-- UPDATE s SET s.TicketPrice = s.TicketPrice * 0.8
-- FROM Sessions s INNER JOIN Movies m ON s.MovieID = m.MovieID INNER JOIN Genres g ON m.GenreID = g.GenreID
-- WHERE g.GenreName = N'Анімація та Сімейні';

-- Варіант 9: продажі за годинами доби
SELECT DATEPART(hour, s.StartTime) AS [Година], COUNT(t.TicketID) AS [Квитків], SUM(s.TicketPrice) AS [Виручка]
FROM Sessions s INNER JOIN Tickets t ON s.SessionID = t.SessionID
GROUP BY DATEPART(hour, s.StartTime) ORDER BY [Година];

-- Варіант 10: прибутковість залів (від max до min)
SELECT h.HallName, SUM(s.TicketPrice) AS Revenue
FROM Halls h INNER JOIN Sessions s ON h.HallID = s.HallID INNER JOIN Tickets t ON s.SessionID = t.SessionID
GROUP BY h.HallName ORDER BY SUM(s.TicketPrice) DESC;
