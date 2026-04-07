import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { provideNativeDateAdapter } from '@angular/material/core';
import { of, throwError } from 'rxjs';
import { RoomTypeManagementComponent } from './roomtype-management.component';
import { RoomTypeService, AmenityService } from '../../../core/services/api.services';
import { AmenityRequestService } from '../../../core/services/amenity-request.service';
import { ToastService } from '../../../core/services/toast.service';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';

const MOCK_ROOM_TYPES = [
  { roomTypeId: 'rt-001', name: 'Deluxe', description: 'Nice', maxOccupancy: 2, isActive: true, roomCount: 5, amenityList: [{ amenityId: 'a1', name: 'WiFi', iconName: 'wifi' }] },
  { roomTypeId: 'rt-002', name: 'Suite',  description: 'Luxury', maxOccupancy: 4, isActive: false, roomCount: 2, amenityList: [] },
];
const MOCK_AMENITIES = [
  { amenityId: 'a1', name: 'WiFi', category: 'Tech', isActive: true },
  { amenityId: 'a2', name: 'Pool', category: 'Services', isActive: true },
];
const MOCK_RATES = [
  { roomTypeRateId: 'rate-001', roomTypeId: 'rt-001', startDate: '2025-06-01', endDate: '2025-06-30', rate: 1500 }
];

describe('RoomTypeManagementComponent', () => {
  let component: RoomTypeManagementComponent;
  let fixture: ComponentFixture<RoomTypeManagementComponent>;
  let roomTypeSpy: jasmine.SpyObj<RoomTypeService>;
  let amenitySpy: jasmine.SpyObj<AmenityService>;
  let amenityReqSpy: jasmine.SpyObj<AmenityRequestService>;
  let toastSpy: jasmine.SpyObj<ToastService>;

  beforeEach(async () => {
    roomTypeSpy   = jasmine.createSpyObj('RoomTypeService',   ['getRoomTypes', 'addRoomType', 'updateRoomType', 'toggleRoomTypeStatus', 'addRate', 'updateRate', 'getRates']);
    amenitySpy    = jasmine.createSpyObj('AmenityService',    ['getAmenities']);
    amenityReqSpy = jasmine.createSpyObj('AmenityRequestService', ['create']);
    toastSpy      = jasmine.createSpyObj('ToastService',      ['success', 'error']);

    roomTypeSpy.getRoomTypes.and.returnValue(of(MOCK_ROOM_TYPES));
    amenitySpy.getAmenities.and.returnValue(of(MOCK_AMENITIES));
    roomTypeSpy.addRoomType.and.returnValue(of(undefined));
    roomTypeSpy.updateRoomType.and.returnValue(of(undefined));
    roomTypeSpy.toggleRoomTypeStatus.and.returnValue(of(undefined));
    roomTypeSpy.addRate.and.returnValue(of(undefined));
    roomTypeSpy.updateRate.and.returnValue(of(undefined));
    roomTypeSpy.getRates.and.returnValue(of(MOCK_RATES));
    amenityReqSpy.create.and.returnValue(of({} as any));

    await TestBed.configureTestingModule({
      imports: [RoomTypeManagementComponent],
      providers: [
        provideAnimationsAsync(), provideHttpClient(), provideHttpClientTesting(),
        provideRouter([]), provideNativeDateAdapter(),
        { provide: RoomTypeService,      useValue: roomTypeSpy },
        { provide: AmenityService,       useValue: amenitySpy },
        { provide: AmenityRequestService, useValue: amenityReqSpy },
        { provide: ToastService,         useValue: toastSpy },
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(RoomTypeManagementComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => expect(component).toBeTruthy());

  // ── ngOnInit ──────────────────────────────────────────────────────────────

  it('ngOnInit — should call getRoomTypes', () => {
    expect(roomTypeSpy.getRoomTypes).toHaveBeenCalled();
  });

  it('ngOnInit — should call getAmenities', () => {
    expect(amenitySpy.getAmenities).toHaveBeenCalled();
  });

  it('ngOnInit — should populate roomTypes signal', () => {
    expect(component.roomTypes().length).toBe(2);
  });

  it('ngOnInit — should populate amenities signal', () => {
    expect(component.amenities().length).toBe(2);
  });

  // ── Initial state ─────────────────────────────────────────────────────────

  it('isSaving — should start as false', () => expect(component.isSaving()).toBeFalse());
  it('editingId — should start as null', () => expect(component.editingId()).toBeNull());
  it('showAddForm — should start as false', () => expect(component.showAddForm()).toBeFalse());

  // ── add ───────────────────────────────────────────────────────────────────

  it('add — should call addRoomType when form is valid', () => {
    component.addForm.patchValue({ name: 'Standard', maxOccupancy: 2 });
    component.add();
    expect(roomTypeSpy.addRoomType).toHaveBeenCalled();
  });

  it('add — should show success toast', () => {
    component.addForm.patchValue({ name: 'Standard', maxOccupancy: 2 });
    component.add();
    expect(toastSpy.success).toHaveBeenCalledOnceWith('Room type added.');
  });

  it('add — should NOT call service when form is invalid', () => {
    component.add();
    expect(roomTypeSpy.addRoomType).not.toHaveBeenCalled();
  });

  it('add — should mark all touched when form is invalid', () => {
    component.add();
    expect(component.addForm.get('name')?.touched).toBeTrue();
  });

  it('add — should reset isSaving to false on error', () => {
    roomTypeSpy.addRoomType.and.returnValue(throwError(() => new Error('fail')));
    component.addForm.patchValue({ name: 'Standard', maxOccupancy: 2 });
    component.add();
    expect(component.isSaving()).toBeFalse();
  });

  // ── startEdit / saveEdit ──────────────────────────────────────────────────

  it('startEdit — should set editingId', () => {
    component.startEdit(MOCK_ROOM_TYPES[0] as any);
    expect(component.editingId()).toBe('rt-001');
  });

  it('startEdit — should patch editForm', () => {
    component.startEdit(MOCK_ROOM_TYPES[0] as any);
    expect(component.editForm.get('name')?.value).toBe('Deluxe');
  });

  it('saveEdit — should call updateRoomType', () => {
    component.startEdit(MOCK_ROOM_TYPES[0] as any);
    component.saveEdit();
    expect(roomTypeSpy.updateRoomType).toHaveBeenCalled();
  });

  it('saveEdit — should show success toast', () => {
    component.startEdit(MOCK_ROOM_TYPES[0] as any);
    component.saveEdit();
    expect(toastSpy.success).toHaveBeenCalledOnceWith('Room type updated.');
  });

  it('saveEdit — should clear editingId on success', () => {
    component.startEdit(MOCK_ROOM_TYPES[0] as any);
    component.saveEdit();
    expect(component.editingId()).toBeNull();
  });

  // ── toggleStatus ──────────────────────────────────────────────────────────

  it('toggleStatus — should call toggleRoomTypeStatus with inverted isActive', () => {
    component.toggleStatus(MOCK_ROOM_TYPES[0] as any); // isActive: true → false
    expect(roomTypeSpy.toggleRoomTypeStatus).toHaveBeenCalledWith('rt-001', false);
  });

  it('toggleStatus — should show success toast', () => {
    component.toggleStatus(MOCK_ROOM_TYPES[0] as any);
    expect(toastSpy.success).toHaveBeenCalled();
  });

  // ── Rates ─────────────────────────────────────────────────────────────────

  it('toggleRates — should load rates for room type', () => {
    component.toggleRates('rt-001');
    expect(roomTypeSpy.getRates).toHaveBeenCalledWith('rt-001');
  });

  it('toggleRates — should collapse when called twice on same id', () => {
    component.toggleRates('rt-001');
    component.toggleRates('rt-001');
    expect(component.expandedRateId()).toBeNull();
  });

  it('getRatesFor — should return rates for a room type', () => {
    component.ratesMap.set({ 'rt-001': MOCK_RATES as any });
    expect(component.getRatesFor('rt-001').length).toBe(1);
  });

  it('getRatesFor — should return empty array for unknown id', () => {
    expect(component.getRatesFor('unknown')).toEqual([]);
  });

  it('openAddRate — should set showAddRateFor', () => {
    component.openAddRate('rt-001');
    expect(component.showAddRateFor()).toBe('rt-001');
  });

  it('saveAddRate — should call addRate when form is valid', () => {
    component.addRateForm.patchValue({ roomTypeId: 'rt-001', startDate: new Date('2025-07-01'), endDate: new Date('2025-07-31'), rate: 1500 });
    component.addRateForm.get('startDate')?.setErrors(null);
    component.addRateForm.get('endDate')?.setErrors(null);
    component.saveAddRate();
    expect(roomTypeSpy.addRate).toHaveBeenCalled();
  });

  it('saveAddRate — should show success toast', () => {
    component.addRateForm.patchValue({ roomTypeId: 'rt-001', startDate: new Date('2025-07-01'), endDate: new Date('2025-07-31'), rate: 1500 });
    component.addRateForm.get('startDate')?.setErrors(null);
    component.addRateForm.get('endDate')?.setErrors(null);
    component.saveAddRate();
    expect(toastSpy.success).toHaveBeenCalledOnceWith('Rate added.');
  });

  it('saveAddRate — should NOT call service when form is invalid', () => {
    component.saveAddRate();
    expect(roomTypeSpy.addRate).not.toHaveBeenCalled();
  });

  it('startEditRate — should set editingRateId', () => {
    component.startEditRate(MOCK_RATES[0] as any);
    expect(component.editingRateId()).toBe('rate-001');
  });

  it('cancelEditRate — should clear editingRateId', () => {
    component.startEditRate(MOCK_RATES[0] as any);
    component.cancelEditRate();
    expect(component.editingRateId()).toBeNull();
  });

  it('saveEditRate — should call updateRate', () => {
    component.startEditRate(MOCK_RATES[0] as any);
    component.editRateForm.get('startDate')?.setErrors(null);
    component.editRateForm.get('endDate')?.setErrors(null);
    component.saveEditRate('rt-001');
    expect(roomTypeSpy.updateRate).toHaveBeenCalled();
  });

  // ── getAmenityNames ───────────────────────────────────────────────────────

  it('getAmenityNames — should return amenity names joined', () => {
    const result = component.getAmenityNames(MOCK_ROOM_TYPES[0] as any);
    expect(result).toBe('WiFi');
  });

  it('getAmenityNames — should return "—" when no amenities', () => {
    const result = component.getAmenityNames(MOCK_ROOM_TYPES[1] as any);
    expect(result).toBe('—');
  });

  // ── Amenity request ───────────────────────────────────────────────────────

  it('submitAmenityRequest — should call create when form is valid', () => {
    component.amenityReqForm.patchValue({ amenityName: 'Sauna', category: 'Services' });
    component.submitAmenityRequest();
    expect(amenityReqSpy.create).toHaveBeenCalled();
  });

  it('submitAmenityRequest — should show success toast', () => {
    component.amenityReqForm.patchValue({ amenityName: 'Sauna', category: 'Services' });
    component.submitAmenityRequest();
    expect(toastSpy.success).toHaveBeenCalledOnceWith('Amenity request submitted!');
  });

  it('submitAmenityRequest — should NOT call service when form is invalid', () => {
    component.submitAmenityRequest();
    expect(amenityReqSpy.create).not.toHaveBeenCalled();
  });

  // ── onPage ────────────────────────────────────────────────────────────────

  it('onPage — should update currentPage and reload', () => {
    roomTypeSpy.getRoomTypes.calls.reset();
    component.onPage({ pageIndex: 1, pageSize: 10, length: 20 } as any);
    expect(component.currentPage).toBe(2);
    expect(roomTypeSpy.getRoomTypes).toHaveBeenCalled();
  });
});
