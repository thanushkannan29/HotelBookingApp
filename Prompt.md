# Hotel Booking System — Kiro Change Specification

> **Stack**: .NET 8 Web API (C#) + Angular 17 (standalone components, Angular Material)  
> **DB**: MS SQL Server via EF Core  
> **Auth**: JWT with roles: `Guest`, `Admin`, `SuperAdmin`  
> **Analysis date**: Based on full backend + frontend source review

---

## CHANGE 1 — SuperAdmin Amenity Management (CRUD + Pagination)

### Problem
The backend has a full `SuperAdminAmenityController` at `POST /api/superadmin/amenities` (Create) and `PUT /api/superadmin/amenities` (Update). The `IAmenityService` also has `CreateAmenityAsync` and `UpdateAmenityAsync`. However:
- There is **no GET (list) endpoint** for superadmin to view all amenities (active + inactive) with pagination
- There is **no DELETE or toggle-status endpoint** for amenities
- The frontend superadmin has **no Amenity Management page** at all — only `amenity-requests` exists
- The `PublicAmenityController` only returns active amenities; superadmin needs to see all including inactive

### Backend Changes

**File: `Interfaces/IAmenityService.cs`**
Add these method signatures:
```csharp
Task<PagedAmenityResponseDto> GetAllAmenitiesPagedAsync(int page, int pageSize, string? search, string? category);
Task<bool> ToggleAmenityStatusAsync(Guid amenityId);
Task<bool> DeleteAmenityAsync(Guid amenityId);
```

**File: `Models/DTOs/Amenity/AmenityDtos.cs`**
Add new DTO:
```csharp
public class PagedAmenityResponseDto
{
    public int TotalCount { get; set; }
    public IEnumerable<AmenityResponseDto> Amenities { get; set; } = new List<AmenityResponseDto>();
}
```

**File: `Services/AmenityService.cs`**
Implement the three new methods:
- `GetAllAmenitiesPagedAsync`: Query all amenities (no `IsActive` filter), support search on `Name` and `Category`, server-side pagination with `Skip/Take`, order by `Category` then `Name`
- `ToggleAmenityStatusAsync`: Flip `IsActive` flag, save, return new value
- `DeleteAmenityAsync`: Hard delete only if no `RoomTypeAmenity` records reference this amenity; otherwise throw `ConflictException("Amenity is in use by one or more room types.")`

**File: `Controllers/Public/PublicAmenityController.cs`** (the file that also contains `SuperAdminAmenityController`)
Add to `SuperAdminAmenityController`:
```csharp
[HttpGet]
public async Task<IActionResult> GetAll(
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 10,
    [FromQuery] string? search = null,
    [FromQuery] string? category = null)
{
    var result = await _service.GetAllAmenitiesPagedAsync(page, pageSize, search, category);
    return Ok(new { success = true, data = result });
}

[HttpPatch("{id}/toggle-status")]
public async Task<IActionResult> ToggleStatus(Guid id)
{
    var isActive = await _service.ToggleAmenityStatusAsync(id);
    return Ok(new { success = true, data = new { isActive } });
}

[HttpDelete("{id}")]
public async Task<IActionResult> Delete(Guid id)
{
    await _service.DeleteAmenityAsync(id);
    return Ok(new { success = true, message = "Amenity deleted." });
}
```

### Frontend Changes

**New file: `src/app/features/superadmin/amenity-management/superadmin-amenity-management.component.ts`**

Create a standalone Angular component with:
- Mat table columns: `name`, `category`, `iconName`, `status`, `actions`
- Server-side paginator (`MatPaginatorModule`) wired to `page` and `pageSize`
- Search input with `debounceTime(400)` triggering reload
- Category filter `MatSelect` (options: All, Room, Bathroom, Tech, Services, Food)
- Each row: **Edit** button (opens inline edit form), **Toggle Active/Inactive** slide toggle, **Delete** button (with confirm dialog)
- Add New Amenity form at top (collapsible): fields `name` (required), `category` (required, select), `iconName` (optional text)
- On save calls `POST /api/superadmin/amenities`; on update calls `PUT /api/superadmin/amenities`
- Status chip: green = Active, grey = Inactive

**New file: `src/app/core/services/amenity.service.ts`** (or extend existing if present)
```typescript
getAllPaged(page: number, pageSize: number, search?: string, category?: string): Observable<PagedAmenityResponseDto>
create(dto: CreateAmenityDto): Observable<AmenityResponseDto>
update(dto: UpdateAmenityDto): Observable<AmenityResponseDto>
toggleStatus(id: string): Observable<{ isActive: boolean }>
delete(id: string): Observable<void>
```
All calls go to `/api/superadmin/amenities`.

**File: `src/app/features/superadmin/superadmin.routes.ts`**
Add route:
```typescript
{
  path: 'amenities',
  loadComponent: () => import('./amenity-management/superadmin-amenity-management.component')
    .then(m => m.SuperadminAmenityManagementComponent),
}
```

**File: Superadmin sidebar/nav component**
Add "Amenities" nav link pointing to `/superadmin/amenities`.

---

## CHANGE 2 — Review Contribution Points (100 Credits per Review)

### Problem
Currently `ReviewService.AddReviewAsync` creates the review but does **not** credit the wallet. `DeleteReviewAsync` does not deduct. There is no `contributionPoints` or review credit concept in the `ReviewResponseDto`, `MyReviewsResponseDto`, or guest profile.

### Backend Changes

**File: `Services/ReviewService.cs` — `AddReviewAsync`**
After `await _reviewRepo.AddAsync(review)` and before `await _unitOfWork.CommitAsync()`, add:
```csharp
await _walletService.CreditAsync(userId, 100m, "Review contribution reward");
```
Inject `IWalletService` into `ReviewService` constructor.

**File: `Services/ReviewService.cs` — `DeleteReviewAsync`**
After fetching the review and verifying ownership, before delete:
```csharp
await _walletService.DebitAsync(review.UserId, 100m, "Review contribution reversed on deletion");
```
If wallet balance would go negative, still allow deletion but debit only down to zero (do not throw).

**File: `Interfaces/IWalletService.cs`**
Add if not present:
```csharp
Task CreditAsync(Guid userId, decimal amount, string description);
Task DebitAsync(Guid userId, decimal amount, string description);
```

**File: `Services/WalletService.cs`**
Implement `CreditAsync` and `DebitAsync` — add/subtract from `Wallet.Balance`, create a `WalletTransaction` record with `Type = "Credit"` or `"Debit"`, save via unit of work.

**File: `Models/DTOs/Review/ReviewDtos.cs`**
Add field to `ReviewResponseDto` and `MyReviewsResponseDto`:
```csharp
public int ContributionPoints { get; set; } // Always 100 per review
```

**File: `Services/ReviewService.cs` — `MapToDto`**
Set `ContributionPoints = 100` in the mapping.

**File: `Models/DTOs/UserDetails/UserDetailsDtos.cs`** (or wherever `UserProfileResponseDto` is)
Add:
```csharp
public int TotalReviewPoints { get; set; }
```

**File: `Services/UserService.cs` — `GetProfileAsync`**
Add a query to count reviews by this user and set `TotalReviewPoints = reviewCount * 100`.

### Frontend Changes

**File: Guest Reviews page (`src/app/features/guest/reviews/guest-reviews.component.ts`)**
- Below each review card, show a green badge: `🏆 +100 pts` contribution label
- Add a total contribution points summary at the top: "Your Review Contribution: X pts"

**File: `src/app/features/guest/profile/guest-profile.component.ts`**
- In the profile info section, add a "Review Contribution Points" display field
- Show value from `profile.totalReviewPoints` with a star/trophy icon
- Label: "Review Points" with sub-text "Earn 100 pts for every review"

**File: Hotel details page — reviews section**
- Below each reviewer's name show their contribution: `🏆 100 pts` in small muted text

---

## CHANGE 3 — Tiered Cancellation Policy with Optional Cancellation Fee

### Problem
Current flow: guest cancels → always creates `RefundRequest` → admin manually approves/rejects → full refund or nothing. There is no time-based refund tier and no optional pre-paid cancellation protection fee.

### Backend Changes

**File: `Models/Reservation.cs`**
Add two new fields:
```csharp
/// <summary>Whether the guest paid the 10% cancellation protection fee at booking time</summary>
public bool CancellationFeePaid { get; set; } = false;

/// <summary>Actual cancellation fee amount paid (10% of TotalAmount)</summary>
public decimal CancellationFeeAmount { get; set; } = 0;
```

**Migration**: Add EF Core migration `AddCancellationFeeToReservation`.

**File: `Models/DTOs/Reservation/ReservationDtos.cs` — `CreateReservationDto`**
Add:
```csharp
/// <summary>Guest opts in to pay 10% cancellation protection fee</summary>
public bool PayCancellationFee { get; set; } = false;
```

**File: `Services/ReservationService.cs` — `CreateReservationAsync`**
In pricing calculation, if `dto.PayCancellationFee == true`:
```csharp
var cancellationFeeAmount = Math.Round(totalAmount * 0.10m, 2);
reservation.CancellationFeePaid = true;
reservation.CancellationFeeAmount = cancellationFeeAmount;
reservation.FinalAmount += cancellationFeeAmount; // add to total charge
```

**File: `Services/ReservationService.cs` — `CancelReservationAsync`**
Replace the current simple refund-request creation with this logic:

```csharp
// Calculate days until check-in
var today = DateOnly.FromDateTime(DateTime.UtcNow);
var daysUntilCheckIn = res.CheckInDate.DayNumber - today.DayNumber;

decimal refundPercent = 0;
string refundNote;

if (res.CancellationFeePaid)
{
    // Guest paid protection fee — always 100% refund regardless of timing
    refundPercent = 100;
    refundNote = "Full refund — cancellation protection fee was paid.";
}
else
{
    // No protection fee — apply tier policy
    if (daysUntilCheckIn >= 5)
    {
        refundPercent = 50;
        refundNote = "50% refund — cancelled 5+ days before check-in.";
    }
    else if (daysUntilCheckIn >= 3)
    {
        refundPercent = 25;
        refundNote = "25% refund — cancelled 3-4 days before check-in.";
    }
    else
    {
        refundPercent = 0;
        refundNote = "No refund — cancelled within 2 days of check-in.";
    }
}

var hasPaid = res.Transactions?.Any(t => t.Status == PaymentStatus.Success) ?? false;
if (hasPaid && refundPercent > 0)
{
    decimal refundAmount = Math.Round(res.TotalAmount * (refundPercent / 100m), 2);
    await _refundRequestService.CreateRefundRequestAsync(
        res.ReservationId, userId, reason, refundAmount, refundNote);
}
else if (hasPaid && refundPercent == 0)
{
    // Log that no refund is due — do not create a RefundRequest
}
```

**File: `Models/DTOs/RefundRequest/RefundRequestDtos.cs`**  
Add `RefundAmount` and `RefundNote` parameters to `CreateRefundRequestAsync` signature and persist them in the `RefundRequest` record.

**File: `Models/RefundRequest.cs`**
Add:
```csharp
public decimal RefundAmount { get; set; } = 0;
public string? RefundNote { get; set; }
```
Add migration `AddRefundAmountToRefundRequest`.

**File: `Services/RefundRequestService.cs`**
Update `CreateRefundRequestAsync` to accept and store `refundAmount` and `refundNote`. When admin approves, the refund amount is now the pre-calculated `RefundAmount` (not the full reservation amount).

**File: `Models/DTOs/Reservation/ReservationDtos.cs` — Response DTOs**
Add to reservation response:
```csharp
public bool CancellationFeePaid { get; set; }
public decimal CancellationFeeAmount { get; set; }
public string CancellationPolicyText { get; set; } = string.Empty;
// Computed: "Full refund anytime" if fee paid, else "50%/25%/0% based on timing"
```

### Frontend Changes

**File: Booking confirmation/payment page (`src/app/features/booking/`)**
- Add a checkbox: "Add Cancellation Protection (+10% of room cost)"
- Show the fee amount dynamically (e.g., "₹300 extra for full refund anytime")
- When checked, include `payCancellationFee: true` in `CreateReservationDto`
- Show the two policies clearly side-by-side:
  - **Without protection**: 5+ days = 50%, 3 days = 25%, <3 days = No refund
  - **With protection** (₹X extra): Cancel anytime, full refund

**File: Guest booking list/detail component**
- Show cancellation protection badge on bookings where `cancellationFeePaid === true`
- On the cancel button, show a preview modal with exact refund amount before confirming:
  - Calculate `daysUntilCheckIn` from `checkInDate`
  - Display: "You will receive ₹X refund" or "No refund applicable"

**File: Refund requests (Guest and Admin)**
- Show `refundNote` field explaining why that refund percentage was applied
- Show `refundAmount` as the pre-approved amount (admin sees this as the amount to pay)

---

## CHANGE 4 — Inventory Freeing on Failed Transactions

### Problem
`MarkTransactionFailedAsync` in `TransactionService` sets `transaction.Status = Failed` and resets `reservation.Status = Pending` but **does NOT restore inventory** (`ReservedInventory`). This means rooms remain locked even after a transaction is marked failed by the admin.

### Backend Changes

**File: `Services/TransactionService.cs` — `MarkTransactionFailedAsync`**

After `transaction.Status = PaymentStatus.Failed` and before `SaveChangesAsync`, add inventory restoration:

```csharp
// Restore inventory when transaction is marked failed
var reservationRooms = await _reservationRoomRepo.GetQueryable()
    .Where(rr => rr.ReservationId == transaction.ReservationId)
    .ToListAsync();

if (reservationRooms.Any())
{
    var roomTypeId = reservationRooms.First().RoomTypeId;
    var reservation = transaction.Reservation!;
    var dates = GetDateRange(reservation.CheckInDate, reservation.CheckOutDate);
    var inventories = await _inventoryRepo.GetQueryable()
        .Where(i => i.RoomTypeId == roomTypeId && dates.Contains(i.Date))
        .ToListAsync();
    var roomCount = reservationRooms.Count;
    foreach (var inv in inventories)
        inv.ReservedInventory = Math.Max(0, inv.ReservedInventory - roomCount);
}
```

Inject `IRepository<Guid, ReservationRoom>` into `TransactionService` constructor.

**File: `Services/TransactionService.cs` — `RecordFailedPaymentAsync`**
Check if this method also needs inventory restoration — if this is called for Razorpay failures (where inventory was reserved at booking time), apply the same inventory restoration logic.

**Verify**: `CancelReservationAsync` in `ReservationService` already restores inventory correctly — confirmed in source review, no change needed there.

**Verify**: `ReservationCleanupService` (background) already restores inventory for expired pending reservations — confirmed, no change needed.

---

## CHANGE 5 — Role-Based Support / Contact Forms

### Problem
The current `/contact` page is static HTML with only email/phone links. There are no in-app forms, no database storage, no superadmin view of submissions.

### Backend Changes

**New file: `Models/SupportTicket.cs`**
```csharp
public class SupportTicket
{
    [Key] public Guid SupportTicketId { get; set; }
    public SupportTicketType TicketType { get; set; }
    public string SubmitterName { get; set; } = string.Empty;
    public string SubmitterEmail { get; set; } = string.Empty;
    public Guid? UserId { get; set; }          // null for unauthenticated
    public Guid? HotelId { get; set; }         // for hotel complaints
    public string Subject { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public SupportTicketStatus Status { get; set; } = SupportTicketStatus.Open;
    public string? AdminNote { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public User? User { get; set; }
}

public enum SupportTicketType
{
    BugReport = 1,
    SiteProblem = 2,
    HotelComplaint = 3,       // Guest only
    HotelAdminSuggestion = 4  // Hotel Admin only
}

public enum SupportTicketStatus
{
    Open = 1,
    InProgress = 2,
    Resolved = 3,
    Closed = 4
}
```

Add `DbSet<SupportTicket> SupportTickets` to `HotelBookingContext`. Add migration `AddSupportTickets`.

**New file: `Interfaces/ISupportTicketService.cs`**
```csharp
Task<SupportTicketResponseDto> CreatePublicTicketAsync(CreatePublicTicketDto dto);
Task<SupportTicketResponseDto> CreateGuestTicketAsync(Guid userId, CreateGuestTicketDto dto);
Task<SupportTicketResponseDto> CreateAdminTicketAsync(Guid userId, CreateAdminSuggestionDto dto);
Task<PagedSupportTicketResponseDto> GetAllTicketsAsync(int page, int pageSize, string? search, string? type, string? status);
Task<SupportTicketResponseDto> UpdateTicketStatusAsync(Guid ticketId, string status, string? note);
```

**New file: `Services/SupportTicketService.cs`** — implement the interface above.

**New file: `Controllers/Public/SupportController.cs`**
```csharp
// Public (unauthenticated) — Bug reports and site problems only
[Route("api/support/public")]
[HttpPost] → CreatePublicTicketAsync (types: BugReport, SiteProblem only; validate in service)

// Guest — Hotel complaints
[Route("api/support/guest")]
[Authorize(Roles = "Guest")]
[HttpPost] → CreateGuestTicketAsync (type: HotelComplaint)

// Admin — Suggestions
[Route("api/support/admin")]
[Authorize(Roles = "Admin")]
[HttpPost] → CreateAdminTicketAsync (type: HotelAdminSuggestion)

// SuperAdmin — View all, update status
[Route("api/superadmin/support-tickets")]
[Authorize(Roles = "SuperAdmin")]
[HttpGet] → GetAllTicketsAsync (paged, search, filter by type and status)
[HttpPatch("{id}/status")] → UpdateTicketStatusAsync
```

**DTOs needed** (`Models/DTOs/Support/SupportTicketDtos.cs`):
- `CreatePublicTicketDto`: `name`, `email`, `subject`, `description`, `ticketType` (BugReport or SiteProblem only)
- `CreateGuestTicketDto`: `hotelId` (optional), `hotelName` (free text), `subject`, `description`
- `CreateAdminSuggestionDto`: `subject`, `description`
- `SupportTicketResponseDto`: all fields
- `PagedSupportTicketResponseDto`: `totalCount`, `tickets`

Register `ISupportTicketService` / `SupportTicketService` in `Program.cs`.

### Frontend Changes

**File: `src/app/features/contact/contact.component.ts`**
Rewrite to a dynamic role-based form component. Import `AuthService` to detect current user role.

**Logic**:
- **Not logged in**: Show form with fields: Name, Email, Subject, Description. `TicketType` dropdown shows only "Bug Report" and "Site Problem".
- **Logged in as Guest**: Show the same bug/site form PLUS a second card "Report a Hotel Problem" with fields: Hotel Name (text), Subject, Description. This submits to `POST /api/support/guest`.
- **Logged in as Admin**: Show the same bug/site form PLUS a second card "Suggest an Improvement" with fields: Subject, Description. This submits to `POST /api/support/admin`.
- **SuperAdmin**: No submission forms; show a link to `/superadmin/support-tickets` dashboard.

Each form shows a success message on submit and resets.

**New file: `src/app/features/superadmin/support-tickets/superadmin-support-tickets.component.ts`**
- Mat table: columns `type`, `submitterName`, `submitterEmail`, `subject`, `status`, `createdAt`, `actions`
- Server-side pagination, search (on subject/name/email), filter by `TicketType` and `Status`
- Actions: View details (expand row or dialog), Update Status (dropdown: Open, In Progress, Resolved, Closed), add Admin Note

**File: `src/app/features/superadmin/superadmin.routes.ts`**
Add:
```typescript
{
  path: 'support-tickets',
  loadComponent: () => import('./support-tickets/superadmin-support-tickets.component')
    .then(m => m.SuperadminSupportTicketsComponent),
}
```
Add "Support Tickets" to superadmin sidebar nav.

---

## CHANGE 6 — Replace City DB Table with `country-state-city` npm Package

### Problem
Cities are stored in a database table manually maintained by superadmin. This is hard to scale. The `country-state-city` npm package provides India's full state/city data via API-like calls without a DB.

### Backend Changes

**Deprecate** `GET /api/public/cities` and `GET /api/public/cities/all` endpoints (keep them temporarily but add deprecation headers).

The backend city APIs for **SuperAdmin CRUD** (`/api/superadmin/cities`) can be **removed entirely** — city management becomes frontend-only via the npm package.

**Important**: Check all columns in `Hotel`, `Reservation`, `UserProfileDetails` that store `City` and `State` as plain strings — these remain as-is (strings). No DB schema change needed. The change is purely in how the frontend populates these fields.

**Keep** `City` table in DB and `SuperAdminCityController` **only if** hotels are currently being searched/filtered by city using the DB cities table. If the hotel search uses free-text city string matching, the DB cities table can be fully removed. Check `HotelService.SearchHotelsAsync` — if it does `WHERE h.City = @city` string match, then DB is not needed.

**Recommendation**: Remove the DB `cities` table and related controller/service only after confirming no foreign key or join depends on it. Replace all city lookups with the npm package on the frontend.

### Frontend Changes

**Install package**:
```bash
npm install country-state-city
```

**New file: `src/app/core/services/location.service.ts`**
```typescript
import { Injectable } from '@angular/core';
import { City, State, ICity, IState } from 'country-state-city';

@Injectable({ providedIn: 'root' })
export class LocationService {
  private readonly COUNTRY_CODE = 'IN'; // India only

  getStates(): IState[] {
    return State.getStatesOfCountry(this.COUNTRY_CODE);
  }

  getCitiesOfState(stateCode: string): ICity[] {
    return City.getCitiesOfState(this.COUNTRY_CODE, stateCode);
  }

  getStateByCode(stateCode: string): IState | undefined {
    return State.getStateByCodeAndCountry(stateCode, this.COUNTRY_CODE);
  }

  searchCities(query: string): ICity[] {
    if (!query || query.length < 2) return [];
    const all = City.getCitiesOfCountry(this.COUNTRY_CODE) || [];
    return all
      .filter(c => c.name.toLowerCase().startsWith(query.toLowerCase()))
      .slice(0, 20);
  }

  getStateNameByCity(cityName: string): string {
    const all = City.getCitiesOfCountry(this.COUNTRY_CODE) || [];
    const match = all.find(c => c.name.toLowerCase() === cityName.toLowerCase());
    if (!match) return '';
    const state = State.getStateByCodeAndCountry(match.stateCode, this.COUNTRY_CODE);
    return state?.name || '';
  }
}
```

**New/Updated file: `src/app/shared/components/city-autocomplete/city-autocomplete.component.ts`**

Update the existing `CityAutocompleteComponent` to use `LocationService.searchCities()` instead of calling `GET /api/public/cities?search=`. The component already exists and is imported in hotel-list and profile — just change the data source.

```typescript
// Replace HTTP call with:
this.locationService.searchCities(query) // returns ICity[] synchronously
```

**File: Guest Profile Component (`guest-profile.component.ts`)**
- Replace the plain city/state text inputs with the city autocomplete
- When user selects a city, **auto-fill the State field** using `locationService.getStateNameByCity(cityName)`
- State field becomes read-only once a city is selected (can be cleared)

**File: Admin Registration / Hotel creation forms**
- Same city autocomplete with state auto-fill wherever `city` and `state` fields appear in hotel admin profile or hotel registration

**File: Hotel list / search page**
- The search bar's city input should use the city autocomplete from `LocationService` instead of the old `CityService.search()` HTTP call

**Remove** `src/app/core/services/city.service.ts` HTTP methods for public city search (`search()`, `getAll()`). Keep only the SuperAdmin CRUD methods if superadmin city management page is being kept, otherwise remove the whole file.

---

## CHANGE 7 — Homepage: Hotels by State (replacing Hotels by City)

### Problem
The home page currently shows "Hotels by City" — `cityGroups` signal containing `{ cityName, hotels }`. This is a narrow view. Hotels should be grouped by **State** so users see all hotels across all cities in a state.

### Backend Changes

**File: `Interfaces/IHotelService.cs`**
Add:
```csharp
Task<IEnumerable<string>> GetActiveStatesAsync();
Task<IEnumerable<HotelListItemDto>> GetHotelsByStateAsync(string stateName);
```

**File: `Services/HotelService.cs`**
Implement:
- `GetActiveStatesAsync`: Query distinct `Hotel.State` values (need to ensure `Hotel` model has a `State` field — if not, add it; check if `Hotel` stores city only and derive state from city, or add `State` column)
- `GetHotelsByStateAsync`: `WHERE h.State = stateName AND h.IsActive = true`, return up to 10 hotels per state ordered by rating

**Check `Hotel` model**: If it currently only has `City` string, add a `State` string column:
```csharp
[MaxLength(100)]
public string State { get; set; } = string.Empty;
```
Add migration `AddStateToHotel`. Populate from existing city data using the `country-state-city` package logic or a one-time data migration.

**File: `Controllers/Public/PublicHotelController.cs`**
Add:
```csharp
[HttpGet("active-states")]
public async Task<IActionResult> GetActiveStates()
{
    var result = await _service.GetActiveStatesAsync();
    return Ok(new { success = true, data = result });
}

[HttpGet("by-state/{stateName}")]
public async Task<IActionResult> GetByState(string stateName)
{
    var result = await _service.GetHotelsByStateAsync(stateName);
    return Ok(new { success = true, data = result });
}
```

### Frontend Changes

**File: `src/app/features/hotel/hotel-list/hotel-list.component.ts`**

Replace `cityGroups` with `stateGroups`:
```typescript
stateGroups = signal<{ stateName: string; hotels: HotelListItemDto[] }[]>([]);
```

In `ngOnInit`, replace city group loading:
```typescript
// OLD: hotelService.getCities() then getHotelsByCity(city)
// NEW:
this.hotelService.getActiveStates().subscribe(states => {
  const limited = states.slice(0, 6); // show up to 6 states
  const groups: { stateName: string; hotels: HotelListItemDto[] }[] = [];
  for (const state of limited) {
    this.hotelService.getHotelsByState(state).subscribe(hotels => {
      groups.push({ stateName: state, hotels: hotels.slice(0, 10) });
      if (groups.length === limited.length)
        this.stateGroups.set(groups.sort((a, b) => a.stateName.localeCompare(b.stateName)));
    });
  }
});
```

**File: `src/app/features/hotel/hotel-list/hotel-list.component.html`**
Replace the city section template:
```html
<!-- OLD: *ngFor city -->
<!-- NEW: -->
@for (group of stateGroups(); track group.stateName) {
  <section class="state-section mb-5">
    <h3 class="section-title">
      <mat-icon>location_on</mat-icon> Hotels in {{ group.stateName }}
    </h3>
    <div class="hotel-scroll-row">
      @for (hotel of group.hotels; track hotel.hotelId) {
        <app-hotel-card [hotel]="hotel" />
      }
    </div>
  </section>
}
```

**File: `src/app/core/services/hotel.service.ts`**
Add:
```typescript
getActiveStates(): Observable<string[]>
getHotelsByState(stateName: string): Observable<HotelListItemDto[]>
```

---

## CHANGE 8 — Revenue Page: Remove Manual "Mark Sent" Button

### Problem
At `http://localhost:4200/superadmin/revenue`, there is an "Actions" column with a "Mark Sent" button. The commission is **automatically created** by `SuperAdminRevenueBackgroundService` when a reservation is `Completed`. The "Mark Sent" status (`Pending` → `Sent`) is a manual bookkeeping action that implies the superadmin physically sent the collected commission somewhere — but this is misleading since the 2% is automatically tracked.

### Recommended Solution
The `Status` field (`Pending`/`Sent`) and `MarkSentAsync` endpoint are optional tracking features. Since commission is auto-collected (tracked, not physically sent), the "Sent" status has no real meaning in the current flow.

**Option A (Recommended)**: Remove the Actions column and the `mark-sent` button entirely from the frontend. The revenue table becomes read-only. Keep the backend endpoint in case it's needed later but remove the button.

**Option B**: Rename "Mark Sent" to "Mark as Acknowledged" and clarify the UI with a tooltip: "Mark this commission record as reviewed" — making it a bookkeeping confirmation, not a payment action.

### Frontend Changes (Option A)

**File: `src/app/features/superadmin/revenue/superadmin-revenue.component.ts`**
- Remove `'actions'` from `displayedColumns` array
- Remove the `<ng-container matColumnDef="actions">` block from the template
- Remove the `markSent()` method
- Remove `RevenueService.markSent()` call
- The `Status` column now just shows "Pending" or "Sent" as informational chips

**File: `src/app/core/services/revenue.service.ts`**
- Remove `markSent(id: string)` method (or keep it deprecated)

The backend `PATCH /api/superadmin/revenue/{id}/mark-sent` can remain for potential future use.

---

## CHANGE 9 — Remove Unused `Amenities` String Column from `RoomType`

### Problem
`RoomType.Amenities` is a legacy `string` column (comment says "kept for backward compat"). The actual amenity relationship uses `RoomTypeAmenity` join table (many-to-many). The string column is empty/unused but still exists in the DB schema and causes confusion.

### Backend Changes

**Check first**: Query the DB to confirm `RoomType.Amenities` is always empty string or null:
```sql
SELECT COUNT(*) FROM RoomTypes WHERE Amenities IS NOT NULL AND Amenities != ''
```

**If all empty**: 
1. Remove `public string Amenities { get; set; } = string.Empty;` from `Models/RoomType.cs`
2. Add EF Core migration: `RemoveAmenitiesStringFromRoomType`
3. Search all DTOs and services for `.Amenities` mapping from `RoomType` and remove those references
4. In `RoomTypePublicDto`, keep `amenities: string[]` field — populate it from `RoomTypeAmenities` join table instead

**File: `Services/RoomTypeService.cs`**
Update any mapping that reads from `roomType.Amenities` string → change to read from `roomType.RoomTypeAmenities` navigation property.

**File: `Models/DTOs/RoomType/RoomTypeDtos.cs`**
Ensure response DTOs derive the amenity string list from `RoomTypeAmenities` not from the raw `Amenities` field.

---

## CHANGE 10 — Fix Server-Side Pagination Across All Pages

### Problem
Several Angular pages use client-side pagination or broken paginator wiring. The "Previous" button and search/sort may not be properly server-side. Based on code review:

### Pages to Audit and Fix

For each page below, ensure:
1. `MatPaginator` `length` is bound to `totalCount()` from the API
2. `pageIndex` change triggers new API call (not client-side slice)
3. Search input uses `debounceTime(400)` and resets `currentPage = 1` before calling API
4. Sort changes reset `currentPage = 1` before calling API
5. "Previous" button works because `MatPaginator` is correctly wired (it works automatically when `length` and `pageSize` are correct)

**Pages to fix**:

| Page | Component File | Known Issue |
|------|---------------|-------------|
| SuperAdmin Hotels | `superadmin/hotel-control/hotel-control.component.ts` | Verify search resets page |
| SuperAdmin Cities | `superadmin/city-management/city-management.component.ts` | Appears correct — verify |
| SuperAdmin Amenity Requests | `superadmin/amenity-requests/superadmin-amenity-requests.component.ts` | No sort column |
| SuperAdmin Revenue | `superadmin/revenue/superadmin-revenue.component.ts` | No search/sort |
| Admin Rooms | `admin/room-management/room-management.component.ts` | Verify `totalCount` binding |
| Admin RoomTypes | `admin/room-management/roomtype-management.component.ts` | Verify `totalCount` binding |
| Admin Reservations | `admin/reservation-management/reservation-management.component.ts` | Verify |
| Admin Refunds | `admin/refund-management/refund-management.component.ts` | Verify `totalCount` |
| Admin Transactions | `admin/transactions/admin-transactions.component.ts` | Verify |
| Guest Reservations | `features/booking/booking-list/booking-list.component.ts` | Uses `/history` endpoint — verify |

**Standard fix pattern** for each component:
```typescript
// 1. Add search subject
private searchSubject = new Subject<string>();

ngOnInit() {
  this.searchSubject.pipe(debounceTime(400), distinctUntilChanged())
    .subscribe(() => { this.currentPage = 1; this.load(); });
}

onSearch(value: string) {
  this.searchQuery = value;
  this.searchSubject.next(value);
}

onSort(column: string) {
  this.sortBy = column;
  this.sortDir = this.sortDir === 'asc' ? 'desc' : 'asc';
  this.currentPage = 1;
  this.load();
}

onPage(e: PageEvent) {
  this.currentPage = e.pageIndex + 1;  // MatPaginator is 0-indexed
  this.pageSize = e.pageSize;
  this.load();
}
```

**Important**: Ensure `mat-paginator [length]="totalCount()"` is set — this is what enables the Previous button. If `length` is 0 or undefined, Previous/Next both break.

---

## CHANGE 11 — SuperAdmin Amenity Requests: Show Active Amenities List with Edit/Delete

### Note on Current State
The `SuperadminAmenityRequestsComponent` handles **requests** (approve/reject hotel admin requests). This is separate from the new **Amenity Management** page in Change 1.

However, the current amenity requests page is missing:
- A link/tab to view the **master amenities list** (approved amenities that are now active)
- After approving a request, the admin should be able to see it in the amenities list

### Frontend Changes

**File: `src/app/features/superadmin/amenity-requests/superadmin-amenity-requests.component.ts`**
Add a tab group (`MatTabsModule`) at the top:
- Tab 1: "Requests" — existing approve/reject table (current content)
- Tab 2: "Active Amenities" — embedded view of all active amenities with Edit/Delete/Toggle buttons (reuse the `SuperadminAmenityManagementComponent` or inline a simplified version)

Or alternatively, add a navigation button "View All Amenities →" linking to `/superadmin/amenities`.

---

## Summary of New Files to Create

### Backend (C# .NET)
| File | Purpose |
|------|---------|
| `Models/SupportTicket.cs` | Support ticket entity + enums |
| `Models/DTOs/Support/SupportTicketDtos.cs` | All support ticket DTOs |
| `Interfaces/ISupportTicketService.cs` | Service interface |
| `Services/SupportTicketService.cs` | Service implementation |
| `Controllers/Public/SupportController.cs` | All support ticket endpoints |
| `Migrations/AddCancellationFeeToReservation.cs` | EF migration |
| `Migrations/AddRefundAmountToRefundRequest.cs` | EF migration |
| `Migrations/AddSupportTickets.cs` | EF migration |
| `Migrations/AddStateToHotel.cs` | EF migration (if Hotel.State missing) |
| `Migrations/RemoveAmenitiesStringFromRoomType.cs` | EF migration |

### Frontend (Angular)
| File | Purpose |
|------|---------|
| `core/services/location.service.ts` | country-state-city wrapper |
| `core/services/amenity.service.ts` | Amenity CRUD service |
| `features/superadmin/amenity-management/superadmin-amenity-management.component.ts` | Amenity CRUD page |
| `features/superadmin/support-tickets/superadmin-support-tickets.component.ts` | View all support tickets |

---

## Summary of Modified Files

### Backend
- `Models/Reservation.cs` — add `CancellationFeePaid`, `CancellationFeeAmount`
- `Models/RefundRequest.cs` — add `RefundAmount`, `RefundNote`
- `Models/RoomType.cs` — remove `Amenities` string column
- `Interfaces/IAmenityService.cs` — add 3 methods
- `Interfaces/IWalletService.cs` — add `CreditAsync`, `DebitAsync`
- `Services/AmenityService.cs` — implement 3 new methods
- `Services/ReviewService.cs` — add wallet credit/debit on add/delete
- `Services/WalletService.cs` — implement `CreditAsync`, `DebitAsync`
- `Services/ReservationService.cs` — tiered cancellation logic
- `Services/TransactionService.cs` — inventory restore on mark-failed
- `Services/RefundRequestService.cs` — accept `refundAmount`, `refundNote`
- `Services/UserService.cs` — include `totalReviewPoints` in profile
- `Services/HotelService.cs` — add `GetActiveStatesAsync`, `GetHotelsByStateAsync`
- `Controllers/Public/PublicAmenityController.cs` — add GET, PATCH, DELETE to superadmin controller
- `Controllers/Public/PublicHotelController.cs` — add `active-states` and `by-state` endpoints
- `Models/DTOs/Amenity/AmenityDtos.cs` — add `PagedAmenityResponseDto`
- `Models/DTOs/Review/ReviewDtos.cs` — add `ContributionPoints`
- `Models/DTOs/Reservation/ReservationDtos.cs` — add cancellation fee fields
- `Program.cs` — register `ISupportTicketService`

### Frontend
- `app/features/contact/contact.component.ts` + `.html` — role-based forms
- `app/features/superadmin/superadmin.routes.ts` — add amenities + support-tickets routes
- `app/features/superadmin/revenue/superadmin-revenue.component.ts` — remove mark-sent
- `app/features/superadmin/amenity-requests/superadmin-amenity-requests.component.ts` — add tab/link to amenities
- `app/features/guest/reviews/guest-reviews.component.ts` — show contribution points
- `app/features/guest/profile/guest-profile.component.ts` — show total review points
- `app/features/hotel/hotel-list/hotel-list.component.ts` + `.html` — hotels by state
- `app/features/hotel/hotel-details/` — show reviewer contribution points
- `app/features/booking/` payment page — cancellation fee opt-in checkbox
- `app/features/booking/booking-list/` — show cancellation policy + refund preview
- `app/core/services/hotel.service.ts` — add state-related methods
- `app/shared/components/city-autocomplete/city-autocomplete.component.ts` — use LocationService
- All paginated components — apply standard server-side pagination fix pattern

---

## Implementation Order (Recommended)

1. **Change 9** — Remove unused `Amenities` string from RoomType (low risk, clean DB)
2. **Change 4** — Fix inventory restore on failed transaction (bug fix, no new features)
3. **Change 10** — Fix all pagination (bug fix)
4. **Change 8** — Remove mark-sent button from revenue (UI cleanup)
5. **Change 2** — Review contribution points / wallet credits
6. **Change 1** — Amenity management CRUD for SuperAdmin
7. **Change 3** — Tiered cancellation policy
8. **Change 6** — Replace city DB with country-state-city npm
9. **Change 7** — Hotels by state on homepage
10. **Change 5** — Role-based support forms
11. **Change 11** — Link amenity requests to amenity management

---

*End of Kiro Change Specification*