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
Create PROCEDURE proc_GetTopHotels
AS
BEGIN
    SELECT TOP 10
        h.HotelId,
        h.Name,
        h.City,
        h.ImageUrl,

        --  Review Aggregation
        ISNULL(r.AverageRating, 0) AS AverageRating,
        ISNULL(r.ReviewCount, 0) AS ReviewCount,

        --  Price Aggregation
        ISNULL(p.StartingPrice, 0) AS StartingPrice

    FROM Hotels h

    --  Reviews aggregated separately
    LEFT JOIN (
        SELECT 
            HotelId,
            AVG(CAST(Rating AS DECIMAL(5,2))) AS AverageRating,
            COUNT(*) AS ReviewCount
        FROM Reviews
        GROUP BY HotelId
    ) r ON r.HotelId = h.HotelId

    --  Price aggregated separately
    LEFT JOIN (
        SELECT 
            rt.HotelId,
            MIN(rtRate.Rate) AS StartingPrice
        FROM RoomTypes rt
        INNER JOIN RoomTypeRates rtRate 
            ON rtRate.RoomTypeId = rt.RoomTypeId
        GROUP BY rt.HotelId
    ) p ON p.HotelId = h.HotelId

    WHERE h.IsActive = 1

    ORDER BY 
        ISNULL(r.AverageRating, 0) DESC,
        ISNULL(r.ReviewCount, 0) DESC
END
