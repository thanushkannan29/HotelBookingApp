select * from Users 
select * from Hotels
select * from UserProfileDetails
select * from Rooms
select * from ReservationRooms--booking complte means guest was checked in the hotel need to update in table
order by RoomId
select * from RoomTypeRates
select * from RoomTypes
select * from RoomTypeInventories
order by ReservedInventory desc
select * from Reservations
select * from Transactions 
select * from Wallets
select * from WalletTransactions
select * from Reviews
select * from Logs -- my log controller need to remove
select * from AuditLogs
select * from RefundRequests 
select * from Amenities
--changing rejected to pending so check approve is doing refund
SELECT * 
FROM RefundRequests 
WHERE RefundRequestId = 'C79096F3-75E7-461D-B974-5C79A09CE7B2';
UPDATE RefundRequests
SET Status = 1
WHERE RefundRequestId = 'C79096F3-75E7-461D-B974-5C79A09CE7B2';

--my superadmin mail id
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
