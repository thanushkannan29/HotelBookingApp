import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { provideNativeDateAdapter } from '@angular/material/core';
import { of, throwError } from 'rxjs';
import { InventoryManagementComponent } from './inventory-management.component';
import { InventoryService, RoomTypeService } from '../../../core/services/api.services';
import { ToastService } from '../../../core/services/toast.service';
import { InventoryResponseDto, RoomTypeListDto } from '../../../core/models/models';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';

const MOCK_ROOM_TYPES: RoomTypeListDto[] = [
  { roomTypeId: 'rt-001', name: 'Deluxe',   description: '', maxOccupancy: 2, isActive: true, roomCount: 5 },
  { roomTypeId: 'rt-002', name: 'Suite',    description: '', maxOccupancy: 4, isActive: true, roomCount: 2 },
];

const MOCK_INVENTORY: InventoryResponseDto[] = [
  { roomTypeInventoryId: 'inv-001', date: '2025-06-01', totalInventory: 10, reservedInventory: 3, available: 7 },
  { roomTypeInventoryId: 'inv-002', date: '2025-06-02', totalInventory: 10, reservedInventory: 5, available: 5 },
  { roomTypeInventoryId: 'inv-003', date: '2025-06-03', totalInventory: 10, reservedInventory: 0, available: 10 },
];

describe('InventoryManagementComponent', () => {
  let component: InventoryManagementComponent;
  let fixture: ComponentFixture<InventoryManagementComponent>;
  let inventorySpy: jasmine.SpyObj<InventoryService>;
  let roomTypeSpy: jasmine.SpyObj<RoomTypeService>;
  let toastSpy: jasmine.SpyObj<ToastService>;

  function setValidAddForm() {
    component.addForm.patchValue({
      roomTypeId: 'rt-001',
      startDate: new Date('2025-07-01'),
      endDate: new Date('2025-09-30'),
      totalInventory: 12,
    });
    component.addForm.get('startDate')?.setErrors(null);
    component.addForm.get('endDate')?.setErrors(null);
    component.addForm.updateValueAndValidity();
  }

  beforeEach(async () => {
    inventorySpy = jasmine.createSpyObj('InventoryService', ['getInventory', 'addInventory', 'updateInventory']);
    roomTypeSpy  = jasmine.createSpyObj('RoomTypeService',  ['getRoomTypes']);
    toastSpy     = jasmine.createSpyObj('ToastService',     ['success', 'error']);

    roomTypeSpy.getRoomTypes.and.returnValue(of(MOCK_ROOM_TYPES));
    inventorySpy.getInventory.and.returnValue(of(MOCK_INVENTORY));
    inventorySpy.addInventory.and.returnValue(of(undefined));
    inventorySpy.updateInventory.and.returnValue(of(undefined));

    await TestBed.configureTestingModule({
      imports: [InventoryManagementComponent],
      providers: [
        provideAnimationsAsync(), provideHttpClient(), provideHttpClientTesting(),
        provideRouter([]), provideNativeDateAdapter(),
        { provide: InventoryService, useValue: inventorySpy },
        { provide: RoomTypeService,  useValue: roomTypeSpy },
        { provide: ToastService,     useValue: toastSpy },
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(InventoryManagementComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  // ── Creation ──────────────────────────────────────────────────────────────

  it('should create', () => expect(component).toBeTruthy());

  // ── Initial state ─────────────────────────────────────────────────────────

  it('isSaving — should start as false', () => expect(component.isSaving()).toBeFalse());
  it('editingId — should start as null', () => expect(component.editingId()).toBeNull());
  it('editValue — should start as 0', () => expect(component.editValue()).toBe(0));
  it('inventories — should start as empty array', () => expect(component.inventories()).toEqual([]));

  // ── ngOnInit ──────────────────────────────────────────────────────────────

  it('ngOnInit — should call getRoomTypes', () => {
    expect(roomTypeSpy.getRoomTypes).toHaveBeenCalledOnceWith();
  });

  it('ngOnInit — should populate roomTypes signal', () => {
    expect(component.roomTypes().length).toBe(2);
    expect(component.roomTypes()[0].name).toBe('Deluxe');
  });

  // ── Form validation ───────────────────────────────────────────────────────

  it('addForm — should be invalid initially', () => expect(component.addForm.invalid).toBeTrue());
  it('addForm — totalInventory defaults to 1', () => expect(component.addForm.get('totalInventory')?.value).toBe(1));
  it('viewForm — should be invalid initially', () => expect(component.viewForm.invalid).toBeTrue());

  it('addForm — should be invalid when roomTypeId is empty', () => {
    component.addForm.patchValue({ roomTypeId: '', startDate: new Date(), endDate: new Date(), totalInventory: 5 });
    expect(component.addForm.invalid).toBeTrue();
  });

  it('addForm — should be invalid when totalInventory is 0', () => {
    component.addForm.patchValue({ roomTypeId: 'rt-001', startDate: new Date(), endDate: new Date(), totalInventory: 0 });
    expect(component.addForm.get('totalInventory')?.invalid).toBeTrue();
  });

  // ── loadInventory ─────────────────────────────────────────────────────────

  it('loadInventory — should call getInventory with formatted dates', () => {
    component.viewForm.patchValue({ roomTypeId: 'rt-001', start: new Date('2025-06-01'), end: new Date('2025-06-03') });
    component.loadInventory();
    expect(inventorySpy.getInventory).toHaveBeenCalledWith('rt-001', '2025-06-01', '2025-06-03');
  });

  it('loadInventory — should populate inventories signal', () => {
    component.viewForm.patchValue({ roomTypeId: 'rt-001', start: new Date('2025-06-01'), end: new Date('2025-06-03') });
    component.loadInventory();
    expect(component.inventories().length).toBe(3);
    expect(component.inventories()[0].date).toBe('2025-06-01');
  });

  it('loadInventory — should NOT call getInventory when viewForm is incomplete', () => {
    inventorySpy.getInventory.calls.reset();
    component.loadInventory();
    expect(inventorySpy.getInventory).not.toHaveBeenCalled();
  });

  it('loadInventory — should set isLoading to false after load', () => {
    component.viewForm.patchValue({ roomTypeId: 'rt-001', start: new Date('2025-06-01'), end: new Date('2025-06-03') });
    component.loadInventory();
    expect(component.isLoading()).toBeFalse();
  });

  it('loadInventory — should set isLoading to false on error', () => {
    inventorySpy.getInventory.and.returnValue(throwError(() => new Error('fail')));
    component.viewForm.patchValue({ roomTypeId: 'rt-001', start: new Date('2025-06-01'), end: new Date('2025-06-03') });
    component.loadInventory();
    expect(component.isLoading()).toBeFalse();
  });

  // ── addInventory ──────────────────────────────────────────────────────────

  it('addInventory — should call addInventory service with formatted dates', () => {
    setValidAddForm();
    component.addInventory();
    expect(inventorySpy.addInventory).toHaveBeenCalledOnceWith({
      roomTypeId: 'rt-001', startDate: '2025-07-01', endDate: '2025-09-30', totalInventory: 12
    });
  });

  it('addInventory — should show success toast', () => {
    setValidAddForm();
    component.addInventory();
    expect(toastSpy.success).toHaveBeenCalledOnceWith('Inventory set successfully.');
  });

  it('addInventory — should reset isSaving to false on success', () => {
    setValidAddForm();
    component.addInventory();
    expect(component.isSaving()).toBeFalse();
  });

  it('addInventory — should NOT call service when form is invalid', () => {
    component.addInventory();
    expect(inventorySpy.addInventory).not.toHaveBeenCalled();
  });

  it('addInventory — should mark all fields touched when form is invalid', () => {
    component.addInventory();
    expect(component.addForm.get('roomTypeId')?.touched).toBeTrue();
  });

  it('addInventory — should reset isSaving to false on error', () => {
    inventorySpy.addInventory.and.returnValue(throwError(() => new Error('fail')));
    setValidAddForm();
    component.addInventory();
    expect(component.isSaving()).toBeFalse();
  });

  // ── startEditInv / saveEditInv ────────────────────────────────────────────

  it('startEditInv — should set editingId', () => {
    component.startEditInv(MOCK_INVENTORY[0]);
    expect(component.editingId()).toBe('inv-001');
  });

  it('startEditInv — should set editValue to totalInventory', () => {
    component.startEditInv(MOCK_INVENTORY[0]);
    expect(component.editValue()).toBe(10);
  });

  it('saveEditInv — should call updateInventory with correct values', () => {
    component.startEditInv(MOCK_INVENTORY[0]);
    component.editValue.set(15);
    component.saveEditInv(MOCK_INVENTORY[0]);
    expect(inventorySpy.updateInventory).toHaveBeenCalledOnceWith({ roomTypeInventoryId: 'inv-001', totalInventory: 15 });
  });

  it('saveEditInv — should show success toast', () => {
    component.startEditInv(MOCK_INVENTORY[0]);
    component.saveEditInv(MOCK_INVENTORY[0]);
    expect(toastSpy.success).toHaveBeenCalledOnceWith('Inventory updated.');
  });

  it('saveEditInv — should clear editingId after save', () => {
    component.startEditInv(MOCK_INVENTORY[0]);
    component.saveEditInv(MOCK_INVENTORY[0]);
    expect(component.editingId()).toBeNull();
  });

  it('saveEditInv — should use current editValue not original', () => {
    component.startEditInv(MOCK_INVENTORY[0]);
    component.editValue.set(20);
    component.saveEditInv(MOCK_INVENTORY[0]);
    expect(inventorySpy.updateInventory).toHaveBeenCalledWith(jasmine.objectContaining({ totalInventory: 20 }));
  });

  // ── onPage ────────────────────────────────────────────────────────────────

  it('onPage — should update currentPage and pageSize', () => {
    component.allInventories.set(MOCK_INVENTORY);
    component.onPage({ pageIndex: 1, pageSize: 2, length: 3 } as any);
    expect(component.currentPage).toBe(2);
    expect(component.pageSize).toBe(2);
  });
});
