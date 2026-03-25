# 🏨 StayHub Hotel Booking System — Kiro Full-Stack Update Prompt

> **Project**: StayHub Hotel Booking System  
> **Backend**: ASP.NET Core Web API (.NET 10), Entity Framework Core, SQL Server  
> **Frontend**: Angular 21, Angular Material, Bootstrap  
> **Roles**: Guest · Admin (Hotel Admin) · SuperAdmin  

---

## 📋 INSTRUCTIONS FOR KIRO

You have access to the full **Backend** (ASP.NET Core WebAPI) and **Frontend** (Angular 21) source code.  
Read every file before making changes. Apply **all** tasks below in order. After all corrections are complete, refactor services into small, single-responsibility sub-functions (micro-code style) as the final step.

---

## 🗄️ SECTION 1 — DATABASE & MODELS (Backend)

### 1.1 — New Tables / Model Changes

#### a) `City` & `State` Tables (replace static `IndianCities.cs` static data)
Create the following new EF Core models and add them to `HotelBookingContext`:

```csharp
// Models/City.cs
public class City {
    public Guid CityId { get; set; }
    public string CityName { get; set; }
    public string StateName { get; set; }
    public string PinCode { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}
```

Remove `IndianCities.cs` static data. Add `DbSet<City> Cities` to `HotelBookingContext`.  
SuperAdmin manages cities via new CRUD API (see Section 2.5).  
All hotel/booking city fields must now be fetched from this table.

#### b) `Wallet` Table
```csharp
public class Wallet {
    public Guid WalletId { get; set; }
    public Guid UserId { get; set; }
    public decimal Balance { get; set; } = 0;
    public DateTime UpdatedAt { get; set; }
    public User? User { get; set; }
    public ICollection<WalletTransaction>? WalletTransactions { get; set; }
}

public class WalletTransaction {
    public Guid WalletTransactionId { get; set; }
    public Guid WalletId { get; set; }
    public decimal Amount { get; set; }
    public string Type { get; set; } // "Credit" | "Debit"
    public string Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public Wallet? Wallet { get; set; }
}
```

Add `DbSet<Wallet>` and `DbSet<WalletTransaction>` to context.  
Every Guest gets a Wallet created automatically on registration.  
Refund amounts must credit the guest's Wallet (not just log a refund status).  
Guest can use Wallet balance to pay for bookings (partial or full).

#### c) `PromoCode` Table
```csharp
public class PromoCode {
    public Guid PromoCodeId { get; set; }
    public string Code { get; set; }          // unique random code
    public Guid UserId { get; set; }          // only this user can use it
    public Guid HotelId { get; set; }         // only for this hotel
    public Guid ReservationId { get; set; }   // reservation that earned it
    public decimal DiscountPercent { get; set; }
    public DateTime ExpiryDate { get; set; }
    public bool IsUsed { get; set; } = false;
    public DateTime CreatedAt { get; set; }
    public User? User { get; set; }
    public Hotel? Hotel { get; set; }
    public Reservation? Reservation { get; set; }
}
```

Add `DbSet<PromoCode>` to context.

**Promo discount rules (based on TotalAmount of completed reservation):**
- `TotalAmount <= 500` → 5% discount promo
- `TotalAmount <= 1000` → 10% discount promo  
- `TotalAmount <= 2000` → 15% discount promo  
- `TotalAmount <= 5000` → 20% discount promo  
- `TotalAmount > 5000` → 25% discount promo (maximum)

Send promo code automatically when a reservation status becomes `Completed`.

#### d) `AmenityRequest` Table (Admin requests new amenity to SuperAdmin)
```csharp
public class AmenityRequest {
    public Guid AmenityRequestId { get; set; }
    public Guid RequestedByAdminId { get; set; }
    public Guid AdminHotelId { get; set; }
    public string AmenityName { get; set; }
    public string Category { get; set; }
    public string? IconName { get; set; }
    public AmenityRequestStatus Status { get; set; } = AmenityRequestStatus.Pending;
    public string? SuperAdminNote { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public User? RequestedByAdmin { get; set; }
}
public enum AmenityRequestStatus { Pending = 1, Approved = 2, Rejected = 3 }
```

Add `DbSet<AmenityRequest>` to context.

#### e) `SuperAdminRevenue` Table
```csharp
public class SuperAdminRevenue {
    public Guid SuperAdminRevenueId { get; set; }
    public Guid ReservationId { get; set; }
    public Guid HotelId { get; set; }
    public decimal ReservationAmount { get; set; }
    public decimal CommissionAmount { get; set; }  // 2% of ReservationAmount
    public string SuperAdminUpiId { get; set; }    // "thanushstayhubsuperadmin@okaxis"
    public string Status { get; set; }             // "Pending" | "Sent"
    public DateTime CreatedAt { get; set; }
    public Reservation? Reservation { get; set; }
    public Hotel? Hotel { get; set; }
}
```

Add `DbSet<SuperAdminRevenue>` to context.

#### f) Update `RoomType` Model — link Amenities properly
- Replace `public string Amenities { get; set; }` with a many-to-many join:
```csharp
public ICollection<RoomTypeAmenity>? RoomTypeAmenities { get; set; }
```
```csharp
public class RoomTypeAmenity {
    public Guid RoomTypeId { get; set; }
    public Guid AmenityId { get; set; }
    public RoomType? RoomType { get; set; }
    public Amenity? Amenity { get; set; }
}
```

Add `DbSet<RoomTypeAmenity>` to context. Configure composite key in `OnModelCreating`.

#### g) Update `Reservation` Model — add GST & Promo fields
Add the following fields to `Reservation`:
```csharp
public decimal GstPercent { get; set; } = 0;
public decimal GstAmount { get; set; } = 0;
public decimal DiscountPercent { get; set; } = 0;
public decimal DiscountAmount { get; set; } = 0;
public decimal WalletAmountUsed { get; set; } = 0;
public string? PromoCodeUsed { get; set; }
public decimal FinalAmount { get; set; }    // TotalAmount + GstAmount - DiscountAmount - WalletAmountUsed
```

#### h) Update `Hotel` Model — add GST field
```csharp
public decimal GstPercent { get; set; } = 0;   // Hotel Admin sets GST %
```

#### i) Update `Transaction` Model — add WalletTransaction reference
```csharp
public bool WalletUsed { get; set; } = false;
public decimal WalletAmountUsed { get; set; } = 0;
```

### 1.2 — EF Migration
After all model changes, generate and apply EF migration:
```
dotnet ef migrations add FullSystemUpdate
dotnet ef database update
```

---

## 🔧 SECTION 2 — BACKEND API (Controllers + Services + Interfaces)

### 2.1 — Wallet APIs

**Interface**: `IWalletService`  
**Service**: `WalletService`  
**Controller**: `WalletController`

APIs:
- `GET /api/guest/wallet` — Get current guest's wallet balance and transaction history (paged)
- `POST /api/guest/wallet/topup` — Guest adds money to wallet `{ amount: decimal }`
- `GET /api/admin/wallet/guest/{userId}` — Admin views a guest's wallet (for refund tracking)

Logic:
- On reservation `Completed` status: auto-credit refund amount to wallet if a refund was approved
- When booking uses wallet, deduct from wallet and record `WalletTransaction` with type `"Debit"`
- Register `IWalletService` / `WalletService` in `Program.cs`

### 2.2 — PromoCode APIs

**Interface**: `IPromoCodeService`  
**Service**: `PromoCodeService`  
**Controller**: `PromoCodeController`

APIs:
- `GET /api/guest/promo-codes` — List all promo codes for the current guest
- `POST /api/guest/promo-codes/validate` — Validate promo code `{ code, hotelId, totalAmount }` → returns discount percent and discount amount
- Internal method: `GeneratePromoForCompletedReservationAsync(reservationId)` called from background service

Logic:
- Only the owner guest can use their promo code
- Promo is hotel-specific (can only be used at the same hotel where stay was completed)
- On use, mark `IsUsed = true`
- On validation, check expiry date and `IsUsed == false`
- Register in `Program.cs`

### 2.3 — Fix Reservation Service (Booking Logic)

In `ReservationService.CreateReservationAsync`:

1. **Block same-day booking**: If `dto.CheckInDate == DateOnly.FromDateTime(DateTime.UtcNow)`, throw `ValidationException("Same-day booking is not allowed.")`

2. **Apply GST**: Fetch hotel's `GstPercent`, calculate `GstAmount = TotalAmount * GstPercent / 100`

3. **Apply Promo Code**:
   - If `dto.PromoCodeUsed` is provided, call `IPromoCodeService.ValidateAsync`
   - Calculate `DiscountAmount = TotalAmount * DiscountPercent / 100`
   - Mark promo as used after successful booking

4. **Apply Wallet Payment**:
   - If `dto.WalletAmountToUse > 0`, check guest wallet balance, deduct from wallet
   - `FinalAmount = TotalAmount + GstAmount - DiscountAmount - WalletAmountUsed`
   - Ensure `FinalAmount >= 0`

5. **Duplicate room booking check**:
   - Before assigning rooms, check `ReservationRooms` table: for each candidate `RoomId`, verify no active reservation (status `Pending` or `Confirmed`) has that room for overlapping dates
   - If conflict exists, skip that room and try next available room

**Update `CreateReservationDto`**:
```csharp
public string? PromoCodeUsed { get; set; }
public decimal WalletAmountToUse { get; set; } = 0;
```

**Update `ReservationResponseDto`** to include: `GstPercent`, `GstAmount`, `DiscountPercent`, `DiscountAmount`, `WalletAmountUsed`, `FinalAmount`

### 2.4 — Fix Admin Reservation Management API

In `AdminReservationController` (inside `AdminHotelAndRefundControllers.cs`):

- Add `status` filter query param: `GET /api/admin/reservations?status=Pending&page=1&pageSize=10`
- Valid status values: `All`, `Pending`, `Confirmed`, `Cancelled`, `Completed`, `NoShow`
- Fix pagination: return `{ totalCount, reservations }` object
- Add `search` query param: filter by reservation code or guest name
- All filtering must happen at the database layer (IQueryable), not in-memory

Fix `IReservationService` interface and `ReservationService` accordingly:
```csharp
Task<PagedReservationResponseDto> GetAdminReservationsAsync(Guid adminUserId, string? status, string? search, int page, int pageSize);
```

### 2.5 — City/State APIs (SuperAdmin manages)

**Interface**: `ICityService`  
**Service**: `CityService`  
**Controller**: `CityController` (SuperAdmin) + public endpoint

APIs:
- `GET /api/public/cities?search=che` — Public autocomplete: returns matching cities (up to 10) filtered by `CityName` starting with search term
- `GET /api/public/cities/all` — All active cities list
- `POST /api/superadmin/cities` — Add city `{ cityName, stateName, pinCode }`
- `PUT /api/superadmin/cities/{id}` — Update city
- `PATCH /api/superadmin/cities/{id}/status` — Toggle IsActive
- `DELETE /api/superadmin/cities/{id}` — Delete city
- `GET /api/superadmin/cities` — Paged list of all cities (active + inactive) for management

### 2.6 — Amenity Request APIs (Admin ↔ SuperAdmin)

**Interface**: `IAmenityRequestService`  
**Service**: `AmenityRequestService`  
**Controllers**: Add to existing `AdminOperationsControllers.cs` and `SuperAdminControllers.cs`

Admin APIs:
- `POST /api/admin/amenity-requests` — Raise request `{ amenityName, category, iconName }`
- `GET /api/admin/amenity-requests` — Admin sees their own requests with status

SuperAdmin APIs:
- `GET /api/superadmin/amenity-requests?status=Pending&page=1&pageSize=10` — All pending/all requests
- `PATCH /api/superadmin/amenity-requests/{id}/approve` — Approve → insert into `Amenities` table, set status = Approved
- `PATCH /api/superadmin/amenity-requests/{id}/reject` — Reject with note `{ note }`

### 2.7 — Fix RoomType APIs to use Amenity table (not free text)

Update `CreateRoomTypeDto` and `UpdateRoomTypeDto`:
- Replace `string Amenities` with `List<Guid> AmenityIds`

Update `RoomTypeService.AddRoomTypeAsync` / `UpdateRoomTypeAsync`:
- After saving RoomType, insert/update `RoomTypeAmenity` join records
- Remove old string-based amenity logic

Update `RoomTypeListDto` and public `RoomTypePublicDto`:
- Return `List<AmenityDto> Amenities` (with `amenityId`, `name`, `iconName`, `category`) instead of plain string list

### 2.8 — Hotel GST API

Add to `AdminHotelAndRefundControllers.cs`:
- `PATCH /api/admin/hotel/gst` — `{ gstPercent: decimal }` — Set hotel GST percentage (0–28 range validation)

Update `HotelService.UpdateHotelGstAsync(Guid adminUserId, decimal gstPercent)`

### 2.9 — Payment / QR Code API

Add to `SharedControllers.cs` or new `PaymentController`:
- `GET /api/guest/payment/qr/{reservationId}` — Returns `{ upiId, amount, qrCodeBase64 }` where `qrCodeBase64` is a QR code image generated for the UPI string `upi://pay?pa={upiId}&pn={hotelName}&am={finalAmount}&cu=INR`

Use the `QRCoder` NuGet package:
```
dotnet add package QRCoder
```

Implement `GenerateQrCodeBase64(string upiString)` in a helper class `QrCodeHelper.cs`.

### 2.10 — SuperAdmin Revenue Background Service

Create `Services/BackgroundServices/SuperAdminRevenueService.cs`:
- Runs every 10 minutes
- Finds all `Reservations` with `Status == Completed` that do NOT have a `SuperAdminRevenue` record
- For each: calculate `CommissionAmount = TotalAmount * 0.02M`
- Insert a `SuperAdminRevenue` record with `SuperAdminUpiId = "thanushstayhubsuperadmin@okaxis"` and `Status = "Pending"`
- Log action to audit log

Register in `Program.cs`:
```csharp
builder.Services.AddHostedService<SuperAdminRevenueService>();
```

### 2.11 — SuperAdmin Revenue API

Add to `SuperAdminControllers.cs`:
- `GET /api/superadmin/revenue?page=1&pageSize=20` — Paged list of all commission records
- `GET /api/superadmin/revenue/summary` — Total pending, total sent, total commission earned
- `PATCH /api/superadmin/revenue/{id}/mark-sent` — Mark a commission as sent

### 2.12 — Auth: Return URL After Login

Update `AuthenticationController.Login`:
- The login endpoint is unchanged, but note the `returnUrl` is handled on the frontend (see Section 3 auth guard fix)
- Ensure login response includes `{ token, role, hotelId, userId }` all in one object for the frontend to extract

### 2.13 — Hotel Search & Filter API (Public)

Update `PublicHotelController.SearchHotels`:
- Add new query parameters to `SearchHotelRequestDto`:
  ```csharp
  public List<Guid>? AmenityIds { get; set; }
  public decimal? MinPrice { get; set; }
  public decimal? MaxPrice { get; set; }
  public string? RoomType { get; set; }
  public string? SortBy { get; set; }   // "price_asc" | "price_desc" | "rating"
  ```
- All filtering must happen via `IQueryable` (database-level), not in-memory
- Return `totalCount` and `hotels` for paginator

### 2.14 — Fix All Pagination to Return `{ totalCount, data[] }`

Audit all paginated APIs. Every paged API must return:
```json
{ "success": true, "data": { "totalCount": 100, "items": [...] } }
```

Fix: `ReservationService`, `RoomService`, `RoomTypeService`, `InventoryService`, `AuditLogService`, `TransactionService`, `ReviewService`, `HotelService`.

### 2.15 — Fix RoomType Rate GET API

Add to `AdminOperationsControllers.cs`:
- `GET /api/admin/roomtypes/{roomTypeId}/rates` — Get all rates for a room type
- `GET /api/admin/roomtypes/{roomTypeId}/rates/current` — Get current applicable rate (by today's date)

Fix `RoomTypeService.GetRatesAsync` to filter by `roomTypeId` (currently may be returning all rates).

### 2.16 — Audit Log Improvements (SuperAdmin)

In `AuditLogService.GetAllAuditLogsAsync`:
- Add optional filters: `hotelId`, `userId`, `action`, `dateFrom`, `dateTo`
- Return paged result with `{ totalCount, logs }`
- Add to `SuperAdminAuditLogController`:
  ```
  GET /api/superadmin/audit-logs?hotelId=...&action=...&dateFrom=...&dateTo=...&page=1&pageSize=20
  ```

### 2.17 — Micro-Code Refactor (LAST STEP)

After all above corrections, refactor all Service files to use small, single-responsibility private helper methods. Each public method should call multiple private sub-functions. Example pattern:

```csharp
// Instead of one giant CreateReservationAsync:
public async Task<ReservationResponseDto> CreateReservationAsync(Guid userId, CreateReservationDto dto) {
    await ValidateDatesAsync(dto);
    var rooms = await AssignRoomsAsync(dto);
    var pricing = await CalculatePricingAsync(dto, rooms);
    var reservation = await SaveReservationAsync(userId, dto, rooms, pricing);
    await ProcessWalletDeductionAsync(userId, pricing);
    await MarkPromoUsedAsync(dto.PromoCodeUsed);
    return MapToResponseDto(reservation, pricing);
}
```

Apply this pattern to all Services: `ReservationService`, `RoomTypeService`, `HotelService`, `RefundRequestService`, `AuthService`.

---

## 🖥️ SECTION 3 — FRONTEND (Angular 21)

### 3.1 — Auth Guard: Redirect to Previous Page After Login

In `auth.guard.ts`:
- When unauthenticated user tries to access a protected route, save the URL to `localStorage` as `returnUrl`
- After successful login in `login.component.ts`, read `returnUrl` from localStorage, navigate there, then remove it from localStorage
- If no `returnUrl`, navigate to role-based dashboard

```typescript
// auth.guard.ts
const returnUrl = state.url;
localStorage.setItem('returnUrl', returnUrl);
router.navigate(['/auth/login']);

// login.component.ts (after success)
const returnUrl = localStorage.getItem('returnUrl') || getDashboardRoute(role);
localStorage.removeItem('returnUrl');
router.navigateByUrl(returnUrl);
```

### 3.2 — Dark / Light Mode

In `app.component.ts` and `navbar.component.ts`:
- Add a theme toggle button in the navbar with 🌙 / ☀️ icons using `mat-icon-button`
- Store preference in `localStorage` as `theme: 'dark' | 'light'`
- Apply Angular Material dark theme by toggling CSS class `dark-theme` on `<body>`
- In `styles.scss`, add:
  ```scss
  body.dark-theme {
    --mat-app-background-color: #121212;
    --mat-app-text-color: #ffffff;
    background-color: #121212;
    color: #ffffff;
  }
  ```
- Load saved theme on app init in `app.component.ts` `ngOnInit`

### 3.3 — City Autocomplete (Fetched from API)

Update `city-autocomplete.component.ts`:
- Remove static `IndianCities` data
- Use `GET /api/public/cities?search=` API with debounce (300ms) using `rxjs debounceTime` and `distinctUntilChanged`
- Show `mat-autocomplete` with city name + state name displayed
- Emit selected city name string to parent
- Use this component everywhere city is entered: hotel search, hotel registration, hotel edit, user profile

### 3.4 — Emoji / Icons — Make UI Interactive

Add relevant emojis and Angular Material icons throughout:
- 🏨 Hotel name/brand in navbar
- 🔍 Search buttons
- 📅 Date pickers labels  
- 🛏️ Room type labels
- 💰 Price displays
- ✅ Confirmed status
- ❌ Cancelled status
- ⏳ Pending status
- 🏆 Completed status
- 👤 Guest profile
- 🔑 Login
- 📊 Dashboard
- 🧾 Transactions
- 💳 Payment
- 🎫 Promo code
- 💼 Wallet

Use `mat-icon` with `fontIcon` for Material icons and Unicode emoji in headings/labels.

### 3.5 — Hotel Search with Master Filter

In `hotel-list.component.ts` and `hotel-list.component.html`:

After initial search results appear, add a **sidebar/top filter panel** with:
- **Amenities filter**: Multi-select checkboxes from `GET /api/public/amenities` (active amenities)
- **Price range filter**: `mat-slider` with min/max price
- **Room type filter**: Text input or dropdown
- **Sort by**: `mat-select` → Price Low→High, Price High→Low, Rating

On filter change, call search API with filter params (debounced 400ms).  
Use Angular Material `mat-expansion-panel` for collapsible filter sections.  
Add a **Clear Filters** button.

### 3.6 — Booking Create Page Fixes

In `booking-create.component.ts`:

1. **Prevent same-day booking**: Disable or show error if `checkInDate === today`

2. **Prevent booking same room twice**: Before submitting, validate locally that selected room IDs are unique

3. **Room type change bug**: When guest changes room type selection in booking form, reset room selection and re-fetch availability for new room type. Use `valueChanges` on roomType form control:
   ```typescript
   this.form.get('roomTypeId')!.valueChanges.pipe(
     distinctUntilChanged()
   ).subscribe(() => {
     this.form.patchValue({ selectedRoomIds: [] });
     this.loadAvailableRooms();
   });
   ```

4. **Promo Code field**: Add promo code input with "Apply" button. Call validate API. Show discount amount if valid. Show error if invalid/expired/used.

5. **Wallet payment toggle**: Show guest's wallet balance. Add a toggle "Use Wallet Balance" with input for amount. Compute and show FinalAmount = Total + GST - Promo Discount - Wallet Used.

6. **GST display**: Show hotel's GST% and GST amount in price breakdown.

7. **QR Code payment**: After reservation created, if payment method is UPI, fetch `GET /api/guest/payment/qr/{reservationId}` and display the QR code image using `<img [src]="'data:image/png;base64,' + qrCodeBase64">`. Also show UPI ID and amount.

### 3.7 — Booking Detail: PDF Download

In `booking-detail.component.ts`:
- Add **"Download Booking PDF"** button using `jsPDF` or `html2canvas + jsPDF`
- PDF should include: Reservation code, Hotel name, Check-in/out dates, Room details, Amount breakdown (Total + GST - Discount - Wallet), Status, Guest name
- Add **"Download Transaction PDF"** button: Transaction ID, date, amount, payment method, status

Install if not present:
```
npm install jspdf html2canvas
```

### 3.8 — Admin: Room Management Fixes

In `room-management.component.ts`:

1. **Add Room**: Fix `POST /api/admin/rooms` call — ensure `roomTypeId` is a valid UUID from the hotel's room types (load room types in dropdown)

2. **View All Rooms**: Fix pagination call to pass `pageNumber` and `pageSize` params correctly. Handle `{ success, data: { totalCount, items } }` response shape.

3. **Edit Room**: Load room data into form before editing. Fix `PUT /api/admin/rooms` endpoint call.

4. **Toggle IsActive**: Fix `PATCH /api/admin/rooms/{roomId}/status?isActive=true|false` call.

5. **Validations**: 
   - Room number: required, max 20 chars
   - Floor: required, numeric, min 0, max 100
   - Room type: required, must select from list

### 3.9 — Admin: Room Type Management Fixes

In `roomtype-management.component.ts`:

1. **Amenities**: Replace free-text amenities input with multi-select from `GET /api/public/amenities`. Show amenity name + icon. If admin wants a new amenity not in list, show "Request New Amenity" button that opens a dialog to submit `POST /api/admin/amenity-requests`.

2. **Get Room Types**: Fix pagination and response mapping for `GET /api/admin/roomtypes?page=1&pageSize=10`

3. **Room Type Rates**: Fix `GET /api/admin/roomtypes/{id}/rates` and rate update form

4. **GST Setting**: Add GST% field in hotel settings page: `PATCH /api/admin/hotel/gst`

### 3.10 — Admin: Inventory Management Fixes

In `inventory-management.component.ts`:

1. Fix `GET /api/admin/inventory?page=1&pageSize=10` — map response correctly
2. Fix Add inventory: `POST /api/admin/inventory` with `{ roomTypeId, date, totalRooms, availableRooms }`
3. Fix Edit inventory: `PUT /api/admin/inventory/{id}`
4. Add date-range filter for inventory view
5. All form fields with validations (required, numeric ranges)

### 3.11 — Admin: Reservation Management Fixes

In `reservation-management.component.ts`:

1. Fix pagination: `GET /api/admin/reservations?status=All&page=1&pageSize=10&search=`
2. Add status filter tabs: **All | Pending | Confirmed | Cancelled | Completed | No Show** using `mat-tab-group`
3. Add search input with debounce (400ms)
4. Fix `mat-paginator`: bind `totalCount` to `length` input, handle `page` event to reload
5. Confirm reservation: `PATCH /api/admin/reservations/{code}/confirm`
6. Show columns: Reservation Code, Guest Name, Room Type, Check-in, Check-out, Amount, Status, Actions

### 3.12 — Admin: Refund Management Fixes

In `refund-management.component.ts`:

1. On approving refund: call `PATCH /api/admin/refund-requests/{id}/approve`
2. After approval, the refund amount must show as credited to guest's wallet (display wallet credit confirmation)
3. Fix pagination for refund requests list

### 3.13 — Admin: Amenity Request Management

Create new component `amenity-requests.component.ts` under `features/admin/`:
- List the admin's own amenity requests with status (Pending / Approved / Rejected)
- Form to submit new amenity request with name, category, optional icon name
- Show status badge with color: yellow (Pending), green (Approved), red (Rejected)
- Add route `/admin/amenity-requests` in `admin.routes.ts`

### 3.14 — SuperAdmin: City Management

Create `features/superadmin/city-management/`:
- `city-management.component.ts` — CRUD for cities
- Table with columns: City Name, State, Pin Code, Is Active, Actions
- Add/Edit city dialog with form and validations
- Toggle active/inactive, delete
- `mat-paginator` with search
- Add route `/superadmin/cities` in `superadmin.routes.ts`

### 3.15 — SuperAdmin: Amenity Request Approval

Create `features/superadmin/amenity-requests/`:
- `superadmin-amenity-requests.component.ts`
- Table of all amenity requests with filters (status: All/Pending/Approved/Rejected)
- Actions: Approve button, Reject button (opens dialog for rejection note)
- `mat-paginator`
- Add route `/superadmin/amenity-requests`

### 3.16 — SuperAdmin: Revenue / Commission Dashboard

Create `features/superadmin/revenue/`:
- `superadmin-revenue.component.ts`
- Summary cards: Total Commission Earned, Pending, Sent
- Table: Hotel Name, Reservation Code, Reservation Amount, Commission (2%), Status, Date
- "Mark as Sent" action button
- `mat-paginator` with filters
- Add route `/superadmin/revenue`

### 3.17 — SuperAdmin: Audit Log Improvements

In `audit-logs.component.ts` (SuperAdmin):
- Add filter bar: Hotel filter, Action filter (text), Date range (from/to) using `mat-datepicker`
- Pass filters to API: `GET /api/superadmin/audit-logs?hotelId=...&action=...&dateFrom=...&dateTo=...`
- Fix `mat-paginator` binding

### 3.18 — Guest: Wallet Page

Create `features/guest/wallet/`:
- `guest-wallet.component.ts`
- Show wallet balance prominently (large card, green color)
- Transaction history table: Description, Amount (+ credit / - debit), Date
- Top-up form: amount input + "Add Money" button calling `POST /api/guest/wallet/topup`
- `mat-paginator` for history
- Add route `/guest/wallet` in `guest.routes.ts`

### 3.19 — Guest: Promo Codes Page

Create `features/guest/promo-codes/`:
- `guest-promo-codes.component.ts`
- Table of all the guest's promo codes: Code, Hotel, Discount%, Expiry, Status (Active/Used/Expired)
- Copy-to-clipboard button for each code
- Add route `/guest/promo-codes` in `guest.routes.ts`

### 3.20 — Navbar Updates

In `navbar.component.ts` and `navbar.component.html`:
- Add 🏨 emoji before brand name "StayHub"
- Add dark/light mode toggle button (moon/sun icon)
- Guest links: My Bookings, My Reviews, Transactions, Wallet 💰, Promo Codes 🎫, Profile
- Admin links: Dashboard, Rooms, Room Types, Inventory, Reservations, Refunds, Reviews, Transactions, Amenity Requests, Audit Logs
- SuperAdmin links: Dashboard, Hotels, Cities, Amenity Requests, Revenue, Audit Logs
- Highlight active route with `routerLinkActive="active"`

### 3.21 — Angular Material Pagination — Fix All Pages

For **every** component that uses `mat-paginator`:
- Bind `[length]="totalCount"` from API response
- Bind `[pageSize]="pageSize"` 
- Handle `(page)` event to call `loadData(event.pageIndex + 1, event.pageSize)`
- Use backend filtering (do NOT slice frontend arrays)
- Add `mat-form-field` with `matInput` search box above each table (debounced 400ms)
- Components to fix: `room-management`, `roomtype-management`, `inventory-management`, `reservation-management`, `refund-management`, `admin-transactions`, `audit-logs`, `booking-list`, `guest-transactions`, `guest-reviews`, `superadmin hotel-control`

### 3.22 — UI/UX — Angular Material Improvements

Apply consistently across all pages:
- Use `mat-card` with `mat-card-header`, `mat-card-content`, `mat-card-actions` for all content blocks
- Status badges: use `mat-chip` with color bindings based on status
- All tables: use `mat-table` with `matSort` on relevant columns
- All forms: use `mat-form-field` with `matInput`, `mat-error` for validation messages, `mat-hint` for helper text
- Loading states: show `mat-spinner` (centered) while API calls are in progress
- Empty states: show a card with icon + message when list is empty (e.g., "🏨 No hotels found")
- Success/error feedback: use `MatSnackBar` consistently (replace any alert() calls)
- All dialogs: use `MatDialog` with `mat-dialog-content` and `mat-dialog-actions`
- Use `mat-stepper` in booking create flow for multi-step experience (Step 1: Select Room Type, Step 2: Select Dates & Rooms, Step 3: Apply Promo/Wallet, Step 4: Payment)
- Add `mat-tooltip` on icon buttons for accessibility

### 3.23 — Bootstrap Integration

Where Angular Material does not provide grid layout support:
- Use Bootstrap 5 grid (`container`, `row`, `col-md-*`) for layout
- Hotel card grid in search results: `col-sm-12 col-md-6 col-lg-4`
- Dashboard summary cards: `col-sm-6 col-lg-3`
- Do NOT mix Bootstrap components (buttons, forms) with Angular Material — use Bootstrap only for grid/spacing utilities

### 3.24 — All Frontend Validations

For every form in the application, ensure the following validations with visible `mat-error` messages:

**Auth forms**:
- Email: required, valid email format
- Password: required, min 8 chars, must have uppercase + number + special char
- Name: required, min 3 chars, max 150 chars

**Hotel registration form**:
- Hotel name: required, max 200 chars
- Address: required, max 500 chars
- City: required (from city autocomplete)
- Contact: required, 10-digit phone number pattern
- UPI ID: optional, format `xxxxx@bank` regex validation

**Room form**:
- Room number: required, max 20 chars, alphanumeric
- Floor: required, numeric, 0–100
- Room type: required

**Booking form**:
- Check-in: required, cannot be today or in past
- Check-out: required, must be after check-in
- Number of rooms: required, min 1, max 10
- Wallet amount: cannot exceed wallet balance or total payable

**Price/Rate form**:
- Rate: required, min 1, max 99999
- Date range: start must be before end

### 3.25 — All API Services Update in Frontend

Update `core/services/` files to include all new APIs:

**New service file**: `wallet.service.ts`
```typescript
getWallet(): Observable<WalletDto>
topUp(amount: number): Observable<WalletDto>
getTransactions(page, pageSize): Observable<PagedWalletTransactionsDto>
```

**New service file**: `promo.service.ts`
```typescript
getMyPromoCodes(): Observable<PromoCodeDto[]>
validatePromo(code, hotelId, totalAmount): Observable<PromoValidationDto>
```

**New service file**: `city.service.ts`
```typescript
searchCities(search: string): Observable<CityDto[]>
getAllCities(page, pageSize): Observable<PagedCityDto>   // SuperAdmin
addCity(dto): Observable<CityDto>
updateCity(id, dto): Observable<CityDto>
toggleCityStatus(id, isActive): Observable<void>
deleteCity(id): Observable<void>
```

**New service file**: `amenity-request.service.ts`
```typescript
raiseRequest(dto): Observable<AmenityRequestDto>
getMyRequests(): Observable<AmenityRequestDto[]>
getAllRequests(status, page, pageSize): Observable<PagedAmenityRequestDto>
approveRequest(id): Observable<void>
rejectRequest(id, note): Observable<void>
```

**New service file**: `superadmin-revenue.service.ts`
```typescript
getRevenueSummary(): Observable<RevenueSummaryDto>
getRevenueList(page, pageSize): Observable<PagedRevenueDto>
markSent(id): Observable<void>
```

Update `hotel.service.ts`:
- `searchHotels(dto: SearchHotelRequestDto)` — add filter params
- `setHotelGst(gstPercent: number): Observable<void>`

Update `booking.service.ts`:
- `createBooking(dto)` — include `promoCodeUsed`, `walletAmountToUse`
- `getPaymentQr(reservationId)`: Observable<`{ upiId, amount, qrCodeBase64 }`>

Update all models in `core/models/models.ts` for new DTOs:
- `WalletDto`, `WalletTransactionDto`, `PagedWalletTransactionsDto`
- `PromoCodeDto`, `PromoValidationDto`
- `CityDto`, `PagedCityDto`
- `AmenityRequestDto`, `PagedAmenityRequestDto`
- `RevenueSummaryDto`, `PagedRevenueDto`
- Update `CreateReservationDto` with `promoCodeUsed?`, `walletAmountToUse?`
- Update `ReservationDetailsDto` with `gstPercent`, `gstAmount`, `discountPercent`, `discountAmount`, `walletAmountUsed`, `finalAmount`
- Update `HotelDetailsDto` with `gstPercent`

---

## 🔄 SECTION 4 — BACKGROUND SERVICES (Backend)

### 4.1 — Existing Background Services — Verify & Fix

Check these existing services still compile and work correctly after model changes:
- `HotelDeactivationRefundService.cs` — refunds to guest wallet (update to credit wallet)
- `NoShowAutoCancelService.cs` — ensure it sets `Status = NoShow` correctly
- `ReservationCleanupService.cs` — ensure it cancels expired pending reservations

### 4.2 — PromoCode Generation Background Service

Create `Services/BackgroundServices/PromoCodeGenerationService.cs`:
- Runs every 5 minutes
- Finds reservations with `Status == Completed` that do NOT have a `PromoCode` generated
- Calculates discount percent based on `TotalAmount` using the rules from Section 1.1c
- Generates a unique promo code string (format: `STAY-XXXXXX` where X is alphanumeric)
- Sets expiry to 30 days from now
- Inserts `PromoCode` record
- Logs to audit log

Register in `Program.cs`:
```csharp
builder.Services.AddHostedService<PromoCodeGenerationService>();
```

### 4.3 — SuperAdmin Revenue Commission Service

(Already described in Section 2.10 — ensure it is registered and working)

---

## ✅ SECTION 5 — VALIDATION CHECKLIST (Kiro must verify each item)

Before finishing, verify each of the following:

**Backend**:
- [ ] All new models added to `HotelBookingContext` with proper relationships
- [ ] Migration generated and applied
- [ ] All new interfaces defined in `Interfaces/` folder
- [ ] All new services registered in `Program.cs` with `AddScoped`
- [ ] All new background services registered with `AddHostedService`
- [ ] `IUnitOfWork` updated if new repositories needed
- [ ] All controllers use `[Authorize(Roles = "...")]` correctly
- [ ] No in-memory filtering in paged APIs (all filtering via IQueryable)
- [ ] `ReservationService.CreateReservationAsync` blocks same-day booking
- [ ] Duplicate room assignment check in booking
- [ ] Wallet credited on refund approval
- [ ] Promo code validated: user-specific, hotel-specific, expiry, used status
- [ ] GST applied in reservation total calculation
- [ ] QR code generation returns base64 PNG

**Frontend**:
- [ ] Auth guard saves `returnUrl` and navigates back after login
- [ ] Dark/light mode toggle persists in localStorage
- [ ] City autocomplete fetches from API (not static data)
- [ ] All `mat-paginator` bound to API `totalCount` (not local array length)
- [ ] All pagination uses backend filtering
- [ ] Search inputs debounced at 400ms
- [ ] All forms have `mat-error` validation messages
- [ ] No `alert()` calls — replaced with `MatSnackBar`
- [ ] QR code displayed in booking payment step
- [ ] PDF download for booking detail and transaction
- [ ] Booking room type change resets room selection
- [ ] Promo code apply shows discount
- [ ] Wallet balance shown and deductible in booking
- [ ] All new routes added to route files
- [ ] Dark theme class applied to `<body>`
- [ ] Emojis and icons added to navbar and key headings

---

## 📁 SECTION 6 — FILES TO CREATE (New Files Summary)

### Backend — New Files:
- `Models/City.cs`
- `Models/Wallet.cs`
- `Models/WalletTransaction.cs`
- `Models/PromoCode.cs`
- `Models/AmenityRequest.cs`
- `Models/SuperAdminRevenue.cs`
- `Models/RoomTypeAmenity.cs`
- `Models/DTOs/City/CityDtos.cs`
- `Models/DTOs/Wallet/WalletDtos.cs`
- `Models/DTOs/PromoCode/PromoCodeDtos.cs`
- `Models/DTOs/AmenityRequest/AmenityRequestDtos.cs`
- `Models/DTOs/Revenue/RevenueDto.cs`
- `Interfaces/IWalletService.cs`
- `Interfaces/IPromoCodeService.cs`
- `Interfaces/ICityService.cs`
- `Interfaces/IAmenityRequestService.cs`
- `Interfaces/ISuperAdminRevenueService.cs`
- `Services/WalletService.cs`
- `Services/PromoCodeService.cs`
- `Services/CityService.cs`
- `Services/AmenityRequestService.cs`
- `Services/SuperAdminRevenueService.cs`
- `Services/BackgroundServices/SuperAdminRevenueService.cs`
- `Services/BackgroundServices/PromoCodeGenerationService.cs`
- `Helpers/QrCodeHelper.cs`
- `Controllers/Public/PublicCityController.cs`
- `Controllers/Guest/GuestWalletController.cs`
- `Controllers/Guest/GuestPromoCodeController.cs`
- `Controllers/Guest/GuestPaymentQrController.cs`
- New migration file (auto-generated)

### Frontend — New Files:
- `features/guest/wallet/guest-wallet.component.ts|html|scss`
- `features/guest/promo-codes/guest-promo-codes.component.ts|html|scss`
- `features/admin/amenity-requests/amenity-requests.component.ts|html|scss`
- `features/superadmin/city-management/city-management.component.ts|html|scss`
- `features/superadmin/amenity-requests/superadmin-amenity-requests.component.ts|html|scss`
- `features/superadmin/revenue/superadmin-revenue.component.ts|html|scss`
- `core/services/wallet.service.ts`
- `core/services/promo.service.ts`
- `core/services/city.service.ts`
- `core/services/amenity-request.service.ts`
- `core/services/superadmin-revenue.service.ts`

---

## 🎨 SECTION 7 — UI THEME & STYLE GUIDE

Apply across entire frontend:

**Color Palette** (Angular Material custom theme in `styles.scss`):
- Primary: `#1565C0` (deep blue)
- Accent: `#FF8F00` (amber/gold)
- Warn: `#C62828` (deep red)
- Success: `#2E7D32` (dark green)

**Typography**: Use Angular Material typography with Roboto font

**Card elevation**: `mat-elevation-z4` for content cards, `mat-elevation-z8` for modals/dialogs

**Status chip colors**:
- Pending: `yellow` / `warn` palette
- Confirmed: `green` / `primary`
- Cancelled: `red` / `warn`
- Completed: `blue` / `primary`
- NoShow: `grey`

**Spacing**: Use Angular Material spacing utilities and Bootstrap `gap-*` classes

**Responsive breakpoints**: All tables must scroll horizontally on mobile (`overflow-x: auto`)

---

*End of Kiro Prompt — Apply all sections in order. Complete Section 1 (DB/Models) and run migration before touching Services or Frontend.*
