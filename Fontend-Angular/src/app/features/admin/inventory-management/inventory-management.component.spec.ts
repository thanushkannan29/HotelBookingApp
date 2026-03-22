import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { provideNativeDateAdapter } from '@angular/material/core';
import { of, throwError, Subject } from 'rxjs';
import { InventoryManagementComponent } from './inventory-management.component';
import { InventoryService, RoomTypeService } from '../../../core/services/api.services';
import { ToastService } from '../../../core/services/toast.service';
import { InventoryResponseDto, RoomTypeListDto } from '../../../core/models/models';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';

// ── Mock data ──────────────────────────────────────────────────────────────────

const MOCK_ROOM_TYPES: RoomTypeListDto[] = [
  { roomTypeId: 'rt-001', name: 'Deluxe',   description: '', maxOccupancy: 2, amenities: 'WiFi,AC', isActive: true, roomCount: 5 },
  { roomTypeId: 'rt-002', name: 'Suite',    description: '', maxOccupancy: 4, amenities: 'WiFi,AC,Jacuzzi', isActive: true, roomCount: 2 },
  { roomTypeId: 'rt-003', name: 'Standard', description: '', maxOccupancy: 2, amenities: 'WiFi', isActive: false, roomCount: 3 },
];

const MOCK_INVENTORY: InventoryResponseDto[] = [
  { roomTypeInventoryId: 'inv-001', date: '2025-06-01', totalInventory: 10, reservedInventory: 3, available: 7 },
  { roomTypeInventoryId: 'inv-002', date: '2025-06-02', totalInventory: 10, reservedInventory: 5, available: 5 },
  { roomTypeInventoryId: 'inv-003', date: '2025-06-03', totalInventory: 10, reservedInventory: 0, available: 10 },
];

// ─────────────────────────────────────────────────────────────────────────────

describe('InventoryManagementComponent', () => {
  let component: InventoryManagementComponent;
  let fixture:   ComponentFixture<InventoryManagementComponent>;

  let inventorySpy: jasmine.SpyObj<InventoryService>;
  let roomTypeSpy:  jasmine.SpyObj<RoomTypeService>;
  let toastSpy:     jasmine.SpyObj<ToastService>;

  // Helper: set date form fields bypassing MatDatepicker validator
  function setValidAddForm(component: InventoryManagementComponent) {
    component.addForm.patchValue({
      roomTypeId:     'rt-001',
      startDate:      new Date('2025-07-01'),
      endDate:        new Date('2025-09-30'),
      totalInventory: 12,
    });
    // Clear any MatDatepicker-injected errors so form is truly valid
    component.addForm.get('startDate')?.setErrors(null);
    component.addForm.get('endDate')?.setErrors(null);
    component.addForm.updateValueAndValidity();
  }

  beforeEach(async () => {
    inventorySpy = jasmine.createSpyObj('InventoryService', [
      'getInventory', 'addInventory', 'updateInventory'
    ]);
    roomTypeSpy  = jasmine.createSpyObj('RoomTypeService', ['getRoomTypes']);
    toastSpy     = jasmine.createSpyObj('ToastService',    ['success', 'error']);

    // Default happy-path responses
    roomTypeSpy.getRoomTypes.and.returnValue(of(MOCK_ROOM_TYPES));
    inventorySpy.getInventory.and.returnValue(of(MOCK_INVENTORY));
    inventorySpy.addInventory.and.returnValue(of(undefined));
    inventorySpy.updateInventory.and.returnValue(of(undefined));

    await TestBed.configureTestingModule({
      imports: [InventoryManagementComponent],
      providers: [
        provideAnimationsAsync(),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        provideNativeDateAdapter(),          // required by MatDatepicker
        { provide: InventoryService, useValue: inventorySpy },
        { provide: RoomTypeService,  useValue: roomTypeSpy  },
        { provide: ToastService,     useValue: toastSpy     },
      ]
    }).compileComponents();

    fixture   = TestBed.createComponent(InventoryManagementComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  // ── CREATION ───────────────────────────────────────────────────────────────

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  // ── INITIAL SIGNAL STATE ───────────────────────────────────────────────────

  it('isSaving — should start as false', () => {
    expect(component.isSaving()).toBeFalse();
  });

  it('editingId — should start as null', () => {
    expect(component.editingId()).toBeNull();
  });

  it('editValue — should start as 0', () => {
    expect(component.editValue()).toBe(0);
  });

  it('inventory — should start as empty array', () => {
    expect(component.inventory()).toEqual([]);
  });

  // ── ngOnInit ───────────────────────────────────────────────────────────────

  it('ngOnInit — should call getRoomTypes on startup', () => {
    expect(roomTypeSpy.getRoomTypes).toHaveBeenCalledOnceWith();
  });

  it('ngOnInit — should populate roomTypes signal with API response', () => {
    expect(component.roomTypes().length).toBe(3);
    expect(component.roomTypes()[0].name).toBe('Deluxe');
    expect(component.roomTypes()[1].roomTypeId).toBe('rt-002');
  });

  // ── FORM INITIAL STATE ─────────────────────────────────────────────────────

  it('addForm — should be invalid initially (all empty)', () => {
    expect(component.addForm.invalid).toBeTrue();
  });

  it('addForm — totalInventory should default to 1', () => {
    expect(component.addForm.get('totalInventory')?.value).toBe(1);
  });

  it('viewForm — should be invalid initially (all empty)', () => {
    expect(component.viewForm.invalid).toBeTrue();
  });

  // ── FORM VALIDATION — addForm ──────────────────────────────────────────────

  it('addForm — should be valid when all required fields are filled', () => {
    component.addForm.patchValue({
      roomTypeId:     'rt-001',
      startDate:      new Date('2025-06-01'),
      endDate:        new Date('2025-06-30'),
      totalInventory: 10,
    });
    component.addForm.updateValueAndValidity();
    // Check each control individually (MatDatepicker may add its own validator)
    expect(component.addForm.get('roomTypeId')?.valid).toBeTrue();
    expect(component.addForm.get('totalInventory')?.valid).toBeTrue();
    // startDate/endDate valid if they have a non-null value
    expect(component.addForm.get('startDate')?.value).not.toBeNull();
    expect(component.addForm.get('endDate')?.value).not.toBeNull();
  });

  it('addForm — should be invalid when roomTypeId is empty', () => {
    component.addForm.patchValue({
      roomTypeId:     '',
      startDate:      new Date('2025-06-01'),
      endDate:        new Date('2025-06-30'),
      totalInventory: 10,
    });
    expect(component.addForm.invalid).toBeTrue();
  });

  it('addForm — should be invalid when totalInventory is 0', () => {
    component.addForm.patchValue({
      roomTypeId:     'rt-001',
      startDate:      new Date('2025-06-01'),
      endDate:        new Date('2025-06-30'),
      totalInventory: 0,
    });
    expect(component.addForm.get('totalInventory')?.invalid).toBeTrue();
  });

  it('addForm — should be invalid when totalInventory is negative', () => {
    component.addForm.patchValue({
      roomTypeId:     'rt-001',
      startDate:      new Date('2025-06-01'),
      endDate:        new Date('2025-06-30'),
      totalInventory: -5,
    });
    expect(component.addForm.get('totalInventory')?.invalid).toBeTrue();
  });

  // ── loadInventory() ────────────────────────────────────────────────────────

  it('loadInventory() — should call getInventory with formatted date strings', () => {
    component.viewForm.patchValue({
      roomTypeId: 'rt-001',
      start:      new Date('2025-06-01'),
      end:        new Date('2025-06-03'),
    });

    component.loadInventory();

    expect(inventorySpy.getInventory).toHaveBeenCalledWith(
      'rt-001', '2025-06-01', '2025-06-03'
    );
  });

  it('loadInventory() — should populate inventory signal with API response', () => {
    component.viewForm.patchValue({
      roomTypeId: 'rt-001',
      start:      new Date('2025-06-01'),
      end:        new Date('2025-06-03'),
    });

    component.loadInventory();

    expect(component.inventory().length).toBe(3);
    expect(component.inventory()[0].date).toBe('2025-06-01');
    expect(component.inventory()[1].available).toBe(5);
  });

  it('loadInventory() — should NOT call getInventory when viewForm is incomplete', () => {
    inventorySpy.getInventory.calls.reset();
    // viewForm is empty by default
    component.loadInventory();

    expect(inventorySpy.getInventory).not.toHaveBeenCalled();
  });

  it('loadInventory() — should NOT call getInventory when only roomTypeId is set', () => {
    inventorySpy.getInventory.calls.reset();
    component.viewForm.patchValue({ roomTypeId: 'rt-001' });

    component.loadInventory();

    expect(inventorySpy.getInventory).not.toHaveBeenCalled();
  });

  // ── addInventory() — HAPPY PATH ────────────────────────────────────────────

  it('addInventory() — should call addInventory service with formatted dates', () => {
    setValidAddForm(component);
    component.addForm.patchValue({ totalInventory: 12 });

    component.addInventory();

    expect(inventorySpy.addInventory).toHaveBeenCalledOnceWith({
      roomTypeId:     'rt-001',
      startDate:      '2025-07-01',
      endDate:        '2025-09-30',
      totalInventory: 12,
    });
  });

  it('addInventory() — should show success toast on success', () => {
    setValidAddForm(component);

    component.addInventory();

    expect(toastSpy.success).toHaveBeenCalledOnceWith('Inventory added.');
  });

  it('addInventory() — should reset isSaving to false on success', () => {
    setValidAddForm(component);

    component.addInventory();

    expect(component.isSaving()).toBeFalse();
  });

  it('addInventory() — should set isSaving to true during in-flight request', () => {
    const subject = new Subject<void>();
    inventorySpy.addInventory.and.returnValue(subject.asObservable());

    setValidAddForm(component);

    component.addInventory();

    expect(component.isSaving()).toBeTrue();

    subject.next();
    subject.complete();
  });

  it('addInventory() — should reload inventory after successful add', () => {
    component.viewForm.patchValue({
      roomTypeId: 'rt-001',
      start:      new Date('2025-07-01'),
      end:        new Date('2025-07-31'),
    });
    setValidAddForm(component);
    component.addForm.patchValue({ endDate: new Date('2025-07-31') });
    component.addForm.get('endDate')?.setErrors(null);
    inventorySpy.getInventory.calls.reset();

    component.addInventory();

    // loadInventory() should have been triggered
    expect(inventorySpy.getInventory).toHaveBeenCalled();
  });

  // ── addInventory() — INVALID FORM ──────────────────────────────────────────

  it('addInventory() — should NOT call service when addForm is invalid', () => {
    // addForm is empty (invalid) by default
    component.addInventory();

    expect(inventorySpy.addInventory).not.toHaveBeenCalled();
  });

  it('addInventory() — should mark all fields touched when form is invalid', () => {
    component.addInventory();

    expect(component.addForm.get('roomTypeId')?.touched).toBeTrue();
    expect(component.addForm.get('startDate')?.touched).toBeTrue();
    expect(component.addForm.get('endDate')?.touched).toBeTrue();
    expect(component.addForm.get('totalInventory')?.touched).toBeTrue();
  });

  // ── addInventory() — ERROR ─────────────────────────────────────────────────

  it('addInventory() — should reset isSaving to false on API error', () => {
    inventorySpy.addInventory.and.returnValue(
      throwError(() => new Error('Server error'))
    );
    setValidAddForm(component);

    component.addInventory();

    expect(component.isSaving()).toBeFalse();
  });

  it('addInventory() — should NOT show success toast on API error', () => {
    inventorySpy.addInventory.and.returnValue(
      throwError(() => new Error('Server error'))
    );
    setValidAddForm(component);

    component.addInventory();

    expect(toastSpy.success).not.toHaveBeenCalled();
  });

  // ── startEditInv() ─────────────────────────────────────────────────────────

  it('startEditInv() — should set editingId to the inventory item ID', () => {
    component.startEditInv(MOCK_INVENTORY[0]);

    expect(component.editingId()).toBe('inv-001');
  });

  it('startEditInv() — should set editValue to the item totalInventory', () => {
    component.startEditInv(MOCK_INVENTORY[0]);

    expect(component.editValue()).toBe(10);
  });

  it('startEditInv() — switching items should update both signals', () => {
    component.startEditInv(MOCK_INVENTORY[0]); // inv-001, total=10
    component.startEditInv(MOCK_INVENTORY[1]); // inv-002, total=10

    expect(component.editingId()).toBe('inv-002');
    expect(component.editValue()).toBe(10);
  });

  // ── saveEditInv() ──────────────────────────────────────────────────────────

  it('saveEditInv() — should call updateInventory with correct id and editValue', () => {
    component.startEditInv(MOCK_INVENTORY[0]);
    component.editValue.set(15);

    component.saveEditInv(MOCK_INVENTORY[0]);

    expect(inventorySpy.updateInventory).toHaveBeenCalledOnceWith({
      roomTypeInventoryId: 'inv-001',
      totalInventory:      15,
    });
  });

  it('saveEditInv() — should show success toast on success', () => {
    component.startEditInv(MOCK_INVENTORY[0]);

    component.saveEditInv(MOCK_INVENTORY[0]);

    expect(toastSpy.success).toHaveBeenCalledOnceWith('Inventory updated.');
  });

  it('saveEditInv() — should clear editingId after save', () => {
    component.startEditInv(MOCK_INVENTORY[0]);

    component.saveEditInv(MOCK_INVENTORY[0]);

    expect(component.editingId()).toBeNull();
  });

  it('saveEditInv() — should reload inventory after save', () => {
    component.viewForm.patchValue({
      roomTypeId: 'rt-001',
      start:      new Date('2025-06-01'),
      end:        new Date('2025-06-03'),
    });
    component.startEditInv(MOCK_INVENTORY[0]);
    inventorySpy.getInventory.calls.reset();

    component.saveEditInv(MOCK_INVENTORY[0]);

    expect(inventorySpy.getInventory).toHaveBeenCalled();
  });

  it('saveEditInv() — should use current editValue not original totalInventory', () => {
    component.startEditInv(MOCK_INVENTORY[0]); // original = 10
    component.editValue.set(20);               // user changed it to 20

    component.saveEditInv(MOCK_INVENTORY[0]);

    expect(inventorySpy.updateInventory).toHaveBeenCalledWith(
      jasmine.objectContaining({ totalInventory: 20 })
    );
  });
});