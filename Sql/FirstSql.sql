select * from Users
select * from Hotels
select * from UserProfileDetails
select * from Rooms
select * from ReservationRooms
order by RoomId
select * from RoomTypeRates
select * from RoomTypes
select * from RoomTypeInventories -- check toommorw moring cancel or remove this thanush [HttpPatch("adjust")]//this is for reception work offine reserve the room for customer with cash
order by ReservedInventory desc
select * from Reservations
select * from Transactions
select * from Reviews
select * from Logs
--my superadmin mail id
--thanuhs need to check more service only inventory check for now and remove inventory -ajust tommorow from api
--need to ask mam tommorow about golbal exception have working in response showing but app crash is need to add try catch ask pk and chatgbt
select * from Users where Email='Thanush@test.com'
--after add migration run this below command
UPDATE Users
SET Role = 3
WHERE Email = 'Thanush@test.com';

SELECT UserId, Name, Email, Role
FROM Users
WHERE Email = 'Thanush@test.com';


-- to select roomtypeinventoryId for that roomtypei
select * from RoomTypeInventories where RoomTypeId='4E4BCE29-70A0-403B-90E4-6D105F730FC2'
--To check count of users,hotels and userprofiledetails
SELECT COUNT(*) FROM Users 
SELECT COUNT(*) FROM Hotels 
SELECT COUNT(*) FROM UserProfileDetails 

--Get HotelIds
SELECT HotelId, Name FROM Hotels;

--get roomTypeIds
SELECT RoomTypeId, Name FROM RoomTypes;


SELECT DISTINCT hotelId, RoomTypeId, Name
FROM RoomTypes;
SELECT
    hotelId,
    RoomTypeId,Name
FROM RoomTypes
ORDER BY hotelId, Name ;
