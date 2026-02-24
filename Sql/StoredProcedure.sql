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

