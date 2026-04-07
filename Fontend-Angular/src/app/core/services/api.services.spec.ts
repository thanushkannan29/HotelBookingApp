import { TestBed } from '@angular/core/testing';
import {
  HttpTestingController,
  provideHttpClientTesting
} from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

import {
  TransactionService,
  ReviewService,
  UserService,
  DashboardService,
  AuditLogService,
  LogService,
  RoomTypeService,
  RoomService,
  InventoryService
} from './api.services';

import {
  CreatePaymentDto,
  RefundRequestDto,
  CreateReviewDto,
  UpdateReviewDto,
  GetHotelReviewsRequestDto,
  UpdateUserProfileDto,
  CreateRoomTypeDto,
  UpdateRoomTypeDto,
  CreateRoomTypeRateDto,
  GetRateByDateRequestDto,
  CreateRoomDto,
  UpdateRoomDto,
  CreateInventoryDto,
  UpdateInventoryDto
} from '../models/models';

const BASE = environment.apiUrl;

// ─────────────────────────────────────────────────────────────────────────────
// HELPER: creates a fresh TestBed with mock HTTP for each describe block
// ─────────────────────────────────────────────────────────────────────────────
function setupTestBed() {
  TestBed.configureTestingModule({
    providers: [
      provideHttpClient(),
      provideHttpClientTesting()
    ]
  });
  return {
    http: TestBed.inject(HttpTestingController)
  };
}

// ─────────────────────────────────────────────────────────────────────────────
// TransactionService
// ─────────────────────────────────────────────────────────────────────────────
describe('TransactionService', () => {
  let service: TransactionService;
  let http: HttpTestingController;

  beforeEach(() => {
    ({ http } = setupTestBed());
    service = TestBed.inject(TransactionService);
  });

  afterEach(() => http.verify()); // fails if any unmocked request was made

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('createPayment() — should POST to /transactions and return transaction data', () => {
    const dto: CreatePaymentDto = { reservationId: 'res-001', paymentMethod: 1 };
    const mockTx = {
      transactionId: 'tx-001',
      reservationId: 'res-001',
      amount: 5000,
      paymentMethod: 1,
      status: 2,           // Success
      transactionDate: '2025-01-15T10:00:00Z'
    };

    service.createPayment(dto).subscribe(result => {
      expect(result.transactionId).toBe('tx-001');
      expect(result.status).toBe(2);
      expect(result.amount).toBe(5000);
    });

    const req = http.expectOne(`${BASE}/transactions`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(dto);
    req.flush({ success: true, data: mockTx });
  });

  it('createPayment() — should send correct paymentMethod in body', () => {
    const dto: CreatePaymentDto = { reservationId: 'res-002', paymentMethod: 3 }; // UPI

    service.createPayment(dto).subscribe();

    const req = http.expectOne(`${BASE}/transactions`);
    expect(req.request.body.paymentMethod).toBe(3);
    req.flush({ success: true, data: { transactionId: 'tx-002', amount: 2600, status: 2, paymentMethod: 3, reservationId: 'res-002', transactionDate: '2025-01-15T10:00:00Z' } });
  });

  it('directRefund() — should POST to /transactions/{id}/refund', () => {
    const dto: RefundRequestDto = { reason: 'Duplicate booking' };
    const mockTx = {
      transactionId: 'tx-001',
      reservationId: 'res-001',
      amount: 5000,
      paymentMethod: 1,
      status: 4,           // Refunded
      transactionDate: '2025-01-15T10:00:00Z'
    };

    service.directRefund('tx-001', dto).subscribe(result => {
      expect(result.status).toBe(4); // Refunded
      expect(result.transactionId).toBe('tx-001');
    });

    const req = http.expectOne(`${BASE}/transactions/tx-001/refund`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body.reason).toBe('Duplicate booking');
    req.flush({ success: true, data: mockTx });
  });

  it('getTransactions() — should POST to /transactions/list with page and pageSize in body', () => {
    service.getTransactions(1, 10).subscribe(result => {
      expect(result.totalCount).toBe(5);
      expect(result.transactions.length).toBe(1);
    });

    const req = http.expectOne(`${BASE}/transactions/list`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body.page).toBe(1);
    expect(req.request.body.pageSize).toBe(10);
    req.flush({
      success: true,
      data: {
        totalCount: 5,
        transactions: [{ transactionId: 'tx-001', amount: 5000, status: 2, paymentMethod: 1, reservationId: 'res-001', transactionDate: '2025-01-15T10:00:00Z' }]
      }
    });
  });

  it('getTransactions() — page 2 should send correct page in body', () => {
    service.getTransactions(2, 5).subscribe();

    const req = http.expectOne(`${BASE}/transactions/list`);
    expect(req.request.body.page).toBe(2);
    expect(req.request.body.pageSize).toBe(5);
    req.flush({ success: true, data: { totalCount: 10, transactions: [] } });
  });
});

// ─────────────────────────────────────────────────────────────────────────────
// ReviewService
// ─────────────────────────────────────────────────────────────────────────────
describe('ReviewService', () => {
  let service: ReviewService;
  let http: HttpTestingController;

  beforeEach(() => {
    ({ http } = setupTestBed());
    service = TestBed.inject(ReviewService);
  });

  afterEach(() => http.verify());

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('addReview() — should POST to /reviews with rating and comment', () => {
    const dto: CreateReviewDto = {
      hotelId: 'hotel-001',
      reservationId: 'res-001',
      rating: 5,
      comment: 'Wonderful experience, highly recommend!'
    };
    const mockReview = {
      reviewId: 'rev-001', hotelId: 'hotel-001', userId: 'usr-001',
      userName: 'Alice', reservationId: 'res-001', reservationCode: 'RES-001',
      rating: 5, comment: 'Wonderful experience, highly recommend!',
      createdDate: '2025-01-15T10:00:00Z', contributionPoints: 100
    };

    service.addReview(dto).subscribe(result => {
      expect(result.reviewId).toBe('rev-001');
      expect(result.rating).toBe(5);
      expect(result.comment).toBe('Wonderful experience, highly recommend!');
    });

    const req = http.expectOne(`${BASE}/reviews`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body.hotelId).toBe('hotel-001');
    expect(req.request.body.rating).toBe(5);
    req.flush({ success: true, data: mockReview });
  });

  it('addReview() — should include optional imageUrl when provided', () => {
    const dto: CreateReviewDto = {
      hotelId: 'hotel-001',
      reservationId: 'res-001',
      rating: 4,
      comment: 'Nice view from room',
      imageUrl: 'https://example.com/photo.jpg'
    };

    service.addReview(dto).subscribe();

    const req = http.expectOne(`${BASE}/reviews`);
    expect(req.request.body.imageUrl).toBe('https://example.com/photo.jpg');
    req.flush({ success: true, data: { reviewId: 'rev-002', hotelId: 'hotel-001', userId: 'usr-001', userName: 'Alice', reservationId: 'res-001', reservationCode: 'RES-001', rating: 4, comment: 'Nice view from room', imageUrl: 'https://example.com/photo.jpg', createdDate: '2025-01-15T10:00:00Z', contributionPoints: 80 } });
  });

  it('updateReview() — should PUT to /reviews/{id}', () => {
    const dto: UpdateReviewDto = { rating: 4, comment: 'Updated: Good stay overall' };
    const mockReview = {
      reviewId: 'rev-001', hotelId: 'hotel-001', userId: 'usr-001', userName: 'Alice',
      reservationId: 'res-001', reservationCode: 'RES-001',
      rating: 4, comment: 'Updated: Good stay overall',
      createdDate: '2025-01-15T10:00:00Z', contributionPoints: 80
    };

    service.updateReview('rev-001', dto).subscribe(result => {
      expect(result.rating).toBe(4);
      expect(result.comment).toBe('Updated: Good stay overall');
    });

    const req = http.expectOne(`${BASE}/reviews/rev-001`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body.rating).toBe(4);
    req.flush({ success: true, data: mockReview });
  });

  it('deleteReview() — should DELETE /reviews/{id} and return void', () => {
    service.deleteReview('rev-001').subscribe(result => {
      expect(result).toBeUndefined();
    });

    const req = http.expectOne(`${BASE}/reviews/rev-001`);
    expect(req.request.method).toBe('DELETE');
    req.flush({ success: true, message: 'Review deleted successfully.' });
  });

  it('getHotelReviews() — should POST to /reviews/hotel with pagination', () => {
    const dto: GetHotelReviewsRequestDto = { hotelId: 'hotel-001', page: 1, pageSize: 10 };

    service.getHotelReviews(dto).subscribe(result => {
      expect(result.totalCount).toBe(25);
      expect(result.reviews.length).toBe(0);
    });

    const req = http.expectOne(`${BASE}/reviews/hotel`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body.hotelId).toBe('hotel-001');
    expect(req.request.body.page).toBe(1);
    req.flush({ success: true, data: { totalCount: 25, reviews: [] } });
  });
});

// ─────────────────────────────────────────────────────────────────────────────
// UserService
// ─────────────────────────────────────────────────────────────────────────────
describe('UserService', () => {
  let service: UserService;
  let http: HttpTestingController;

  beforeEach(() => {
    ({ http } = setupTestBed());
    service = TestBed.inject(UserService);
  });

  afterEach(() => http.verify());

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('getProfile() — should GET /user-profile and return user data', () => {
    const mockProfile = {
      userId: 'usr-001', email: 'thanush@test.com', role: 'Guest',
      name: 'Thanush K', phoneNumber: '9840650390',
      address: '12 Anna Nagar', state: 'Tamil Nadu',
      city: 'Chennai', pincode: '600040',
      profileImageUrl: null, createdAt: '2025-01-01T00:00:00Z'
    };

    service.getProfile().subscribe(result => {
      expect(result.name).toBe('Thanush K');
      expect(result.email).toBe('thanush@test.com');
      expect(result.role).toBe('Guest');
      expect(result.city).toBe('Chennai');
    });

    const req = http.expectOne(`${BASE}/user-profile`);
    expect(req.request.method).toBe('GET');
    req.flush({ success: true, data: mockProfile });
  });

  it('updateProfile() — should PUT /user-profile with only changed fields', () => {
    const dto: UpdateUserProfileDto = {
      name: 'Thanush Kumar',
      phoneNumber: '9840650390',
      city: 'Coimbatore'
    };
    const mockUpdated = {
      userId: 'usr-001', email: 'thanush@test.com', role: 'Guest',
      name: 'Thanush Kumar', phoneNumber: '9840650390',
      address: '12 Anna Nagar', state: 'Tamil Nadu',
      city: 'Coimbatore', pincode: '600040',
      profileImageUrl: null, createdAt: '2025-01-01T00:00:00Z'
    };

    service.updateProfile(dto).subscribe(result => {
      expect(result.name).toBe('Thanush Kumar');
      expect(result.city).toBe('Coimbatore');
    });

    const req = http.expectOne(`${BASE}/user-profile`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body.name).toBe('Thanush Kumar');
    expect(req.request.body.city).toBe('Coimbatore');
    req.flush({ success: true, data: mockUpdated });
  });
});

// ─────────────────────────────────────────────────────────────────────────────
// DashboardService
// ─────────────────────────────────────────────────────────────────────────────
describe('DashboardService', () => {
  let service: DashboardService;
  let http: HttpTestingController;

  beforeEach(() => {
    ({ http } = setupTestBed());
    service = TestBed.inject(DashboardService);
  });

  afterEach(() => http.verify());

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('getAdminDashboard() — should GET /dashboard/admin with hotel stats', () => {
    const mockDashboard = {
      hotelId: 'hotel-001', hotelName: 'Grand Palace',
      isActive: true, isBlockedBySuperAdmin: false,
      totalRooms: 20, activeRooms: 18, totalRoomTypes: 3,
      totalReservations: 120, pendingReservations: 5,
      activeReservations: 10, completedReservations: 100, cancelledReservations: 5,
      totalRevenue: 600000, totalReviews: 45, averageRating: 4.3
    };

    service.getAdminDashboard().subscribe(result => {
      expect(result.hotelName).toBe('Grand Palace');
      expect(result.totalRevenue).toBe(600000);
      expect(result.averageRating).toBe(4.3);
      expect(result.isActive).toBeTrue();
    });

    const req = http.expectOne(`${BASE}/dashboard/admin`);
    expect(req.request.method).toBe('GET');
    req.flush({ success: true, data: mockDashboard });
  });

  it('getGuestDashboard() — should GET /dashboard/guest with booking stats', () => {
    const mockDashboard = {
      totalBookings: 8, activeBookings: 2,
      completedBookings: 5, cancelledBookings: 1,
      totalSpent: 40000
    };

    service.getGuestDashboard().subscribe(result => {
      expect(result.totalBookings).toBe(8);
      expect(result.totalSpent).toBe(40000);
    });

    const req = http.expectOne(`${BASE}/dashboard/guest`);
    expect(req.request.method).toBe('GET');
    req.flush({ success: true, data: mockDashboard });
  });

  it('getSuperAdminDashboard() — should GET /dashboard/superadmin with platform stats', () => {
    const mockDashboard = {
      totalHotels: 50, activeHotels: 46, blockedHotels: 4,
      totalUsers: 1200, totalReservations: 5000,
      totalRevenue: 25000000, totalReviews: 800
    };

    service.getSuperAdminDashboard().subscribe(result => {
      expect(result.totalHotels).toBe(50);
      expect(result.blockedHotels).toBe(4);
      expect(result.totalRevenue).toBe(25000000);
    });

    const req = http.expectOne(`${BASE}/dashboard/superadmin`);
    expect(req.request.method).toBe('GET');
    req.flush({ success: true, data: mockDashboard });
  });
});

// ─────────────────────────────────────────────────────────────────────────────
// AuditLogService
// ─────────────────────────────────────────────────────────────────────────────
describe('AuditLogService', () => {
  let service: AuditLogService;
  let http: HttpTestingController;

  beforeEach(() => {
    ({ http } = setupTestBed());
    service = TestBed.inject(AuditLogService);
  });

  afterEach(() => http.verify());

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('getAdminAuditLogs() — should POST to /admin/audit-logs/list with body', () => {
    service.getAdminAuditLogs(1, 20).subscribe(result => {
      expect(result.totalCount).toBe(10);
      expect(result.logs.length).toBe(1);
    });

    const req = http.expectOne(`${BASE}/admin/audit-logs/list`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body.page).toBe(1);
    expect(req.request.body.pageSize).toBe(20);
    req.flush({
      success: true,
      data: {
        totalCount: 10,
        logs: [{ auditLogId: 'al-001', action: 'HotelUpdated', entityName: 'Hotel', entityId: 'hotel-001', changes: '{}', createdAt: '2025-01-10T10:00:00Z' }]
      }
    });
  });

  it('getAllAuditLogs() — should POST to /superadmin/audit-logs/list for page 2', () => {
    service.getAllAuditLogs(2, 20).subscribe(result => {
      expect(result.totalCount).toBe(200);
    });

    const req = http.expectOne(`${BASE}/superadmin/audit-logs/list`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body.page).toBe(2);
    expect(req.request.body.pageSize).toBe(20);
    req.flush({ success: true, data: { totalCount: 200, logs: [] } });
  });
});

// ─────────────────────────────────────────────────────────────────────────────
// LogService
// ─────────────────────────────────────────────────────────────────────────────
describe('LogService', () => {
  let service: LogService;
  let http: HttpTestingController;

  beforeEach(() => {
    ({ http } = setupTestBed());
    service = TestBed.inject(LogService);
  });

  afterEach(() => http.verify());

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('getMyLogs() — should POST to /logs/my-logs with body', () => {
    service.getMyLogs(1, 10).subscribe(result => {
      expect(result.totalCount).toBe(3);
      expect(result.logs.length).toBe(0);
    });

    const req = http.expectOne(`${BASE}/logs/my-logs`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body.page).toBe(1);
    expect(req.request.body.pageSize).toBe(10);
    req.flush({ success: true, data: { totalCount: 3, logs: [] } });
  });

  it('getAllLogs() — should POST to /logs/list with body', () => {
    service.getAllLogs(1, 20).subscribe(result => {
      expect(result.totalCount).toBe(50);
    });

    const req = http.expectOne(`${BASE}/logs/list`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body.page).toBe(1);
    expect(req.request.body.pageSize).toBe(20);
    req.flush({ success: true, data: { totalCount: 50, logs: [] } });
  });
});

// ─────────────────────────────────────────────────────────────────────────────
// RoomTypeService
// ─────────────────────────────────────────────────────────────────────────────
describe('RoomTypeService', () => {
  let service: RoomTypeService;
  let http: HttpTestingController;

  beforeEach(() => {
    ({ http } = setupTestBed());
    service = TestBed.inject(RoomTypeService);
  });

  afterEach(() => http.verify());

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('getRoomTypes() — should POST /admin/roomtypes/list and return list', () => {
    const mockRoomTypes = [
      { roomTypeId: 'rt-001', name: 'Deluxe', description: 'Spacious deluxe room', maxOccupancy: 2, amenities: 'WiFi, AC, TV', isActive: true, roomCount: 5 },
      { roomTypeId: 'rt-002', name: 'Suite', description: 'Luxury suite', maxOccupancy: 4, amenities: 'WiFi, AC, Jacuzzi, Minibar', isActive: true, roomCount: 2 }
    ];

    service.getRoomTypes().subscribe(result => {
      expect(result.length).toBe(2);
      expect(result[0].name).toBe('Deluxe');
      expect(result[1].maxOccupancy).toBe(4);
    });

    const req = http.expectOne(`${BASE}/admin/roomtypes/list`);
    expect(req.request.method).toBe('POST');
    req.flush({ success: true, data: mockRoomTypes });
  });

  it('addRoomType() — should POST to /admin/roomtypes and return void', () => {
    const dto: CreateRoomTypeDto = {
      name: 'Standard', description: 'Basic comfortable room',
      maxOccupancy: 2, amenityIds: []
    };

    service.addRoomType(dto).subscribe(result => {
      expect(result).toBeUndefined();
    });

    const req = http.expectOne(`${BASE}/admin/roomtypes`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body.name).toBe('Standard');
    expect(req.request.body.maxOccupancy).toBe(2);
    req.flush({ success: true, message: 'RoomType added successfully.' });
  });

  it('updateRoomType() — should PUT to /admin/roomtypes with updated data', () => {
    const dto: UpdateRoomTypeDto = {
      roomTypeId: 'rt-001', name: 'Deluxe Plus',
      description: 'Upgraded deluxe', maxOccupancy: 3, amenityIds: ['a1', 'a2']
    };

    service.updateRoomType(dto).subscribe(result => {
      expect(result).toBeUndefined();
    });

    const req = http.expectOne(`${BASE}/admin/roomtypes`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body.roomTypeId).toBe('rt-001');
    expect(req.request.body.name).toBe('Deluxe Plus');
    req.flush({ success: true, message: 'RoomType updated successfully.' });
  });

  it('toggleRoomTypeStatus() — should PATCH with isActive=false to deactivate', () => {
    service.toggleRoomTypeStatus('rt-001', false).subscribe(result => {
      expect(result).toBeUndefined();
    });

    const req = http.expectOne(r => r.url === `${BASE}/admin/roomtypes/rt-001/status`);
    expect(req.request.method).toBe('PATCH');
    expect(req.request.params.get('isActive')).toBe('false');
    req.flush({ success: true, message: 'RoomType status updated.' });
  });

  it('toggleRoomTypeStatus() — should PATCH with isActive=true to activate', () => {
    service.toggleRoomTypeStatus('rt-002', true).subscribe();

    const req = http.expectOne(r => r.url === `${BASE}/admin/roomtypes/rt-002/status`);
    expect(req.request.params.get('isActive')).toBe('true');
    req.flush({ success: true, message: 'RoomType status updated.' });
  });

  it('addRate() — should POST to /admin/roomtypes/rate with date range and rate', () => {
    const dto: CreateRoomTypeRateDto = {
      roomTypeId: 'rt-001',
      startDate: '2025-04-01',
      endDate: '2025-06-30',
      rate: 5500
    };

    service.addRate(dto).subscribe(result => {
      expect(result).toBeUndefined();
    });

    const req = http.expectOne(`${BASE}/admin/roomtypes/rate`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body.rate).toBe(5500);
    expect(req.request.body.startDate).toBe('2025-04-01');
    req.flush({ success: true, message: 'Rate added successfully.' });
  });

  it('getRateByDate() — should POST to /admin/roomtypes/rate-by-date and return decimal', () => {
    const dto: GetRateByDateRequestDto = { roomTypeId: 'rt-001', date: '2025-05-15' };

    service.getRateByDate(dto).subscribe(result => {
      expect(result).toBe(5500);
    });

    const req = http.expectOne(`${BASE}/admin/roomtypes/rate-by-date`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body.roomTypeId).toBe('rt-001');
    req.flush({ success: true, data: 5500 });
  });
});

// ─────────────────────────────────────────────────────────────────────────────
// RoomService
// ─────────────────────────────────────────────────────────────────────────────
describe('RoomService', () => {
  let service: RoomService;
  let http: HttpTestingController;

  beforeEach(() => {
    ({ http } = setupTestBed());
    service = TestBed.inject(RoomService);
  });

  afterEach(() => http.verify());

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('getRooms() — should POST /admin/rooms/list with page and pageSize in body', () => {
    const mockRooms = [
      { roomId: 'r-001', roomNumber: '101', floor: 1, roomTypeId: 'rt-001', roomTypeName: 'Deluxe', isActive: true },
      { roomId: 'r-002', roomNumber: '102', floor: 1, roomTypeId: 'rt-001', roomTypeName: 'Deluxe', isActive: true }
    ];

    service.getRooms(1, 10).subscribe(result => {
      expect(result.length).toBe(2);
      expect(result[0].roomNumber).toBe('101');
      expect(result[1].floor).toBe(1);
    });

    const req = http.expectOne(`${BASE}/admin/rooms/list`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body.page).toBe(1);
    expect(req.request.body.pageSize).toBe(10);
    req.flush({ success: true, data: mockRooms });
  });

  it('addRoom() — should POST to /admin/rooms with room details', () => {
    const dto: CreateRoomDto = { roomNumber: '201', floor: 2, roomTypeId: 'rt-002' };

    service.addRoom(dto).subscribe(result => {
      expect(result).toBeUndefined();
    });

    const req = http.expectOne(`${BASE}/admin/rooms`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body.roomNumber).toBe('201');
    expect(req.request.body.floor).toBe(2);
    expect(req.request.body.roomTypeId).toBe('rt-002');
    req.flush({ success: true, message: 'Room added successfully.' });
  });

  it('updateRoom() — should PUT to /admin/rooms with updated room details', () => {
    const dto: UpdateRoomDto = {
      roomId: 'r-001', roomNumber: '101A', floor: 1, roomTypeId: 'rt-002'
    };

    service.updateRoom(dto).subscribe(result => {
      expect(result).toBeUndefined();
    });

    const req = http.expectOne(`${BASE}/admin/rooms`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body.roomId).toBe('r-001');
    expect(req.request.body.roomNumber).toBe('101A');
    req.flush({ success: true, message: 'Room updated successfully.' });
  });

  it('toggleRoomStatus() — should PATCH /admin/rooms/{id}/status with isActive param', () => {
    service.toggleRoomStatus('r-001', false).subscribe(result => {
      expect(result).toBeUndefined();
    });

    const req = http.expectOne(r => r.url === `${BASE}/admin/rooms/r-001/status`);
    expect(req.request.method).toBe('PATCH');
    expect(req.request.params.get('isActive')).toBe('false');
    req.flush({ success: true, message: 'Room status updated.' });
  });

  it('toggleRoomStatus() — should send isActive=true when activating', () => {
    service.toggleRoomStatus('r-002', true).subscribe();

    const req = http.expectOne(r => r.url === `${BASE}/admin/rooms/r-002/status`);
    expect(req.request.params.get('isActive')).toBe('true');
    req.flush({ success: true, message: 'Room status updated.' });
  });
});

// ─────────────────────────────────────────────────────────────────────────────
// InventoryService
// ─────────────────────────────────────────────────────────────────────────────
describe('InventoryService', () => {
  let service: InventoryService;
  let http: HttpTestingController;

  beforeEach(() => {
    ({ http } = setupTestBed());
    service = TestBed.inject(InventoryService);
  });

  afterEach(() => http.verify());

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('getInventory() — should GET /admin/inventory with roomTypeId, start, end params', () => {
    const mockInventory = [
      { roomTypeInventoryId: 'inv-001', date: '2025-03-01', totalInventory: 10, reservedInventory: 3, available: 7 },
      { roomTypeInventoryId: 'inv-002', date: '2025-03-02', totalInventory: 10, reservedInventory: 5, available: 5 },
      { roomTypeInventoryId: 'inv-003', date: '2025-03-03', totalInventory: 10, reservedInventory: 0, available: 10 }
    ];

    service.getInventory('rt-001', '2025-03-01', '2025-03-03').subscribe(result => {
      expect(result.length).toBe(3);
      expect(result[0].available).toBe(7);
      expect(result[1].reservedInventory).toBe(5);
      expect(result[2].totalInventory).toBe(10);
    });

    const req = http.expectOne(r => r.url === `${BASE}/admin/inventory`);
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('roomTypeId')).toBe('rt-001');
    expect(req.request.params.get('start')).toBe('2025-03-01');
    expect(req.request.params.get('end')).toBe('2025-03-03');
    req.flush({ success: true, data: mockInventory });
  });

  it('addInventory() — should POST to /admin/inventory with date range and count', () => {
    const dto: CreateInventoryDto = {
      roomTypeId: 'rt-001',
      startDate: '2025-07-01',
      endDate: '2025-09-30',
      totalInventory: 12
    };

    service.addInventory(dto).subscribe(result => {
      expect(result).toBeUndefined();
    });

    const req = http.expectOne(`${BASE}/admin/inventory`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body.roomTypeId).toBe('rt-001');
    expect(req.request.body.totalInventory).toBe(12);
    expect(req.request.body.startDate).toBe('2025-07-01');
    req.flush({ success: true, message: 'Inventory added successfully.' });
  });

  it('updateInventory() — should PUT to /admin/inventory with new totalInventory', () => {
    const dto: UpdateInventoryDto = {
      roomTypeInventoryId: 'inv-001',
      totalInventory: 15
    };

    service.updateInventory(dto).subscribe(result => {
      expect(result).toBeUndefined();
    });

    const req = http.expectOne(`${BASE}/admin/inventory`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body.roomTypeInventoryId).toBe('inv-001');
    expect(req.request.body.totalInventory).toBe(15);
    req.flush({ success: true, message: 'Inventory updated successfully.' });
  });

  it('updateInventory() — should send the exact ID and value in request body', () => {
    const dto: UpdateInventoryDto = { roomTypeInventoryId: 'inv-999', totalInventory: 8 };

    service.updateInventory(dto).subscribe();

    const req = http.expectOne(`${BASE}/admin/inventory`);
    expect(req.request.body.roomTypeInventoryId).toBe('inv-999');
    expect(req.request.body.totalInventory).toBe(8);
    req.flush({ success: true, message: 'Inventory updated successfully.' });
  });
});
