import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { provideNativeDateAdapter } from '@angular/material/core';
import { of, throwError } from 'rxjs';
import { RoomManagementComponent } from './room-management.component';
import { RoomService, RoomTypeService } from '../../../core/services/api.services';
import { ToastService } from '../../../core/services/toast.service';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';

const MOCK_ROOMS = [
  { roomId: 'r-001', roomNumber: '101', floor: 1, roomTypeId: 'rt-001', roomTypeName: 'Deluxe', isActive: true },
  { roomId: 'r-002', roomNumber: '102', floor: 1, roomTypeId: 'rt-001', roomTypeName: 'Deluxe', isActive: false },
];
const MOCK_ROOM_TYPES = [
  { roomTypeId: 'rt-001', name: 'Deluxe', description: '', maxOccupancy: 2, isActive: true, roomCount: 5 }
];

describe('RoomManagementComponent', () => {
  let component: RoomManagementComponent;
  let fixture: ComponentFixture<RoomManagementComponent>;
  let roomSpy: jasmine.SpyObj<RoomService>;
  let roomTypeSpy: jasmine.SpyObj<RoomTypeService>;
  let toastSpy: jasmine.SpyObj<ToastService>;

  beforeEach(async () => {
    roomSpy     = jasmine.createSpyObj('RoomService',     ['getRooms', 'addRoom', 'updateRoom', 'toggleRoomStatus', 'getRoomOccupancy']);
    roomTypeSpy = jasmine.createSpyObj('RoomTypeService', ['getRoomTypes']);
    toastSpy    = jasmine.createSpyObj('ToastService',    ['success', 'error']);

    roomSpy.getRooms.and.returnValue(of(MOCK_ROOMS));
    roomTypeSpy.getRoomTypes.and.returnValue(of(MOCK_ROOM_TYPES));
    roomSpy.addRoom.and.returnValue(of(undefined));
    roomSpy.updateRoom.and.returnValue(of(undefined));
    roomSpy.toggleRoomStatus.and.returnValue(of(undefined));
    roomSpy.getRoomOccupancy.and.returnValue(of([]));

    await TestBed.configureTestingModule({
      imports: [RoomManagementComponent],
      providers: [
        provideAnimationsAsync(), provideHttpClient(), provideHttpClientTesting(),
        provideRouter([]), provideNativeDateAdapter(),
        { provide: RoomService,     useValue: roomSpy },
        { provide: RoomTypeService, useValue: roomTypeSpy },
        { provide: ToastService,    useValue: toastSpy },
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(RoomManagementComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => expect(component).toBeTruthy());

  // ── ngOnInit ──────────────────────────────────────────────────────────────

  it('ngOnInit — should call getRooms', () => {
    expect(roomSpy.getRooms).toHaveBeenCalledWith(1, 10);
  });

  it('ngOnInit — should call getRoomTypes', () => {
    expect(roomTypeSpy.getRoomTypes).toHaveBeenCalled();
  });

  it('ngOnInit — should populate rooms signal', () => {
    expect(component.rooms().length).toBe(2);
  });

  it('ngOnInit — should populate roomTypes signal', () => {
    expect(component.roomTypes().length).toBe(1);
  });

  it('loading — should be false after load', () => {
    expect(component.loading()).toBeFalse();
  });

  // ── Initial state ─────────────────────────────────────────────────────────

  it('isSaving — should start as false', () => expect(component.isSaving()).toBeFalse());
  it('showAddForm — should start as false', () => expect(component.showAddForm()).toBeFalse());
  it('editingRoom — should start as null', () => expect(component.editingRoom()).toBeNull());

  // ── addRoom ───────────────────────────────────────────────────────────────

  it('addRoom — should call addRoom service when form is valid', () => {
    component.addForm.patchValue({ roomNumber: '201', floor: 2, roomTypeId: 'rt-001' });
    component.addRoom();
    expect(roomSpy.addRoom).toHaveBeenCalledOnceWith(jasmine.objectContaining({ roomNumber: '201', floor: 2 }));
  });

  it('addRoom — should show success toast', () => {
    component.addForm.patchValue({ roomNumber: '201', floor: 2, roomTypeId: 'rt-001' });
    component.addRoom();
    expect(toastSpy.success).toHaveBeenCalledOnceWith('Room added.');
  });

  it('addRoom — should NOT call service when form is invalid', () => {
    component.addRoom();
    expect(roomSpy.addRoom).not.toHaveBeenCalled();
  });

  it('addRoom — should mark all touched when form is invalid', () => {
    component.addRoom();
    expect(component.addForm.get('roomNumber')?.touched).toBeTrue();
  });

  it('addRoom — should reset isSaving to false on error', () => {
    roomSpy.addRoom.and.returnValue(throwError(() => new Error('fail')));
    component.addForm.patchValue({ roomNumber: '201', floor: 2, roomTypeId: 'rt-001' });
    component.addRoom();
    expect(component.isSaving()).toBeFalse();
  });

  // ── startEdit / saveEdit ──────────────────────────────────────────────────

  it('startEdit — should set editingRoom', () => {
    component.startEdit(MOCK_ROOMS[0] as any);
    expect(component.editingRoom()?.roomId).toBe('r-001');
  });

  it('startEdit — should patch editForm', () => {
    component.startEdit(MOCK_ROOMS[0] as any);
    expect(component.editForm.get('roomNumber')?.value).toBe('101');
  });

  it('saveEdit — should call updateRoom', () => {
    component.startEdit(MOCK_ROOMS[0] as any);
    component.saveEdit();
    expect(roomSpy.updateRoom).toHaveBeenCalled();
  });

  it('saveEdit — should show success toast', () => {
    component.startEdit(MOCK_ROOMS[0] as any);
    component.saveEdit();
    expect(toastSpy.success).toHaveBeenCalledOnceWith('Room updated.');
  });

  it('saveEdit — should clear editingRoom on success', () => {
    component.startEdit(MOCK_ROOMS[0] as any);
    component.saveEdit();
    expect(component.editingRoom()).toBeNull();
  });

  it('saveEdit — should reset isSaving to false on error', () => {
    roomSpy.updateRoom.and.returnValue(throwError(() => new Error('fail')));
    component.startEdit(MOCK_ROOMS[0] as any);
    component.saveEdit();
    expect(component.isSaving()).toBeFalse();
  });

  // ── toggleStatus ──────────────────────────────────────────────────────────

  it('toggleStatus — should call toggleRoomStatus with inverted isActive', () => {
    component.toggleStatus(MOCK_ROOMS[0] as any); // isActive: true → false
    expect(roomSpy.toggleRoomStatus).toHaveBeenCalledWith('r-001', false);
  });

  it('toggleStatus — should show success toast', () => {
    component.toggleStatus(MOCK_ROOMS[0] as any);
    expect(toastSpy.success).toHaveBeenCalled();
  });

  // ── onPage ────────────────────────────────────────────────────────────────

  it('onPage — should update currentPage and reload', () => {
    roomSpy.getRooms.calls.reset();
    component.onPage({ pageIndex: 1, pageSize: 10, length: 20 } as any);
    expect(component.currentPage).toBe(2);
    expect(roomSpy.getRooms).toHaveBeenCalledWith(2, 10);
  });

  // ── loadRooms error ───────────────────────────────────────────────────────

  it('loadRooms — should set loading to false on error', () => {
    roomSpy.getRooms.and.returnValue(throwError(() => new Error('fail')));
    component.loadRooms();
    expect(component.loading()).toBeFalse();
  });

  // ── onOccupancyDateChange ─────────────────────────────────────────────────

  it('onOccupancyDateChange — should call getRoomOccupancy with formatted date', () => {
    component.onOccupancyDateChange(new Date('2025-06-15'));
    expect(roomSpy.getRoomOccupancy).toHaveBeenCalledWith('2025-06-15');
  });

  it('onOccupancyDateChange — should do nothing when date is null', () => {
    roomSpy.getRoomOccupancy.calls.reset();
    component.onOccupancyDateChange(null);
    expect(roomSpy.getRoomOccupancy).not.toHaveBeenCalled();
  });
});
