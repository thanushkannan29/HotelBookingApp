import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { provideNativeDateAdapter } from '@angular/material/core';
import { ActivatedRoute } from '@angular/router';
import { of } from 'rxjs';
import { HotelDetailsComponent } from './hotel-details.component';
import { HotelService } from '../../../core/services/hotel.service';
import { AuthService } from '../../../core/services/auth.service';
import { HotelDetailsDto, RoomAvailabilityDto } from '../../../core/models/models';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';

// ── Mock data ──────────────────────────────────────────────────────────────────

const MOCK_HOTEL: HotelDetailsDto = {
  hotelId:       'hotel-001',
  name:          'Grand Palace',
  address:       '1 MG Road',
  city:          'Chennai',
  state:         'TN',
  description:   'A luxury hotel in the heart of the city.',
  imageUrl:      'https://example.com/img.jpg',
  contactNumber: '9840650390',
  averageRating: 4.5,
  reviewCount:   120,
  gstPercent:    18,
  amenities:     ['WiFi', 'Pool', 'Gym'],
  reviews: [
    { userName: 'Thanush K', rating: 5, comment: 'Wonderful!', createdDate: '2025-01-10T10:00:00Z' },
    { userName: 'Ravi',      rating: 4, comment: 'Very good.', createdDate: '2025-01-05T10:00:00Z' },
  ],
  roomTypes: [
    { roomTypeId: 'rt-001', name: 'Deluxe', description: 'Spacious', maxOccupancy: 2, amenities: ['WiFi', 'AC'], amenityList: [{ amenityId: 'a1', name: 'WiFi', category: 'Tech', iconName: 'wifi' }, { amenityId: 'a2', name: 'AC', category: 'Room', iconName: 'ac_unit' }] },
    { roomTypeId: 'rt-002', name: 'Suite',  description: 'Luxury',   maxOccupancy: 4, amenities: ['WiFi'],       amenityList: [{ amenityId: 'a1', name: 'WiFi', category: 'Tech', iconName: 'wifi' }] },
  ]
};

const MOCK_AVAILABILITY: RoomAvailabilityDto[] = [
  { roomTypeId: 'rt-001', roomTypeName: 'Deluxe', pricePerNight: 3500, availableRooms: 5 },
  { roomTypeId: 'rt-002', roomTypeName: 'Suite',  pricePerNight: 7000, availableRooms: 2 },
];

// ─────────────────────────────────────────────────────────────────────────────

describe('HotelDetailsComponent', () => {
  let component: HotelDetailsComponent;
  let fixture:   ComponentFixture<HotelDetailsComponent>;

  let hotelSpy: jasmine.SpyObj<HotelService>;
  let authSpy:  jasmine.SpyObj<AuthService>;

  beforeEach(async () => {
    hotelSpy = jasmine.createSpyObj('HotelService', ['getHotelDetails', 'getAvailability']);
    authSpy  = jasmine.createSpyObj('AuthService', ['isGuest', 'isAuthenticated'], {
      currentUser: () => ({ userId: 'usr-001', userName: 'Thanush K', role: 'Guest' })
    });

    hotelSpy.getHotelDetails.and.returnValue(of(MOCK_HOTEL));
    hotelSpy.getAvailability.and.returnValue(of(MOCK_AVAILABILITY));
    authSpy.isGuest.and.returnValue(true);
    authSpy.isAuthenticated.and.returnValue(true);

    await TestBed.configureTestingModule({
      imports: [HotelDetailsComponent],
      providers: [
        provideAnimationsAsync(),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        provideNativeDateAdapter(),
        { provide: HotelService,  useValue: hotelSpy },
        { provide: AuthService,   useValue: authSpy  },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: { get: () => 'hotel-001' } } }
        },
      ]
    }).compileComponents();

    fixture   = TestBed.createComponent(HotelDetailsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  // ── CREATION ───────────────────────────────────────────────────────────────

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  // ── CONSTANTS ──────────────────────────────────────────────────────────────

  it('stars — should be [1, 2, 3, 4, 5]', () => {
    expect(component.stars).toEqual([1, 2, 3, 4, 5]);
  });

  // ── INITIAL STATE ──────────────────────────────────────────────────────────

  it('isLoadingAvail — should be false after availability loads', () => {
    expect(component.isLoadingAvail()).toBeFalse();
  });

  it('hotelId — should be set from route param', () => {
    expect(component.hotelId).toBe('hotel-001');
  });

  // ── ngOnInit ───────────────────────────────────────────────────────────────

  it('ngOnInit — should call getHotelDetails with route hotelId', () => {
    expect(hotelSpy.getHotelDetails).toHaveBeenCalledOnceWith('hotel-001');
  });

  it('ngOnInit — should populate hotel signal', () => {
    expect(component.hotel()).not.toBeNull();
    expect(component.hotel()?.name).toBe('Grand Palace');
    expect(component.hotel()?.city).toBe('Chennai');
  });

  it('ngOnInit — should load availability with default dates', () => {
    expect(hotelSpy.getAvailability).toHaveBeenCalled();
    const args = hotelSpy.getAvailability.calls.mostRecent().args;
    expect(args[0]).toBe('hotel-001');
    // checkIn and checkOut should be date strings (YYYY-MM-DD)
    expect(args[1]).toMatch(/^\d{4}-\d{2}-\d{2}$/);
    expect(args[2]).toMatch(/^\d{4}-\d{2}-\d{2}$/);
  });

  it('ngOnInit — should populate availability signal', () => {
    expect(component.availability().length).toBe(2);
    expect(component.availability()[0].roomTypeName).toBe('Deluxe');
  });

  it('ngOnInit — should patch dateForm with today and today+2', () => {
    const ci = component.dateForm.get('checkIn')?.value as Date | null;
    const co = component.dateForm.get('checkOut')?.value as Date | null;
    expect(ci).not.toBeNull();
    expect(co).not.toBeNull();
    // checkIn = tomorrow, checkOut = today+2, so diff = 1 day
    const diffDays = Math.round((co!.getTime() - ci!.getTime()) / 86400000);
    expect(diffDays).toBe(1);
  });

  it('ngOnInit — should use empty string hotelId when route param is null', async () => {
    hotelSpy.getHotelDetails.and.returnValue(of(MOCK_HOTEL));
    hotelSpy.getAvailability.and.returnValue(of([]));

    await TestBed.resetTestingModule();
    await TestBed.configureTestingModule({
      imports: [HotelDetailsComponent],
      providers: [
        provideAnimationsAsync(),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        provideNativeDateAdapter(),
        { provide: HotelService, useValue: hotelSpy },
        { provide: AuthService,  useValue: authSpy  },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: { get: () => null } } } },
      ]
    }).compileComponents();

    const f   = TestBed.createComponent(HotelDetailsComponent);
    const cmp = f.componentInstance;
    f.detectChanges();

    expect(cmp.hotelId).toBe('');
  });

  // ── AVAILABILITY DEDUPLICATION ─────────────────────────────────────────────

  it('should deduplicate availability by roomTypeId keeping minimum availableRooms', () => {
    const duplicated: RoomAvailabilityDto[] = [
      { roomTypeId: 'rt-001', roomTypeName: 'Deluxe', pricePerNight: 3500, availableRooms: 5 },
      { roomTypeId: 'rt-001', roomTypeName: 'Deluxe', pricePerNight: 3500, availableRooms: 3 }, // lower
      { roomTypeId: 'rt-002', roomTypeName: 'Suite',  pricePerNight: 7000, availableRooms: 2 },
    ];
    hotelSpy.getAvailability.and.returnValue(of(duplicated));

    const ci = new Date('2025-06-01');
    const co = new Date('2025-06-03');
    component['loadAvailability'](ci, co);

    const avail = component.availability();
    expect(avail.length).toBe(2);                      // deduplicated
    const deluxe = avail.find(a => a.roomTypeId === 'rt-001');
    expect(deluxe?.availableRooms).toBe(3);            // kept minimum
  });

  // ── checkOutMin GETTER ─────────────────────────────────────────────────────

  it('checkOutMin — should return today when checkIn is null', () => {
    component.dateForm.patchValue({ checkIn: null });
    expect(component.checkOutMin).toBeInstanceOf(Date);
  });

  it('checkOutMin — should return checkIn + 1 day when checkIn is set', () => {
    // Use a future date so checkIn+1 > today
    const future = new Date();
    future.setFullYear(future.getFullYear() + 1);
    future.setMonth(5); future.setDate(10); // June 10 next year
    component.dateForm.patchValue({ checkIn: future });
    const min = component.checkOutMin;
    expect(min.getDate()).toBe(11);
    expect(min.getMonth()).toBe(5);
  });

  // ── checkInStr / checkOutStr GETTERS ───────────────────────────────────────

  it('checkInStr — should return empty string when checkIn is null', () => {
    component.dateForm.patchValue({ checkIn: null });
    expect(component.checkInStr).toBe('');
  });

  it('checkInStr — should return ISO date string when checkIn is set', () => {
    component.dateForm.patchValue({ checkIn: new Date('2025-06-01') });
    expect(component.checkInStr).toBe('2025-06-01');
  });

  it('checkOutStr — should return empty string when checkOut is null', () => {
    component.dateForm.patchValue({ checkOut: null });
    expect(component.checkOutStr).toBe('');
  });

  it('checkOutStr — should return ISO date string when checkOut is set', () => {
    component.dateForm.patchValue({ checkOut: new Date('2025-06-03') });
    expect(component.checkOutStr).toBe('2025-06-03');
  });

  // ── totalNights GETTER ─────────────────────────────────────────────────────

  it('totalNights — should return 0 when dates are not set', () => {
    component.dateForm.patchValue({ checkIn: null, checkOut: null });
    expect(component.totalNights).toBe(0);
  });

  it('totalNights — should return 2 for a 2-night stay', () => {
    component.dateForm.patchValue({
      checkIn:  new Date('2025-06-01'),
      checkOut: new Date('2025-06-03'),
    });
    expect(component.totalNights).toBe(2);
  });

  it('totalNights — should return 7 for a week-long stay', () => {
    component.dateForm.patchValue({
      checkIn:  new Date('2025-06-01'),
      checkOut: new Date('2025-06-08'),
    });
    expect(component.totalNights).toBe(7);
  });

  it('totalNights — should return 0 when checkOut equals checkIn', () => {
    const d = new Date('2025-06-01');
    component.dateForm.patchValue({ checkIn: d, checkOut: d });
    expect(component.totalNights).toBe(0);
  });

  it('totalNights — should return 0 when checkOut is before checkIn', () => {
    component.dateForm.patchValue({
      checkIn:  new Date('2025-06-05'),
      checkOut: new Date('2025-06-01'),
    });
    expect(component.totalNights).toBe(0);
  });

  // ── DATE CHANGE TRIGGERS RELOAD ────────────────────────────────────────────

  it('should reload availability when checkIn date changes to a valid range', () => {
    hotelSpy.getAvailability.calls.reset();

    component.dateForm.patchValue({
      checkIn:  new Date('2025-07-01'),
      checkOut: new Date('2025-07-03'),
    });

    expect(hotelSpy.getAvailability).toHaveBeenCalled();
  });

  it('should NOT reload availability when checkOut is before checkIn', () => {
    // Patch both dates atomically then reset spy — patchValue triggers valueChanges per field
    // First set both values without emitting, then verify the guard works
    component.dateForm.patchValue({
      checkIn:  new Date('2025-07-10'),
      checkOut: new Date('2025-07-01'),
    }, { emitEvent: false });

    hotelSpy.getAvailability.calls.reset();

    // Manually trigger a value change — the guard should block since co < ci
    component.dateForm.get('checkOut')?.setValue(new Date('2025-07-01'));

    expect(hotelSpy.getAvailability).not.toHaveBeenCalled();
  });

  // ── TEMPLATE RENDERS ───────────────────────────────────────────────────────

  it('should display hotel name in the template', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    expect(fixture.nativeElement.textContent).toContain('Grand Palace');
  });

  it('should display hotel city in the template', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    expect(fixture.nativeElement.textContent).toContain('Chennai');
  });
});