import { TestBed } from '@angular/core/testing';
import {
  HttpTestingController,
  provideHttpClientTesting
} from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { HotelService } from './hotel.service';
import {
  SearchHotelRequestDto,
  UpdateHotelDto
} from '../models/models';

const BASE = environment.apiUrl;

// ── Reusable mock data ────────────────────────────────────────────────────────

const MOCK_HOTEL_LIST_ITEM = {
  hotelId: 'hotel-001',
  name: 'Grand Palace',
  city: 'Chennai',
  imageUrl: 'https://example.com/img.jpg',
  averageRating: 4.5,
  reviewCount: 120,
  startingPrice: 3500
};

const MOCK_HOTEL_DETAILS = {
  hotelId: 'hotel-001',
  name: 'Grand Palace',
  address: '1 MG Road',
  city: 'Chennai',
  description: 'A luxury hotel in the heart of the city.',
  imageUrl: 'https://example.com/img.jpg',
  contactNumber: '9840650390',
  averageRating: 4.5,
  reviewCount: 120,
  amenities: ['WiFi', 'Pool', 'Gym'],
  reviews: [],
  roomTypes: []
};

const MOCK_AVAILABILITY = {
  roomTypeId: 'rt-001',
  roomTypeName: 'Deluxe',
  pricePerNight: 3500,
  availableRooms: 5
};

const MOCK_SA_HOTEL = {
  hotelId: 'hotel-001',
  name: 'Grand Palace',
  city: 'Chennai',
  contactNumber: '9840650390',
  isActive: true,
  isBlockedBySuperAdmin: false,
  createdAt: '2024-01-01T00:00:00Z',
  totalReservations: 200,
  totalRevenue: 700000
};

// ─────────────────────────────────────────────────────────────────────────────

describe('HotelService', () => {
  let service: HotelService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });
    service = TestBed.inject(HotelService);
    http    = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify()); // fails if any unexpected HTTP call was made

  // ── CREATION ───────────────────────────────────────────────────────────────

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  // ── PUBLIC: getTopHotels ───────────────────────────────────────────────────

  it('getTopHotels() — should GET /public/hotels/top and return list', () => {
    service.getTopHotels().subscribe(result => {
      expect(result.length).toBe(2);
      expect(result[0].name).toBe('Grand Palace');
      expect(result[0].averageRating).toBe(4.5);
    });

    const req = http.expectOne(`${BASE}/public/hotels/top`);
    expect(req.request.method).toBe('GET');
    req.flush({
      success: true,
      data: [
        MOCK_HOTEL_LIST_ITEM,
        { ...MOCK_HOTEL_LIST_ITEM, hotelId: 'hotel-002', name: 'Sea View Inn', averageRating: 4.2 }
      ]
    });
  });

  it('getTopHotels() — should return empty array when no hotels exist', () => {
    service.getTopHotels().subscribe(result => {
      expect(result.length).toBe(0);
    });

    http.expectOne(`${BASE}/public/hotels/top`)
        .flush({ success: true, data: [] });
  });

  // ── PUBLIC: getCities ──────────────────────────────────────────────────────

  it('getCities() — should GET /public/hotels/cities and return string array', () => {
    service.getCities().subscribe(result => {
      expect(result.length).toBe(3);
      expect(result).toContain('Chennai');
      expect(result).toContain('Mumbai');
    });

    const req = http.expectOne(`${BASE}/public/hotels/cities`);
    expect(req.request.method).toBe('GET');
    req.flush({ success: true, data: ['Chennai', 'Mumbai', 'Bangalore'] });
  });

  // ── PUBLIC: getHotelsByCity ────────────────────────────────────────────────

  it('getHotelsByCity() — should GET /public/hotels/by-city with city query param', () => {
    service.getHotelsByCity('Chennai').subscribe(result => {
      expect(result.length).toBe(1);
      expect(result[0].city).toBe('Chennai');
    });

    const req = http.expectOne(r => r.url === `${BASE}/public/hotels/by-city`);
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('city')).toBe('Chennai');
    req.flush({ success: true, data: [MOCK_HOTEL_LIST_ITEM] });
  });

  it('getHotelsByCity() — should encode city name correctly in params', () => {
    service.getHotelsByCity('New Delhi').subscribe();

    const req = http.expectOne(r => r.url === `${BASE}/public/hotels/by-city`);
    expect(req.request.params.get('city')).toBe('New Delhi');
    req.flush({ success: true, data: [] });
  });

  // ── PUBLIC: searchHotelsWithFilters ───────────────────────────────────────

  it('searchHotelsWithFilters() — should POST to /public/hotels/search with full request body', () => {
    const searchReq: SearchHotelRequestDto = {
      city: 'Chennai',
      checkIn: '2025-06-01',
      checkOut: '2025-06-03',
      pageNumber: 1,
      pageSize: 10
    };

    service.searchHotelsWithFilters(searchReq).subscribe(result => {
      expect(result.hotels.length).toBe(1);
      expect(result.recordsCount).toBe(1);
      expect(result.pageNumber).toBe(1);
    });

    const req = http.expectOne(`${BASE}/public/hotels/search`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body.city).toBe('Chennai');
    expect(req.request.body.checkIn).toBe('2025-06-01');
    expect(req.request.body.checkOut).toBe('2025-06-03');
    expect(req.request.body.pageNumber).toBe(1);
    req.flush({
      success: true,
      data: { hotels: [MOCK_HOTEL_LIST_ITEM], pageNumber: 1, recordsCount: 1 }
    });
  });

  it('searchHotelsWithFilters() — should pass pageNumber and pageSize in body', () => {
    const searchReq: SearchHotelRequestDto = {
      city: 'Mumbai',
      checkIn: '2025-07-10',
      checkOut: '2025-07-12',
      pageNumber: 2,
      pageSize: 5
    };

    service.searchHotelsWithFilters(searchReq).subscribe();

    const req = http.expectOne(`${BASE}/public/hotels/search`);
    expect(req.request.body.pageNumber).toBe(2);
    expect(req.request.body.pageSize).toBe(5);
    req.flush({ success: true, data: { hotels: [], pageNumber: 2, recordsCount: 0 } });
  });

  // ── PUBLIC: getHotelDetails ────────────────────────────────────────────────

  it('getHotelDetails() — should GET /public/hotels/{id}/full-details', () => {
    service.getHotelDetails('hotel-001').subscribe(result => {
      expect(result.hotelId).toBe('hotel-001');
      expect(result.name).toBe('Grand Palace');
      expect(result.city).toBe('Chennai');
      expect(result.amenities).toContain('WiFi');
    });

    const req = http.expectOne(`${BASE}/public/hotels/hotel-001/full-details`);
    expect(req.request.method).toBe('GET');
    req.flush({ success: true, data: MOCK_HOTEL_DETAILS });
  });

  it('getHotelDetails() — should embed the hotelId correctly in URL', () => {
    service.getHotelDetails('hotel-999').subscribe();

    const req = http.expectOne(`${BASE}/public/hotels/hotel-999/full-details`);
    expect(req.request.method).toBe('GET');
    req.flush({ success: true, data: { ...MOCK_HOTEL_DETAILS, hotelId: 'hotel-999' } });
  });

  // ── PUBLIC: getAvailability ────────────────────────────────────────────────

  it('getAvailability() — should GET /public/hotels/{id}/availability with date params', () => {
    service.getAvailability('hotel-001', '2025-06-01', '2025-06-03').subscribe(result => {
      expect(result.length).toBe(1);
      expect(result[0].roomTypeName).toBe('Deluxe');
      expect(result[0].availableRooms).toBe(5);
      expect(result[0].pricePerNight).toBe(3500);
    });

    const req = http.expectOne(r => r.url === `${BASE}/public/hotels/hotel-001/availability`);
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('checkIn')).toBe('2025-06-01');
    expect(req.request.params.get('checkOut')).toBe('2025-06-03');
    req.flush({ success: true, data: [MOCK_AVAILABILITY] });
  });

  it('getAvailability() — should return fully booked room types (availableRooms = 0)', () => {
    service.getAvailability('hotel-001', '2025-12-24', '2025-12-26').subscribe(result => {
      expect(result[0].availableRooms).toBe(0);
    });

    http.expectOne(r => r.url === `${BASE}/public/hotels/hotel-001/availability`)
        .flush({
          success: true,
          data: [{ ...MOCK_AVAILABILITY, availableRooms: 0 }]
        });
  });

  it('getAvailability() — should embed hotelId in URL path correctly', () => {
    service.getAvailability('hotel-999', '2025-08-01', '2025-08-05').subscribe();

    const req = http.expectOne(r => r.url === `${BASE}/public/hotels/hotel-999/availability`);
    expect(req.request.params.get('checkIn')).toBe('2025-08-01');
    expect(req.request.params.get('checkOut')).toBe('2025-08-05');
    req.flush({ success: true, data: [] });
  });

  // ── ADMIN: updateHotel ─────────────────────────────────────────────────────

  it('updateHotel() — should PUT to /admin/hotels with full dto', () => {
    const dto: UpdateHotelDto = {
      name: 'Grand Palace Updated',
      address: '2 MG Road',
      city: 'Chennai',
      state: 'TN',
      description: 'Newly renovated luxury hotel.',
      contactNumber: '9840650390',
      imageUrl: 'https://example.com/new-img.jpg'
    };

    service.updateHotel(dto).subscribe(result => {
      expect(result).toBeUndefined();
    });

    const req = http.expectOne(`${BASE}/admin/hotels`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body.name).toBe('Grand Palace Updated');
    expect(req.request.body.city).toBe('Chennai');
    expect(req.request.body.imageUrl).toBe('https://example.com/new-img.jpg');
    req.flush({ success: true, message: 'Hotel updated successfully.' });
  });

  // ── ADMIN: toggleHotelStatus ───────────────────────────────────────────────

  it('toggleHotelStatus() — should PATCH /admin/hotels/status with isActive=true', () => {
    service.toggleHotelStatus(true).subscribe(result => {
      expect(result).toBeUndefined();
    });

    const req = http.expectOne(r => r.url === `${BASE}/admin/hotels/status`);
    expect(req.request.method).toBe('PATCH');
    expect(req.request.params.get('isActive')).toBe('true');
    expect(req.request.body).toEqual({});
    req.flush({ success: true, message: 'Hotel status updated successfully.' });
  });

  it('toggleHotelStatus() — should PATCH with isActive=false to deactivate hotel', () => {
    service.toggleHotelStatus(false).subscribe();

    const req = http.expectOne(r => r.url === `${BASE}/admin/hotels/status`);
    expect(req.request.params.get('isActive')).toBe('false');
    req.flush({ success: true, message: 'Hotel status updated successfully.' });
  });

  // ── SUPERADMIN: getAllHotelsForSuperAdmin ──────────────────────────────────

  it('getAllHotelsForSuperAdmin() — should POST /superadmin/hotels/list and return paged list', () => {
    service.getAllHotelsForSuperAdmin().subscribe(result => {
      expect(result.hotels.length).toBe(2);
      expect(result.hotels[0].name).toBe('Grand Palace');
      expect(result.hotels[0].totalRevenue).toBe(700000);
      expect(result.hotels[1].isBlockedBySuperAdmin).toBeTrue();
    });

    const req = http.expectOne(`${BASE}/superadmin/hotels/list`);
    expect(req.request.method).toBe('POST');
    req.flush({
      success: true,
      data: {
        totalCount: 2,
        hotels: [
          MOCK_SA_HOTEL,
          { ...MOCK_SA_HOTEL, hotelId: 'hotel-002', name: 'Blocked Inn', isActive: false, isBlockedBySuperAdmin: true }
        ]
      }
    });
  });

  // ── SUPERADMIN: blockHotel ─────────────────────────────────────────────────

  it('blockHotel() — should PATCH /superadmin/hotels/{id}/block with empty body', () => {
    service.blockHotel('hotel-002').subscribe(result => {
      expect(result).toBeUndefined();
    });

    const req = http.expectOne(`${BASE}/superadmin/hotels/hotel-002/block`);
    expect(req.request.method).toBe('PATCH');
    expect(req.request.body).toEqual({});
    req.flush({ success: true, message: 'Hotel has been blocked.' });
  });

  it('blockHotel() — should embed hotelId correctly in the URL', () => {
    service.blockHotel('hotel-999').subscribe();

    const req = http.expectOne(`${BASE}/superadmin/hotels/hotel-999/block`);
    expect(req.request.method).toBe('PATCH');
    req.flush({ success: true, message: 'Hotel has been blocked.' });
  });

  // ── SUPERADMIN: unblockHotel ───────────────────────────────────────────────

  it('unblockHotel() — should PATCH /superadmin/hotels/{id}/unblock with empty body', () => {
    service.unblockHotel('hotel-002').subscribe(result => {
      expect(result).toBeUndefined();
    });

    const req = http.expectOne(`${BASE}/superadmin/hotels/hotel-002/unblock`);
    expect(req.request.method).toBe('PATCH');
    expect(req.request.body).toEqual({});
    req.flush({ success: true, message: 'Hotel has been unblocked.' });
  });

  it('unblockHotel() — should embed hotelId correctly in the URL', () => {
    service.unblockHotel('hotel-001').subscribe();

    const req = http.expectOne(`${BASE}/superadmin/hotels/hotel-001/unblock`);
    expect(req.request.method).toBe('PATCH');
    req.flush({ success: true, message: 'Hotel has been unblocked.' });
  });
});