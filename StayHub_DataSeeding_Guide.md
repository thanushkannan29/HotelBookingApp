# 🏨 StayHub — Complete Data Seeding Guide

Use this file to seed all data via the frontend UI or Swagger (`/swagger`).
Base URL: `https://localhost:7xxx/api` (check your launchSettings.json for port)

---

## STEP 1 — SuperAdmin Login

SuperAdmin is seeded automatically if you have a seed in your DB, otherwise register via Swagger:

```
POST /api/auth/register-superadmin
{
  "name": "Thanush SuperAdmin",
  "email": "superadmin@stayhub.com",
  "password": "SuperAdmin@123"
}
```

**Login:**
```
POST /api/auth/login
{
  "email": "superadmin@stayhub.com",
  "password": "SuperAdmin@123"
}
```
Save the token — use as `Bearer <token>` for all SuperAdmin calls.

---

## STEP 2 — SuperAdmin: Add 20 Cities

```
POST /api/superadmin/cities
Authorization: Bearer <superadmin_token>
```

Run each one:

```json
{ "cityName": "Mumbai", "stateName": "Maharashtra", "pinCode": "400001" }
{ "cityName": "Delhi", "stateName": "Delhi", "pinCode": "110001" }
{ "cityName": "Bangalore", "stateName": "Karnataka", "pinCode": "560001" }
{ "cityName": "Chennai", "stateName": "Tamil Nadu", "pinCode": "600001" }
{ "cityName": "Hyderabad", "stateName": "Telangana", "pinCode": "500001" }
{ "cityName": "Kolkata", "stateName": "West Bengal", "pinCode": "700001" }
{ "cityName": "Pune", "stateName": "Maharashtra", "pinCode": "411001" }
{ "cityName": "Ahmedabad", "stateName": "Gujarat", "pinCode": "380001" }
{ "cityName": "Jaipur", "stateName": "Rajasthan", "pinCode": "302001" }
{ "cityName": "Surat", "stateName": "Gujarat", "pinCode": "395001" }
{ "cityName": "Lucknow", "stateName": "Uttar Pradesh", "pinCode": "226001" }
{ "cityName": "Kochi", "stateName": "Kerala", "pinCode": "682001" }
{ "cityName": "Goa", "stateName": "Goa", "pinCode": "403001" }
{ "cityName": "Agra", "stateName": "Uttar Pradesh", "pinCode": "282001" }
{ "cityName": "Varanasi", "stateName": "Uttar Pradesh", "pinCode": "221001" }
{ "cityName": "Mysore", "stateName": "Karnataka", "pinCode": "570001" }
{ "cityName": "Udaipur", "stateName": "Rajasthan", "pinCode": "313001" }
{ "cityName": "Shimla", "stateName": "Himachal Pradesh", "pinCode": "171001" }
{ "cityName": "Manali", "stateName": "Himachal Pradesh", "pinCode": "175131" }
{ "cityName": "Ooty", "stateName": "Tamil Nadu", "pinCode": "643001" }
```

---

## STEP 3 — Register 5 Hotel Admins (Full Hotel Setup)

```
POST /api/auth/register-hotel-admin
```

### Hotel 1 — Mumbai Luxury
```json
{
  "name": "Raj Sharma",
  "email": "admin.mumbai@stayhub.com",
  "password": "Admin@123",
  "hotelName": "The Grand Mumbai Palace",
  "address": "Marine Drive, Nariman Point",
  "city": "Mumbai",
  "description": "A 5-star luxury hotel overlooking the Arabian Sea with world-class amenities and stunning views of Marine Drive.",
  "contactNumber": "9876543210"
}
```

### Hotel 2 — Delhi Heritage
```json
{
  "name": "Priya Kapoor",
  "email": "admin.delhi@stayhub.com",
  "password": "Admin@123",
  "hotelName": "Imperial Delhi Heritage",
  "address": "Connaught Place, New Delhi",
  "city": "Delhi",
  "description": "A heritage property in the heart of Delhi blending colonial architecture with modern luxury.",
  "contactNumber": "9876543211"
}
```

### Hotel 3 — Bangalore Tech Stay
```json
{
  "name": "Arjun Nair",
  "email": "admin.bangalore@stayhub.com",
  "password": "Admin@123",
  "hotelName": "Silicon Valley Suites Bangalore",
  "address": "MG Road, Indiranagar",
  "city": "Bangalore",
  "description": "Modern business hotel in the IT hub of India, perfect for corporate travelers and tech professionals.",
  "contactNumber": "9876543212"
}
```

### Hotel 4 — Goa Beach Resort
```json
{
  "name": "Sunita Fernandes",
  "email": "admin.goa@stayhub.com",
  "password": "Admin@123",
  "hotelName": "Sunset Beach Resort Goa",
  "address": "Calangute Beach Road, North Goa",
  "city": "Goa",
  "description": "A beachfront resort with private beach access, water sports, and spectacular sunset views.",
  "contactNumber": "9876543213"
}
```

### Hotel 5 — Jaipur Royal
```json
{
  "name": "Vikram Singh",
  "email": "admin.jaipur@stayhub.com",
  "password": "Admin@123",
  "hotelName": "Rajputana Royal Jaipur",
  "address": "Civil Lines, Near Hawa Mahal",
  "city": "Jaipur",
  "description": "A royal heritage hotel inspired by Rajput architecture, offering authentic Rajasthani hospitality.",
  "contactNumber": "9876543214"
}
```

---

## STEP 4 — For Each Hotel Admin: Complete Setup

Login as each hotel admin and run these steps.

### 4A — Update Hotel (add UPI ID, image, GST)

```
PUT /api/admin/hotels
Authorization: Bearer <admin_token>
```

**Mumbai:**
```json
{
  "name": "The Grand Mumbai Palace",
  "address": "Marine Drive, Nariman Point, Mumbai",
  "city": "Mumbai",
  "description": "A 5-star luxury hotel overlooking the Arabian Sea.",
  "contactNumber": "9876543210",
  "imageUrl": "https://images.unsplash.com/photo-1566073771259-6a8506099945?w=800",
  "upiId": "grandmumbai@okaxis"
}
```

**Set GST:**
```
PATCH /api/admin/hotels/gst
{ "gstPercent": 18 }
```

---

### 4B — Add Room Types

```
POST /api/admin/roomtypes
Authorization: Bearer <admin_token>
```

**Standard Room:**
```json
{
  "name": "Standard Room",
  "description": "Comfortable room with all basic amenities, ideal for solo travelers.",
  "maxOccupancy": 2,
  "amenityIds": [
    "10000000-0000-0000-0000-000000000001",
    "10000000-0000-0000-0000-000000000002",
    "10000000-0000-0000-0000-000000000003",
    "10000000-0000-0000-0000-000000000013"
  ],
  "imageUrl": "https://images.unsplash.com/photo-1631049307264-da0ec9d70304?w=800"
}
```

**Deluxe Room:**
```json
{
  "name": "Deluxe Room",
  "description": "Spacious deluxe room with city view and premium furnishings.",
  "maxOccupancy": 2,
  "amenityIds": [
    "10000000-0000-0000-0000-000000000001",
    "10000000-0000-0000-0000-000000000002",
    "10000000-0000-0000-0000-000000000003",
    "10000000-0000-0000-0000-000000000009",
    "10000000-0000-0000-0000-000000000013",
    "10000000-0000-0000-0000-000000000014"
  ],
  "imageUrl": "https://images.unsplash.com/photo-1618773928121-c32242e63f39?w=800"
}
```

**Suite:**
```json
{
  "name": "Executive Suite",
  "description": "Luxurious suite with separate living area, premium minibar, and panoramic views.",
  "maxOccupancy": 4,
  "amenityIds": [
    "10000000-0000-0000-0000-000000000001",
    "10000000-0000-0000-0000-000000000002",
    "10000000-0000-0000-0000-000000000003",
    "10000000-0000-0000-0000-000000000009",
    "10000000-0000-0000-0000-000000000011",
    "10000000-0000-0000-0000-000000000013",
    "10000000-0000-0000-0000-000000000014",
    "10000000-0000-0000-0000-000000000015"
  ],
  "imageUrl": "https://images.unsplash.com/photo-1582719478250-c89cae4dc85b?w=800"
}
```

---

### 4C — Add Rates for Each Room Type

First get your room type IDs from:
```
GET /api/admin/roomtypes
```

Then add rates:
```
POST /api/admin/roomtypes/rate
Authorization: Bearer <admin_token>
```

**Standard Room rate:**
```json
{
  "roomTypeId": "<standard_room_type_id>",
  "startDate": "2026-01-01",
  "endDate": "2026-12-31",
  "rate": 2500
}
```

**Deluxe Room rate:**
```json
{
  "roomTypeId": "<deluxe_room_type_id>",
  "startDate": "2026-01-01",
  "endDate": "2026-12-31",
  "rate": 4500
}
```

**Suite rate:**
```json
{
  "roomTypeId": "<suite_room_type_id>",
  "startDate": "2026-01-01",
  "endDate": "2026-12-31",
  "rate": 9500
}
```

---

### 4D — Add Rooms

```
POST /api/admin/rooms
Authorization: Bearer <admin_token>
```

Add 5 Standard rooms (101–105):
```json
{ "roomNumber": "101", "floor": 1, "roomTypeId": "<standard_room_type_id>" }
{ "roomNumber": "102", "floor": 1, "roomTypeId": "<standard_room_type_id>" }
{ "roomNumber": "103", "floor": 1, "roomTypeId": "<standard_room_type_id>" }
{ "roomNumber": "104", "floor": 1, "roomTypeId": "<standard_room_type_id>" }
{ "roomNumber": "105", "floor": 1, "roomTypeId": "<standard_room_type_id>" }
```

Add 4 Deluxe rooms (201–204):
```json
{ "roomNumber": "201", "floor": 2, "roomTypeId": "<deluxe_room_type_id>" }
{ "roomNumber": "202", "floor": 2, "roomTypeId": "<deluxe_room_type_id>" }
{ "roomNumber": "203", "floor": 2, "roomTypeId": "<deluxe_room_type_id>" }
{ "roomNumber": "204", "floor": 2, "roomTypeId": "<deluxe_room_type_id>" }
```

Add 2 Suites (301–302):
```json
{ "roomNumber": "301", "floor": 3, "roomTypeId": "<suite_room_type_id>" }
{ "roomNumber": "302", "floor": 3, "roomTypeId": "<suite_room_type_id>" }
```

---

### 4E — Set Inventory

```
POST /api/admin/inventory
Authorization: Bearer <admin_token>
```

For each room type, set inventory for the full year:
```json
{
  "roomTypeId": "<standard_room_type_id>",
  "startDate": "2026-01-01",
  "endDate": "2026-12-31",
  "totalInventory": 5
}
```
```json
{
  "roomTypeId": "<deluxe_room_type_id>",
  "startDate": "2026-01-01",
  "endDate": "2026-12-31",
  "totalInventory": 4
}
```
```json
{
  "roomTypeId": "<suite_room_type_id>",
  "startDate": "2026-01-01",
  "endDate": "2026-12-31",
  "totalInventory": 2
}
```

---

## STEP 5 — Register 2 Guest Users

```
POST /api/auth/register
```

### Guest 1
```json
{
  "name": "Aditya Kumar",
  "email": "aditya@guest.com",
  "password": "Guest@123"
}
```

### Guest 2
```json
{
  "name": "Meera Patel",
  "email": "meera@guest.com",
  "password": "Guest@123"
}
```

Login as Guest 1:
```
POST /api/auth/login
{ "email": "aditya@guest.com", "password": "Guest@123" }
```

---

## STEP 6 — Guest: Top Up Wallet

```
POST /api/guest/wallet/topup
Authorization: Bearer <guest_token>
{ "amount": 5000 }
```

---

## STEP 7 — Guest: Create Bookings

First search for hotels:
```
POST /api/public/hotels/search
{
  "city": "Mumbai",
  "checkIn": "2026-04-10",
  "checkOut": "2026-04-13",
  "pageNumber": 1,
  "pageSize": 10
}
```

Get hotel details to find room type IDs:
```
GET /api/public/hotels/<hotelId>/full-details
```

Check availability:
```
GET /api/public/hotels/<hotelId>/availability?checkIn=2026-04-10&checkOut=2026-04-13
```

Create reservation:
```
POST /api/guest/reservations
Authorization: Bearer <guest_token>
{
  "hotelId": "<hotel_id>",
  "roomTypeId": "<deluxe_room_type_id>",
  "checkInDate": "2026-04-10",
  "checkOutDate": "2026-04-13",
  "numberOfRooms": 1,
  "walletAmountToUse": 0
}
```

Make payment:
```
POST /api/transactions
Authorization: Bearer <guest_token>
{
  "reservationId": "<reservation_id>",
  "paymentMethod": 3
}
```

---

## STEP 8 — Admin: Confirm & Complete Reservation

Login as hotel admin, then:

```
PATCH /api/admin/reservations/<code>/confirm
Authorization: Bearer <admin_token>
```

```
PATCH /api/admin/reservations/<code>/complete
Authorization: Bearer <admin_token>
```

This auto-generates a promo code for the guest.

---

## STEP 9 — Guest: Check Promo Codes & Wallet

```
GET /api/guest/promo-codes
Authorization: Bearer <guest_token>
```

```
GET /api/guest/wallet
Authorization: Bearer <guest_token>
```

---

## STEP 10 — Guest: Write a Review

```
POST /api/reviews
Authorization: Bearer <guest_token>
{
  "hotelId": "<hotel_id>",
  "reservationId": "<reservation_id>",
  "rating": 4.5,
  "comment": "Excellent stay! The rooms were clean and the staff was very helpful."
}
```

---

## STEP 11 — SuperAdmin: Check Revenue

```
GET /api/superadmin/revenue/summary
Authorization: Bearer <superadmin_token>
```

```
GET /api/superadmin/revenue?page=1&pageSize=20
Authorization: Bearer <superadmin_token>
```

---

## AMENITY IDs Reference (seeded automatically)

| ID | Name | Category |
|----|------|----------|
| `10000000-0000-0000-0000-000000000001` | WiFi | Tech |
| `10000000-0000-0000-0000-000000000002` | AC | Room |
| `10000000-0000-0000-0000-000000000003` | TV | Room |
| `10000000-0000-0000-0000-000000000004` | Pool | Services |
| `10000000-0000-0000-0000-000000000005` | Parking | Services |
| `10000000-0000-0000-0000-000000000006` | Gym | Services |
| `10000000-0000-0000-0000-000000000007` | Restaurant | Food |
| `10000000-0000-0000-0000-000000000008` | Bar | Food |
| `10000000-0000-0000-0000-000000000009` | Room Service | Services |
| `10000000-0000-0000-0000-000000000010` | Laundry | Services |
| `10000000-0000-0000-0000-000000000011` | Spa | Services |
| `10000000-0000-0000-0000-000000000012` | Breakfast Included | Food |
| `10000000-0000-0000-0000-000000000013` | Safe | Room |
| `10000000-0000-0000-0000-000000000014` | Mini Bar | Room |
| `10000000-0000-0000-0000-000000000015` | Balcony | Room |
| `10000000-0000-0000-0000-000000000016` | Sea View | Room |
| `10000000-0000-0000-0000-000000000017` | Mountain View | Room |
| `10000000-0000-0000-0000-000000000018` | Wheelchair Access | Services |
| `10000000-0000-0000-0000-000000000019` | Pet Friendly | Services |
| `10000000-0000-0000-0000-000000000020` | Kids Area | Services |
| `10000000-0000-0000-0000-000000000021` | Conference Room | Services |
| `10000000-0000-0000-0000-000000000022` | Airport Shuttle | Services |
| `10000000-0000-0000-0000-000000000023` | CCTV | Services |
| `10000000-0000-0000-0000-000000000024` | 24h Reception | Services |
| `10000000-0000-0000-0000-000000000025` | Heating | Room |
| `10000000-0000-0000-0000-000000000026` | Elevator | Services |
| `10000000-0000-0000-0000-000000000027` | Hair Dryer | Bathroom |
| `10000000-0000-0000-0000-000000000028` | Iron | Room |
| `10000000-0000-0000-0000-000000000029` | Coffee Maker | Room |
| `10000000-0000-0000-0000-000000000030` | Bathtub | Bathroom |

---

## Quick Credentials Summary

| Role | Email | Password |
|------|-------|----------|
| SuperAdmin | superadmin@stayhub.com | SuperAdmin@123 |
| Admin (Mumbai) | admin.mumbai@stayhub.com | Admin@123 |
| Admin (Delhi) | admin.delhi@stayhub.com | Admin@123 |
| Admin (Bangalore) | admin.bangalore@stayhub.com | Admin@123 |
| Admin (Goa) | admin.goa@stayhub.com | Admin@123 |
| Admin (Jaipur) | admin.jaipur@stayhub.com | Admin@123 |
| Guest 1 | aditya@guest.com | Guest@123 |
| Guest 2 | meera@guest.com | Guest@123 |
