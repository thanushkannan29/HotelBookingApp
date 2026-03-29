You are a senior full-stack developer. Analyze and fix my Hotel Booking Application by checking BOTH backend (.NET Web API) and frontend (Angular). Ensure consistency, validation, and production-level quality.

--------------------------------------------------
🔹 1. CHECK-IN DATE VALIDATION (CRITICAL FIX)
--------------------------------------------------
Issue:
- Currently allowing today's date as check-in.

Fix required:
- Disallow today's date.
- Only allow check-in from NEXT DAY onwards.

Apply this fix in:
1. http://localhost:4200/hotels/{hotelId}
2. http://localhost:4200/hotels (homepage search)

Reference (already working correctly):
http://localhost:4200/booking/create?... (this logic is correct)

Tasks:
- Reuse same validation logic from booking/create page.
- Apply in Angular (date picker minDate).
- Also enforce in backend (.NET API validation) to prevent bypass.

--------------------------------------------------
🔹 2. SUPER ADMIN PROFILE - USER NOT FOUND
--------------------------------------------------
URL:
http://localhost:4200/superadmin/profile

Error:
NotFoundException: "User not found."

Backend Code:
GetProfileAsync(Guid userId)

Tasks:
- Debug why userId is invalid/null.
- Check authentication token → ensure correct userId extraction.
- Verify:
  - JWT claims mapping
  - Controller userId retrieval
  - User exists in DB
- Handle gracefully:
  - Return proper error response instead of crash
- Ensure UserDetails is always loaded or handled safely.

--------------------------------------------------
🔹 3. BOOKING LIST PAGINATION FIX
--------------------------------------------------
URL:
http://localhost:4200/booking/list

Issues:
- Previous button not working
- No sorting
- No search

Tasks:
- Implement FULL server-side pagination:
  - pageNumber
  - pageSize
  - totalRecords
- Add:
  - Sorting (date, price, status)
  - Search (bookingId, hotel name)
- Fix Angular paginator (MatPaginator):
  - Previous/Next must work correctly
- Ensure API + UI are synced.

--------------------------------------------------
🔹 4. APPLY SERVER-SIDE PAGINATION (ALL MODULES)
--------------------------------------------------
Implement consistent pagination + sorting + search + Angular Material UI in ALL below pages:

GUEST:
- /guest/wallet
- /guest/promo-codes
- /guest/reviews

ADMIN:
- /admin/reservations
- /admin/rooms
- /admin/inventory
- /admin/reviews
- /admin/transactions
- /admin/amenity-requests
- /admin/roomtypes
- /admin/audit-logs

SUPER ADMIN:
- /superadmin/hotels
- /superadmin/audit-logs
- /superadmin/error-logs
- /superadmin/revenue
- /superadmin/amenities
- /superadmin/amenity-requests

Requirements:
- Server-side pagination (NOT client-side)
- Sorting (asc/desc)
- Search (global + column level if possible)
- Angular Material Table + MatPaginator + MatSort
- Backend:
  - IQueryable with filtering
  - Efficient queries (AsNoTracking)
  - Return DTO with:
    items
    totalCount

--------------------------------------------------
🔹 5. BOOKING PDF IMPROVEMENT
--------------------------------------------------
URL:
http://localhost:4200/booking/{bookingId}

Task:
- Improve "Download PDF"

Requirements:
- Professional template
- Include:
  - Hotel details
  - Guest details
  - Booking dates
  - Room info
  - Price breakdown
  - GST
  - Payment status
- Add:
  - Logo
  - Proper spacing
  - Clean typography
- Backend: generate structured PDF (use iText7 or QuestPDF)

--------------------------------------------------
🔹 6. ROOM TYPE RATE UI IMPROVEMENT
--------------------------------------------------
URL:
http://localhost:4200/admin/roomtypes

Current:
- Rate exists but UI is not interactive.

Required:
- Add "Set Rate" button/link
- Open modal/dialog
- Clean UI (Angular Material dialog)
- Editable pricing with validation
- Save via API
- Show updated rate instantly

--------------------------------------------------
🔹 7. GENERAL IMPROVEMENTS
--------------------------------------------------
- Ensure all APIs follow consistent response format
- Add proper error handling
- Validate all inputs (frontend + backend)
- Avoid duplicate logic (reuse shared services)
- Optimize performance (lazy loading, pagination)
- Ensure UI responsiveness and clean UX

--------------------------------------------------
🔹 OUTPUT REQUIRED
--------------------------------------------------
Provide:
1. Backend changes (.NET code updates)
2. Frontend fixes (Angular code)
3. API contract updates
4. Any DB changes if needed
5. Step-by-step explanation of fixes
