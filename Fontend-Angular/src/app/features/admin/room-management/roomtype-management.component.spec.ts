import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { provideNativeDateAdapter } from '@angular/material/core';
import { of, throwError, Subject } from 'rxjs';
import { RoomTypeManagementComponent } from './roomtype-management.component';
import { RoomTypeService } from '../../../core/services/api.services';
import { ToastService } from '../../../core/services/toast.service';
import { RoomTypeListDto } from '../../../core/models/models';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';

// ── Mock data ──────────────────────────────────────────────────────────────────

const MOCK_ROOM_TYPES: RoomTypeListDto[] = [
  { roomTypeId: 'rt-001', name: 'Deluxe',   description: 'Spacious deluxe room', maxOccupancy: 2, amenities: 'WiFi,AC,TV',         isActive: true,  roomCount: 5 },
  { roomTypeId: 'rt-002', name: 'Suite',    description: 'Luxury suite',          maxOccupancy: 4, amenities: 'WiFi,AC,Jacuzzi',    isActive: true,  roomCount: 2 },
  { roomTypeId: 'rt-003', name: 'Standard', description: 'Basic room',            maxOccupancy: 2, amenities: 'WiFi',               isActive: false, roomCount: 8 },
];

// ─────────────────────────────────────────────────────────────────────────────

describe('RoomTypeManagementComponent', () => {
  let component: RoomTypeManagementComponent;
  let fixture:   ComponentFixture<RoomTypeManagementComponent>;

  let roomTypeSpy: jasmine.SpyObj<RoomTypeService>;
  let toastSpy:    jasmine.SpyObj<ToastService>;

  beforeEach(async () => {
    roomTypeSpy = jasmine.createSpyObj('RoomTypeService', [
      'getRoomTypes', 'addRoomType', 'updateRoomType',
      'toggleRoomTypeStatus', 'addRate'
    ]);
    toastSpy = jasmine.createSpyObj('ToastService', ['success', 'error']);

    // Default happy-path responses
    roomTypeSpy.getRoomTypes.and.returnValue(of(MOCK_ROOM_TYPES));
    roomTypeSpy.addRoomType.and.returnValue(of(undefined));
    roomTypeSpy.updateRoomType.and.returnValue(of(undefined));
    roomTypeSpy.toggleRoomTypeStatus.and.returnValue(of(undefined));
    roomTypeSpy.addRate.and.returnValue(of(undefined));

    await TestBed.configureTestingModule({
      imports: [RoomTypeManagementComponent],
      providers: [
        provideAnimationsAsync(),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        provideNativeDateAdapter(),           // required by MatDatepicker
        { provide: RoomTypeService, useValue: roomTypeSpy },
        { provide: ToastService,    useValue: toastSpy    },
      ]
    }).compileComponents();

    fixture   = TestBed.createComponent(RoomTypeManagementComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  // ── CREATION ───────────────────────────────────────────────────────────────

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  // ── INITIAL SIGNAL STATE ───────────────────────────────────────────────────

  it('showAddForm — should start as false', () => {
    expect(component.showAddForm()).toBeFalse();
  });

  it('editingId — should start as null', () => {
    expect(component.editingId()).toBeNull();
  });

  it('isSaving — should start as false', () => {
    expect(component.isSaving()).toBeFalse();
  });

  it('showRateForm — should start as null', () => {
    expect(component.showRateForm()).toBeNull();
  });

  // ── ngOnInit / load() ──────────────────────────────────────────────────────

  it('ngOnInit — should call getRoomTypes on startup', () => {
    expect(roomTypeSpy.getRoomTypes).toHaveBeenCalledOnceWith();
  });

  it('load() — should populate roomTypes signal with API response', () => {
    expect(component.roomTypes().length).toBe(3);
    expect(component.roomTypes()[0].name).toBe('Deluxe');
    expect(component.roomTypes()[2].isActive).toBeFalse();
  });

  // ── FORM INITIAL STATE ─────────────────────────────────────────────────────

  it('addForm — should be invalid initially', () => {
    expect(component.addForm.invalid).toBeTrue();
  });

  it('addForm — maxOccupancy should default to 2', () => {
    expect(component.addForm.get('maxOccupancy')?.value).toBe(2);
  });

  it('rateForm — should be invalid initially', () => {
    expect(component.rateForm.invalid).toBeTrue();
  });

  it('rateForm — rate should default to 0', () => {
    expect(component.rateForm.get('rate')?.value).toBe(0);
  });

  // ── FORM VALIDATION — addForm ──────────────────────────────────────────────

  it('addForm — should be valid when name and required fields are filled', () => {
    component.addForm.patchValue({ name: 'Deluxe', maxOccupancy: 2 });
    expect(component.addForm.valid).toBeTrue();
  });

  it('addForm — should be invalid when name is empty', () => {
    component.addForm.patchValue({ name: '', maxOccupancy: 2 });
    expect(component.addForm.invalid).toBeTrue();
  });

  it('addForm — should be invalid when maxOccupancy is 0', () => {
    component.addForm.patchValue({ name: 'Suite', maxOccupancy: 0 });
    expect(component.addForm.get('maxOccupancy')?.invalid).toBeTrue();
  });

  it('addForm — description and amenities are optional', () => {
    component.addForm.patchValue({ name: 'Suite', maxOccupancy: 4, description: '', amenities: '' });
    expect(component.addForm.valid).toBeTrue();
  });

  // ── FORM VALIDATION — rateForm ─────────────────────────────────────────────

  it('rateForm — should be valid when all fields are filled correctly', () => {
    component.rateForm.patchValue({
      roomTypeId: 'rt-001',
      startDate:  new Date('2025-07-01'),
      endDate:    new Date('2025-09-30'),
      rate:       5000,
    });
    expect(component.rateForm.valid).toBeTrue();
  });

  it('rateForm — should be invalid when rate is 0', () => {
    component.rateForm.patchValue({
      roomTypeId: 'rt-001',
      startDate:  new Date('2025-07-01'),
      endDate:    new Date('2025-09-30'),
      rate:       0,
    });
    expect(component.rateForm.get('rate')?.invalid).toBeTrue();
  });

  it('rateForm — should be invalid when startDate is missing', () => {
    component.rateForm.patchValue({
      roomTypeId: 'rt-001',
      startDate:  null,
      endDate:    new Date('2025-09-30'),
      rate:       5000,
    });
    expect(component.rateForm.invalid).toBeTrue();
  });

  // ── add() — HAPPY PATH ─────────────────────────────────────────────────────

  it('add() — should call addRoomType with form values', () => {
    component.addForm.patchValue({ name: 'Standard', description: 'Basic', maxOccupancy: 2, amenities: 'WiFi' });

    component.add();

    expect(roomTypeSpy.addRoomType).toHaveBeenCalledOnceWith(
      jasmine.objectContaining({ name: 'Standard', maxOccupancy: 2 })
    );
  });

  it('add() — should show success toast on success', () => {
    component.addForm.patchValue({ name: 'Standard', maxOccupancy: 2 });

    component.add();

    expect(toastSpy.success).toHaveBeenCalledOnceWith('Room type added.');
  });

  it('add() — should hide add form after success', () => {
    component.showAddForm.set(true);
    component.addForm.patchValue({ name: 'Standard', maxOccupancy: 2 });

    component.add();

    expect(component.showAddForm()).toBeFalse();
  });

  it('add() — should reset form with maxOccupancy defaulting to 2 after success', () => {
    component.addForm.patchValue({ name: 'Standard', maxOccupancy: 4 });

    component.add();

    expect(component.addForm.get('name')?.value).toBeFalsy();
    expect(component.addForm.get('maxOccupancy')?.value).toBe(2);
  });

  it('add() — should reload room types after success', () => {
    component.addForm.patchValue({ name: 'Standard', maxOccupancy: 2 });
    roomTypeSpy.getRoomTypes.calls.reset();

    component.add();

    expect(roomTypeSpy.getRoomTypes).toHaveBeenCalled();
  });

  it('add() — should reset isSaving to false on success', () => {
    component.addForm.patchValue({ name: 'Standard', maxOccupancy: 2 });

    component.add();

    expect(component.isSaving()).toBeFalse();
  });

  it('add() — should set isSaving to true during in-flight request', () => {
    const subject = new Subject<void>();
    roomTypeSpy.addRoomType.and.returnValue(subject.asObservable());
    component.addForm.patchValue({ name: 'Standard', maxOccupancy: 2 });

    component.add();

    expect(component.isSaving()).toBeTrue();

    subject.next();
    subject.complete();
  });

  // ── add() — INVALID FORM ───────────────────────────────────────────────────

  it('add() — should NOT call service when addForm is invalid', () => {
    component.add();
    expect(roomTypeSpy.addRoomType).not.toHaveBeenCalled();
  });

  it('add() — should mark all fields touched when form is invalid', () => {
    component.add();
    expect(component.addForm.get('name')?.touched).toBeTrue();
    expect(component.addForm.get('maxOccupancy')?.touched).toBeTrue();
  });

  // ── add() — ERROR ──────────────────────────────────────────────────────────

  it('add() — should reset isSaving to false on API error', () => {
    roomTypeSpy.addRoomType.and.returnValue(throwError(() => new Error('fail')));
    component.addForm.patchValue({ name: 'Standard', maxOccupancy: 2 });

    component.add();

    expect(component.isSaving()).toBeFalse();
  });

  it('add() — should NOT show success toast on API error', () => {
    roomTypeSpy.addRoomType.and.returnValue(throwError(() => new Error('fail')));
    component.addForm.patchValue({ name: 'Standard', maxOccupancy: 2 });

    component.add();

    expect(toastSpy.success).not.toHaveBeenCalled();
  });

  // ── startEdit() ────────────────────────────────────────────────────────────

  it('startEdit() — should set editingId to the room type ID', () => {
    component.startEdit(MOCK_ROOM_TYPES[0]);
    expect(component.editingId()).toBe('rt-001');
  });

  it('startEdit() — should patch editForm with all room type values', () => {
    component.startEdit(MOCK_ROOM_TYPES[0]);
    expect(component.editForm.get('roomTypeId')?.value).toBe('rt-001');
    expect(component.editForm.get('name')?.value).toBe('Deluxe');
    expect(component.editForm.get('maxOccupancy')?.value).toBe(2);
    expect(component.editForm.get('amenities')?.value).toBe('WiFi,AC,TV');
  });

  it('startEdit() — switching room types updates editingId and form', () => {
    component.startEdit(MOCK_ROOM_TYPES[0]);
    component.startEdit(MOCK_ROOM_TYPES[1]);
    expect(component.editingId()).toBe('rt-002');
    expect(component.editForm.get('name')?.value).toBe('Suite');
    expect(component.editForm.get('maxOccupancy')?.value).toBe(4);
  });

  // ── saveEdit() — HAPPY PATH ────────────────────────────────────────────────

  it('saveEdit() — should call updateRoomType with editForm values', () => {
    component.startEdit(MOCK_ROOM_TYPES[0]);

    component.saveEdit();

    expect(roomTypeSpy.updateRoomType).toHaveBeenCalledOnceWith(
      jasmine.objectContaining({ roomTypeId: 'rt-001', name: 'Deluxe' })
    );
  });

  it('saveEdit() — should show success toast on success', () => {
    component.startEdit(MOCK_ROOM_TYPES[0]);

    component.saveEdit();

    expect(toastSpy.success).toHaveBeenCalledOnceWith('Room type updated.');
  });

  it('saveEdit() — should clear editingId after success', () => {
    component.startEdit(MOCK_ROOM_TYPES[0]);

    component.saveEdit();

    expect(component.editingId()).toBeNull();
  });

  it('saveEdit() — should reload room types after success', () => {
    component.startEdit(MOCK_ROOM_TYPES[0]);
    roomTypeSpy.getRoomTypes.calls.reset();

    component.saveEdit();

    expect(roomTypeSpy.getRoomTypes).toHaveBeenCalled();
  });

  it('saveEdit() — should reset isSaving to false on success', () => {
    component.startEdit(MOCK_ROOM_TYPES[0]);

    component.saveEdit();

    expect(component.isSaving()).toBeFalse();
  });

  it('saveEdit() — should set isSaving to true during in-flight request', () => {
    const subject = new Subject<void>();
    roomTypeSpy.updateRoomType.and.returnValue(subject.asObservable());
    component.startEdit(MOCK_ROOM_TYPES[0]);

    component.saveEdit();

    expect(component.isSaving()).toBeTrue();

    subject.next();
    subject.complete();
  });

  // ── saveEdit() — INVALID / ERROR ───────────────────────────────────────────

  it('saveEdit() — should NOT call service when editForm is invalid', () => {
    component.saveEdit();
    expect(roomTypeSpy.updateRoomType).not.toHaveBeenCalled();
  });

  it('saveEdit() — should reset isSaving to false on API error', () => {
    roomTypeSpy.updateRoomType.and.returnValue(throwError(() => new Error('fail')));
    component.startEdit(MOCK_ROOM_TYPES[0]);

    component.saveEdit();

    expect(component.isSaving()).toBeFalse();
  });

  it('saveEdit() — should NOT show success toast on API error', () => {
    roomTypeSpy.updateRoomType.and.returnValue(throwError(() => new Error('fail')));
    component.startEdit(MOCK_ROOM_TYPES[0]);

    component.saveEdit();

    expect(toastSpy.success).not.toHaveBeenCalled();
  });

  // ── toggleStatus() ─────────────────────────────────────────────────────────

  it('toggleStatus() — should call toggleRoomTypeStatus with inverted isActive', () => {
    component.toggleStatus(MOCK_ROOM_TYPES[0]); // isActive: true → pass false
    expect(roomTypeSpy.toggleRoomTypeStatus).toHaveBeenCalledOnceWith('rt-001', false);
  });

  it('toggleStatus() — should pass true when room type is currently inactive', () => {
    component.toggleStatus(MOCK_ROOM_TYPES[2]); // isActive: false → pass true
    expect(roomTypeSpy.toggleRoomTypeStatus).toHaveBeenCalledOnceWith('rt-003', true);
  });

  it('toggleStatus() — should show "activated" toast when activating inactive type', () => {
    component.toggleStatus(MOCK_ROOM_TYPES[2]); // isActive: false → activating
    expect(toastSpy.success).toHaveBeenCalledOnceWith('Room type activated.');
  });

  it('toggleStatus() — should show "deactivated" toast when deactivating active type', () => {
    component.toggleStatus(MOCK_ROOM_TYPES[0]); // isActive: true → deactivating
    expect(toastSpy.success).toHaveBeenCalledOnceWith('Room type deactivated.');
  });

  it('toggleStatus() — should flip isActive in roomTypes signal for toggled type', () => {
    component.toggleStatus(MOCK_ROOM_TYPES[0]); // was true → becomes false

    const updated = component.roomTypes().find(r => r.roomTypeId === 'rt-001');
    expect(updated?.isActive).toBeFalse();
  });

  it('toggleStatus() — should activate an inactive room type in the signal', () => {
    component.toggleStatus(MOCK_ROOM_TYPES[2]); // was false → becomes true

    const updated = component.roomTypes().find(r => r.roomTypeId === 'rt-003');
    expect(updated?.isActive).toBeTrue();
  });

  it('toggleStatus() — should NOT change other room types in the signal', () => {
    component.toggleStatus(MOCK_ROOM_TYPES[0]); // only rt-001 changes

    const rt2 = component.roomTypes().find(r => r.roomTypeId === 'rt-002');
    const rt3 = component.roomTypes().find(r => r.roomTypeId === 'rt-003');
    expect(rt2?.isActive).toBe(MOCK_ROOM_TYPES[1].isActive);
    expect(rt3?.isActive).toBe(MOCK_ROOM_TYPES[2].isActive);
  });

  // ── openRateForm() ─────────────────────────────────────────────────────────

  it('openRateForm() — should set showRateForm to the room type ID', () => {
    component.openRateForm('rt-001');
    expect(component.showRateForm()).toBe('rt-001');
  });

  it('openRateForm() — should patch rateForm roomTypeId', () => {
    component.openRateForm('rt-002');
    expect(component.rateForm.get('roomTypeId')?.value).toBe('rt-002');
  });

  it('openRateForm() — switching to a different room type updates both signal and form', () => {
    component.openRateForm('rt-001');
    component.openRateForm('rt-003');
    expect(component.showRateForm()).toBe('rt-003');
    expect(component.rateForm.get('roomTypeId')?.value).toBe('rt-003');
  });

  // ── addRate() — HAPPY PATH ─────────────────────────────────────────────────

  it('addRate() — should call addRate service with formatted date strings', () => {
    component.openRateForm('rt-001');
    component.rateForm.patchValue({
      roomTypeId: 'rt-001',
      startDate:  new Date('2025-07-01'),
      endDate:    new Date('2025-09-30'),
      rate:       5500,
    });

    component.addRate();

    expect(roomTypeSpy.addRate).toHaveBeenCalledOnceWith({
      roomTypeId: 'rt-001',
      startDate:  '2025-07-01',
      endDate:    '2025-09-30',
      rate:       5500,
    });
  });

  it('addRate() — should show success toast on success', () => {
    component.rateForm.patchValue({
      roomTypeId: 'rt-001',
      startDate:  new Date('2025-07-01'),
      endDate:    new Date('2025-09-30'),
      rate:       5500,
    });

    component.addRate();

    expect(toastSpy.success).toHaveBeenCalledOnceWith('Rate added successfully.');
  });

  it('addRate() — should close rate form (showRateForm → null) after success', () => {
    component.openRateForm('rt-001');
    component.rateForm.patchValue({
      roomTypeId: 'rt-001',
      startDate:  new Date('2025-07-01'),
      endDate:    new Date('2025-09-30'),
      rate:       5500,
    });

    component.addRate();

    expect(component.showRateForm()).toBeNull();
  });

  it('addRate() — should reset isSaving to false on success', () => {
    component.rateForm.patchValue({
      roomTypeId: 'rt-001',
      startDate:  new Date('2025-07-01'),
      endDate:    new Date('2025-09-30'),
      rate:       5500,
    });

    component.addRate();

    expect(component.isSaving()).toBeFalse();
  });

  it('addRate() — should set isSaving to true during in-flight request', () => {
    const subject = new Subject<void>();
    roomTypeSpy.addRate.and.returnValue(subject.asObservable());
    component.rateForm.patchValue({
      roomTypeId: 'rt-001',
      startDate:  new Date('2025-07-01'),
      endDate:    new Date('2025-09-30'),
      rate:       5500,
    });

    component.addRate();

    expect(component.isSaving()).toBeTrue();

    subject.next();
    subject.complete();
  });

  // ── addRate() — INVALID FORM ───────────────────────────────────────────────

  it('addRate() — should NOT call service when rateForm is invalid', () => {
    component.addRate();
    expect(roomTypeSpy.addRate).not.toHaveBeenCalled();
  });

  it('addRate() — should mark all fields touched when form is invalid', () => {
    component.addRate();
    expect(component.rateForm.get('roomTypeId')?.touched).toBeTrue();
    expect(component.rateForm.get('startDate')?.touched).toBeTrue();
    expect(component.rateForm.get('endDate')?.touched).toBeTrue();
    expect(component.rateForm.get('rate')?.touched).toBeTrue();
  });

  // ── addRate() — ERROR ──────────────────────────────────────────────────────

  it('addRate() — should reset isSaving to false on API error', () => {
    roomTypeSpy.addRate.and.returnValue(throwError(() => new Error('fail')));
    component.rateForm.patchValue({
      roomTypeId: 'rt-001',
      startDate:  new Date('2025-07-01'),
      endDate:    new Date('2025-09-30'),
      rate:       5500,
    });

    component.addRate();

    expect(component.isSaving()).toBeFalse();
  });

  it('addRate() — should NOT show success toast on API error', () => {
    roomTypeSpy.addRate.and.returnValue(throwError(() => new Error('fail')));
    component.rateForm.patchValue({
      roomTypeId: 'rt-001',
      startDate:  new Date('2025-07-01'),
      endDate:    new Date('2025-09-30'),
      rate:       5500,
    });

    component.addRate();

    expect(toastSpy.success).not.toHaveBeenCalled();
  });
});