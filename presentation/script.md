# Thanush StayHub — Presentation Script
**Presenter:** Thanush Kannan · Software Engineer Trainee · NAF Inc.
**Audience:** Board of Directors & Senior Engineering Heads
**Duration:** ~15–20 minutes (slides) + live demo

---

## Before You Start

- Open `index.html` in Chrome/Edge (full screen with `F` key or F11)
- Have the live app running at `http://localhost:4200`
- Have the backend running at `https://localhost:7208`
- Use `←` `→` arrow keys to navigate slides

---

## SLIDE 1 — Title Slide

> *"Good [morning/afternoon], everyone. Thank you for taking the time today."*

> *"My name is Thanush Kannan, Software Engineer Trainee at NAF. Today I'm presenting Thanush StayHub — a full-stack hotel booking and management platform I built from scratch as part of my training."*

> *"This is a production-ready application — not a prototype. It handles real payment flows, real-time inventory, automated background jobs, and even has an AI chatbot. Let me walk you through it."*

**Pause — let the title slide breathe for 5–8 seconds.**

---

## SLIDE 2 — Problem Statement & Solution

> *"Let's start with the why."*

> *"The hotel industry still runs on fragmented tools. Guests search on one platform, book on another, and call the front desk for cancellations. Hotel managers juggle spreadsheets for room availability and manually process refunds. There's no single source of truth."*

> *"StayHub solves this with one unified platform built around three roles."*

**Point to the three role cards:**

> *"Guests get a complete self-service experience — search, book, pay, cancel, review, and even chat with an AI assistant. Hotel Admins get a full operations dashboard — rooms, rates, inventory, reservations, and financial reports. And the SuperAdmin has platform-wide control — activating hotels, managing amenities, tracking commission revenue."*

> *"Every role has exactly what they need, nothing more."*

---

## SLIDE 3 — Tech Stack & Architecture

> *"Now let me talk about what's under the hood."*

**Frontend:**
> *"The frontend is Angular 21 — using the latest standalone component architecture with Angular Signals for state management. No NgRx complexity. Clean, reactive, and fast."*

**Backend:**
> *"The backend is ASP.NET Core on .NET 10 — the latest release. I implemented a clean layered architecture: Controllers handle HTTP, Services contain all business logic, and a Generic Repository with Unit of Work handles data access. This means zero business logic leaks into controllers."*

**Database:**
> *"SQL Server with EF Core 10. Code-first migrations, Fluent API for constraints, and query splitting to avoid Cartesian product explosions on complex joins."*

**Security:**
> *"JWT Bearer tokens with HMAC-SHA256 password hashing. Role-based guards on the frontend, role-based authorization attributes on every API endpoint. Plus IP rate limiting — 60 requests per minute per IP."*

**Payments:**
> *"Razorpay integration for real payment flows — UPI, cards, net banking. Plus a platform wallet system with top-up, reward points for reviews, and promo code discounts."*

**Background Services:**
> *"Four automated background services run alongside the API. Unpaid reservations auto-cancel after 10 minutes. No-shows are automatically flagged. When a hotel is deactivated, all affected guests are automatically refunded. And inventory is restored on every cancellation."*

> *"These run silently — no manual intervention needed."*

---

## SLIDE 4 — Database Table Relationships

> *"Let me show you the data model."*

> *"The database has 20 tables. The core flow is: a User makes a Reservation, which links to one or more ReservationRooms — that's the bridge table for multi-room bookings. Each room belongs to a RoomType, which has date-based pricing via RoomTypeRates and per-date availability via RoomTypeInventory."*

> *"When a booking is made, the system increments ReservedInventory for every date in the stay. When cancelled, it decrements. This is how we guarantee zero overbooking."*

**Point to the constraints section:**

> *"Key constraints: GUID primary keys throughout — no sequential IDs that can be guessed. Unique constraint on email. Unique constraint on hotel-room number pairs. And critically — a unique constraint on UserId + ReservationId for reviews, enforcing the business rule that one guest gets one review per booking."*

> *"City and State columns are indexed for fast hotel search queries."*

---

## 🖥️ LIVE DEMO — Switch to the App

> *"Now let me show you the actual application."*

**Suggested demo flow (5–8 minutes):**

1. **Home page** — Show the hotel listing, search by city/dates
2. **Hotel details** — Show room types, amenities, availability calendar
3. **Booking flow** — Create a booking, apply promo code, show wallet usage
4. **Payment** — Show Razorpay integration (use test credentials)
5. **Guest dashboard** — Show booking history, wallet balance, review submission
6. **AI Chatbot** — Open the chatbot, ask "What hotels are available in Chennai?" — show role-aware response
7. **Admin dashboard** — Switch to admin, show revenue stats, room management, inventory calendar
8. **SuperAdmin** — Show hotel control panel, amenity management, commission revenue

> *"Everything you just saw — search, booking, payment, refunds, reviews, the AI chatbot — all of it is fully tested."*

---

## SLIDE 5 — Testing Coverage & AI Chatbot

> *"Let me talk about quality."*

**Backend:**
> *"The backend has 96% line coverage and 75% branch coverage — measured with xUnit, Moq, and FluentAssertions. Every service class is tested: AuthService, ReservationService, HotelService, WalletService, all four background services, the repository layer, global exception middleware, and DTO models."*

> *"75% branch coverage on a complex booking system with payment flows, cancellation logic, and automated refunds — that's a strong number."*

**Frontend:**
> *"The frontend has 100% component and service coverage — every Angular component, every service, guards, and interceptors are tested using Jasmine and Karma with Angular's testing utilities and HttpClientTestingModule."*

**AI Chatbot:**
> *"The AI chatbot uses Groq's LLaMA 3.1 model — free, fast inference. It's role-aware: a guest gets booking help, an admin gets operational guidance, a SuperAdmin gets platform-level answers. It maintains conversation history, capped at 6 messages for token efficiency."*

---

## Closing

> *"To summarize — StayHub is a complete, production-ready hotel booking platform. Three roles, real payments, automated operations, AI assistance, and comprehensive test coverage."*

> *"The architecture is clean and scalable. Adding a new feature — say, loyalty points or multi-currency support — would be straightforward because the layers are properly separated."*

> *"I'm happy to answer any questions about the architecture, the business logic, the testing approach, or anything you saw in the demo."*

> *"Thank you."*

---

## Likely Questions & Answers

**Q: How does the inventory system prevent overbooking?**
A: When a reservation is created, the service checks `RoomTypeInventory` for every date in the stay. If `ReservedInventory >= TotalInventory` on any date, the booking is rejected. On confirmation, `ReservedInventory` is incremented atomically via Unit of Work. On cancellation, it's decremented by the background service.

**Q: How secure is the JWT implementation?**
A: Tokens are signed with HS256 using a 32+ character secret key. `MapInboundClaims = false` prevents claim name remapping. Tokens have expiry validation. On 401 responses, the frontend automatically logs the user out. Passwords use HMAC-SHA256 with a random salt per user.

**Q: Why Angular Signals instead of NgRx?**
A: Signals are Angular's built-in reactivity primitive — no extra library, no boilerplate. For this scale of application, signals with computed values give us all the reactivity we need. NgRx would add complexity without benefit here.

**Q: How does the AI chatbot know the user's context?**
A: The system prompt changes based on the user's role. A guest gets a prompt focused on booking help. An admin gets a prompt about hotel operations. The chatbot also receives the last 6 messages of conversation history so it can give contextual follow-up answers.

**Q: What happens if the payment fails after a reservation is created?**
A: The reservation is created in `Pending` status with a 10-minute expiry. The `ReservationCleanupService` background job runs every minute and auto-cancels any pending reservation past its expiry, restoring inventory via `InventoryRestoreHelper`.
