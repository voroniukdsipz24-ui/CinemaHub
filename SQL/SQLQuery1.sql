SELECT        Movies.Title, Movies.AgeRating, Genres.GenreName, Halls.HallName, Sessions.StartTime, Sessions.TicketPrice, Halls.Capacity
FROM            Genres INNER JOIN
                         Movies ON Genres.GenreID = Movies.GenreID INNER JOIN
                         Sessions ON Movies.MovieID = Sessions.MovieID INNER JOIN
                         Halls ON Sessions.HallID = Halls.HallID
WHERE        (Halls.IsVIP = 0)
ORDER BY Sessions.StartTime