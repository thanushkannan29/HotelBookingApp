/* Hotelservice and i hotelservice admin+public
home page and admin profile page
*/
CREATE PROCEDURE proc_SearchHotels
    @City NVARCHAR(100),
    @Offset INT,
    @PageSize INT,
    @CheckIn DATE,
    @CheckOut DATE
AS
BEGIN
    SELECT *
    FROM Hotels
    WHERE City = @City
      AND IsActive = 1
    ORDER BY Name
    OFFSET @Offset ROWS
    FETCH NEXT @PageSize ROWS ONLY
END



/* Room (Admin)*/

Create PROCEDURE proc_GetRoomsByHotel
    @HotelId UNIQUEIDENTIFIER,
    @Offset INT,
    @PageSize INT
AS
BEGIN
    SELECT r.RoomId,
           r.RoomNumber,
           r.Floor,
           r.IsActive,
           rt.Name AS RoomTypeName
    FROM Rooms r
    INNER JOIN RoomTypes rt ON r.RoomTypeId = rt.RoomTypeId
    WHERE r.HotelId = @HotelId
    ORDER BY r.RoomNumber
    OFFSET @Offset ROWS
    FETCH NEXT @PageSize ROWS ONLY
END

/* Inventory  */
CREATE PROCEDURE proc_GetInventoryByRoomType
    @RoomTypeId UNIQUEIDENTIFIER,
    @StartDate DATE,
    @EndDate DATE
AS
BEGIN
    SELECT *
    FROM RoomTypeInventories
    WHERE RoomTypeId = @RoomTypeId
      AND Date BETWEEN @StartDate AND @EndDate
    ORDER BY Date
END


/*  Get Top 10 hotel search */
CREATE PROCEDURE proc_GetTopHotels
AS
BEGIN
    SELECT TOP 10
        h.HotelId,
        h.Name,
        h.City,
        h.ImageUrl,

        ISNULL(AVG(CAST(r.Rating AS DECIMAL(5,2))),0) AS AverageRating,
        COUNT(r.ReviewId) AS ReviewCount,

        MIN(rtRate.Rate) AS StartingPrice

    FROM Hotels h

    LEFT JOIN Reviews r ON r.HotelId = h.HotelId
    LEFT JOIN RoomTypes rt ON rt.HotelId = h.HotelId
    LEFT JOIN RoomTypeRates rtRate ON rtRate.RoomTypeId = rt.RoomTypeId

    WHERE h.IsActive = 1

    GROUP BY h.HotelId, h.Name, h.City, h.ImageUrl

    ORDER BY 
        ISNULL(AVG(CAST(r.Rating AS DECIMAL(5,2))),0) DESC,
        COUNT(r.ReviewId) DESC
END
