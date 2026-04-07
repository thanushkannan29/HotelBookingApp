# Thanush StayHub — Presentation Script
**Duration: 25–40 Minutes | Slides + Live Demo**
**Presenter: Thanush Kannan | Software Engineer Trainee · NAF**

---

## PRE-PRESENTATION CHECKLIST

Before they walk in, make sure:

- [ ] `Presentation.html` open in browser — full screen (F11)
- [ ] Angular frontend running locally (`ng serve`)
- [ ] Backend API running (`dotnet run`)
- [ ] Three browser tabs ready: Presentation | Guest login | Admin login
- [ ] SuperAdmin credentials noted
- [ ] Test data already in DB (hotels, rooms, at least one booking)

---

## TIMING GUIDE

| Section | Time |
|---|---|
| Opening + Slides 1–5 | 7 min |
| Live Demo — Guest Role | 10 min |
| Live Demo — Admin Role | 8 min |
| Live Demo — SuperAdmin Role | 7 min |
| Slides 7, 9, 10, 11 | 4 min |
| Closing + Q&A | 5 min |
| **Total** | **~41 min** |

> **If running short (25 min target):** Skip Guest Transactions, Refund Requests, City Management, and SuperAdmin Profile. Core flow is: Guest booking → **AI Chatbot demo** → Admin managing → SuperAdmin controlling.

---

## OPENING — Slide 1: Title
> ⏱ 2 minutes | *Open Presentation.html, full screen*

"Good afternoon. My name is Thanush Kannan, I'm a Software Engineer Trainee at NAF.

Today I'm going to walk you through my final project — **Thanush StayHub** — a full-stack hotel booking platform I built from scratch.

This is not just a demo app. It has real business logic — role-based access, dynamic pricing, wallet payments, automated background jobs, and over 90% test coverage.

I'll use both the presentation slides and the live running application to show you everything."

---

## SLIDE 2 — Project Overview
> ⏱ 2 minutes

"The problem I solved is simple — hotels need a platform to manage rooms, pricing, and reservations, and guests need a clean booking experience.

My solution is a three-role platform:
- A **Guest** who books hotels
- A **Hotel Admin** who manages their property
- A **SuperAdmin** who controls the entire platform

The numbers: 21 database entities, 4 automated background services, and 90-plus percent test coverage."

---

## SLIDE 3 — Tech Stack
> ⏱ 1 minute

"For the frontend I used **Angular 21** with standalone components, Angular Material UI, TypeScript, SCSS with CSS variables, and RxJS with Angular Signals.

For the backend — **ASP.NET Core 8 Web API**, Entity Framework Core 8, SQL Server, JWT authentication, and xUnit with Moq for testing.

The architecture follows Repository Pattern and Unit of Work — which I'll show you briefly."

---

## SLIDE 4 — Architecture
> ⏱ 1 minute

"The request flow is clean and layered. Angular sends HTTP requests, the JWT interceptor attaches the token, the controller handles routing and authorization, the service layer runs the business logic, and the repository handles database access through EF Core.

I also have four background services running on timers — reservation cleanup, no-show auto-cancel, hotel deactivation refunds, and inventory restore. These run automatically without any user action."

---

## SLIDE 5 — User Roles
> ⏱ 1 minute

"Three roles, three completely separate experiences. Let me now switch to the live app and show you each one."

---

## LIVE DEMO — PART 1: GUEST ROLE
> ⏱ 7–8 minutes | *Switch to browser with running Angular app*

### 1. Landing Page
"This is the home page. You can see the hotel listing with the infinite carousel, search by city, and filter by dates. The design uses Playfair Display for headings and DM Sans for body — matching a premium hotel brand feel."

### 2. Register / Login as Guest
"Let me log in as a guest. Notice the JWT token is stored and the auth interceptor attaches it to every request automatically. The route guard prevents access to protected pages without authentication."

### 3. Browse Hotels
"I can browse hotels, see ratings, location, and available room types. Clicking a hotel shows the detail page with room types, amenities, and dynamic pricing."

### 4. Book a Room
"Let me make a booking. I select check-in and check-out dates — the system checks real-time inventory from the RoomTypeInventory table. The price is calculated dynamically from RoomTypeRate date ranges. GST is applied based on the hotel's setting.

I can apply a promo code here — it's single-use, user and hotel scoped. I can also use my wallet balance to reduce the final amount."

### 5. Payment — Razorpay Integration
"I select UPI as the payment method. This opens the **Razorpay payment gateway** — a real payment integration. The guest can pay via UPI, Credit Card, Debit Card, or Net Banking. The Razorpay checkout pre-fills the hotel's UPI ID for UPI payments.

There's a **10-minute payment window** with a live countdown timer. If the guest doesn't pay in time, the reservation is automatically expired and inventory is restored by the background service.

If the guest closes Razorpay without paying, a failed payment is recorded. They can resume the payment later from My Bookings.

Once payment is successful, the transaction is recorded, the reservation is confirmed, and a ReservationCode is generated."

### 6. My Bookings
"In My Bookings I can see all reservations with their status — Pending, Confirmed, Completed, Cancelled. I can cancel here — if I paid the 10% cancellation protection fee, I get a partial refund to my wallet."

### 7. Wallet — Razorpay Top-Up
"The wallet shows my balance and a full credit/debit ledger — every refund and payment is tracked in WalletTransaction.

I can also **top up my wallet using Razorpay** — enter an amount, the Razorpay modal opens, pay via UPI or card, and the balance is credited instantly. At checkout I can use wallet balance to partially or fully cover the booking amount."

### 8. Booking Detail & QR Code
"Clicking any booking opens the detail page — full breakdown of rooms, pricing, GST, promo discount, wallet used, and the final amount. There's also a QR code generated on confirmation that the hotel uses for check-in verification."

### 9. Reviews
"After a completed stay I can write one review per reservation — rating and comment. Writing a review earns ₹100 wallet reward automatically."

### 10. My Promo Codes
"After completing a stay, a promo code is automatically generated — 5% to 25% discount, hotel-specific, valid for 90 days, single-use. I can see all my promo codes here."

### 11. Guest Transactions
"Full transaction history — every payment I made, the method, amount, and status."

### 12. Refund Requests
"If I need a refund outside the normal cancellation flow, I can submit a refund request here. The hotel admin reviews and approves or rejects it. Approved refunds go straight to my wallet."

### 13. Guest Dashboard & Profile
"The guest dashboard shows a summary of my bookings, wallet balance, and recent activity. The profile page lets me update my personal details, address, and profile photo."

### 14. AI Chatbot — Groq + Llama 3.1
"This is one of the standout features — I have a floating AI chatbot powered by the Groq API using the Llama 3.1 model. Let me open it.

The chatbot is role-aware — it loads a different system prompt depending on whether you're a Guest, Admin, SuperAdmin, or not logged in. It knows all the platform policies — cancellation rules, promo code details, wallet usage, booking flow.

It strictly only answers questions about StayHub — if you ask it something off-topic like the weather, it politely refuses. It auto-closes when you navigate to another page, and it formats responses with bold text and bullet points."

### 15. Support Request
"I can submit a support ticket from the Contact page — even without logging in. The SuperAdmin responds and marks it resolved."

### 16. Dark Mode
"Quick demo — I can toggle dark mode. The entire UI switches — Angular Material theme plus all CSS variables update instantly. It's persisted in localStorage."

---

## SLIDE 6 — Dark & Light Mode
> ⏱ 30 seconds | *Switch back to slides*

"This slide shows the exact colour tokens — Indigo primary, Amber accent in light mode. Blue and Amber in dark mode. Both fully themed with Angular Material overrides."

---

## LIVE DEMO — PART 2: HOTEL ADMIN ROLE
> ⏱ 7–8 minutes | *Log in as Admin*

### 1. Admin Dashboard
"The admin dashboard shows revenue charts, occupancy stats, recent reservations, and booking trends — all scoped to this admin's hotel only. They cannot see other hotels."

### 2. Hotel Management
"The admin can edit their hotel profile — name, address, contact, GST percentage, UPI ID for payments, and hotel image."

### 3. Room Types
"Here the admin creates room types — Deluxe, Suite, Standard. Each has a name, max occupancy, description, and image. They assign amenities from the master list."

### 4. Rooms
"Under rooms, they add physical rooms — Room 101, 102 — each linked to a room type and floor."

### 5. Dynamic Pricing
"This is one of the key features — date-range pricing. The admin sets different rates for different periods. Peak season gets a higher rate. The price is locked at booking time in the ReservationRoom table."

### 6. Inventory Management
"Per-date inventory management. The admin sets total rooms available per day. The system tracks reserved vs available. When a booking is cancelled or expires, inventory is restored automatically by the background service."

### 7. Reservations
"The admin sees all reservations for their hotel. They can mark check-in, view guest details, and manage cancellations."

### 8. Reviews
"The admin sees all guest reviews and can post a reply to each one."

### 9. Transactions
"Full transaction history — payment method, amount, status, wallet usage."

### 10. Amenity Requests
"If the admin needs a new amenity that's not in the master list, they submit a request here. The SuperAdmin approves or rejects it."

### 11. Refund Management
"Guests can submit refund requests. The admin sees them here and can approve or reject. Approved refunds are automatically credited to the guest's wallet."

### 12. Bug Reports / Support
"The admin can also submit bug reports or platform issues to the SuperAdmin from the support section."

### 13. Audit Logs
"Every critical change — hotel update, refund approval — is logged here with a JSON diff showing exactly what changed and who changed it."

---

## LIVE DEMO — PART 3: SUPERADMIN ROLE
> ⏱ 5–6 minutes | *Log in as SuperAdmin*

### 1. SuperAdmin Dashboard
"The SuperAdmin sees platform-wide stats — total revenue, total hotels, total bookings, commission earned."

### 2. Hotel Control
"The SuperAdmin can approve new hotels, activate or deactivate them, and block a hotel — which prevents the admin from reactivating it. When a hotel is deactivated, the background service automatically refunds all active bookings to guest wallets."

### 3. Revenue
"Every confirmed booking generates a 2% commission recorded in SuperAdminRevenue. This page shows the full breakdown per hotel and per reservation."

### 4. Amenity Management
"The SuperAdmin manages the global amenity master list — adding, editing, activating amenities that all hotels can use."

### 5. Amenity Requests
"Pending requests from hotel admins appear here. The SuperAdmin can approve — which adds it to the master list — or reject with a note."

### 6. Support Requests
"All support tickets from guests, admins, and even unauthenticated users appear here. The SuperAdmin responds and marks them resolved."

### 7. Audit Logs & Error Logs
"Full platform audit trail and error logs with stack traces, HTTP method, controller, and user info — all captured automatically by the global exception middleware."

### 8. City Management
"The SuperAdmin manages the city list used across the platform for hotel search and filtering. Adding a new city here makes it available in the hotel search dropdown."

### 9. SuperAdmin Profile
"The SuperAdmin can also manage their own profile from the profile section."

---

## SLIDE 7 — Booking Flow
> ⏱ 1 minute | *Switch back to slides*

"To summarise the booking flow — 8 steps from search to check-out. The key technical points are: real-time inventory check, price locked at booking, wallet and promo applied at checkout, QR code generated on confirmation, and inventory automatically restored on cancellation."

---

## SLIDE 9 — Data Model
> ⏱ 1 minute

"The data model has 21 entities across 5 domains — Identity, Hotel, Room, Booking, and Finance. All relationships are enforced via EF Core foreign key constraints."

---

## SLIDE 10 — ER Diagram
> ⏱ 1 minute

"This is the full ER diagram — you can scroll to see all entities and their relationships. The SVG lines show the direction of each association with 1:1, 1:N, and M:N labels.

Key relationships to note:
- User links to Hotel for admins, and to Reservation for guests
- Reservation connects to ReservationRoom which locks the room and price
- RoomType has dynamic rates, per-date inventory, and amenities via a join table
- Every booking generates a Transaction and a SuperAdminRevenue record
- Wallet has a full ledger via WalletTransaction"

---

## SLIDE 11 — Testing
> ⏱ 2 minutes

"Testing was a major focus. I have over 90% coverage using xUnit and Moq.

I tested every service — auth, reservation, wallet, hotel, room type, promo code, amenity requests, reviews, transactions, and revenue.

I also tested all four background services, the global exception middleware, the DbContext configuration, the generic repository, and DTO model validation.

This gives confidence that the business logic is correct and regressions are caught early."

---

## CLOSING — Slide 12: Thank You
> ⏱ 1 minute

"To summarise — Thanush StayHub is a production-ready full-stack application built with Angular 21 and ASP.NET Core 8. It demonstrates clean architecture, real business logic, automated background processing, and comprehensive testing.

I built this entirely during my training period at NAF.

Thank you for your time. I'm happy to answer any questions or dive deeper into any specific part of the code or architecture."

---

## QUICK ANSWERS FOR LIKELY QUESTIONS

**"Why Angular Material?"**
> "It gives a consistent, accessible component library out of the box. I customised it with CSS variables to match the brand theme — indigo primary, amber accent, with full dark mode support."

**"How does JWT work here?"**
> "On login the backend generates a JWT with the user's role. The Angular auth interceptor attaches it as a Bearer token to every API call. The backend validates it and checks the role via `[Authorize(Roles=...)]`."

**"What happens if a hotel is deleted while someone has a booking?"**
> "The HotelDeactivationRefundService background job runs on a timer, finds all active reservations for deactivated hotels, and automatically refunds the amount to the guest's wallet."

**"How is inventory managed?"**
> "RoomTypeInventory has one row per room type per date. On booking, ReservedInventory increments. On cancellation or expiry, the InventoryRestoreHelper decrements it back. Available = Total minus Reserved."

**"Why Repository + Unit of Work?"**
> "It decouples the service layer from EF Core directly, makes unit testing easier with mocks, and ensures multiple DB operations in one request are atomic."

**"How does dynamic pricing work?"**
> "RoomTypeRate stores date ranges with a rate per room type. When a guest selects dates, the system finds the matching rate for each night and calculates the total. The price is then locked in ReservationRoom so future rate changes don't affect existing bookings."

**"What is the cancellation protection fee?"**
> "At booking time the guest can optionally pay 10% of the total as a cancellation protection fee. If they cancel later, they get a partial refund. Without it, cancellation may result in no refund depending on the policy."

**"How does the chatbot work?"**
> "It's built with the Groq API using the Llama 3.1 8B Instant model. I wrote four different system prompts — one for each role. The Guest prompt knows cancellation policies, promo code rules, wallet usage, and booking flow. The Admin prompt knows reservation management and hotel settings. The SuperAdmin prompt knows platform controls. The Public prompt is for unauthenticated visitors. The chatbot strictly refuses to answer anything outside the StayHub platform scope."

**"How does Razorpay work in your project?"**
> "Razorpay is integrated in two places. First, for booking payment — when the guest selects UPI, Card, or NetBanking, the Razorpay checkout modal opens with the hotel's UPI ID pre-filled. The amount is passed in paise (smallest currency unit). On success, the handler calls our backend to record the transaction. Second, for wallet top-up — the guest enters an amount, Razorpay opens, and on success the backend credits the wallet. Both use the Razorpay test key in the environment file."

**"What is the 10-minute payment window?"**
> "When a reservation is created, an ExpiryTime is set 10 minutes in the future. A live countdown timer shows on the payment page. If the guest doesn't pay in time, the ReservationCleanup background service marks it expired and restores the inventory. The guest can also resume an incomplete payment from My Bookings — the app detects the pending reservation and jumps straight to the payment step."

**"Why Groq for the chatbot?"**
> "Groq provides extremely fast inference — the Llama 3.1 model responds in under a second. It's also free-tier friendly for a training project, and the API is OpenAI-compatible so the integration is clean."

**"What is jsPDF used for?"**
> "jsPDF is used to export booking details and transaction summaries as downloadable PDF files — guests can save their booking confirmation as a PDF."

---

## NOTES FOR YOURSELF

- Speak slowly and clearly — don't rush through the live demo
- If something breaks in the demo, stay calm and explain what it should do
- Point to the screen when switching between slides and live app
- Mention "I built this" naturally — own your work
- If they ask to see code, open the service layer — `ReservationService.cs` is a good one to show
- The ER diagram slide is a great visual anchor — spend a moment on it

---

*Good luck tomorrow Thanush — you've built something solid. Present it with confidence.*
